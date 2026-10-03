# Đặc tả Module Community

## 1. Trách nhiệm nghiệp vụ

Module Community chịu trách nhiệm quản trị không gian máy chủ cộng đồng (Server / Guild), cấu trúc phòng trò chuyện (Channels & Categories), tư cách thành viên (Memberships), vai trò (Roles) và hệ thống phân quyền (Bitwise RBAC).

---

## 2. Các thực thể chính (Entities)

Schema CSDL: `community`

| Bảng | Trách nhiệm |
|---|---|
| `community.servers` | Thông tin máy chủ cộng đồng (tên, mô tả, chủ sở hữu, cấu hình công khai/riêng tư). |
| `community.server_members` | Danh sách thành viên tham gia máy chủ và thời gian gia nhập. |
| `community.channels` | Danh sách các kênh (văn bản, thoại, danh mục cha). |
| `community.roles` | Vai trò trong máy chủ và giá trị quyền Bitwise tương ứng. |
| `community.member_roles` | Quan hệ gán vai trò cho từng thành viên. |
| `community.channel_overrides` | Ngoại lệ phân quyền theo từng kênh cho vai trò hoặc cá nhân. |
| `community.server_invites` | Mã mời tham gia máy chủ (thời hạn, số lần dùng tối đa). |
| `community.server_bans` | Danh sách người dùng bị cấm khỏi máy chủ. |

---

## 3. Cơ chế phân quyền Bitwise RBAC

Quyền được biểu diễn bằng mặt nạ bit (Bitwise Permissions) trên kiểu số nguyên lớn (`long` / 64-bit mask).

### Thứ tự giải quyết quyền (Resolution Order):
1. **Chủ sở hữu (Server Owner):** Có toàn quyền tuyệt đối đối với toàn bộ máy chủ và mọi kênh. Bỏ qua mọi giới hạn.
2. **Quyền vai trò cơ sở (Base Roles):** Hợp (`OR`) tất cả quyền của các vai trò mà thành viên đang nắm giữ.
3. **Ngoại lệ kênh theo vai trò (Channel Overrides by Role):**
   - Áp dụng các bit `DENY` trước (Từ chối ưu tiên).
   - Áp dụng các bit `ALLOW`.
4. **Ngoại lệ kênh theo cá nhân (Channel Overrides by User):** Áp dụng cuối cùng để ghi đè quy tắc cấp vai trò.

---

## 4. Giao diện tích hợp nội bộ (`SCDC.Contracts`)

Module cung cấp interface `IChannelAccessChecker` để các module khác (như `Messaging`, `Calls`) xác minh quyền truy cập mà không cần truy vấn trực tiếp bảng của Community:

```csharp
public interface IChannelAccessChecker
{
    Task<bool> CanViewChannelAsync(Guid userId, Guid channelId, CancellationToken ct = default);
    Task<bool> CanSendMessageAsync(Guid userId, Guid channelId, CancellationToken ct = default);
    Task<bool> CanConnectVoiceAsync(Guid userId, Guid channelId, CancellationToken ct = default);
}
```

---

## 5. Trạng thái phát triển hiện tại

- Schema PostgreSQL: Đã hoàn thiện trong `database/postgres/schema.sql`.
- Module Descriptor: Đã đăng ký trong `services/Modules/Community/CommunityModule.cs`.
- Nghiệp vụ cốt lõi: Đang xây dựng logic tính toán Bitwise RBAC và các API quản lý Server/Channel.
