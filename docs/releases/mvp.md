# SCDC — Hồ sơ bàn giao MVP

Cập nhật: 2026-10-08. Phạm vi ba tính năng theo DEC-114, một API host theo [DEC-116](../records/decisions/README.md#dec-116), phân công kế hoạch theo [DEC-117](../records/decisions/README.md#dec-117). Phạm vi chức năng Community MVP đã chọn theo [DEC-118](../records/decisions/README.md#dec-118); chi tiết triển khai/nghiệm thu từng gói, phạm vi Identity/DM và lịch toàn MVP còn cần khóa. Chưa có xác nhận hoàn tất.

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
| Identity | Đăng ký, xác minh email, đăng nhập, duy trì phiên và đăng xuất; tận dụng code hiện có | [Use case và đối chiếu source](../features/accounts/README.md), [API hiện tại](../features/accounts/README.md) | Đóng các gap còn lại và kiểm chứng đầy đủ vòng đời tài khoản theo v1; không xóa phần đã làm chỉ để giảm scope |
| Community | Phạm vi đã chọn: tạo/tìm/tham gia public/immediate, phòng text, lịch sử/gửi/nhận tin và mất quyền; hồi quy role/ACL đã có | [Baseline Community](#community-scope), [UC-COM](../features/community/README.md#use-cases) | Các đường approval/lời mời, quản lý server/owner/leave, xóa phòng, sửa/xóa tin và voice theo đặc tả v1 |
| Direct Messaging | Tìm người, mở hội thoại hai người, gửi/nhận văn bản, xem lại lịch sử sau reload hoặc mở lại | [Quy tắc DM](../features/messaging/direct-messaging.md#requirements), [hợp đồng](../features/messaging/direct-messaging.md#contracts), [AC](../features/messaging/direct-messaging.md#acceptance) | Bổ sung và kiểm chứng đầy đủ sửa/xóa, retry không trùng, phân trang/reconnect và các tình huống đồng thời của v1 |

Ngày 2026-10-07 đã chọn [gói Community đầu tiên](../records/verification/community/create-view.md#scope-first-package): UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03. Các gói đã kiểm chứng và công việc tiếp theo được quản lý tại [các chủ đề Community](../features/community/README.md). Gói đầu đạt chưa đồng nghĩa toàn bộ UC-COM-03 đạt. Các UC vẫn giữ quy tắc của đặc tả nguồn; thêm mốc MVP không tự thay đổi quyền, nội dung tin hoặc hợp đồng API.

Phạm vi Community dưới đây đã được người dùng chọn; bảng Identity/DM vẫn là đề xuất luồng. Trước từng gói, Vg và người thực hiện khóa UC/AC, nhánh ngoại lệ, contract và cách kiểm chứng. Bản này chưa xác nhận nghiệm thu hoặc khóa toàn bộ mốc MVP.

<a id="community-scope"></a>

## Phạm vi Community MVP đã chọn

Ngày 2026-10-07 người dùng chọn luồng tối thiểu “tạo/tìm/tham gia, phòng text, lịch sử/gửi/nhận tin và xử lý mất quyền”; các chức năng role/ACL đã làm được giữ và kiểm thử hồi quy. Ghi nhận tại DEC-118. Bảng này xác định phạm vi chức năng và gate; trạng thái thực hiện chỉ ghi tại [Community status](../features/community/README.md).

| Luồng | UC và tiêu chí nguồn trong phạm vi | Ranh giới mốc |
|---|---|---|
| Tạo, tìm và xem cộng đồng | UC-COM-01/02 và phần UC-COM-03; [AC Servers](../features/community/servers.md#acceptance) | Tạo public/private; danh sách/detail/tư cách; tìm và preview public, không lộ dữ liệu private |
| Tham gia | Phần public/immediate UC-COM-06; [AC Memberships](../features/community/memberships.md#acceptance) | Join lặp/đồng thời không tạo tư cách trùng; member nhận quyền hiện hành, epoch cũ không được dùng lại; approval/lời mời thuộc mở rộng |
| Phòng text và lịch sử | Phần text UC-COM-16/17 và tải phòng của UC-COM-03; [AC Channels](../features/community/channels.md#acceptance) | Tạo/list/detail phòng theo view; lịch sử phân trang chỉ đọc trong phòng được cấp quyền; voice và xóa phòng thuộc mở rộng |
| Gửi/nhận tin văn bản | UC-COM-23 và phần chat UC-COM-25; [AC tích hợp](../features/messaging/channel-messaging.md#acceptance) | Lưu bền, gửi/retry chống trùng, nhận realtime và đối soát sau reconnect; sửa/xóa tin UC-COM-24 thuộc mở rộng |
| Xử lý mất quyền và hồi quy | Phần chat UC-COM-25; UC-COM-18/20/21/22 đã có và [AC Permissions](../features/community/access-control.md#acceptance) | Giữ edit phòng, role/assignment và ACL; mutation quyền/session chặn HTTP, gửi, subscribe/resume và kết nối đang mở; owner vẫn chịu kiểm tra account/session |

Phần UC-COM-04/05/07–15, UC-COM-19/24 và voice/media được theo dõi cho mốc mở rộng/v1 tại [bảng công việc](../project/planning.md#community-work-items). Chúng tiếp tục có đặc tả/AC/TC đầy đủ; lựa chọn này không xóa code hoặc yêu cầu v1. Những UC phối hợp chỉ được đánh dấu đạt trong phạm vi đã chọn, không đóng cả UC từ một phần test.

<a id="community-acceptance"></a>

### Điều kiện hoàn tất Community trong mốc MVP

| Gate | Bằng chứng cần ghi trên bản bàn giao |
|---|---|
| COM-MVP-01: Toàn luồng | Hai tài khoản verified và một outsider chạy tạo → tìm → public/immediate join → phòng text → lịch sử/gửi/nhận trên DB/API/Hub thật; reload/restart đọc lại dữ liệu đã commit |
| COM-MVP-02: Lưu tin và retry | Mất response, retry cùng operation và gửi đồng thời không tạo tin trùng; tin/sequence/outbox nguyên tử; lỗi/lease hết hạn rollback; giao diện chỉ báo đã gửi sau xác nhận lưu |
| COM-MVP-03: Lịch sử/reconnect | Keyset không bỏ/trùng tin đã commit theo contract; cursor gắn actor/space/quyền; reconnect bù tin thiếu, dedup realtime, xử lý cursor lỗi và trạng thái rỗng/lỗi bằng dữ liệu thật |
| COM-MVP-04: Quyền hiện hành | Outsider, hidden room, stale epoch, phiên/account sai bị chặn đọc/gửi/subscribe/resume; management không tự cấp view; proof đổi quyền tranh gửi/subscribe tới commit |
| COM-MVP-05: Mất quyền đang kết nối | Đo từ commit thay đổi role/ACL hoặc thu hồi session/account đến ngừng truy cập chat trên kết nối đang mở trong ≤5 giây theo DEC-083; ghi timestamp, cách đo, restart/lỗi authority và kết quả fail-close; HTTP guard riêng chưa đủ |
| COM-MVP-06: Giao diện và hồi quy | Đổi actor/mất quyền không phục hồi dữ liệu riêng từ request trễ/cache; mutation lỗi không tự POST lại; create/join/search/role/ACL/edit vẫn đạt, gồm Unicode, CAS, epoch, pagination và operation recovery |
| COM-MVP-07: Chạy lại và hồ sơ | Commit code, schema/ledger, cấu hình/keyring, dataset/tài khoản và bộ chạy rõ; actual result gắn UC/AC/TC và artifact; liệt kê lỗi/phần chưa kiểm chứng, có người kiểm tra lại theo quy trình MVP |

Gate áp dụng cho luồng Community đã chọn, chưa có gate nào được coi đạt chỉ từ việc bổ sung bảng này. Test gói nền hiện có được đối chiếu tại [verification.md](../records/verification/community/README.md); các proof Messaging/Hub và deadline thu hồi cần chạy trong gói tương ứng. Gate release, người duyệt DEC-111 và phạm vi toàn MVP vẫn theo phần [nghiệm thu](#acceptance); yêu cầu v1 về tải/trình duyệt/media/restore được giữ nguyên.

<a id="packages"></a>

## Gói việc và phân công kế hoạch

Phân công dưới đây theo DEC-117 và [nguồn nhân sự](../project/planning.md#team). Mỗi người theo đầu ra đến khi chạy được, gồm dữ liệu, backend, frontend, kiểm thử và tích hợp theo nhu cầu; mỗi người giữ một gói chính đang làm. [Phân bổ công suất](../project/planning.md#capacity) tính cả hướng dẫn/review và giữ 20% dự phòng; đây chưa phải bằng chứng các gói đã hoàn tất.

| Gói | Đầu ra bàn giao | Người phụ trách | Phụ thuộc trực tiếp |
|---|---|---|---|
| MVP-ID | Hoàn thiện phần Identity còn lại đủ dùng cho Community/DM; hợp đồng phiên/user/job email giữa module | Vg | Ước lượng công sức còn lại theo [hiện trạng Accounts](../features/accounts/README.md) |
| MVP-COM | Luồng tạo/tham gia cộng đồng, phòng text và quyền truy cập cơ bản chạy từ DB tới frontend | Vg | MVP-ID; hợp đồng nền nhắn tin và kiểm tra quyền với Sáng |
| MVP-DM | Nhắn tin riêng từ DB tới frontend; nền lưu/gửi/nhận/lịch sử dùng chung cho tin phòng | Sáng | MVP-ID; quy tắc DM và hợp đồng quyền/tin phòng với Vg |
| MVP-SYS | Hai đầu ra trọn gói: Email Worker; bộ chạy Compose/config/CI, dữ liệu demo và kiểm tra nhanh | Thái | Job contract do Identity cung cấp; cấu hình một host hiện có; kỳ vọng kiểm thử nghiệp vụ do Vg/Sáng cung cấp |

MVP-COM và MVP-DM thực hiện song song sau khi thống nhất hợp đồng nhắn tin/quyền. MVP-SYS bắt đầu ngay từ môi trường hiện có và job email, không chờ hai tính năng kia hoàn tất. Chuyển nhiều service là gói v1; không cần làm trước để bắt đầu MVP.

Vg/Sáng tự kiểm thử gói nghiệp vụ của mình; Thái cung cấp dataset, bộ chạy và tổng hợp kết quả. Khi một người quá tải, điều chuyển một đầu ra đã có contract/AC rõ theo [quy tắc cân tải](../project/planning.md#capacity), đồng thời giảm phần việc cũ của người nhận. Không giao Thái toàn bộ frontend/QA hoặc thêm gửi file vào scope để lấp công suất.

Worker là tiến trình backend trong phạm vi Identity. Identity giữ logic cấp/consume token và hiệu lực liên kết; worker thực hiện gửi, retry và ghi kết quả theo policy đã chọn. Hợp đồng gửi email hiện còn cần hoàn thiện tại [Accounts](../features/accounts/email-links.md#contracts); outbox chỉ tham chiếu token đã băm chưa đủ để dựng liên kết.

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

Ghi kết quả bằng [mẫu hồ sơ](../records/templates/release-record.md), loại bàn giao MVP; không đánh dấu tiêu chí đã đạt từ việc có code hoặc có test. Người làm tự kiểm tra gói của mình, người khác kiểm tra lại luồng chính; người kiểm tra và thời điểm được ghi trong hồ sơ.

Các chỉ tiêu đầy đủ về tải, trình duyệt, media, backup/restore và phát hành công khai nằm trong [v1](v1.md#acceptance). Bản MVP phải kiểm chứng các điều kiện của scope đã chọn; không dùng nhãn thử nghiệm để bỏ kiểm tra xác thực, quyền truy cập hoặc lưu dữ liệu của luồng đó. Bổ sung chỉ tiêu vào MVP cần ghi phạm vi, cách đo và công sức tương ứng.

## Cách bắt đầu một gói

1. Chọn gói và danh sách UC/AC; đối chiếu đúng mục đặc tả nguồn.
2. Chốt phụ thuộc trực tiếp, người làm/rà soát, đầu ra và cách thử; ghi riêng nội dung chưa quyết định.
3. Triển khai một luồng chạy thật, thử trên các tài khoản/dataset đã chọn và tích hợp ngay.
4. Ghi kết quả và lỗi; mở gói kế tiếp hoặc đưa phần hoàn thiện còn lại vào kế hoạch v1.

Nếu phát hiện một quy tắc v1 cần đổi để thực hiện MVP, ghi quyết định và cập nhật đặc tả/hợp đồng bị ảnh hưởng. Không tạo một bộ quy tắc mới nằm riêng trong task hoặc bản sao tài liệu MVP.
