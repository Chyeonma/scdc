# SCDC — Trạng thái Community

Cập nhật: 2026-10-07. Nguồn duy nhất ghi trạng thái triển khai hiện tại của Community. Quy tắc và use case ở [specs/](specs/README.md); thiết kế ở [design/](design/README.md); bằng chứng lịch sử ở từng hồ sơ nghiệm thu.

<a id="implementation"></a>

## Phần đã có

| Phạm vi | Trạng thái | Code và bằng chứng |
|---|---|---|
| Tạo cộng đồng — UC-COM-01 | Backend và WebClient đã được kiểm chứng cho gói đã chọn | `feat/community-create-view`; backend `79fa627`, frontend `4203e05`; [nghiệm thu](delivery/create-view/acceptance.md) |
| List/detail/tư cách — phần UC-COM-03 | Đã kiểm chứng phần danh sách/detail/own membership; chưa đánh dấu toàn bộ UC-COM-03 đạt | Cùng gói tạo/xem; [phạm vi](delivery/create-view/plan.md), [nghiệm thu](delivery/create-view/acceptance.md) |
| Tìm kiếm và public summary — UC-COM-02 | API tìm kiếm/discovery đã kiểm chứng; nối preview → join trực tiếp; đường gửi request approval còn UC-COM-07 | `feat/community-search`; [thiết kế](design/search.md), [nghiệm thu](delivery/search/acceptance.md) |
| Tham gia trực tiếp — phần public/immediate UC-COM-06 | API và WebClient đã kiểm chứng; join lặp/đồng thời và rejoin fixture đạt; requests/phòng còn phụ thuộc | `feat/community-join`, backend `e5b5750`, frontend `e0cf116`; [nghiệm thu](delivery/direct-join/acceptance.md) |
| Leave, requests, invitations, metadata management | Chưa triển khai các gói chức năng | [Lộ trình theo phụ thuộc](delivery/README.md#use-case-delivery) |
| Quản lý role/ACL và phòng | Chưa triển khai gói quản lý; gói tạo chỉ có owner/@everyone và quyền đọc cần thiết | [Permissions](specs/permissions.md), [Channels](specs/channels.md) |
| Tin phòng, Hub/dispatcher/realtime | Chưa có runtime của gói tích hợp | [Use case tích hợp](specs/integration.md#use-cases) |

Code Community trên `main` vẫn ở nền module. Gói tạo/xem nằm trên `feat/community-create-view`; `feat/community-join` kế thừa nhánh này; `feat/community-search` kế thừa join. Chưa merge code vào `main`. Tài liệu độc lập được commit trên `main`, rồi đồng bộ sang feature. Người dùng tự push nhánh tài liệu/code; trạng thái remote cần kiểm tra khi bàn giao, không suy từ báo cáo kiểm thử.

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
| 6. Mở rộng Community theo gói | Join public/immediate và search/discovery đã kiểm chứng; các gói mở rộng còn lại chưa bắt đầu | [Gói join](delivery/direct-join/acceptance.md), [gói tìm kiếm](delivery/search/acceptance.md) |
| 7. Messaging và realtime | Chưa bắt đầu | [Tích hợp](specs/integration.md#use-cases) |

<a id="remaining"></a>

## Việc còn lại

- Gói search/discovery UC-COM-02 đã có [bằng chứng](delivery/search/acceptance.md) trên `feat/community-search`. Đề xuất gói tiếp theo là vai trò/quyền UC-COM-20/21, làm nền cho phòng và duyệt tham gia; chưa bắt đầu, cần người dùng duyệt phạm vi riêng.
- UC-COM-03 còn phòng/lịch sử và đối soát theo mutation leave/rejoin; UC-COM-02 còn đường gửi request approval của UC-COM-07. Không dùng kết quả search/join để đóng các phụ thuộc còn lại.
- UC-COM-06 còn đường chuyển approval/pending và mở phòng; join hiện tại trả conflict với approval. Rejoin chỉ kiểm chứng bằng record left fixture; chưa có leave API, cleanup không thay proof FK/CAS epoch của gói role/ACL.
- Các OQ/ACL-O giữ tại nguồn chủ trì: [Servers](specs/servers.md#gaps), [Memberships](specs/memberships.md#gaps), [Invitations](specs/invitations.md#gaps), [Channels](specs/channels.md#gaps), [Permissions](specs/permissions.md#gaps), [tích hợp](specs/integration.md#gaps). Giữ nguyên quyết định và yêu cầu proof.
- Những phần thu hồi quyền, Messaging lifecycle, restore và Media có phụ thuộc/bằng chứng riêng theo [thiết kế tích hợp](design/integration.md) và [vòng đời dữ liệu](../../data-lifecycle.md).

Sau mỗi gói, cập nhật trang này và ghi hồ sơ nghiệm thu gắn commit. Các trang khác dẫn tới trang này thay vì duy trì thêm bảng tiến độ Community.
