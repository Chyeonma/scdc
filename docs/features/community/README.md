# SCDC — Cộng đồng

Người dùng tạo hoặc tham gia cộng đồng (server), mời thành viên, tạo phòng và phân quyền. Module Community sở hữu cộng đồng, thành viên, lời mời, phòng và quyền. Tin nhắn trong phòng thuộc Messaging; trạng thái tài khoản và phiên thuộc Identity.

**Trạng thái:** trong nền `900e226`, Community và DM đã chạy chung một ứng dụng API. Còn chuyển sang v1: tin phòng, nhận realtime theo quyền phòng và ngắt kết nối khi đổi vai trò hoặc ACL.

**Người phụ trách:** Vg; Sáng làm phần tin phòng, Thái làm Media và file (xem [phân công](../../project/planning.md#team)).

<a id="mục-lục"></a>

## Các trang

| Trang | Nội dung |
|---|---|
| [Cộng đồng và chủ sở hữu](servers.md) | Tạo, tìm kiếm, thông tin cộng đồng, công khai hay riêng tư, cách tham gia, chuyển chủ sở hữu |
| [Thành viên](memberships.md) | Tham gia, xin duyệt, duyệt, rời, tham gia lại |
| [Lời mời](invitations.md) | Link mời, giới hạn lượt và hạn dùng, mời đích danh |
| [Phòng](channels.md) | Tạo, xem, sửa, xóa phòng; loại phòng; phối hợp với Messaging và Media |
| [Vai trò và quyền](access-control.md) | Vai trò, gán vai trò, quyền xem, ACL, quyền quản lý |
| [Tin phòng](../messaging/channel-messaging.md) | Lịch sử, gửi, sửa, xóa tin; realtime; mất quyền (Messaging thực hiện) |

Mỗi trang có quy tắc, ca sử dụng, giao diện, dữ liệu, API, tiêu chí chấp nhận và bảng trạng thái. Bảng trạng thái phân biệt phần đã có, phần mục tiêu và phần chưa có bằng chứng; tiến độ không tính theo số ca sử dụng hay số test.

## Tài liệu liên quan

| Tài liệu | Nội dung |
|---|---|
| [Cơ chế Community](mechanics.md) | Tổ chức backend, transaction, guard, chống trùng, phiên bản, cursor và migration |
| [Hướng dẫn Community](../../guides/development.md#community) | Cấu hình, chạy DB, API, giao diện và kiểm thử trên máy |
| [Bảng truy vết](traceability.md) | Liên kết mã COM, UC, API, AC, TC tới nơi định nghĩa |
| [Hồ sơ kiểm thử](../../records/verification/community/README.md) | Bằng chứng theo commit và phần chưa chứng minh |
| [Phạm vi MVP](../../releases/mvp.md#community-scope) | Chức năng đã chọn cho MVP và điều kiện hoàn tất |
| [Kế hoạch](../../project/planning.md#community-work-items) | Công việc COM-W01 đến 13, ưu tiên và phụ thuộc |

Bằng chứng Community ngày 2026-10-08 chỉ đúng với phiên bản code lúc đó. [Hồ sơ tích hợp 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md) ghi kết quả khi ghép với DM.

<a id="requirements"></a>

## Hành trình chính

1. Tìm cộng đồng công khai hoặc mở link mời.
2. Tham gia ngay, gửi yêu cầu chờ duyệt, hoặc dùng lời mời hợp lệ, tùy cấu hình cộng đồng.
3. Xem các phòng và lịch sử được phép xem.
4. Gửi, sửa, xóa tin theo quyền và nhận cập nhật.
5. Rời cộng đồng, theo điều kiện về tư cách thành viên và quyền chủ sở hữu ([thành viên](memberships.md)).

Đây là đặc tả đầy đủ cho [v1](../../releases/v1.md). MVP đã chọn luồng: tạo, tìm, tham gia, vào phòng chữ, xem lịch sử, gửi nhận tin và xử lý mất quyền. Phần chưa xong làm tiếp trong v1. Gửi file xem [gửi file](../files/README.md); thoại và video xem [Media](../media/README.md).

<a id="use-cases"></a>

## Ca sử dụng

Danh sách UC-COM, module thực hiện, API và tiêu chí chấp nhận nằm trong [bảng truy vết](traceability.md#use-case-coverage).

<a id="quy-ước-duy-trì"></a>

## Quy ước khi sửa tài liệu

- Mỗi quy tắc, ca sử dụng, tiêu chí chấp nhận và ca kiểm thử chỉ định nghĩa ở một trang. Giữ nguyên mã khi chuyển vị trí.
- OpenAPI và schema là hợp đồng máy đọc; SQL là cấu trúc DB thật; fixture là dữ liệu kiểm thử. Trang tính năng giải thích và dẫn tới các file này.
- Hồ sơ kiểm thử giữ nguyên kết quả cũ. Hiện trạng mới cập nhật ở trang tính năng, không sửa kết luận cũ.
- Quyết định ghi tại [hồ sơ quyết định](../../records/decisions/README.md).

---

Nguồn: REQ-002, REQ-004, SCP-003, SCP-004, phần tin phòng của SCP-005, [DEC-171](../../records/decisions/README.md#dec-171), DEC-173.
