# SCDC — Trạng thái Community

Cập nhật: 2026-10-07. Nguồn duy nhất ghi trạng thái triển khai hiện tại của Community. Quy tắc và use case ở [specs/](specs/README.md); thiết kế ở [design/](design/README.md); bằng chứng lịch sử ở từng hồ sơ nghiệm thu.

<a id="implementation"></a>

## Phần đã có

| Phạm vi | Trạng thái | Code và bằng chứng |
|---|---|---|
| Tạo cộng đồng — UC-COM-01 | Backend và WebClient đã được kiểm chứng cho gói đã chọn | `feat/community-create-view`; backend `79fa627`, frontend `4203e05`; [nghiệm thu](delivery/create-view/acceptance.md) |
| List/detail/tư cách — phần UC-COM-03 | Đã kiểm chứng phần danh sách/detail/own membership; chưa đánh dấu toàn bộ UC-COM-03 đạt | Cùng gói tạo/xem; [phạm vi](delivery/create-view/plan.md), [nghiệm thu](delivery/create-view/acceptance.md) |
| Public summary — phần route của UC-COM-02 | Projection dùng chung đã kiểm chứng; search/discovery chưa triển khai | [Phạm vi route](design/create-view.md#api) và [bằng chứng backend](delivery/create-view/acceptance.md#backend) |
| Join/leave, requests, invitations, metadata management | Chưa triển khai các gói chức năng | [Lộ trình theo phụ thuộc](delivery/README.md#use-case-delivery) |
| Quản lý role/ACL và phòng | Chưa triển khai gói quản lý; gói tạo chỉ có owner/@everyone và quyền đọc cần thiết | [Permissions](specs/permissions.md), [Channels](specs/channels.md) |
| Tin phòng, Hub/dispatcher/realtime | Chưa có runtime của gói tích hợp | [Use case tích hợp](specs/integration.md#use-cases) |

Code Community trên `main` vẫn ở nền module. Implementation gói tạo/xem nằm trên `feat/community-create-view`, chưa merge code vào `main`. Tài liệu độc lập được commit trên `main`, rồi đồng bộ sang feature. Người dùng đã chọn tự push hai nhánh; trạng thái remote phải được kiểm tra khi bàn giao, không suy từ báo cáo kiểm thử.

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
| 6. Mở rộng Community theo gói | Đã duyệt bắt đầu gói public/immediate UC-COM-06; đang triển khai, chưa nghiệm thu | [Kế hoạch](delivery/direct-join/plan.md), [thiết kế](design/direct-join.md) |
| 7. Messaging và realtime | Chưa bắt đầu | [Tích hợp](specs/integration.md#use-cases) |

<a id="remaining"></a>

## Việc còn lại

- Gói tiếp theo cần duyệt scope, ngoại lệ và kiểm chứng trước khi triển khai; không tự mở rộng phạm vi từ việc đã đạt gói tạo/xem.
- UC-COM-03 còn phòng/lịch sử và đối soát theo mutation leave/rejoin; UC-COM-02 còn search/discovery. Không dùng kết quả gói đầu để đóng toàn bộ hai UC.
- Các OQ/ACL-O giữ tại nguồn chủ trì: [Servers](specs/servers.md#gaps), [Memberships](specs/memberships.md#gaps), [Invitations](specs/invitations.md#gaps), [Channels](specs/channels.md#gaps), [Permissions](specs/permissions.md#gaps), [tích hợp](specs/integration.md#gaps). Giữ nguyên quyết định và yêu cầu proof.
- Những phần thu hồi quyền, Messaging lifecycle, restore và Media có phụ thuộc/bằng chứng riêng theo [thiết kế tích hợp](design/integration.md) và [vòng đời dữ liệu](../../data-lifecycle.md).

Sau mỗi gói, cập nhật trang này và ghi hồ sơ nghiệm thu gắn commit. Các trang khác dẫn tới trang này thay vì duy trì thêm bảng tiến độ Community.
