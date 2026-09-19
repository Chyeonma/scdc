# Fixture Messaging v1 — P0-T03

Ngày: **19/09/2026**. Tài liệu này biến P0-T03 thành điều kiện chạy lặp lại được cho các integration test Messaging. Nó không xác nhận endpoint Messaging đã tồn tại; user test thật được tạo qua API Identity đang có, còn dữ liệu chat sẽ do từng test slice tạo.

Nguồn: [test web factory](../../tests/SCDC.Api.Tests/Infrastructure/SCDCWebApplicationFactory.cs), [Identity flow test](../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs), [config Testing](../../services/SCDC.Api/appsettings.Testing.json), [compose test](../../compose.test.yaml), [schema bootstrap](../../database/postgres/schema.sql), [seed](../../database/postgres/seed.sql), [chính sách](policies.md), [contract](contracts.md).

## 1. Boundary của test database — P0-T03.1

Integration test dùng database `scdc_chat_test` tại port `5433`. Đây là database khác hẳn database local `scdc_chat` ở port `5432`; test host trong `SCDCWebApplicationFactory` chạy environment `Testing` và nạp `appsettings.Testing.json`. Không dùng `ASPNETCORE_ENVIRONMENT=Development` cho test vì file Development trỏ vào database local thường.

Khởi tạo lần đầu từ repository root:

```powershell
podman compose -f compose.test.yaml up -d
dotnet test tests/SCDC.Api.Tests/SCDC.Api.Tests.csproj
```

Docker Compose có thể thay `podman compose`. Trước khi chạy test, xác minh endpoint container healthy:

```powershell
podman compose -f compose.test.yaml ps
```

Test factory dùng `Host=localhost;Port=5433;Database=scdc_chat_test;Username=scdc_test;Password=scdc_test` mặc định. CI hoặc môi trường riêng chỉ được override bằng `ConnectionStrings__Database` khi connection string vẫn trỏ tới database test chuyên dụng. Không truyền connection string production/development qua biến này.

`schema.sql` và `seed.sql` được PostgreSQL chạy **chỉ khi volume test mới tạo**. Seed cung cấp quan hệ minh họa, nhưng password/token seed không dùng để login. Test không được dựa vào ID seed hoặc sửa dữ liệu seed; mỗi test tạo dữ liệu riêng và tự dọn dữ liệu đó.

### Reset an toàn database test

Khi cần xây lại toàn bộ fixture sau đổi schema, chỉ xóa volume **đúng tên test** rồi khởi tạo lại. Lệnh này xóa toàn bộ dữ liệu `scdc_chat_test`, không tác động database local `scdc_chat`.

```powershell
podman compose -f compose.test.yaml down -v
podman compose -f compose.test.yaml up -d
```

Kiểm tra tên service/file trước khi chạy. Không dùng `database/postgres/schema.sql` trên database đã có dữ liệu cần giữ: script mở đầu bằng `DROP SCHEMA ... CASCADE`.

## 2. Tài khoản và kịch bản actor — P0-T03.1/.3

Mỗi test Messaging tạo actor qua chuỗi API Identity thật, không insert trực tiếp password/token:

1. Sinh suffix ngẫu nhiên cho username/email, ví dụ `msg_a_<guid-12>@example.test`.
2. `POST /api/v1/auth/register` với password test hợp lệ.
3. Trong environment Testing, lấy `developmentVerificationToken` từ response rồi gọi `POST /api/v1/auth/verify-email`.
4. `POST /api/v1/auth/login`, giữ access token/session riêng cho từng actor.
5. Cleanup trong `finally`, chỉ xóa resource có ID/suffix mà test đã tạo. Không truncate schema hoặc xóa user seed để cleanup một test.

`IdentityV1FlowTests` là mẫu có sẵn cho register/verify/login và cleanup. Khi Messaging có endpoint, tạo `MessagingScenario`/fixture dùng API helpers chung; không copy token giữa actor, không dùng test account cố định và không giả `authorUserId` trong request.

| Actor | Trạng thái | Dùng trong P1–P3 | Dùng trong P4+ |
|---|---|---|---|
| A | Active, token/session riêng | Người mở DM/gửi tin | Owner group hoặc server member có quyền |
| B | Active, token/session riêng | Người nhận DM/đối chiếu history/realtime | Thành viên group/channel có quyền |
| C | Active, token/session riêng nhưng không thuộc DM A–B | Kiểm tra đoán ID, history, send, subscribe và file bị từ chối | Thành viên không có quyền hoặc thêm vào group để test lịch sử/revoke |

Mỗi test dùng tối thiểu A, B, C. C chỉ được thêm vào group ở P4-T01; trước đó C phải không có membership. Test channel tạo server/channel/role riêng, cấp quyền tối thiểu đúng case rồi cleanup theo server/space ID của test.

### Ma trận fixture bắt buộc

| ID | Setup | Kết quả cần test ở slice nhận việc |
|---|---|---|
| FX-01 | A và B Active, không block, không có DM | A/B mở đồng thời nhận cùng DM; cả hai gửi/đọc theo P1/P2 |
| FX-02 | C Active nhưng không thuộc DM A–B | C nhận NotFound chung khi read/history/send/subscribe theo P1–P3 |
| FX-03 | A block B sau khi đã có DM | Mở/gửi/thao tác tương tác bị từ chối theo P0-T01; history theo policy |
| FX-04 | A/B/C group mới | C xem toàn bộ history sau khi vào, mất access khi rời/bị xóa, xử lý thêm lại theo P4/P5 |
| FX-05 | Channel riêng có A quyền read/send, B read-only, C ngoài server | Capability và HTTP/Hub đều khớp quyền Community ở P4 |
| FX-06 | A có hai session; B có một session | Revoke một session chỉ ảnh hưởng connection đúng session ở P3 |

