# Community

Community sở hữu cộng đồng, membership, lời mời, metadata/vòng đời phòng và quyền. Messaging sở hữu tin phòng, lịch sử và Hub chat; Identity cung cấp trạng thái tài khoản/phiên. Vị trí tài liệu và prefix route không đổi ranh giới module.

Phạm vi: REQ-002/004, SCP-003/004 và phần tin phòng của SCP-005. Mã UC-COM truy vết hành trình người dùng; module thực hiện được ghi trong từng use case.

## Mục lục

| Chủ đề | Nội dung |
|---|---|
| [Cộng đồng và chủ sở hữu](servers.md) | Tạo, discovery, metadata/visibility/join mode, ownership và phần đọc tư cách |
| [Thành viên](memberships.md) | Join/request/approval/leave/rejoin, membership epoch |
| [Lời mời](invitations.md) | Link, token/lượt/hạn, mời đích danh và transition |
| [Phòng](channels.md) | Create/list/detail/edit/delete, kind và lifecycle phối hợp Messaging/Media |
| [Vai trò và quyền truy cập](access-control.md) | Role/assignment, policy view, ACL snapshot, quyền quản lý và guard |
| [Tin phòng](../messaging/channel-messaging.md) | History/send/edit/delete, realtime, reconnect và mất quyền; Messaging thực hiện |

Mỗi chủ đề giữ quy tắc, use case, UX, thiết kế dữ liệu/API, tiêu chí kiểm chứng và trạng thái theo khả năng. Bảng trạng thái phân biệt phần hiện có, mục tiêu và phần chưa chứng minh; không tính tiến độ từ số UC/test.

| Tài liệu liên quan | Nội dung |
|---|---|
| [Cấu trúc và cơ chế Community](../../system/community.md) | Tổ chức backend, điều kiện chung, transaction/guard, operation, version/epoch, cursor và migration |
| [Hướng dẫn Community](../../guides/community-development.md) | Cấu hình, local database/API/UI, runner và kiểm thử |
| [Truy vết mã](traceability.md) | Liên kết COM/UC/API/AC/TC đến nguồn định nghĩa |
| [Hồ sơ kiểm chứng](../../records/verification/community/README.md) | Bằng chứng theo commit, main-merge và phần chưa chứng minh |
| [Phạm vi MVP](../../releases/mvp.md#community-scope) | Khả năng được chọn và gate hoàn tất Community |
| [Kế hoạch công việc](../../project/planning.md#community-work-items) | COM-W01–13, ưu tiên/phụ thuộc và đầu mối kế hoạch |

<a id="requirements"></a>

## Phạm vi và hành trình

Đặc tả đầy đủ cho [v1](../../releases/v1.md); [MVP](../../releases/mvp.md) chọn luồng tạo/tìm/tham gia → phòng text → lịch sử/gửi/nhận → xử lý mất quyền trong một API host. Gửi file trong phòng để đợt sau theo DEC-023; thoại/video và chia sẻ màn hình theo [Media](../media/README.md).

1. Tìm cộng đồng công khai hoặc mở liên kết mời.
2. Tham gia ngay, gửi yêu cầu chờ duyệt hoặc dùng lời mời hợp lệ theo cấu hình.
3. Xem phòng và lịch sử được cấp quyền.
4. Gửi/sửa/xóa tin theo quyền và nhận cập nhật.
5. Rời theo điều kiện tư cách/ownership.

<a id="use-cases"></a>

## Use case và module thực hiện

Danh mục UC-COM, module thực hiện, API và AC/TC nằm tại [bảng truy vết](traceability.md#use-case-coverage). Quy tắc và luồng chi tiết nằm trong chủ đề được liên kết từ bảng.

<a id="quy-ước-duy-trì"></a>

## Quy ước duy trì

- Mỗi quy tắc/use case/AC/TC có một nguồn tại chủ đề; giữ nguyên mã khi thay vị trí.
- OpenAPI/schema giữ contract máy đọc; SQL giữ DDL thực thi; fixture giữ dữ liệu kiểm chứng. Chủ đề giải thích quyết định và dẫn đến các artefact này.
- Hồ sơ kiểm chứng giữ nguyên kết quả lịch sử theo revision. Thay đổi hiện trạng cập nhật tại chủ đề, không sửa kết luận cũ thành kết quả mới.
- Quyết định dự án giữ mã tại [decision records](../../records/decisions/README.md); release chỉ chọn phạm vi và gate, không chép lại quy tắc.
