# SCDC — Gói tìm kiếm cộng đồng công khai

Người dùng cho phép tiếp tục ngày 2026-10-07. Gói chọn tìm kiếm/summary của [UC-COM-02](../../specs/servers.md#uc-com-02), nối vào join public/immediate đã có trên `feat/community-join`; code ở `feat/community-search`.

## Phạm vi và đầu ra

- GET `/api/v1/servers/search?q=...&limit=...&cursor=...`: q 2–100 UTF-16 sau trim, tìm literal một phần tên; case-insensitive/accent-sensitive theo DEC-093.
- Exact-name trước, rồi key tên/UUID; trang mặc định 20, tối đa 50; chỉ summary public/active/nondeleted, kể cả khi actor đã là member.
- Key tên trim/NFC/ToLowerInvariant thống nhất giữa create, query và backfill; migration 002 giữ nguyên tên hiển thị/version/visibility/owner/epoch.
- Cursor Data Protection theo actor/query chuẩn hóa/limit/position/expiry; từng trang lọc lại trạng thái hiện hành.
- Khu vực Khám phá có tìm kiếm, tải/rỗng/lỗi/phân trang; mở kết quả → detail/join → own list và quay lại cùng từ khóa.
- Không tìm rỗng để liệt kê toàn bộ, không tạo membership khi chỉ tìm/xem; không thêm UC-COM-07/08, metadata management, room hoặc realtime.

## Tiêu chí kiểm chứng

| Nhóm | Bằng chứng |
|---|---|
| Privacy/Identity | Private/inactive/deleted không xuất hiện, member chỉ nhận summary; anonymous/unverified/session invalid bị chặn |
| Query và Unicode | Q 1/2/100/101 UTF-16, emoji/combining/Unicode lỗi/trống; case/NFC tương đương nhưng dấu khác; `%`, `_`, backslash và SQL metacharacters tìm literal |
| Thứ tự/phân trang | Duplicate names, exact-first, keyset/tie UUID; limit 20/50; cursor không dùng với actor/query/limit/purpose khác hoặc tamper/expiry |
| Thay đổi hiện hành | Unseen server đổi private/inactive giữa trang bị loại từ cursor cũ; không có snapshot public cố định |
| Migration | Upgrade 001 → 002 và bootstrap/seed tương đương; backfill cùng key .NET, giữ version/data, replay checksum no-op, dữ liệu invalid hoặc fault rollback; không sửa checksum 001 |
| WebClient | API thật search → preview → join/list; rỗng/validation/pagination, request muộn/query/actor đổi, lỗi/refresh từ đầu, keyboard/mobile, approval không mở join trực tiếp |
| Hồi quy | Backend Release, Node, Chromium, production build; docs-check và diff sạch |

Dẫn chiếu [COM-038](../../specs/servers.md#com-038), [AC-COM-01/37](../../specs/servers.md#acceptance), [TC-COM-01/18](../../specs/servers.md#tests) và đường tìm kiếm của [AC-COM-02](../../specs/memberships.md#ac-com-02). Private switch dùng fixture/SQL có khóa; chưa chứng minh API UC-COM-04 hoặc pending transition. Đường approval của UC-COM-02 còn UC-COM-07.

[Thiết kế](../../design/search.md) · [Trạng thái](../../status.md).
