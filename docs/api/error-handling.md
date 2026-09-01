# Quy ước xử lý lỗi API

Tài liệu này mô tả cách backend SCDC biểu diễn kết quả nghiệp vụ, ánh xạ lỗi
sang HTTP và trả lỗi nhất quán cho client.

## Mục tiêu

- Application và Domain không phụ thuộc vào HTTP.
- Client luôn nhận được một cấu trúc lỗi nhất quán.
- Swagger mô tả đúng response thành công và response lỗi.
- Lỗi ngoài dự kiến không làm lộ stack trace hoặc thông tin nội bộ.
- Mỗi response lỗi có `errorCode` để client xử lý và `traceId` để tra log.

## Luồng xử lý

```text
Domain/Application
        |
        | Result hoặc Result<T>
        v
API Controller
        |
        | ApiErrorMapper
        v
HTTP status + ProblemDetails
```

Exception ngoài dự kiến không đi qua `Result`. `GlobalExceptionHandler` bắt
exception, ghi log và trả response `500` an toàn.

## Response thành công

Response thành công trả trực tiếp DTO đúng kiểu, không bọc trong một envelope
như `{ "success": true, "data": ... }`.

| Status | Trường hợp sử dụng |
|---:|---|
| `200 OK` | Đọc hoặc cập nhật và cần trả dữ liệu |
| `201 Created` | Tạo mới resource |
| `204 No Content` | Thành công nhưng không cần response body |

Ví dụ:

```json
{
  "id": "0195f28e-5b07-7ec0-93d8-f8cad7edfa57",
  "username": "alice"
}
```

## Response lỗi

Lỗi sử dụng media type `application/problem+json` theo cấu trúc
`ProblemDetails`.

```json
{
  "type": "https://scdc.dev/problems/not-found",
  "title": "Resource not found.",
  "status": 404,
  "detail": "User was not found.",
  "instance": "/api/v1/users/0195f28e-5b07-7ec0-93d8-f8cad7edfa57",
  "errorCode": "Identity.UserNotFound",
  "traceId": "00-4f27c41f5ce14f28a95d4af231ae15ac-6ce66ecbdbefa717-00"
}
```

Ý nghĩa các trường:

| Trường | Ý nghĩa |
|---|---|
| `type` | URI mô tả nhóm lỗi |
| `title` | Tiêu đề ngắn theo loại lỗi |
| `status` | HTTP status code |
| `detail` | Mô tả an toàn dành cho client |
| `instance` | Đường dẫn request gây lỗi |
| `errorCode` | Mã lỗi ổn định để client xử lý |
| `traceId` | Mã dùng để đối chiếu log backend |

## Ánh xạ ErrorType sang HTTP

Application sử dụng `ErrorType`; API host chịu trách nhiệm ánh xạ sang HTTP.

| ErrorType | HTTP | Ví dụ |
|---|---:|---|
| `Validation` | `400` | Dữ liệu đầu vào không hợp lệ |
| `Unauthorized` | `401` | Thiếu token hoặc token không hợp lệ |
| `Forbidden` | `403` | Đã đăng nhập nhưng không có quyền |
| `NotFound` | `404` | Không tìm thấy resource |
| `Conflict` | `409` | Trùng email, username hoặc xung đột trạng thái |
| `TooManyRequests` | `429` | Vượt giới hạn request |
| `ServiceUnavailable` | `503` | Dependency tạm thời không sẵn sàng |

`500 Internal Server Error` không phải lỗi nghiệp vụ. Application handler
không chủ động tạo `Result` có status `500`; exception ngoài dự kiến được xử lý
tập trung bởi `GlobalExceptionHandler`.

## Lỗi validation

Validation error có thêm object `errors`, trong đó key là tên field và value là
danh sách thông báo tương ứng.

