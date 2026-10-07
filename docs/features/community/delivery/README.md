# SCDC — Các gói triển khai Community

Phạm vi các gói và quy trình thực hiện. Trạng thái hiện tại được quản lý riêng tại [status.md](../status.md).

<a id="use-case-delivery"></a>

## Kế hoạch và gói triển khai đầu tiên

Áp dụng [quy trình dự án](../../../project.md#process) cho từng nhóm UC: rà soát luồng/ngoại lệ và AC/TC → đối chiếu UX, API, dữ liệu/giao dịch → xác định phụ thuộc và gói việc → triển khai cùng kiểm thử → tích hợp và ghi bằng chứng. Các bước được lặp theo nhóm chức năng; không đợi hoàn tất mọi module mới kiểm thử luồng đầu tiên.

Trước mỗi bước, trình bày phạm vi, đầu ra và nội dung cần quyết định để người dùng duyệt. Sau mỗi bước, báo những gì đã làm, kết quả kiểm tra và phần còn thiếu. Việc duyệt một bước chỉ áp dụng cho phạm vi bước đó.

Theo thỏa thuận ngày 2026-10-07, các phần đã hoàn thành và kiểm tra được commit/push theo tiến độ: tài liệu độc lập trên `main`, code trên nhánh theo gói chức năng (gói đầu là `feat/community-create-view`). Quyền commit/push không thay thế việc duyệt bước tiếp theo hoặc duyệt merge nhánh code.

Các nhóm dưới đây là lộ trình tổng thể; nhóm 1 được bắt đầu bằng [gói tạo/xem cộng đồng](create-view/plan.md#first-package), rồi bổ sung tìm kiếm, tham gia và chỉnh sửa theo các gói tiếp theo.

| Nhóm | Use case | Đầu vào kỹ thuật cần có | Đầu ra cần kiểm chứng |
|---|---|---|---|
| 1. Server và tư cách | [UC-COM-01](../specs/servers.md#uc-com-01), [UC-COM-02](../specs/servers.md#uc-com-02), [UC-COM-03](../specs/servers.md#uc-com-03), [UC-COM-04](../specs/servers.md#uc-com-04), [UC-COM-06](../specs/memberships.md#uc-com-06) | Account guard, shared transaction, migration server/membership/@everyone/version/operation và search | Tạo nguyên tử, join trực tiếp, list/detail đúng quyền và tên Unicode; private switch và đóng pending của [UC-COM-04](../specs/servers.md#uc-com-04), [UC-COM-06](../specs/memberships.md#uc-com-06) hoàn thiện cùng request ở nhóm 3 |
| 2. Vai trò và phòng | [UC-COM-16](../specs/channels.md#uc-com-16), [UC-COM-17](../specs/channels.md#uc-com-17), [UC-COM-18](../specs/channels.md#uc-com-18), [UC-COM-19](../specs/channels.md#uc-com-19), [UC-COM-20](../specs/permissions.md#uc-com-20), [UC-COM-21](../specs/permissions.md#uc-com-21), [UC-COM-22](../specs/permissions.md#uc-com-22) | Permission evaluator/catalog, migration role/ACL/epoch, channel guard và Messaging lifecycle | Role limit, assignment/ACL nguyên tử, hidden channel, create/delete và race quyền; voice cần Media lifecycle riêng |
| 3. Tham gia và lời mời | [UC-COM-05](../specs/servers.md#uc-com-05), [UC-COM-06](../specs/memberships.md#uc-com-06), [UC-COM-07](../specs/memberships.md#uc-com-07), [UC-COM-08](../specs/memberships.md#uc-com-08), [UC-COM-09](../specs/invitations.md#uc-com-09), [UC-COM-10](../specs/invitations.md#uc-com-10), [UC-COM-11](../specs/invitations.md#uc-com-11), [UC-COM-12](../specs/invitations.md#uc-com-12), [UC-COM-13](../specs/invitations.md#uc-com-13), [UC-COM-14](../specs/servers.md#uc-com-14), [UC-COM-15](../specs/memberships.md#uc-com-15) | Role/quyền từ nhóm 2, request/invitation migration, token protection/key ring, guard actor/target | Pending/approve/cancel/private switch, lượt cuối, accept/expiry, transfer/leave/rejoin và retry |
| 4. Tin phòng và cập nhật | [UC-COM-17](../specs/channels.md#uc-com-17), [UC-COM-23](../specs/integration.md#uc-com-23), [UC-COM-24](../specs/integration.md#uc-com-24), [UC-COM-25](../specs/integration.md#uc-com-25) | Messaging writer/history dùng chung DM, outbox/Hub/registry/revoker và frontend API | Lưu bền/chống trùng/tác giả, reconnect/routing, thu hồi chat ≤5 giây, scope cache đúng epoch |

Các nhóm gồm API và trạng thái frontend tương ứng, có kiểm thử quyền/đồng thời ngay trong gói. Các guard/lifecycle/shared transaction và migrations được triển khai theo phạm vi từng gói; scope room deleted/restore/media và các đầu vào chưa chốt tiếp tục được theo dõi ở [vấn đề còn mở](../status.md#remaining), không đánh dấu đã nghiệm thu từ danh mục UC.


## Các bước và đầu ra đã thống nhất

Trạng thái các bước nằm tại [status.md](../status.md#steps); bảng này chỉ giữ đầu ra và thứ tự thực hiện.

| Bước | Đầu ra cần bàn giao |
|---|---|
| 1. Chốt phạm vi và thứ tự | Gói đầu UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03; tiêu chí hoàn thành, phần để sau và thứ tự phụ thuộc |
| 2. Rà soát nghiệp vụ gói đầu | [Kết quả rà soát](create-view/plan.md#first-package-business): điều kiện tạo, trạng thái ban đầu, ownership/membership/@everyone, dữ liệu hợp lệ, luồng lỗi và ngoại lệ; ghi riêng đầu vào kỹ thuật còn cần chốt |
| 3. Chốt thiết kế kỹ thuật | [Thiết kế gói tạo/xem](../design/create-view.md): model/schema/migration, API/DTO/lỗi, transaction/retry, hợp đồng Identity và kế hoạch kiểm thử |
| 4. Triển khai backend | Persistence, application, DI/API và kiểm thử quyền/tạo nguyên tử/thử lại/đọc dữ liệu |
| 5. Giao diện và nghiệm thu gói đầu | Nối form/danh sách/detail với API; chạy luồng thật và ghi bằng chứng theo tiêu chí gói |
| 6. Mở rộng Community theo gói | Tiếp theo UC-COM-06 tham gia trực tiếp; bổ sung search/quản lý, role/quyền, phòng và các đường tham gia/rời/lời mời theo phụ thuộc. Phạm vi từng gói được duyệt riêng. |
| 7. Tích hợp Messaging và realtime | Messaging lưu/đọc/gửi tin phòng trên quyền Community; sau đó kiểm chứng Hub, reconnect và xử lý mất quyền |

Gói đầu của bước 6: [tham gia trực tiếp public/immediate](direct-join/plan.md), kế thừa gói tạo/xem trên `feat/community-join`; requests/search và các gói còn lại được duyệt riêng.

Evaluator Permissions được triển khai ở mức cần cho gói hiện hành và đối chiếu fixture khi mở role/ACL. Outbox/revoker cho mutation thu hồi phải hoàn thiện trước khi gói đó được coi đạt; tin phòng/realtime/Media có bằng chứng riêng theo phụ thuộc.
