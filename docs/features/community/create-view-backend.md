# SCDC — Kết quả backend tạo và xem cộng đồng

Cập nhật: 2026-10-07. Bước 4 của [gói đầu](../community.md#first-package) đã được người dùng duyệt và triển khai trên nhánh `feat/community-create-view`. Code chưa merge vào main; tài liệu này ghi bằng chứng backend, chưa nghiệm thu luồng WebClient. Source Community trên `main` vẫn Foundation.

## Phạm vi đã triển khai

| Route | Hành vi đã kiểm chứng |
|---|---|
| `POST /api/v1/servers` | Tạo server public/private với tên và mô tả theo policy; owner membership active và @everyone; 201/Location sau commit, retry hợp lệ 200, đổi payload cùng khóa 409 |
| `GET /api/v1/servers` | Chỉ server/member active của actor; keyset ID giảm dần, limit 1–50, cursor bảo vệ gắn actor/limit/purpose và hết hạn 24 giờ |
| `GET /api/v1/servers/{id}` | Member detail với quyền hiện hành; người ngoài public chỉ nhận sáu trường summary; private/inactive/unknown trả 404 |
| `GET /api/v1/servers/{id}/membership/me` | Membership active/left của chính actor, kể cả own-left trong private; GET không tạo hoặc phục hồi membership |

Gói thực hiện UC-COM-01 ở backend và phần list/detail/tư cách của UC-COM-03. Route summary public giữ quy tắc che dữ liệu; chưa có search/discovery, join/leave API, phòng, quản lý role/ACL, lời mời, Messaging hoặc realtime.

Các lựa chọn trong [thiết kế bước 3](create-view-design.md) đã được thực hiện: UUIDv7/UUIDv4, version string, giới hạn UTF-16, HMAC theo fixture, Data Protection key ring explicit, one-statement read projection, shared PostgreSQL transaction và Identity share guard. Metadata version vẫn do trigger tăng.

Create ghi server, owner membership, @everyone, operation và đúng một event `Community.ServerCreated.v1` trong cùng transaction. Event giữ `published_at=NULL`; chưa có dispatcher. Unique operation loser rollback toàn bộ context/transaction, mở transaction mới rồi đọc winner. Retry đọc detail hiện hành dưới quyền hiện hành; không tạo lại tài nguyên đã mất quyền hoặc thiếu key.

Migration có runner explicit, preflight và ledger/checksum. DB cũ cần mapping visibility/joinMode được rà soát theo server ID; role/grant/status legacy không được tự sửa. Deferred constraints giữ owner active và @everyone không có management grant. Bootstrap/seed DB mới được đồng bộ; API không tự migrate hoặc gọi bootstrap.

## Bằng chứng kiểm thử

Chạy PostgreSQL 18 trong container thử nghiệm riêng, database `scdc_community_test` ở cổng 15432. Migration tests tạo/drop các DB fixture riêng kết thúc `_test`; không dùng DB ứng dụng. Các tài khoản/khóa là dữ liệu tổng hợp.

| Nhóm | Kết quả |
|---|---|
| Identity guard/work scope | 7 ca đạt: revoked/stamp/expiry/unverified/inactive, share lock chặn writer, rollback |
| Migration/bootstrap | 4 ca đạt: legacy mapping explicit, preflight giữ dữ liệu/schema khi fail, checksum/no-op, UTF-16 SQL, seed và deferred invariant |
| API/transaction | 31 ca đạt: route/Location, Unicode và biên dữ liệu, quyền đọc, pagination, restart/rotation, fault injection, unique-key race, tranh logout/đổi mật khẩu, expiry trước commit, timeout 503 và invariants |
| Fingerprint/cursor | 5 ca đạt: HMAC khớp fixture độc lập, Unicode lỗi, expiry/purpose/storage/restart |
| Hồi quy host/Identity | 16 ca hiện có đạt; bổ sung kiểm tra Swagger trả đúng union summary/detail với trường bắt buộc |

Lệnh kiểm chứng cuối trên nhánh backend:

```bash
# Đặt ConnectionStrings__Database tới PostgreSQL thử nghiệm đã chuẩn bị.
# Đặt Modules__Identity__SigningKey và Modules__Identity__ExposeDevelopmentTokens=true.
dotnet test SCDC.slnx --configuration Release --no-restore -m:1
```

Kết quả: **63 passed, 0 failed, 0 skipped**. Build Release không warning/error. Compose config hợp lệ với khóa tổng hợp. Image API build thành công; smoke HTTP trên container chạy bằng user ứng dụng thực hiện register/verify/login, create/list, restart, đọc cursor từ volume và retry cùng operation thành công. Đây chưa phải kiểm thử tải hoặc nghiệm thu UI.

## Code, cấu hình và Git

Các phần nằm trên `feat/community-create-view`:

- `f9abd87`: shared transaction và Identity account access guard.
- `05e61c6`: migration/preflight, bootstrap/seed và invariant.
- `79fa627`: create/read API, persistence, outbox, keys/cursors, cấu hình và kiểm chứng.

Chuyển sang nhánh backend để đọc hướng dẫn runtime trong `services/Modules/Community/README.md`. HMAC cần key ID và base64 secret ít nhất 32 byte; key ring cần thư mục bền đọc/ghi được. Compose dùng `COMMUNITY_OPERATION_KEY` từ `.env` local và volume `scdc-community-keys`. Giữ HMAC key cũ khi rotation để đọc operation còn lưu; giữ key ring và ApplicationName theo môi trường qua restart.

Các commit hiện chỉ có local. Push `main` và nhánh backend vẫn thiếu xác thực GitHub HTTPS; chưa thể đọc username/credential. Không merge code vào main.

## Bước tiếp theo

Bước 5 chờ người dùng duyệt: nối form tạo, danh sách và chi tiết Community với API; xử lý trạng thái rỗng/lỗi/loading, giữ operation key khi đối soát và thử lại; chạy luồng UI thật từ đăng nhập tới create/detail/list/reload và ghi kết quả theo [tiêu chí gói](../community.md#first-package). Các gói tham gia, quyền, lời mời, phòng và realtime được duyệt riêng sau đó.