```json
{
  "type": "https://scdc.dev/problems/validation",
  "title": "Validation failed.",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/auth/register",
  "errorCode": "Common.ValidationFailed",
  "traceId": "00-4f27c41f5ce14f28a95d4af231ae15ac-6ce66ecbdbefa717-00",
  "errors": {
    "email": ["Email không hợp lệ."],
    "password": ["Mật khẩu phải có ít nhất 8 ký tự."]
  }
}
```

Model validation của ASP.NET Core tự động sử dụng cấu trúc này. Application
cũng có thể trả `ValidationError` khi validation phụ thuộc vào use case.

## Sử dụng Result trong Application

Thành công có dữ liệu:

```csharp
return Result.Success(userDto);
```

Thất bại nghiệp vụ:

```csharp
return Result.Failure<UserDto>(
    Error.NotFound(
        "Identity.UserNotFound",
        "User was not found."));
```

Validation nhiều field:

```csharp
return Result.Failure<UserDto>(
    new ValidationError(
        "Identity.RegistrationInvalid",
        "Registration data is invalid.",
        new Dictionary<string, string[]>
        {
            ["email"] = ["Email không hợp lệ."],
            ["password"] = ["Mật khẩu không đủ mạnh."]
        }));
```

Không throw exception cho các kết quả nghiệp vụ bình thường như không tìm thấy,
trùng dữ liệu hoặc không có quyền. Exception chỉ dành cho tình huống bất thường
mà request handler không thể xử lý hợp lý.

## Sử dụng trong Controller

Controller nghiệp vụ kế thừa `ApiControllerBase` để nhận response convention
và các helper ánh xạ `Result`.

```csharp
[Route("api/v1/users")]
public sealed class UsersController : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await handler.Handle(id, cancellationToken);
        return FromResult(result);
    }
}
```

Các helper hiện có:

- `FromResult`: trả `200` hoặc lỗi tương ứng.
- `FromCreatedResult`: trả `201` hoặc lỗi tương ứng.
- `FromNoContentResult`: trả `204` hoặc lỗi tương ứng.

## Quy ước đặt errorCode

Sử dụng PascalCase và dấu chấm để phân tách module:

```text
{Module}.{ErrorName}
```

Ví dụ:

```text
Identity.UserNotFound
Identity.EmailAlreadyExists
Identity.InvalidCredentials
Community.ChannelAccessDenied
Messaging.MessageNotFound
```

Yêu cầu đối với `errorCode`:

- Không chứa nội dung động như user ID hoặc email.
- Không thay đổi sau khi đã được client sử dụng.
- Không dùng message hiển thị làm mã lỗi.
- Client xử lý logic dựa trên `errorCode`, không dựa trên `detail`.

## Bảo mật và logging

- Không trả stack trace, SQL, connection string hoặc tên máy chủ cho client.
- Không trả password, token, secret hoặc dữ liệu nhạy cảm trong `detail`.
- Exception đầy đủ chỉ được ghi ở backend log.
- Khi báo lỗi vận hành, client hoặc người kiểm thử cần cung cấp `traceId`.
- Response `500` luôn sử dụng thông báo chung, không sử dụng `exception.Message`.

## Thêm một loại lỗi mới

Trước khi thêm lỗi, kiểm tra loại hiện có có thể biểu diễn tình huống đó không.
Nếu thực sự cần loại mới:

1. Thêm semantic type vào `ErrorType`.
2. Bổ sung ánh xạ trong `ApiErrorDefaults`.
3. Khai báo response tương ứng trong Swagger nếu cần.
4. Thêm test cho status, `errorCode`, content type và `traceId`.
5. Cập nhật tài liệu này.

## Kiểm thử

Chạy toàn bộ test:

```bash
dotnet test SCDC.slnx --configuration Release
```

Bộ test response hiện kiểm tra:

- Success response `200`.
- Business error `404`.
- Automatic validation `400`.
- Route không tồn tại `404`.
- Exception ngoài dự kiến `500` không rò rỉ chi tiết.
- Error schema được công bố trong Swagger.
