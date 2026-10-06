# Module Community

Đặc tả nghiệp vụ, dữ liệu/API, UX và kiểm thử được quản lý tại
[community.md](../../../docs/features/community.md).
Tổ chức nghiệp vụ đã thống nhất gồm năm phần:

- [Servers](../../../docs/features/community/servers.md): metadata, search, visibility, join mode và owner.
- [Memberships](../../../docs/features/community/memberships.md): membership/epoch, yêu cầu tham gia và leave/rejoin.
- [Invitations](../../../docs/features/community/invitations.md): link mời và lời mời đích danh.
- [Channels](../../../docs/features/community/channels.md): metadata/kind và vòng đời phòng.
- [Permissions](../../../docs/features/community/permissions.md): role, assignment, ACL và evaluator/checker/guard.

[Tích hợp dùng chung](../../../docs/features/community/integration.md) quản lý transaction,
ID/version/retry, migration, tin phòng và realtime. Danh mục UC và bảng COM/AC/TC ở
[trang tổng quan](../../../docs/features/community.md#use-cases); bước triển khai tiếp theo ở
[gói đầu tiên](../../../docs/features/community.md#use-case-delivery).

Cấu trúc code mục tiêu ở [tổ chức nội bộ](../../../docs/features/community.md#organization):
`Features/<Phần>/{Domain,Application}` và Infrastructure dùng chung,
Permissions có Infrastructure cho checker/guard. Các phần cùng project/schema/DbContext khi triển khai;
`CommunityModule.cs` ghép DI. Source hiện mới đăng ký descriptor Foundation, chưa có DbContext hoặc nghiệp vụ.

Ranh giới module và trạng thái source nằm trong [kiến trúc](../../../docs/architecture.md#boundaries).
Setup và lệnh kiểm thử nằm trong [hướng dẫn phát triển](../../../docs/development.md).

Module sở hữu schema `community`; giao tiếp liên module qua `SCDC.Contracts`.
