# SCDC — Community

Phạm vi: REQ-002/004, SCP-003/004 và phần tin phòng của SCP-005. Tài liệu tổ chức theo hành trình tạo/tham gia cộng đồng, mở phòng và giao tiếp. Mã UC-COM dùng để truy vết hành trình; module thực hiện được ghi trong từng use case.

Community sở hữu cộng đồng, membership, phòng và quyền. Messaging sở hữu tin nhắn và Hub chat. Vị trí tài liệu và prefix route không thay ranh giới module.

## Bắt đầu từ đâu

| Cần tìm | Đọc |
|---|---|
| Đã triển khai gì, ở nhánh nào, bước tiếp theo | [Trạng thái Community](status.md) |
| Quy tắc nghiệp vụ và use case | [Danh mục và truy vết](specs/README.md) |
| Cấu trúc module, API, dữ liệu và transaction | [Thiết kế Community](design/README.md) |
| Chọn gói và cách thực hiện | [Các gói triển khai](delivery/README.md) |
| Phạm vi gói tạo/xem | [Kế hoạch](delivery/create-view/plan.md) |
| Thiết kế gói tạo/xem | [Thiết kế](design/create-view.md) |
| Kết quả kiểm chứng gói tạo/xem | [Nghiệm thu](delivery/create-view/acceptance.md) |

<a id="requirements"></a>

## Phạm vi và hành trình

Đặc tả đầy đủ cho [v1](../../releases/v1.md). [MVP](../../releases/mvp.md) chọn các gói tạo/tham gia/phòng text trước trong một API host. Gửi file trong phòng để sang đợt sau theo DEC-023; thoại/video và chia sẻ màn hình có [đặc tả riêng](../voice-video.md). Phạm vi theo mốc được quản lý ở hồ sơ release, không tạo bản sao quy tắc cho từng mốc.

1. Tìm cộng đồng công khai hoặc mở liên kết mời.
2. Tham gia ngay, gửi yêu cầu chờ duyệt hoặc dùng lời mời hợp lệ theo cấu hình.
3. Xem những phòng được cấp quyền và lịch sử tương ứng.
4. Gửi/sửa/xóa tin văn bản theo quyền và nhận cập nhật.
5. Rời cộng đồng theo điều kiện tư cách/ownership.

## Các thành phần

| Thành phần | Trách nhiệm | Nghiệp vụ | Thiết kế |
|---|---|---|---|
| Servers | Metadata, visibility, search, join mode, ownership | [Servers](specs/servers.md) | [Thiết kế](design/servers.md) |
| Memberships | Tư cách/epoch, tham gia, request, duyệt và rời | [Memberships](specs/memberships.md) | [Thiết kế](design/memberships.md) |
| Invitations | Link mời và lời mời đích danh | [Invitations](specs/invitations.md) | [Thiết kế](design/invitations.md) |
| Channels | Metadata, loại phòng và vòng đời | [Channels](specs/channels.md) | [Thiết kế](design/channels.md) |
| Permissions | Vai trò, quyền quản lý và quyền xem phòng | [Permissions](specs/permissions.md) | [Thiết kế](design/permissions.md) |
| Tích hợp | Điều kiện chung, tin phòng và realtime xuyên module | [Use case/UX/AC/TC](specs/integration.md) | [Giao dịch/API/schema](design/integration.md) |

## Quy ước duy trì

- Mỗi quy tắc/use case/AC/TC có một nguồn định nghĩa trong `specs/`; trang truy vết chỉ dẫn liên kết. Giữ nguyên mã khi di chuyển tài liệu.
- `design/` mô tả cách thực hiện và phân biệt thiết kế mục tiêu với phần được gói hiện hành chọn. Quyết định sản phẩm vẫn giữ mã tại [decisions.md](../../decisions.md).
- `status.md` là nguồn trạng thái hiện tại của Community. Các trang tổng quan, project và kiến trúc dẫn tới đó.
- `delivery/<gói>/plan.md` giữ scope, phụ thuộc và tiêu chí; `acceptance.md` giữ bằng chứng gắn commit đã thử. Kết quả lịch sử không được diễn giải thành kết quả của mọi revision sau này.
- Hợp đồng máy đọc ở `docs/contracts/`, fixture ở `docs/fixtures/`; fixture không thay bằng chứng chạy. Archive giữ lịch sử đã thay thế; Git giữ lịch sử chỉnh sửa.

Các tính năng Accounts, Direct Messaging và Media vẫn dùng bố cục hiện có; Community là phần áp dụng bố cục mới đầu tiên.
