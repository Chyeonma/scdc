# SCDC — Đặc tả Community

Chọn thành phần chủ trì để đọc quy tắc, use case, UX và AC/TC. Danh mục dưới đây giữ hành trình; các ma trận COM/UC/API/AC/TC ở [truy vết](traceability.md). Implementation và bằng chứng theo gói tại [status](../status.md).

Trang tra cứu UC/COM/AC/TC; các bảng dẫn tới nguồn định nghĩa chi tiết, không ghi kết quả triển khai.

<a id="use-cases"></a>

## Use case theo hành trình cộng đồng và module phụ trách

Bảng tổng hợp hành vi mục tiêu; quy tắc chi tiết ở từng đặc tả. Tiến độ tại [status.md](../status.md); thiết kế tại [tích hợp](../design/integration.md#contracts).

Community thực hiện quản lý server, membership, phòng, lời mời và quyền. [UC-COM-17](channels.md#uc-com-17) đọc lịch sử qua Messaging; [UC-COM-23](integration.md#uc-com-23), [UC-COM-24](integration.md#uc-com-24) do Messaging thực hiện trên quyền Community; [UC-COM-25](integration.md#uc-com-25) phối hợp Identity/Community/Messaging và WebClient. Chức năng vào phòng thoại/gọi/video thuộc [đặc tả media](../../voice-video/README.md), không được coi đã triển khai khi tạo được metadata phòng voice.

[Điều kiện và ngoại lệ dùng chung](integration.md#use-case-conditions) áp dụng cho toàn bộ UC; nội dung UC nằm ở thành phần chủ trì dưới đây.

Module phụ trách xác định phần triển khai nghiệp vụ, độc lập với vị trí lưu tài liệu và mã UC. Identity kiểm tra tài khoản/phiên, WebClient thực hiện giao diện; các phối hợp bổ sung được ghi tại từng UC. UC-COM-17 tách trách nhiệm metadata/phòng của Community và lịch sử tin của Messaging; UC-COM-25 là luồng tích hợp, Messaging phụ trách Hub/dispatcher còn mỗi module giữ điều kiện và dữ liệu mình sở hữu.

| Use case | Mục tiêu | Module phụ trách | Màn hình |
|---|---|---|---|
| <a id="uc-com-01"></a> [UC-COM-01](servers.md#uc-com-01) | Tạo cộng đồng | Community / Servers | COM-S10 |
| <a id="uc-com-02"></a> [UC-COM-02](servers.md#uc-com-02) | Tìm và xem cộng đồng công khai | Community / Servers | COM-S01/02 |
| <a id="uc-com-03"></a> [UC-COM-03](servers.md#uc-com-03) | Xem cộng đồng đang tham gia và tư cách của mình | Community / Servers | COM-S03, danh sách cộng đồng |
| <a id="uc-com-04"></a> [UC-COM-04](servers.md#uc-com-04) | Sửa thông tin và visibility cộng đồng | Community / Servers | COM-S10 |
| <a id="uc-com-05"></a> [UC-COM-05](servers.md#uc-com-05) | Đổi chế độ tham gia | Community / Servers | COM-S07 |
| <a id="uc-com-06"></a> [UC-COM-06](memberships.md#uc-com-06) | Tham gia cộng đồng công khai vào ngay | Community / Memberships | COM-S02 |
| <a id="uc-com-07"></a> [UC-COM-07](memberships.md#uc-com-07) | Gửi, xem và hủy yêu cầu tham gia | Community / Memberships | COM-S02/06 |
| <a id="uc-com-08"></a> [UC-COM-08](memberships.md#uc-com-08) | Duyệt hoặc từ chối yêu cầu tham gia | Community / Memberships | COM-S06 |
| <a id="uc-com-09"></a> [UC-COM-09](invitations.md#uc-com-09) | Tạo, xem và sao chép link mời | Community / Invitations | COM-S05 |
| <a id="uc-com-10"></a> [UC-COM-10](invitations.md#uc-com-10) | Thu hồi link mời | Community / Invitations | COM-S05 |
| <a id="uc-com-11"></a> [UC-COM-11](invitations.md#uc-com-11) | Xem trước và tham gia bằng link mời | Community / Invitations | COM-S02 |
| <a id="uc-com-12"></a> [UC-COM-12](invitations.md#uc-com-12) | Gửi, xem và hủy lời mời đích danh | Community / Invitations | COM-S11, quản lý lời mời |
| <a id="uc-com-13"></a> [UC-COM-13](invitations.md#uc-com-13) | Xem, chấp nhận hoặc từ chối lời mời đích danh | Community / Invitations | COM-S11, inbox người nhận |
| <a id="uc-com-14"></a> [UC-COM-14](servers.md#uc-com-14) | Chuyển chủ sở hữu | Community / Servers | COM-S12 |
| <a id="uc-com-15"></a> [UC-COM-15](memberships.md#uc-com-15) | Rời cộng đồng | Community / Memberships | COM-S03, menu cộng đồng |
| <a id="uc-com-16"></a> [UC-COM-16](channels.md#uc-com-16) | Tạo phòng | Community / Channels | COM-S04 |
| <a id="uc-com-17"></a> [UC-COM-17](channels.md#uc-com-17) | Xem phòng được phép và lịch sử tin văn bản | Community (phòng); Messaging (lịch sử) | COM-S03 |
| <a id="uc-com-18"></a> [UC-COM-18](channels.md#uc-com-18) | Sửa thông tin phòng | Community / Channels | Quản lý phòng từ COM-S03 |
| <a id="uc-com-19"></a> [UC-COM-19](channels.md#uc-com-19) | Xóa phòng | Community / Channels | Quản lý phòng từ COM-S03 |
| <a id="uc-com-20"></a> [UC-COM-20](permissions.md#uc-com-20) | Tạo, sửa và xóa vai trò tự tạo | Community / Permissions | COM-S08 |
| <a id="uc-com-21"></a> [UC-COM-21](permissions.md#uc-com-21) | Gán hoặc thu hồi vai trò thành viên | Community / Permissions | COM-S08 |
| <a id="uc-com-22"></a> [UC-COM-22](permissions.md#uc-com-22) | Xem và thay cấu hình quyền xem phòng | Community / Permissions | COM-S09 |
| <a id="uc-com-23"></a> [UC-COM-23](integration.md#uc-com-23) | Gửi và chủ động thử lại tin văn bản | Messaging | COM-S03, composer |
| <a id="uc-com-24"></a> [UC-COM-24](integration.md#uc-com-24) | Sửa hoặc xóa tin của mình | Messaging | COM-S03, menu tin |
| <a id="uc-com-25"></a> [UC-COM-25](integration.md#uc-com-25) | Nhận cập nhật, kết nối lại và xử lý mất quyền | Tích hợp liên module; Messaging (Hub/dispatcher) | COM-S03, Hub chat và thông báo theo người nhận |
