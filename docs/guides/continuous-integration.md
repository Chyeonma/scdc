# SCDC — CI build và kiểm thử

Workflow [CI](../../.github/workflows/ci.yml) build backend và WebClient, chạy test trên mỗi pull request. Trang này hướng dẫn đọc kết quả và cấu hình check bắt buộc trước khi merge vào `main`.

**Trạng thái:** S1-04 đã có workflow; kết quả chạy local và phần nghiệm thu GitHub còn thiếu nằm trong [hồ sơ kiểm chứng](../records/verification/ci/s1-04-2026-11-13.md).

## Các job

Workflow chạy cho mọi pull request, khi push vào `main` và khi gọi thủ công bằng `workflow_dispatch`. Workflow không lọc đường dẫn để PR chỉ sửa tài liệu cũng có check `CI`. Lần chạy mới trên cùng PR thay thế lần chạy cũ.

| Job | Công việc | Kết quả lưu 7 ngày |
|---|---|---|
| `Backend` | Cài SDK .NET 10, khởi tạo PostgreSQL 18 riêng, restore và build toàn `SCDC.slnx` ở Release, chạy toàn bộ test backend | `backend-test-results`: báo cáo TRX, kể cả khi test thất bại |
| `Frontend` | Cài Node.js 24, `npm ci`, chạy test Node và build Vite | `web-client-build`: thư mục `dist` sau khi test và build đạt |
| `CI` | Chờ hai job trên; chỉ thành công khi cả hai thành công | Trạng thái check dùng cho quy tắc merge |

Backend và frontend chạy độc lập. `CI` vẫn chạy khi một job thất bại, bị bỏ qua hoặc hủy, rồi trả lỗi nếu kết quả của một trong hai khác `success`. Các bước build và test không bỏ qua mã thoát lỗi.

SDK được giới hạn trong major 10 bởi [global.json](../../global.json). NuGet restore theo các project; npm dùng lockfile và cache tải package. Workflow chỉ cần quyền đọc mã nguồn và không dùng secrets của ứng dụng.

## Database kiểm thử

Mỗi lần chạy backend có service PostgreSQL mới với database `scdc_ci_test`, user `scdc_ci`. Mật khẩu trong workflow chỉ phục vụ service tạm này. Health check chờ kết nối TCP trước khi chạy [schema.sql](../../database/postgres/schema.sql); workflow không nạp seed.

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
