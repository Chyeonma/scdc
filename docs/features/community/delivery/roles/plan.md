# SCDC — Gói vai trò và quyền quản lý

Người dùng duyệt tiếp tục ngày 2026-10-07. Code ở `feat/community-permissions`, kế thừa create/view → join → search; tài liệu độc lập trên main. Gói chọn role/assignment và nền policy của [UC-COM-20/21](../../specs/permissions.md#use-cases).

## Phạm vi

- Owner tạo/sửa/xóa custom role, tối đa 20; tên 1–64 UTF-16 sau trim, unique theo key trim/NFC/ToLowerInvariant, phân biệt dấu.
- @everyone tự áp cho membership active; không sửa/xóa/gán tay/cấp management. Catalog giữ đúng năm management codes; union allow, không hierarchy hoặc DENY quản lý.
- Owner đọc roster và thay toàn bộ tập role của target active với membershipId/expectedVersion. Owner/manage_channel_access được đọc role catalog và roster; không trả email.
- Migration 003: role version/key, FK assignment và member override theo epoch, operation create_role có scope server; bootstrap/seed và nâng cấp dữ liệu có kiểm chứng.
- Guard Identity + server lock giữ đến commit, kiểm tra ownership/quyền hiện hành; mutation cùng accessVersion/outbox, no-op không tăng version. Create retry cùng key/body, PATCH/PUT/DELETE CAS và không tự replay.
- WebClient quản lý role và gán cho thành viên bằng API thật; validation, conflict, pending create/reload, lỗi/actor đổi và mobile.
- Evaluator domain đối chiếu 18 fixture quyền nền/owner/role/cá nhân/kind. Chưa mở ACL/channel API, history, Hub/dispatcher hay proof thu hồi ≤5 giây; UC-COM-25 vẫn là phụ thuộc riêng của UC-COM-20/21.

## Kiểm chứng

Owner-only/cross-server/system protection; Unicode/collision/catalog/cap; operation replay/rotation/restart; stale role version/epoch và hai writer; lost response/no replay; rollback outbox/expiry; server lock và slot thứ 20; migration preserve/no-op/checksum/preflight/rollback/FK epoch. Backend Release, Node, Chromium, production build và docs-check.

Dẫn chiếu COM-028/037, ACL-19, AC-COM-17/36/38 và TC-ACL-03/08/09; view/ACL/realtime chỉ đạt phần evaluator, không suy fixture thành API/thu hồi đã đạt. [Thiết kế](../../design/roles.md) · [Trạng thái](../../status.md).
