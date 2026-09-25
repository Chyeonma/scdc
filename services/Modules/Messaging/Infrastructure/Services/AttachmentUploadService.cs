using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class AttachmentUploadService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    SpaceMessageAccess spaceAccess,
    MessageRateLimiter rateLimiter,
    IAttachmentObjectStore objectStore,
    IFileScanner scanner,
    TimeProvider timeProvider,
    ILogger<AttachmentUploadService> logger) : IAttachmentUploadService
{
    private const int MaxSize = 10 * 1024 * 1024;

    public async Task<Result<AttachmentUploadDto>> UploadAsync(Guid actorUserId, Guid spaceId, Guid clientUploadId,
        string fileName, long declaredLength, string? expectedSha256, Stream content,
        CancellationToken cancellationToken)
    {
        var name = fileName.Replace('\\', '/').Split('/').Last().Trim();
        if (actorUserId == Guid.Empty || spaceId == Guid.Empty || clientUploadId == Guid.Empty
            || name.Length is < 1 or > 255
            || name.Any(char.IsControl) || declaredLength is < 1 or > MaxSize
            || expectedSha256 is null || expectedSha256.Length != 64
            || !expectedSha256.All(Uri.IsHexDigit))
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.InvalidAttachment);

        if (await userDirectory.FindByIdAsync(actorUserId, cancellationToken) is null)
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.AccountUnavailable);
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == spaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null) return Result.Failure<AttachmentUploadDto>(MessagingErrors.ResourceNotFound);
        var access = await spaceAccess.CheckAsync(actorUserId, space, cancellationToken);
        if (!access.CanRead) return Result.Failure<AttachmentUploadDto>(MessagingErrors.ResourceNotFound);
        if (space.Status != SpaceStatus.Active) return Result.Failure<AttachmentUploadDto>(MessagingErrors.SpaceNotWritable);
        if (!access.CanSend) return Result.Failure<AttachmentUploadDto>(MessagingErrors.ActionNotAllowed);
        if (space.SpaceType == SpaceType.Direct)
        {
            var pair = await dbContext.DirectConversations.AsNoTracking().SingleAsync(
                item => item.SpaceId == spaceId, cancellationToken);
            var peer = pair.UserLowId == actorUserId ? pair.UserHighId : pair.UserLowId;
            if (await userDirectory.FindByIdAsync(peer, cancellationToken) is null
                || await dbContext.UserBlocks.AsNoTracking().AnyAsync(block =>
                    (block.BlockerUserId == actorUserId && block.BlockedUserId == peer)
                    || (block.BlockerUserId == peer && block.BlockedUserId == actorUserId), cancellationToken))
                return Result.Failure<AttachmentUploadDto>(MessagingErrors.ActionNotAllowed);
        }
        await using var buffer = new MemoryStream((int)declaredLength);
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var count = await content.ReadAsync(chunk, cancellationToken);
            if (count == 0) break;
            if (buffer.Length + count > MaxSize) return Result.Failure<AttachmentUploadDto>(MessagingErrors.InvalidAttachment);
            await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
        if (buffer.Length != declaredLength)
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.InvalidAttachment);
        var bytes = buffer.ToArray();
        var mimeType = DetectMimeType(bytes);
        var expectedMime = Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => null
        };
        if (mimeType is null || mimeType != expectedMime)
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.InvalidAttachment);
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(checksum), Encoding.ASCII.GetBytes(expectedSha256.ToLowerInvariant())))
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.InvalidAttachment);

        var now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockedSpace = await dbContext.Spaces.FromSqlInterpolated(
            $"SELECT * FROM messaging.spaces WHERE id = {spaceId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
        var lockedAccess = lockedSpace is null ? null
            : await spaceAccess.CheckAsync(actorUserId, lockedSpace, cancellationToken);
        if (lockedSpace is null || lockedSpace.Status != SpaceStatus.Active
            || lockedAccess is null || !lockedAccess.CanRead || !lockedAccess.CanSend)
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.ActionNotAllowed);
        var existing = await dbContext.AttachmentUploads.AsNoTracking().SingleOrDefaultAsync(upload =>
            upload.SpaceId == spaceId && upload.OwnerUserId == actorUserId
            && upload.ClientUploadId == clientUploadId, cancellationToken);
        if (existing is not null)
            return existing.ScanStatus == 1 && existing.ExpiresAt > now
                && existing.OriginalName == name && existing.SizeBytes == bytes.Length
                && existing.ChecksumSha256 == checksum && existing.MimeType == mimeType
                ? Result.Success(ToDto(existing))
                : Result.Failure<AttachmentUploadDto>(MessagingErrors.AttachmentUnavailable);
        if (!rateLimiter.TryAcquire(actorUserId))
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.RateLimited);
        var pendingCount = await dbContext.AttachmentUploads.AsNoTracking().CountAsync(upload =>
            upload.OwnerUserId == actorUserId && upload.AttachedMessageId == null
            && upload.ExpiresAt > now && (upload.ScanStatus == 0 || upload.ScanStatus == 1), cancellationToken);
        if (pendingCount >= 20)
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.AttachmentUploadLimitReached);
        var upload = new AttachmentUpload
        {
            Id = Guid.CreateVersion7(), ClientUploadId = clientUploadId,
            SpaceId = spaceId, OwnerUserId = actorUserId,
            ObjectKey = $"staging/{spaceId:N}/{Guid.CreateVersion7():N}",
            OriginalName = name, MimeType = mimeType, SizeBytes = bytes.Length,
            ChecksumSha256 = checksum, ScanStatus = 0,
            CreatedAt = now, ExpiresAt = now.AddHours(24)
        };
        dbContext.AttachmentUploads.Add(upload);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        try
        {
            if (!await scanner.IsCleanAsync(bytes, cancellationToken))
            {
                upload.ScanStatus = 2;
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Failure<AttachmentUploadDto>(MessagingErrors.AttachmentRejected);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Attachment scan unavailable for upload {UploadId}", upload.Id);
            upload.ScanStatus = 3;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.AttachmentScanUnavailable);
        }

        try
        {
            buffer.Position = 0;
            await objectStore.PutAsync(upload.ObjectKey, buffer, buffer.Length, mimeType, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Attachment storage unavailable for upload {UploadId}", upload.Id);
            upload.ScanStatus = 3;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure<AttachmentUploadDto>(MessagingErrors.AttachmentStorageUnavailable);
        }

        upload.ScanStatus = 1;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDto(upload));
    }

    private static AttachmentUploadDto ToDto(AttachmentUpload upload) => new(upload.Id,
        upload.OriginalName, upload.MimeType,
        upload.SizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
        upload.ChecksumSha256, upload.ScanStatus, upload.ExpiresAt);

    private static string? DetectMimeType(byte[] bytes)
    {
        if (bytes.Length >= 45
            && bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })
            && BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)) == 13
            && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)
            && bytes.AsSpan(bytes.Length - 8, 4).SequenceEqual("IEND"u8))
            return "image/png";
        if (bytes.Length >= 4 && bytes.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff })
            && bytes[^2] == 0xff && bytes[^1] == 0xd9) return "image/jpeg";
        if (bytes.Length >= 7 && bytes[^1] == 0x3b
            && (bytes.AsSpan().StartsWith("GIF87a"u8) || bytes.AsSpan().StartsWith("GIF89a"u8)))
            return "image/gif";
        if (bytes.Length >= 12 && bytes.AsSpan().StartsWith("RIFF"u8)
            && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)) == bytes.Length - 8
            && bytes.AsSpan(8).StartsWith("WEBP"u8)) return "image/webp";
        if (bytes.Length >= 10 && bytes.AsSpan().StartsWith("%PDF-"u8)
            && Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 32))).Contains("%%EOF", StringComparison.Ordinal))
            return "application/pdf";
        try
        {
            var text = new UTF8Encoding(false, true).GetString(bytes);
            return text.All(character => !char.IsControl(character) || character is '\r' or '\n' or '\t')
                ? "text/plain" : null;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }
}