## 3. Isolation, cleanup và concurrency

- Tên user/email, display name test và mọi idempotency key phải có suffix test. Space/server/channel tạo mới phải được giữ ID trong fixture để cleanup chính xác.
- Không dùng sequence, timestamp hoặc thứ tự seed làm assertion. Assert theo resource ID/suffix do test sinh và contract của response.
- Data setup và cleanup chạy bằng API nơi API đã tồn tại. Nếu test cần SQL để assert transaction/index/query plan, SQL chỉ truy vấn hoặc xóa theo ID test trong transaction; không thao tác `DELETE`/`TRUNCATE` không có scope.
- Cleanup order phải tôn trọng FK: outbox/inbox/audit → report/interaction/attachment/message → membership/space/channel/server → identity user. Khi một slice có API xóa đúng rule, ưu tiên API đó; sau đó dọn infrastructure record có phạm vi ID test.
- Không chạy các test reset fixture song song. Test tạo resource ngẫu nhiên có thể chạy song song khi không sửa cùng actor/space. Khi thêm test dùng reset database, đặt nó vào xUnit collection không parallel và không trộn với test suite khác.
- Mọi test có retry, race hoặc realtime dùng barrier/timeline rõ ràng, timeout hữu hạn và cleanup trong `finally`; không dùng `Task.Delay` tùy ý làm bằng chứng event đã tới.

## 4. SQL bootstrap và migration — P0-T03.2

`database/postgres/schema.sql` là **bootstrap source of truth cho database mới** và được Compose dùng khi tạo volume mới. Nó không phải migration script. Thay đổi schema sau khi local/CI đã có dữ liệu dùng migration tăng tiến, version hóa theo repository:

```text
database/postgres/migrations/
  20260919_001_messaging_<slug>.sql
```

Quy tắc migration:

1. Mỗi thay đổi có một file mới theo thứ tự tăng; không sửa migration đã áp dụng. File có `\set ON_ERROR_STOP on`, transaction khi PostgreSQL cho phép, và comment owner/task.
2. Migration phải an toàn khi chạy đúng một lần: tạo record trong ledger schema `integration` hoặc fail rõ khi version đã áp dụng. P3/P9 sẽ chốt/triển khai migration runner và ledger; P0 không tạo migration trống.
3. Thay đổi phá vỡ dùng expand → backfill → chuyển code → contract/client → contract database. Không đổi/drop cột, index hoặc constraint đang phục vụ code cũ trong cùng deployment khi chưa có bước tương thích.
4. Test migration trên bản sao/snapshot database test đã có dữ liệu seed và dữ liệu fixture. `schema.sql` chỉ dùng để bootstrap test DB mới sau khi migration đã được phản ánh vào source bootstrap.
5. Viết rollback plan trong PR. Với migration destructive, rollback có thể là forward-fix/restore backup; không hứa rollback SQL khi dữ liệu đã biến đổi không đảo ngược.
6. Chỉ sau khi migration được review và áp dụng vào môi trường thích hợp mới cập nhật `schema.sql` để database mới có cùng cấu trúc. Không chạy schema bootstrap để nâng cấp môi trường cũ.

Tại P1/P2 trước khi có migration runner, migration được chạy có kiểm soát bằng `psql`/DBeaver trên **connection test được kiểm tra tên database trước**, sau đó lưu output/version vào PR. Không tự chạy script SQL có `DROP` trên bất kỳ DB nào.

## 5. Lệnh và tiêu chí nghiệm thu P0-T03

Kiểm tra cấu hình/document trong task này:

```powershell
git diff --check
dotnet build SCDC.slnx
```

Kiểm tra integration sau khi test container healthy:

```powershell
dotnet test tests/SCDC.Api.Tests/SCDC.Api.Tests.csproj
```

P0-T03 hoàn tất khi:

- Test host mặc định dùng `Testing` và database test riêng, không dùng `scdc_chat` local.
- Có lệnh dựng/reset fixture test riêng; reset chỉ nhắm `scdc-postgres-test-data`.
- Có luồng tạo A/B/C qua Identity API và matrix case không có quyền.
- Có quy tắc migration tăng tiến; `schema.sql` được ghi rõ chỉ dùng bootstrap.
- Các slice Messaging có thể dùng fixture để lặp test mà không dựa seed hay ảnh hưởng database phát triển.

## 6. Giới hạn hiện tại và bàn giao

P0-T03 không tự khởi động container, không reset volume và không chạy runtime integration test trong PR tài liệu/cấu hình này. Việc đó cần Docker/Podman sẵn có; lệnh ở mục 1 là cách thực hiện trước P1/P2. Task cũng chưa thêm migration runner, ledger, MessagingScenario hay API Messaging vì các dependency đó chưa tồn tại.

| Subtask | Bằng chứng |
|---|---|
| P0-T03.1 | Mục 1–2; test environment/config/compose riêng và luồng Identity thật |
| P0-T03.2 | Mục 4; bootstrap vs migration và quy trình upgrade database có dữ liệu |
| P0-T03.3 | Mục 2–3; actor A/B/C, fixture matrix, isolation và cleanup |

Tài liệu này là hợp đồng test cho P1–P9. Mỗi slice mới phải thêm test runtime tương ứng thay vì coi ma trận fixture là kết quả đã chạy.
