# SCDC — Hồ sơ bàn giao MVP

Cập nhật: 2026-10-07. Phạm vi ba tính năng theo DEC-114, một API host theo [DEC-116](../decisions.md#dec-116), phân công kế hoạch theo [DEC-117](../decisions.md#dec-117). Đã chọn phạm vi gói Community đầu; danh sách UC/AC toàn MVP và lịch còn cần khóa theo gói, chưa có xác nhận hoàn tất.

## Mục đích và giới hạn

MVP là mốc đầu tiên trước [v1](v1.md): có bản chạy được để ba thành viên bắt đầu làm và tích hợp, tận dụng phần Identity Vg đã xây dựng gần xong. Đây là hồ sơ để bắt đầu công việc; không yêu cầu triển khai toàn bộ bản đặc tả đầy đủ ngay từ đầu.

| Nội dung | Đã xác nhận | Cần chốt theo gói |
|---|---|---|
| Tính năng | Identity, Community, Direct Messaging | UC và nhánh ngoại lệ phải hoàn tất trong từng gói |
| Kiến trúc | MVP dùng một API host Modular Monolith; microservice thuộc v1 theo DEC-116 | Giữ ranh giới module/dữ liệu/hợp đồng hiện có; không cần tách DB/service để bàn giao MVP |
| Media | Gọi điện, phòng thoại, video và chia sẻ màn hình thuộc v1 | Không có gói media bắt buộc để hoàn thành MVP |
| Phát hành | Thêm mốc làm việc trước bản đầy đủ | Môi trường bàn giao và người kiểm tra mốc; MVP không tự cấp quyền mở công khai |
| Lịch và công sức | Mỗi người 3–4 ngày/tuần theo DEC-088 | Ước lượng riêng mốc MVP; không coi lịch 16 tuần của bản đầy đủ là deadline MVP |

## Các luồng tối thiểu đề xuất

| Tính năng | Luồng chạy thật cần làm trước | Đặc tả nguồn | Phần hoàn thiện sau mốc đầu |
|---|---|---|---|
| Identity | Đăng ký, xác minh email, đăng nhập, duy trì phiên và đăng xuất; tận dụng code hiện có | [Use case và đối chiếu source](../features/accounts/specs/use-cases.md#use-cases), [API hiện tại](../features/accounts/design/README.md#api-current) | Đóng các gap còn lại và kiểm chứng đầy đủ vòng đời tài khoản theo v1; không xóa phần đã làm chỉ để giảm scope |
| Community | Tạo/xem cộng đồng của mình; một đường tham gia hoạt động; tạo/xem phòng text và giao tiếp trong phòng | [UC-COM](../features/community/specs/README.md#use-cases), [Servers](../features/community/specs/servers.md), [Memberships](../features/community/specs/memberships.md), [Channels](../features/community/specs/channels.md), [tin phòng](../features/community/specs/integration.md#use-cases) | Đủ các đường tham gia/lời mời, quản lý vai trò/ACL, chuyển owner và các nhánh quản lý trong đặc tả v1 |
| Direct Messaging | Tìm người, mở hội thoại hai người, gửi/nhận văn bản, xem lại lịch sử sau reload hoặc mở lại | [Quy tắc DM](../features/direct-messaging/specs/requirements.md#requirements), [hợp đồng](../features/direct-messaging/design/README.md#contracts), [AC](../features/direct-messaging/specs/acceptance.md#acceptance) | Bổ sung và kiểm chứng đầy đủ sửa/xóa, retry không trùng, phân trang/reconnect và các tình huống đồng thời của v1 |

Ngày 2026-10-07 đã chọn [gói Community đầu tiên](../features/community/delivery/create-view/plan.md#first-package): UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03. Scope/tiêu chí ở kế hoạch gói, các bước và tiến độ hiện tại ở [status.md](../features/community/status.md). Tải phòng/lịch sử và đối soát sau leave/rejoin của UC-COM-03 thuộc các gói sau; gói đầu đạt chưa đồng nghĩa toàn bộ UC-COM-03 đạt. Tiếp theo bổ sung UC-COM-06 và phòng/tin theo phụ thuộc. Các UC vẫn giữ quy tắc của đặc tả nguồn; thêm mốc MVP không tự thay đổi quyền, nội dung tin hoặc hợp đồng API.

Việc chưa đưa một thao tác vào gói đầu không đồng nghĩa tự bỏ nó khỏi toàn bộ MVP. Trước khi khóa mốc, Vg và người thực hiện lập danh sách UC/AC cụ thể, gồm các nhánh cần thiết của luồng đã chọn. Phạm vi tạo/xem của gói Community đầu đã được chọn; bảng trên chưa phải baseline UC/AC của toàn MVP đã được xác nhận.

<a id="packages"></a>

## Gói việc và phân công kế hoạch

Phân công dưới đây theo DEC-117 và [nguồn nhân sự](../project/planning.md#team). Mỗi người theo đầu ra đến khi chạy được, gồm dữ liệu, backend, frontend, kiểm thử và tích hợp theo nhu cầu; mỗi người giữ một gói chính đang làm. [Phân bổ công suất](../project/planning.md#capacity) tính cả hướng dẫn/review và giữ 20% dự phòng; đây chưa phải bằng chứng các gói đã hoàn tất.

| Gói | Đầu ra bàn giao | Người phụ trách | Phụ thuộc trực tiếp |
|---|---|---|---|
| MVP-ID | Hoàn thiện phần Identity còn lại đủ dùng cho Community/DM; hợp đồng phiên/user/job email giữa module | Vg | Code Identity gần xong; chỉ tính công sức còn lại, không bắt đầu lại tính năng |
| MVP-COM | Luồng tạo/tham gia cộng đồng, phòng text và quyền truy cập cơ bản chạy từ DB tới frontend | Vg | MVP-ID; hợp đồng nền nhắn tin và kiểm tra quyền với Sáng |
| MVP-DM | Nhắn tin riêng từ DB tới frontend; nền lưu/gửi/nhận/lịch sử dùng chung cho tin phòng | Sáng | MVP-ID; quy tắc DM và hợp đồng quyền/tin phòng với Vg |
| MVP-SYS | Hai đầu ra trọn gói: Email Worker; bộ chạy Compose/config/CI, dữ liệu demo và kiểm tra nhanh | Thái | Job contract do Identity cung cấp; cấu hình một host hiện có; kỳ vọng kiểm thử nghiệp vụ do Vg/Sáng cung cấp |

MVP-COM và MVP-DM thực hiện song song sau khi thống nhất hợp đồng nhắn tin/quyền. MVP-SYS bắt đầu ngay từ môi trường hiện có và job email, không chờ hai tính năng kia hoàn tất. Chuyển nhiều service là gói v1; không cần làm trước để bắt đầu MVP.

Vg/Sáng tự kiểm thử gói nghiệp vụ của mình; Thái cung cấp dataset, bộ chạy và tổng hợp kết quả. Khi một người quá tải, điều chuyển một đầu ra đã có contract/AC rõ theo [quy tắc cân tải](../project/planning.md#capacity), đồng thời giảm phần việc cũ của người nhận. Không giao Thái toàn bộ frontend/QA hoặc thêm gửi file vào scope để lấp công suất.

Worker là tiến trình backend trong phạm vi Identity. Identity giữ logic cấp/consume token và hiệu lực liên kết; worker thực hiện gửi, retry và ghi kết quả theo policy đã chọn. Hợp đồng gửi email hiện còn cần hoàn thiện tại [Accounts](../features/accounts/design/README.md#detailed-design); outbox chỉ tham chiếu token đã băm chưa đủ để dựng liên kết.

<a id="acceptance"></a>

## Điều kiện hoàn tất MVP đề xuất

| Điều kiện | Bằng chứng cần có |
|---|---|
| Chạy lại được hệ thống | Bản checkout và cấu hình đã ghi rõ dựng được môi trường; dữ liệu không mất chỉ vì restart ứng dụng |
| Ranh giới module đúng | Identity/Community/Messaging chạy trong một API host; dữ liệu/hợp đồng thuộc module tương ứng, worker email hoạt động trong ranh giới Identity. Chứng minh microservice là điều kiện v1 |
| Identity dùng được | Hai tài khoản thử hoàn tất luồng đã chọn và dùng được Community/DM; phiên sai bị chặn |
| Community dùng được | Tạo, tham gia, mở phòng và giao tiếp theo scope; người ngoài không đọc/gửi được dữ liệu riêng |
| DM dùng được | Hai người gửi/nhận và xem lịch sử đã lưu; tài khoản thứ ba không đọc/gửi trong DM đó |
| Trạng thái giao diện đúng | Chỉ báo đã gửi khi tin được lưu; lỗi API hoặc mất kết nối có phản hồi rõ, không chỉ chạy với dữ liệu mẫu |
| Có hồ sơ bàn giao | Commit/build, UC/AC được chọn, dữ liệu thử, thao tác, kết quả, lỗi và phần v1 chưa triển khai được ghi nhận |

Ghi kết quả bằng [mẫu hồ sơ](../templates/release-record.md), loại bàn giao MVP; không đánh dấu tiêu chí đã đạt từ việc có code hoặc có test. Người làm tự kiểm tra gói của mình, người khác kiểm tra lại luồng chính; người kiểm tra và thời điểm được ghi trong hồ sơ.

Các chỉ tiêu đầy đủ về tải, trình duyệt, media, backup/restore và phát hành công khai nằm trong [v1](v1.md#acceptance). Bản MVP phải kiểm chứng các điều kiện của scope đã chọn; không dùng nhãn thử nghiệm để bỏ kiểm tra xác thực, quyền truy cập hoặc lưu dữ liệu của luồng đó. Bổ sung chỉ tiêu vào MVP cần ghi phạm vi, cách đo và công sức tương ứng.

## Cách bắt đầu một gói

1. Chọn gói và danh sách UC/AC; đối chiếu đúng mục đặc tả nguồn.
2. Chốt phụ thuộc trực tiếp, người làm/rà soát, đầu ra và cách thử; ghi riêng nội dung chưa quyết định.
3. Triển khai một luồng chạy thật, thử trên các tài khoản/dataset đã chọn và tích hợp ngay.
4. Ghi kết quả và lỗi; mở gói kế tiếp hoặc đưa phần hoàn thiện còn lại vào kế hoạch v1.

Nếu phát hiện một quy tắc v1 cần đổi để thực hiện MVP, ghi quyết định và cập nhật đặc tả/hợp đồng bị ảnh hưởng. Không tạo một bộ quy tắc mới nằm riêng trong task hoặc bản sao tài liệu MVP.
