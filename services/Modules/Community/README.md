# Module Community

Module sở hữu schema `community`: server, membership, invitation, channel và quyền. Messaging sở hữu tin phòng; Identity cung cấp tài khoản/phiên. Giao tiếp liên module qua [SCDC.Contracts](../../SCDC.Contracts); [CommunityModule.cs](CommunityModule.cs) đăng ký module và DI.

## Tài liệu

- [Các chủ đề Community](../../../docs/features/community/README.md): quy tắc, UX, dữ liệu/API, tiêu chí và hiện trạng.
- [Cơ chế Community dùng chung](../../../docs/system/community.md): tổ chức nội bộ, hợp đồng, khóa và transaction.
- [Kiến trúc hệ thống](../../../docs/system/architecture.md#boundaries): ranh giới module và sở hữu dữ liệu.
- [Chạy local và kiểm thử](../../../docs/guides/community-development.md): cấu hình, database thử, backend và browser.
- [Migration](../../../docs/guides/community-development.md#migration): runner, thứ tự và mapping legacy.
- [Hồ sơ kiểm chứng](../../../docs/records/verification/community/README.md): commit, môi trường, phạm vi và kết quả.
