# SCDC — Gói tham gia trực tiếp cộng đồng

Bước 6 được người dùng cho phép tiếp tục ngày 2026-10-07. Gói đầu chọn đường public/immediate của [UC-COM-06](../../specs/memberships.md#uc-com-06), kế thừa `feat/community-create-view` trên nhánh `feat/community-join`.

## Phạm vi

- Mở URL `/#community/{serverId}`, xem public summary và chủ động bấm Tham gia.
- Kiểm tra Identity và server trong transaction; tạo membership active hoặc trả active membership hiện hành khi gọi lặp.
- Left → active dùng membershipId mới, tăng version, dọn role/ngoại lệ cá nhân cũ; chỉ có quyền mặc định.
- Membership, server accessVersion và outbox commit cùng nhau; UI đọc lại detail/own list sau thành công.
- Kết quả không rõ được đối soát bằng GET trước lần thử tiếp; không tự POST sau 401, timeout, reload hoặc đổi actor.

Search/discovery, requests chờ duyệt, leave/transfer, invitations, quản lý role/phòng và realtime thuộc gói sau. Chưa có bảng/writer pending request nên gói này không có pending để kết thúc. Khi bổ sung requests, mọi đường join phải đóng pending trong transaction theo đặc tả Memberships.

Contract mục tiêu có 200/202; runtime chỉ triển khai public/immediate. Người chưa active gặp approval nhận 409 `JOIN_APPROVAL_REQUIRED`, không tạo membership/request; UI đọc lại mode và giải thích cần được duyệt.

## Tiêu chí kiểm chứng

| Nội dung | Bằng chứng cần có |
|---|---|
| Join mới/lặp | 200 Membership của actor; một epoch active/event/accessVersion; list/detail đúng sau commit |
| Đồng thời | Barrier và khóa server: hai transaction trả cùng membership, một lần ghi |
| Trạng thái hiện hành | Private/inactive/deleted/unknown 404; approval không ghi; mode/visibility đổi trong lúc đợi khóa được đọc lại |
| Identity | Anonymous/unverified/inactive/revoked bị chặn; expiry trước commit rollback |
| Nguyên tử | Fault outbox rollback membership/cleanup/server/event; timeout không tự replay |
| Rejoin | Fixture left có grant cũ: ID mới, version tăng, grant cũ bị dọn; không suy ra leave API đã có |
| WebClient | API thật: preview → join → detail/list/reload; mất response đối soát GET; double click, lỗi, đổi actor/response muộn |
| Hồi quy | Backend Release, Node, Chromium, build WebClient; docs-check và diff sạch |

[TC-COM-03](../../specs/memberships.md#tc-com-03) kiểm chứng phần join/lặp/đồng thời. [AC-COM-02](../../specs/memberships.md#ac-com-02) còn đường search; [TC-COM-16/22](../../specs/memberships.md#tests) còn leave, mode khác, writer role/ACL và lịch sử phòng; không đóng toàn bộ các tiêu chí này.

[Thiết kế](../../design/direct-join.md) · [Trạng thái](../../status.md).
