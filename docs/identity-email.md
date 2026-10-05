# Identity — Gmail SMTP và email worker

Cập nhật: 2026-10-05. Worker chạy bằng `BackgroundService` trong tiến trình API của modular monolith. Người dùng chọn Gmail SMTP với địa chỉ Gmail và App Password (DEC-114). Các luồng tài khoản và API được quản lý tại [Accounts](features/accounts.md).

## Cấu hình Gmail

Theo [hướng dẫn SMTP của Google](https://support.google.com/a/answer/176600?hl=en), adapter dùng `smtp.gmail.com:587`, STARTTLS, địa chỉ Gmail đầy đủ làm username và App Password. [Tạo App Password](https://support.google.com/accounts/answer/185833?hl=vi) yêu cầu bật xác minh hai bước; khả năng sử dụng phụ thuộc chính sách tài khoản Google. Khi đổi mật khẩu Google, cần tạo lại App Password đã bị thu hồi.

Với Compose, chép [.env.example](../.env.example) thành `.env`, rồi điền tại máy chạy:

```dotenv
SCDC_EMAIL_ENABLED=true
SCDC_PUBLIC_ORIGIN=http://localhost:3000
SCDC_GMAIL_ADDRESS=your-address@gmail.com
SCDC_GMAIL_APP_PASSWORD=your-app-password
```

`.env` bị Git ignore. Giữ bí mật ở backend; frontend không nhận thông tin SMTP. Khởi động lại API để nạp cấu hình: `podman compose up -d --build chat-service` (hoặc `docker compose` với engine đang dùng). Compose mặc định `SCDC_EXPOSE_DEVELOPMENT_TOKENS=false`; chỉ bật khi cần debug local. Mặc định `SCDC_EMAIL_ENABLED=false` để phát triển khi chưa có credentials; API vẫn tiếp nhận yêu cầu và worker vẫn loại envelope hết hạn/không còn hợp lệ.

Khi chạy API local bằng `dotnet run`, dùng environment variables hoặc user-secrets với các khóa `Modules:Identity:Email:Enabled`, `PublicOrigin`, `SenderAddress`, `AppPassword`. Environment variable dùng `__` thay `:`. Không cần thay source để thêm credentials.

`PublicOrigin` là origin frontend cố định, không lấy từ request Host. Liên kết là `/auth/verify#token=...` hoặc `/auth/reset#token=...`. HTTP chỉ được phép trong Development; môi trường khác cần HTTPS. Frontend đọc token từ fragment rồi xóa URL ngay, giữ token trong bộ nhớ và chỉ POST khi người dùng bấm xác minh/đặt lại. Không tự gửi lại thao tác consume khi response thất lạc. Auth và hồ sơ HTTP có `Cache-Control: no-store`; nginx dùng `Referrer-Policy: no-referrer`.

## Migration cho database hiện có

Volume PostgreSQL mới chạy `schema.sql` và migration tự động qua init. Volume đã có dữ liệu cần áp dụng [001_identity_email.sql](../database/postgres/migrations/001_identity_email.sql) trước khởi động API mới. Từ root repo, với Compose Development:

```bash
podman compose exec -T postgres psql -U scdc -d scdc_chat --single-transaction -v ON_ERROR_STOP=1 < database/postgres/migrations/001_identity_email.sql
```

Dùng cùng engine đã tạo stack. Migration thêm bảng policy/delivery, backfill token hiện hành và sửa giới hạn hồ sơ thành UTF-16; có thể chạy lại. Nếu hồ sơ cũ vượt giới hạn, transaction thất bại và cần đối chiếu dữ liệu trước chạy lại. `schema.sql` là script tạo lại schema Development, không dùng để nâng cấp database cần giữ dữ liệu.

Lệnh ngắn tương đương: `make db-migrate ENGINE=podman`. Build/restart API không tự chạy migration trên volume cũ. Lỗi `relation "identity.email_deliveries" does not exist` nghĩa database chưa có migration này.

Token cũ chỉ có hash nên không dựng lại được email. Sau migration, yêu cầu liên kết mới qua resend/forgot; cooldown tính từ lần cấp trước vẫn được giữ. Không chuyển các outbox cũ thành email không có token hợp lệ.

Gmail trong `.env` là tài khoản gửi thư. Email nhận recovery phải thuộc tài khoản đã đăng ký trong Identity; unknown email vẫn trả 202 accepted theo contract và không tạo delivery. Kiểm tra credentials SMTP thành công chưa chứng minh thư đã giao.

## Khóa mã hóa và môi trường ngoài Development

`identity.account_tokens` chỉ giữ hash. Token để gửi email nằm trong `identity.email_deliveries.protected_envelope`, được ASP.NET Core Data Protection mã hóa và xác thực; outbox chỉ giữ user/delivery ID và purpose. [IdentityModule](../services/Modules/Identity/IdentityModule.cs) dùng application name `SCDC.Identity` và lưu key ring ngoài database.

Compose Development giữ key ring ở volume `scdc-identity-keys`; local mặc định `artifacts/identity-keys`. Các instance xử lý cùng database cần cùng key ring và application name. Giữ key ring qua restart/deploy để giải mã email còn chờ. Không đưa key ring vào Git hoặc log.

Ngoài Development, cấu hình các khóa sau cùng signing key và connection string riêng:

| Khóa `Modules:Identity:Email:*` | Yêu cầu |
|---|---|
| `PublicOrigin` | Origin HTTPS frontend |
| `KeyRingPath` | Thư mục bền vững, có quyền truy cập phù hợp cho API |
| `KeyCertificatePath` | Đường dẫn PFX/PKCS#12 bảo vệ key ring; bắt buộc ngoài Development |
| `KeyCertificatePassword` | Mật khẩu certificate, truyền bằng secret nếu có |
| `SenderAddress`, `AppPassword` | Credentials khi `Enabled=true` |

Lưu/khôi phục certificate cùng key ring theo môi trường. Cách [bảo vệ key ring](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0) này không thay sổ bảo vệ quyền/nội dung sau restore trong [vòng đời dữ liệu](data-lifecycle.md).

## Xử lý delivery và cleanup

Đăng ký/resend/forgot ghi token, policy, delivery, audit và outbox trong cùng transaction. Resend/reset có cooldown tối thiểu 60 giây theo user/purpose; token mới vô hiệu token cũ cùng mục đích, verify và reset độc lập. Email lạ/đã xác minh/không đủ điều kiện/cooldown có cùng phản hồi accepted công khai.

Worker claim bằng PostgreSQL `FOR UPDATE SKIP LOCKED` và lease, kiểm tra token/tài khoản/email trước gửi, rồi gọi SMTP ngoài transaction. Không gửi delivery chưa commit. Có thể chạy nhiều instance; lease hết hạn được lấy lại sau crash. `LeaseSeconds` phải lớn hơn `SendTimeoutSeconds + 5`.

| Trạng thái / giá trị SQL | Ý nghĩa |
|---|---|
| `Pending` / 0 | Chờ xử lý |
| `Sending` / 1 | Đang có lease |
| `RetryPending` / 2 | Chờ thử lại lỗi tạm thời |
| `ProviderAccepted` / 3 | SMTP chấp nhận email; chưa chứng minh đã vào inbox |
| `Failed` / 4 | Lỗi không retry hoặc đã hết số lần/hạn token |
| `Suppressed` / 5 | Token hết hạn/đã dùng/bị thay thế hoặc tài khoản/email không còn phù hợp |

Mặc định poll 2 giây, batch 10, lease 30 giây, timeout SMTP 10 giây, tối đa 5 lần gửi. Backoff 10/30/90/300 giây cộng jitter 0–3 giây; không retry qua hạn token. Lỗi authentication/recipient vĩnh viễn kết thúc delivery. Timeout sau khi Gmail có thể đã nhận có thể tạo email lặp; Message-ID ổn định không bảo đảm dedup phía Gmail. Server vẫn chỉ chấp nhận một lần dùng token. Email cũ đang được giao không thể thu hồi, nhưng link bị thay thế sẽ bị từ chối.

Envelope bị xóa khi accepted/failed/suppressed. Mỗi vòng poll loại envelope hết hạn/không hợp lệ, kể cả khi sending disabled. Nếu worker dừng, cleanup tiếp tục khi API chạy lại. Outbox requested tương ứng ghi kết quả/attempt/error code và terminal marker; đây là dispatcher riêng cho email Identity, không dispatch các event module khác.

Maintenance chạy lúc khởi động và mỗi giờ: sau 7 ngày xóa delivery metadata/token terminal/refresh family của phiên terminal và ẩn IP/User-Agent/chi tiết thiết bị terminal; audit Identity xóa sau 90 ngày. Giữ active refresh family, timestamp cooldown, security stamp và marker phiên hết hạn/thu hồi. Chỉ xử lý audit/outbox email do Identity sở hữu; các worker retention module khác còn là thiết kế.

Quan sát metadata an toàn, không truy vấn envelope/token vào log:

```sql
SELECT id, purpose, status, attempt_count, next_attempt_at,
       lease_until, last_error_code, created_at, terminal_at
FROM identity.email_deliveries ORDER BY created_at DESC LIMIT 50;
SELECT * FROM identity.schema_migrations;
```

Khi lỗi credentials đã sửa, yêu cầu link mới qua UI/resend/forgot sau cooldown. Không sửa terminal delivery để gửi lại token đã bị thay thế.

## Bằng chứng kiểm thử

Ngày 2026-10-05: 44 backend tests qua trên .NET 10/PostgreSQL 18 với database thử riêng; 11 frontend tests và Vite production build qua trên Node 24. [IdentityCompletionTests](../tests/SCDC.Api.Tests/Identity/IdentityCompletionTests.cs) kiểm tra cooldown/đồng thời, pending reset, UTF-16, lease/retry/timeout/crash recovery, rollback, xóa envelope và cleanup giữ refresh family/marker. Chrome headless local đã chạy đăng ký→verify chủ động→login→lưu hồ sơ→change-password/thoát phiên→reset→login và xác nhận GET link chưa consume/fragment scrub. SMTP dùng sender giả trong test; chưa xác nhận giao Gmail thật vì chưa có App Password.

Chạy test backend với `SCDC_TEST_DATABASE` trỏ tới database thử đã có schema/migration; factory tắt hosted email worker và điều khiển processor trực tiếp. [Hướng dẫn phát triển](development.md#testing) có lệnh test/build. Image API đã build và smoke đăng ký thành công bằng user `app` (UID 1654) với volume key ring riêng, thư mục quyền 700; response không có token Development sử dụng được. Compose config và kiểm tra link docs đã qua. Việc đạt các test này không thay nghiệm thu realtime/admin/restore hoặc toàn bộ MVP.
