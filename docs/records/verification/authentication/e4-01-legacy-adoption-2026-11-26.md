# E4-01 — Nhận diện schema email legacy khi nâng Identity

Nền `main@23b45eb` (PR #54), nhánh `fix/e4-01-legacy-email-schema`.

## Phát hiện trên dev

Lượt nâng dev đầu tiên bị chặn vì đã có `identity.account_token_policies` và
`identity.email_deliveries` từ nhánh email cũ, dù chưa có ledger E4-01 v1.
Có 4 policy và 2 delivery `ProviderAccepted`, không còn envelope. Schema
cột/default/PK/UNIQUE/FK/CHECK khớp schema đã rà. Transaction migration rollback;
Identity cũ đã khởi động lại, ba API readiness trả 200. Backup trước nâng cấp
nằm trong thư mục artifact private, không vào Git hoặc CI.

## Bản sửa

- Giữ nguyên file/checksum migration v1 `2026112601`; CLI vẫn chạy được v1.
- Migration owner v2 `2026112602` tạo mới hoặc nhận diện hai bảng bằng catalog
  signature trên PostgreSQL 18, không dựa riêng vào tên bảng.
- Giữ delivery cũ và policy/cooldown đã phát hành; chỉ chọn token mới hơn khi
  hợp nhất, không lùi `last_issued_at`, không dựng lại token thô.
- Không ghi ledger v1 giả khi nhận diện legacy; giữ ledger v1 thật nếu có.
- Từ chối schema chưa rà, checksum v1 khác hoặc envelope legacy còn hạn chưa
  có kế hoạch chuyển key/context. Không tự xóa delivery để vượt preflight.
- Compose và CI dùng v2; runtime/API không nhận quyền DDL hoặc admin secret.

## Kiểm chứng

| Bộ | Kết quả |
|---|---|
| Build Release | 0 warning/error |
| Migration .NET | 7/7: fresh, backfill, checksum, writer còn chạy, legacy có dữ liệu, schema lạ, envelope legacy còn hạn và chuỗi v1→v2 |
| Job Compose | 3/3: fresh, chạy lại, từ chối checksum khác |
| Bản sao backup dev trên PostgreSQL thử riêng | Migration đạt; giữ mọi dòng user/session/token/delivery, không giảm số policy |

Bản sao dev được phục hồi vào database thử riêng, không khởi động API/worker,
không gửi thư và đã xóa database clone sau đối chiếu. Dump, log restore và
fingerprint dữ liệu chỉ nằm trong artifact private. CI tiếp tục chạy toàn bộ
backend, frontend và các kiểm tra chuyển database trên bản sửa.

Hướng dẫn [nâng Identity](../../../../database/postgres/identity/README.md#email-upgrade);
[hồ sơ tích hợp E4-01](e4-01-2026-11-26.md).
