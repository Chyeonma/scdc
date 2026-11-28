# SCDC — CI build và kiểm thử

Workflow [CI](../../.github/workflows/ci.yml) chọn các bộ kiểm tra theo file thay đổi trên mỗi pull request và main push. Trang này giải thích khi nào cần chờ test backend, khi nào chỉ cần kiểm tra nhẹ và cách giữ check bắt buộc trước khi merge.

**Trạng thái cập nhật 2026-11-28:** tối ưu chọn job để xử lý [ISSUE-001](../project/tasks/issues.md#issue-001--ci-chạy-test-quá-lâu-cho-thay-đổi-nhỏ). Bộ test backend giữ nguyên; chưa chia test dùng chung database để chạy song song.

## Các job

Workflow luôn chạy cho pull request và push vào `main`, nên check `CI` luôn xuất hiện. Bước đầu đọc Git diff đầy đủ để chọn job; không dùng `paths-ignore` để bỏ toàn bộ workflow bắt buộc. Lần chạy mới trên cùng PR thay thế lần chạy cũ. Chạy thủ công bằng `workflow_dispatch` luôn yêu cầu tất cả các job.

| File thay đổi | Job nặng cần chạy |
|---|---|
| Chỉ Markdown hoặc công cụ đồng bộ Trello/kiểm tra docs đã nhận diện | Không có; vẫn chạy toàn bộ test Python và kiểm tra liên kết |
| `clients/WebClient/` | Frontend |
| Implementation trong `services/`, test C# | Backend |
| `database/`, `tools/` | Backend và chuyển database |
| Contracts, Shared.Core, BuildingBlocks, `.Grpc`, `.proto`, JSON/fixture trong `docs/`, workflow và cấu hình chưa nhận diện | Tất cả |

Thay đổi trộn nhiều nhóm lấy hợp các job cần chạy. Dockerfile, Nginx và cấu hình vận hành dùng chế độ đầy đủ. `.md` là tài liệu thuần; không coi toàn bộ `docs/` là tài liệu vì Protobuf và fixture trong đó là đầu vào build/test.

| Job | Công việc | Kết quả lưu 7 ngày |
|---|---|---|
| `Changes, docs and script checks` | Checkout đủ lịch sử, lập kế hoạch, chạy test Python và kiểm tra tài liệu | Kế hoạch nằm trong summary của workflow |
| `Backend` | Cài SDK .NET 10, khởi tạo PostgreSQL 18 riêng, restore và build toàn `SCDC.slnx` ở Release, chạy toàn bộ test backend | `backend-test-results`: báo cáo TRX, kể cả khi test thất bại |
| `Frontend` | Cài Node.js 24, `npm ci`, chạy test Node và build Vite | `web-client-build`: thư mục `dist` sau khi test và build đạt |
| `Identity database transfer` | Hai PostgreSQL instance thử riêng; 14 ca dữ liệu/DDL/sequence, quyền, guard, restore ngắt, chạy lại và phục hồi | `identity-transfer-summary`: chỉ kết quả đã lọc |
| `CI` | Luôn chạy; kiểm tra kế hoạch và kết quả các job được yêu cầu | Trạng thái check dùng cho quy tắc merge |

Các job nặng chạy độc lập sau khi bước lập kế hoạch và kiểm tra nhẹ đạt. `CI` chỉ chấp nhận job `skipped` khi kế hoạch nói rõ job đó không cần chạy. Job bắt buộc bị lỗi, hủy hoặc bỏ qua; kế hoạch thiếu/sai; hoặc kiểm tra nhẹ thất bại đều chặn merge. Không dùng `continue-on-error` để biến lỗi test thành đạt.

[ci_plan.py](../../scripts/ci_plan.py) dùng Git diff, gồm các commit của PR và cả đường dẫn cũ/mới khi rename. Nếu không đọc được diff, gặp đường dẫn mới chưa biết hoặc payload không hợp lệ, hệ thống yêu cầu chạy đầy đủ. [Bộ test](../../scripts/tests/test_ci_plan.py) kiểm các trường hợp chọn job và từ chối kết quả lỗi. Workflow [Documentation links](../../.github/workflows/docs.yml) giữ để chạy thủ công; kiểm tra docs tự động đã nằm trong CI, tránh chạy hai workflow giống nhau khi vừa push vừa mở PR.

SDK được giới hạn trong major 10 bởi [global.json](../../global.json). NuGet restore theo các project; npm dùng lockfile và cache tải package. Workflow chỉ cần quyền đọc mã nguồn và không dùng secrets của ứng dụng.

## Database kiểm thử

Mỗi lần chạy backend có service PostgreSQL mới với database `scdc_ci_test`, user quản trị `scdc_ci`. Mật khẩu trong workflow chỉ phục vụ service tạm này. Health check chờ kết nối TCP trước khi chạy [schema.sql](../../database/postgres/schema.sql) và [module-roles.sql](../../database/postgres/module-roles.sql); workflow không nạp seed. API dùng ba connection string/user Identity, Community, Messaging; fixture dùng `ConnectionStrings:TestDatabase` để tạo dữ liệu và database thử. Cấu hình test tắt pooling Identity và cho Messaging tối đa 50 kết nối để giữ được 40 transaction đồng thời; runtime local mặc định 20 mỗi pool.

User kiểm thử có quyền tạo và xóa các database fixture của test migration. Test tự tạo tài khoản và dữ liệu. Khóa DM ngẫu nhiên và thư mục khóa cursor nằm dưới `RUNNER_TEMP`; test factory cấp khóa Community riêng. Dispatcher nền được tắt vì test realtime chủ động gọi dispatcher để kiểm soát thời điểm phát sự kiện.

Muốn chạy trên máy, làm theo [database thử nghiệm riêng](development.md#local-environment) và cấu hình khóa trong [hướng dẫn kiểm thử](development.md#testing). Các lệnh restore, build và test chính xác nằm ở trường `run` của workflow.

## Bắt buộc CI trước khi merge

Sau khi workflow đã được đẩy lên GitHub và chạy ít nhất một lần, người có quyền quản trị cấu hình ruleset hoặc branch protection cho `main`:

1. Bật yêu cầu pull request trước khi merge, theo [quy trình dự án](../project/workflow.md).
2. Bật **Require status checks to pass before merging**, chọn check **`CI`** do GitHub Actions cung cấp. Job `CI` là check tổng hợp; không chọn chỉ một job con.
3. Áp dụng quy tắc cho các đối tượng cần bảo vệ và kiểm tra quyền bypass phù hợp với nhóm.

Workflow tạo kết quả kiểm tra; quy tắc của repository dùng kết quả đó để chặn merge. Cần kiểm chứng cả hai phần trên GitHub theo [tài liệu required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging).

## Nghiệm thu S1-04 trên GitHub

1. Tạo PR nháp kiểm chứng trên nhánh riêng đã chứa workflow này. Trong `ApiResponseTests.Health_returns_typed_success_response`, tạm đổi expected `healthy` thành `ci-intentional-failure`.
2. Chờ Actions chạy. Xác nhận `Backend` và `CI` thất bại; tải TRX để đối chiếu đúng test vừa sửa. Lưu link PR, run và bằng chứng merge bị chặn bởi check `CI`.
3. Hoàn tác assertion, đẩy commit sửa lên cùng PR; xác nhận `Backend`, `Frontend` và `CI` đều đạt. Đóng PR kiểm chứng sau khi lưu bằng chứng, không merge thay đổi cố ý làm hỏng test.
4. Ghi kết quả GitHub vào hồ sơ kiểm chứng. Chỉ khi PR hỏng bị chặn mới đánh dấu tiêu chí hoàn tất S1-04 đạt; sau đó vẫn cần review theo thẻ.

Nguồn: [S1-04](../project/tasks/step-1.md), [STEP-1](../project/planning.md#step-1).
