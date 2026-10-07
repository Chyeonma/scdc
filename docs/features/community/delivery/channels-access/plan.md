# SCDC — Gói phòng text và ACL

Người dùng duyệt tiếp tục ngày 2026-10-07. Code trên feat/community-channels, kế thừa permissions; tài liệu độc lập trên main.

## Phạm vi

- UC-COM-16: owner/manage_channels tạo phòng text, default view allow; metadata và Messaging space cùng transaction qua IChatSpaceLifecycle; operation UUIDv4/fingerprint/replay không nhân đôi.
- Phần metadata/list của UC-COM-17: chỉ trả phòng hiện hành được view, hidden/deleted/cross-server trả 404; danh sách phân trang kiểm tra lại quyền từng trang.
- UC-COM-18: sửa tên/topic bằng expectedVersion và view/manage_channels; kind bất biến, Unicode key unique theo server đối với phòng active.
- UC-COM-22: owner hoặc manage_channel_access kèm view đọc/thay toàn bộ ACL, expectedAccessVersion, role cùng server và member active đúng epoch; deny giữa role thắng, cá nhân áp cuối, owner sau điều kiện nền. Tự mất view vẫn nhận kết quả commit, lần đọc sau bị chặn.
- Migration 004 có key/kind/defaultView/status/deleted/version/accessVersion; nâng cấp channel legacy yêu cầu mapping kind/defaultView có review, không tự chuyển read-only/archived hoặc sửa tên/quyền.
- IChannelAccessGuard giữ Identity/server/channel locks trong transaction của caller, lease/epoch/version cho Messaging; kiểm chứng chặn race role/ACL và commit. Chưa có writer tin/Hub tiêu thụ nên không tuyên bố realtime đã đạt.
- WebClient dùng API thật: list/detail/create/edit/ACL, pending create/reload, conflict/GET reconciliation, actor/selection đổi, phân trang và mobile.

UC-COM-19 xóa phòng, voice/Media lifecycle, lịch sử/gửi tin và UC-COM-25/dispatcher/thu hồi ≤5 giây ở gói sau. Các UC chỉ đạt phần đã chọn.

## Kiểm chứng

Permission matrix API, hidden privacy/owner foundation, Unicode/collision/immutable kind, operation restart/rotation/concurrent create, metadata/ACL no-op/CAS và epoch. Shared lifecycle/outbox fault rollback cả space/channel; guard share lock chặn role/ACL mutation, revoke trước admission bị chặn, lease hết hạn rollback. Migration map/checksum/preflight/batch/rollback/bootstrap/seed; Release, Node, Chromium, production build và docs-check.

[Thiết kế](../../design/channels-access.md) · [Nghiệm thu](acceptance.md) · [Trạng thái](../../status.md).
