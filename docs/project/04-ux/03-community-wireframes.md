# SCDC — Wireframe cộng đồng, phòng và quyền

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-UX-COM-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Wireframe văn bản đề xuất — chờ rà soát tương tác |
| Căn cứ | [Luồng UX](01-core-user-flows.md), [đặc tả cộng đồng](../03-requirements/02-community-join-and-channels.md), [ma trận quyền](../03-requirements/05-access-control-matrix.md) |

## 1. Khám phá và tham gia

```text
COM-S01 · Khám phá                 COM-S02 · Trang cộng đồng / lời mời
┌───────────────────────────┐     ┌─────────────────────────────────┐
│ Tìm cộng đồng [         ] │     │ Tên cộng đồng                   │
│                           │     │ Mô tả                           │
│ Tên cộng đồng công khai   │     │                                 │
│ Mô tả ngắn          [Xem] │     │ [Tham gia] hoặc [Gửi yêu cầu]    │
│                           │     │ hoặc “Yêu cầu đang chờ duyệt”    │
└───────────────────────────┘     └─────────────────────────────────┘
```

Tìm kiếm chỉ hiện cộng đồng công khai. Từ tìm kiếm, nút tham gia tuân
chế độ vào ngay/chờ duyệt. Liên kết mời hợp lệ mở trang xác nhận đúng
cộng đồng rồi cho vào ngay; không chuyển thành chờ duyệt. Liên kết
hết hạn/thu hồi/không hợp lệ không có nút tham gia hoạt động và không
hiển thị nội dung riêng tư. Chưa là thành viên thì không tải tin phòng.

## 2. Cộng đồng và phòng văn bản

```text
COM-S03 · Màn hình rộng tham chiếu 1280 × 800
┌─────────────┬─────────────────────┬────────────────────────────────┐
│ Cộng đồng   │ Tên cộng đồng [⋯]   │ Tên phòng / chủ đề             │
│             │                     │                                │
│ • Nhóm A    │ Phòng theo chủ đề   │ Lịch sử tin                    │
│ • Nhóm B    │ # chung             │                                │
│             │ # học-tập           │ Tin đã bị xóa                  │
│ [Khám phá]  │                     │                  Tin của tôi   │
│             │ [+ Tạo phòng]*      │                  Đã sửa [⋯]   │
│             │ [Quản lý]*          ├────────────────────────────────┤
│             │                     │ [Nhập tin…              ] [Gửi]│
└─────────────┴─────────────────────┴────────────────────────────────┘
* Chỉ hiện thao tác người dùng được phép thực hiện.
```

Danh sách chỉ chứa phòng được phép xem. Thành viên mới được xem lịch sử
cũ của phòng đó. Không có cấu hình chỉ đọc trong đợt đầu; người xem được
phòng thì gửi được sau khi thỏa điều kiện tài khoản. Giới hạn tin,
sửa/xóa, lỗi và chủ động thử lại giống [wireframe DM](02-account-dm-wireframes.md).

Trạng thái riêng: chưa có phòng được xem, phòng chưa có tin, tải lỗi,
lời mời không dùng được, yêu cầu chờ duyệt, mất quyền khi đang mở và
rời cộng đồng. Sau mất quyền/rời, đóng vùng nội dung phòng và tải lại
danh sách theo quyền; không tiếp tục gửi hoặc nhận tin qua kết nối cũ.

## 3. Quản lý theo quyền

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S04 Tạo phòng | Chủ sở hữu/người có quyền tạo phòng | Tên/chủ đề là trường đề xuất; mặc định mọi thành viên xem; lưu lỗi giữ dữ liệu, tạo thành công về phòng mới |
| COM-S05 Lời mời | Chủ sở hữu/người có quyền tạo lời mời | Tạo, chọn thời hạn, sao chép, xem trạng thái và thu hồi; tập giá trị thời hạn và số lượt dùng còn mở |
| COM-S06 Yêu cầu tham gia | Chủ sở hữu/người có quyền duyệt | Danh sách yêu cầu và nút duyệt; trạng thái đang xử lý/đã duyệt/lỗi; chưa tự thêm chức năng từ chối/hủy chưa chốt |
| COM-S07 Chế độ tham gia | Chủ sở hữu/người có quyền đổi chế độ | Vào ngay/chờ duyệt; ghi rõ liên kết mời hợp lệ vẫn cho vào ngay |
| COM-S08 Vai trò | Chỉ chủ sở hữu | Danh sách vai trò, quyền quản lý theo thao tác, gán/thu hồi thành viên; người khác gọi API cũng bị từ chối |
| COM-S09 Quyền xem phòng | Chủ sở hữu/người có quyền quản lý danh sách xem | Mặc định phòng, cấu hình vai trò, ngoại lệ cá nhân; kết quả theo DEC-057; lưu có version để không ghi đè cấu hình mới hơn |

```text
COM-S09 · Quyền xem phòng
┌─────────────────────────────────────────────────┐
│ Quyền xem #hoc-tap                               │
│ Mặc định: [Mọi thành viên / Giới hạn]             │
│                                                 │
│ Vai trò          Kế thừa / Cho phép / Từ chối     │
│ • Thành viên A   [            lựa chọn         ] │
│                                                 │
│ Ngoại lệ cá nhân Kế thừa / Cho phép / Từ chối     │
│ • @nguoi_dung    [            lựa chọn         ] │
│                                                 │
│ Từ chối giữa vai trò thắng; cá nhân ưu tiên cuối. │
│ Chủ sở hữu luôn xem được phòng.                  │
│ [Hủy]                                    [Lưu]  │
└─────────────────────────────────────────────────┘
```

Bố cục COM-S09 là cách diễn đạt đề xuất của thuật toán quyền. Chỉ chủ
sở hữu sửa định nghĩa hoặc gán vai trò; quyền cấu hình xem phòng không
cho phép tự gán vai trò. Cách tránh người quản lý vô tình tự mất quyền
và phản hồi sau lưu cần kiểm thử trong prototype.

## 4. Trạng thái còn chờ quyết định

Tạo/sửa cộng đồng chưa đủ quy tắc trường, công khai/riêng tư và quyền
đổi. Thêm trực tiếp vào cộng đồng riêng tư chưa rõ người được thêm có
phải đồng ý; chủ sở hữu rời/chuyển quyền chưa chốt. Không đặt nút có
hành vi giả định cho các luồng này trong bản bàn giao đã xác nhận.

Đề xuất màn hình hẹp dùng lần lượt danh sách cộng đồng → danh sách
phòng → hội thoại; quản lý mở thành trang riêng. DEC-059 mới chốt mục
tiêu thiết bị cho tài khoản/DM; phạm vi thiết bị của cộng đồng và media
cần được rà soát riêng ở OQ-007.

Vg rà soát wireframe; Thái dựng prototype và trạng thái; Sáng đối chiếu
quyền/API. Chưa có prototype, kết quả rà soát hoặc kiểm thử khả dụng
được ghi nhận trong tài liệu này.

## 5. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-10-03 | Bổ sung bố cục cộng đồng/phòng, quản lý vai trò và quyền xem; đánh dấu luồng còn thiếu quy tắc. |

[Mục lục hồ sơ](../README.md)
