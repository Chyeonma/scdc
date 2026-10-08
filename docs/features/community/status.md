# SCDC — Trạng thái Community

Cập nhật: 2026-10-08. Nguồn duy nhất ghi trạng thái triển khai hiện tại của Community. Quy tắc và use case ở [specs/](specs/README.md); thiết kế ở [design/](design/README.md); bằng chứng lịch sử ở từng hồ sơ nghiệm thu và [bảng đối chiếu test](delivery/verification.md).

Mốc Community MVP đã chọn theo [DEC-118](../../decisions.md#dec-118): tạo/tìm/tham gia, phòng text, lịch sử/gửi/nhận tin và xử lý mất quyền. Điều kiện hoàn tất nằm tại [hồ sơ MVP](../../releases/mvp.md#community-scope). Nền cộng đồng/phòng/quyền đã được hợp nhất vào `main` từ `feat/community-channels`; luồng giao tiếp trong phòng chưa được tích hợp trên `main`, nên chưa đạt mốc này. Không tính tỷ lệ hoàn thành từ số UC hoặc số test vì phạm vi các gói khác nhau.

<a id="implementation"></a>

## Phần đã có

| Phạm vi | Trạng thái | Code và bằng chứng |
|---|---|---|
| Tạo cộng đồng — UC-COM-01 | Backend và WebClient đã được kiểm chứng cho gói đã chọn | `feat/community-create-view`; backend `79fa627`, frontend `4203e05`; [nghiệm thu](delivery/create-view/acceptance.md) |
| List/detail/tư cách — phần UC-COM-03 | Đã kiểm chứng phần danh sách/detail/own membership; chưa đánh dấu toàn bộ UC-COM-03 đạt | Cùng gói tạo/xem; [phạm vi](delivery/create-view/plan.md), [nghiệm thu](delivery/create-view/acceptance.md) |
| Tìm kiếm và public summary — UC-COM-02 | API tìm kiếm/discovery đã kiểm chứng; nối preview → join trực tiếp; đường gửi request approval còn UC-COM-07 | `feat/community-search`; [thiết kế](design/search.md), [nghiệm thu](delivery/search/acceptance.md) |
| Tham gia trực tiếp — phần public/immediate UC-COM-06 | API và WebClient đã kiểm chứng; join lặp/đồng thời và rejoin fixture đạt; metadata phòng theo quyền đã có ở gói phòng, lịch sử/gửi tin còn thiếu | `feat/community-join`, backend `e5b5750`, frontend `e0cf116`; [nghiệm thu](delivery/direct-join/acceptance.md) |
| Leave, requests, invitations, sửa metadata server/chuyển owner | Chưa triển khai các gói chức năng; sửa metadata phòng đã có ở UC-COM-18 | [Bảng việc còn lại](#work-items) |
| Role và assignment — phần UC-COM-20/21 | Backend/WebClient, policy view HTTP và ảnh hưởng ACL version đã kiểm chứng; thu hồi realtime/tin phòng còn thiếu | `feat/community-permissions`; backend `9a513d1`, WebClient `eae64c3`; [nghiệm thu](delivery/roles/acceptance.md), [kiểm chứng phòng/ACL](delivery/channels-access/acceptance.md) |
| Phòng text — phần UC-COM-16/17, metadata UC-COM-18 | Backend/WebClient create/list/detail/edit đã kiểm chứng; chưa có voice, lịch sử hoặc delete | `feat/community-channels`; backend `eaaa2e3`, WebClient `f8d582d`; [nghiệm thu](delivery/channels-access/acceptance.md) |
| ACL phòng — phần HTTP UC-COM-22 | API/UI snapshot, policy view và guard transaction đã kiểm chứng; subscription/thu hồi realtime còn thiếu | Cùng gói phòng/ACL; [thiết kế](design/channels-access.md), [nghiệm thu](delivery/channels-access/acceptance.md) |
| Tin phòng, Hub/dispatcher/realtime | Chưa có runtime của gói tích hợp | [Use case tích hợp](specs/integration.md#use-cases) |

Code Community trên `main` đã bao gồm chuỗi gói `feat/community-create-view` → `feat/community-join` → `feat/community-search` → `feat/community-permissions` → `feat/community-channels` (nguồn `2052c60`), gồm backend/WebClient, guard/lifecycle tạo space, migration 001–004 và bộ kiểm thử. Các nhánh feature giữ lịch sử từng gói; tài liệu độc lập tiếp tục được quản lý trên `main`. Lần hợp nhất này chưa đưa các nhánh Messaging vào `main`; lịch sử/gửi/nhận tin và thu hồi chat còn cần tích hợp với người phụ trách Messaging. Người dùng tự push; trạng thái remote cần kiểm tra khi bàn giao, không suy từ báo cáo kiểm thử. Bằng chứng mới của lần hợp nhất ghi tại [kiểm chứng main](delivery/verification.md#main-merge).

<a id="steps"></a>

## Các bước đã thống nhất

| Bước | Trạng thái | Nguồn |
|---|---|---|
| 1. Chốt phạm vi | Đã duyệt gói tạo/xem | [Kế hoạch](delivery/create-view/plan.md#first-package) |
| 2. Rà soát nghiệp vụ | Đã rà soát theo quyết định hiện có | [Rà soát](delivery/create-view/plan.md#first-package-business) |
| 3. Chốt thiết kế | Đã chốt thiết kế gói ngày 2026-10-07 | [Thiết kế](design/create-view.md) |
| 4. Backend | Đã triển khai và ghi bằng chứng | [Nghiệm thu backend](delivery/create-view/acceptance.md#backend) |
| 5. WebClient và kiểm chứng luồng | Đã triển khai và ghi bằng chứng | [Nghiệm thu frontend](delivery/create-view/acceptance.md#frontend) |
| Tổ chức lại tài liệu trước bước 6 | Đã áp dụng bố cục mới cho Community | [Tổng quan](README.md), [quy ước quản lý](README.md#quy-ước-duy-trì) |
| 6. Mở rộng Community theo gói | Join public/immediate, search/discovery, role/assignment và phòng text/ACL đã kiểm chứng trong phạm vi từng gói; các gói còn lại chưa bắt đầu | [Gói join](delivery/direct-join/acceptance.md), [tìm kiếm](delivery/search/acceptance.md), [vai trò](delivery/roles/acceptance.md), [phòng/ACL](delivery/channels-access/acceptance.md) |
| 7. Messaging và realtime | Chưa bắt đầu | [Tích hợp](specs/integration.md#use-cases) |

<a id="remaining"></a>

## Việc còn lại

<a id="work-items"></a>

### Công việc để đạt Community MVP

Các hàng dưới đây là gói đề xuất để lập kế hoạch, chưa được duyệt bắt đầu code. Trước mỗi gói cần tham khảo người dùng về scope, thiết kế và cách kiểm chứng theo [quy trình bàn giao](delivery/README.md#use-case-delivery). Đầu mối là phân công kế hoạch theo [DEC-117](../../decisions.md#dec-117), chưa phải xác nhận nhận việc hoặc cam kết lịch. `COM-Wxx` chỉ định danh công việc, không thay mã UC/AC/TC.

| Mã / ưu tiên | UC / phạm vi | Phụ thuộc trực tiếp | Đầu mối kế hoạch | Đầu ra và điều kiện hoàn tất | Trạng thái |
|---|---|---|---|---|---|
| COM-W01 / MVP-1 | UC-COM-17, nền tin dùng chung | Identity guard, channel guard và lifecycle create đã có; hợp đồng [Messaging](../../shared/messaging/README.md) | Sáng; Vg rà soát quyền | Migration/model tin, thứ tự per-space, cursor và reader lịch sử có guard; thử phân trang, room ẩn/cross-server, session sai và epoch cũ trên DB thật | Đề xuất; chưa bắt đầu |
| COM-W02 / MVP-1 | UC-COM-23 gửi/retry | COM-W01; [transaction/lease](design/integration.md#transactions) | Sáng; Vg rà soát Community | Writer dùng guard tới commit; operation chống trùng, tin/sequence/outbox nguyên tử; thử retry/đồng thời/restart và quyền thay đổi tranh commit | Đề xuất; chưa bắt đầu |
| COM-W03 / MVP-2 | UC-COM-17/23 giao diện lịch sử/gửi | COM-W01/02 | Vg phối hợp Sáng | Mở phòng → lịch sử → gửi → reload với API thật; trạng thái lưu/thất bại rõ; retry do người dùng, đổi tài khoản/mất quyền không phục hồi dữ liệu riêng | Đề xuất; chưa bắt đầu |
| COM-W04 / MVP-2 | UC-COM-25 nhận tin | COM-W02; hợp đồng realtime/dispatcher của Messaging | Sáng; Thái hỗ trợ bộ chạy | Outbox dispatcher, Hub và subscription có admission; hai tài khoản nhận tin đã commit, dedup và đối soát sau reconnect bằng sequence/cursor; ghi proof mất kết nối | Đề xuất; chưa bắt đầu |
| COM-W05 / MVP-2 | UC-COM-25 mất quyền; tích hợp UC-COM-20/21/22 | COM-W04; mutation role/ACL và Identity đã có | Sáng + Vg; Thái hỗ trợ đo | HTTP/gửi/subscribe/resume dùng quyền hiện hành; đo kết nối đang mở ngừng truy cập trong ≤5 giây từ commit theo DEC-083, gồm session/account và role/ACL; có race test và fail-close | Đề xuất; chưa bắt đầu |
| COM-W06 / MVP-3 | UC-COM-03/06/16/17/23/25 toàn luồng | COM-W03/04/05 | Vg + Sáng; Thái bộ chạy | Tạo → tìm → public/immediate join → phòng text → lịch sử/gửi/nhận → mất quyền trên DB/API/Hub thật; hồi quy create/join/search/role/ACL/edit; ghi commit, môi trường, actual result và phần chưa đạt | Đề xuất; chưa bắt đầu |
| COM-W07 / MVP-3 | Bàn giao Community trong mốc MVP | COM-W06; tích hợp Identity/DM, bộ chạy MVP-SYS | Vg + Sáng + Thái; người kiểm tra ghi trong hồ sơ | Checkout chạy lại theo [hướng dẫn local](development.md), cấu hình/key/ledger rõ, hồ sơ nghiệm thu đối chiếu [gate Community](../../releases/mvp.md#community-acceptance); merge/phát hành cần quyết định riêng | Đề xuất; chưa bắt đầu |

COM-W01 là đề xuất gói tiếp theo theo phạm vi MVP đã chọn; cần thống nhất scope với nền Messaging/DM trước khi thực hiện. COM-W03 và COM-W04 có thể thực hiện song song sau khi hợp đồng COM-W02 ổn định. Kiểm chứng COM-W05 chạy từ khi có Hub, rồi lặp trong toàn luồng COM-W06; guard transaction hiện có chưa chứng minh deadline thu hồi realtime.

### Công việc mở rộng sau phạm vi MVP đã chọn

| Mã | UC / đầu ra | Phụ thuộc và điều kiện hoàn tất | Đầu mối kế hoạch | Trạng thái |
|---|---|---|---|---|
| COM-W08 | UC-COM-19 xóa phòng | Lifecycle delete channel/space cùng transaction, operation/outbox và chặn HTTP/Hub; proof tranh gửi/xóa và retention theo [Channels](specs/channels.md) | Vg + Sáng | Chưa bắt đầu |
| COM-W09 | UC-COM-04/05/07/08 metadata server, join mode và approval | Server/pending writer nguyên tử, private switch kết thúc request và quyền reviewer; proof CAS/approve/cancel/join đồng thời | Vg | Chưa bắt đầu |
| COM-W10 | UC-COM-09–13 lời mời | Token/lượt dùng/issuer lifetime, mời đích danh và membership epoch; proof consume lượt cuối, thu hồi/accept đồng thời | Vg | Chưa bắt đầu |
| COM-W11 | UC-COM-14/15 chuyển owner và rời | Ownership/membership/epoch/ACL cleanup nguyên tử; owner không tự rời, stale epoch và thu hồi HTTP/realtime; rejoin fixture đã có chưa thay proof leave API | Vg + Sáng | Chưa bắt đầu |
| COM-W12 | UC-COM-24 sửa/xóa tin | Messaging writer, quyền tác giả và placeholder/version/outbox; proof CAS/tranh mutation/quyền hiện hành | Sáng; Vg tích hợp UI | Chưa bắt đầu |
| COM-W13 | Phần voice của UC-COM-16/17/25 và Media | Module/lifecycle Media, admission, provider và cutoff riêng; [đặc tả media](../voice-video/README.md) | Theo gói v1 tại DEC-117 | Chưa bắt đầu |

UC-COM-02 còn đường gửi request approval và UC-COM-06 còn nhánh approval/pending trong phạm vi đầy đủ. UC-COM-03 còn đối soát theo leave/rejoin API. Các phần này vẫn giữ trong đặc tả v1, không dùng kết quả search/join hiện tại để đóng toàn bộ UC.

Các OQ/ACL-O giữ tại nguồn chủ trì: [Servers](specs/servers.md#gaps), [Memberships](specs/memberships.md#gaps), [Invitations](specs/invitations.md#gaps), [Channels](specs/channels.md#gaps), [Permissions](specs/permissions.md#gaps), [tích hợp](specs/integration.md#gaps). Thu hồi quyền, restore và Media giữ proof riêng theo [thiết kế tích hợp](design/integration.md) và [vòng đời dữ liệu](../../data-lifecycle.md).

Sau mỗi gói, cập nhật trang này và ghi hồ sơ nghiệm thu gắn commit. Các trang khác dẫn tới trang này thay vì duy trì thêm bảng tiến độ Community.
