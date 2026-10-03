# SCDC — Wireframe tài khoản và DM cho đợt đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-UX-DM-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Wireframe văn bản và trạng thái đề xuất — chờ Vg rà soát |
| Căn cứ | [Luồng UX](01-core-user-flows.md), [tài khoản](../03-requirements/03-accounts.md), [DM](../03-requirements/01-direct-messaging.md) |

## 1. Phạm vi bàn giao

Các phác thảo dưới đây xác định vùng giao diện, thao tác và phản hồi để
thảo luận với frontend và QA. Chưa chốt màu sắc, kiểu chữ hoặc thiết kế
tương tác trực quan. Kích thước tham chiếu đề xuất: màn hình rộng 1280 ×
800 và màn hình hẹp 390 × 844; đây chưa phải ma trận thiết bị hỗ trợ.

DEC-059 xác nhận mục tiêu bố cục thích ứng cho desktop và trình duyệt
điện thoại của tài khoản/DM. Mỗi màn hình phải có nhãn trường, thứ tự đi bằng bàn phím hợp lý, focus
nhìn thấy được và thông báo lỗi bằng chữ. Trạng thái gửi không chỉ dựa
vào màu. Tiêu chí tiếp cận đầy đủ tiếp tục thuộc OQ-007.

## 2. Tài khoản

```text
ACC-S01 · Đăng ký                    ACC-S02 · Đăng nhập
┌─────────────────────────────┐     ┌─────────────────────────────┐
│ Tạo tài khoản               │     │ Đăng nhập                   │
│ Email             [       ] │     │ Email hoặc tên tài khoản    │
│ Tên tài khoản     [       ] │     │ [                         ] │
│ Mật khẩu          [       ] │     │ Mật khẩu          [       ] │
│ [Tạo tài khoản]             │     │ [Đăng nhập]                 │
│ Đã có tài khoản? Đăng nhập  │     │ Quên mật khẩu? / Đăng ký   │
└─────────────────────────────┘     └─────────────────────────────┘
```

Tên trường đăng ký/đăng nhập đã xác nhận tại DEC-054. Không hiển thị
token kỹ thuật lên màn hình sản phẩm.

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S01 Đăng ký | Đang nhập, dữ liệu sai, đang gửi, thất bại | Giữ email/tên tài khoản đã nhập; lỗi tại trường phù hợp; tránh bấm gửi lặp trong khi chờ |
| ACC-S03 Xác minh email | Đã yêu cầu gửi email, mở liên kết, đang xác minh, thành công, liên kết không dùng được | Hướng dẫn kiểm tra email; liên kết sai/hết hạn/đã dùng có đường quay về đăng nhập; gửi lại phụ thuộc quy tắc tài khoản |
| ACC-S02 Đăng nhập | Sai thông tin, chưa xác minh, hết phiên, lỗi dịch vụ | Không mất ngữ cảnh lý do cần đăng nhập; chỉ về hội thoại sau khi xác thực thành công và kiểm tra quyền |
| ACC-S04 Quên mật khẩu | Nhập email, đang gửi, đã tiếp nhận | Phản hồi không tiết lộ email có tài khoản hay không; không hứa email đã được giao nếu mới tiếp nhận yêu cầu |
| ACC-S05 Đặt lại mật khẩu | Liên kết hợp lệ/không dùng được; mật khẩu mới; hoàn tất | Không hiển thị token; thành công dẫn đến đăng nhập; tác động đến phiên chờ đặc tả tài khoản |

Theo DEC-051, ACC-S03 là điểm dừng của tài khoản chưa xác minh: chỉ
có xác minh và đường khôi phục mật khẩu, chưa vào ứng dụng. Đặt lại
mật khẩu không tự bỏ qua bước xác minh.

## 3. Tìm người và hội thoại trên màn hình rộng

```text
DM-S01 / DM-S02 · Tham chiếu 1280 × 800
┌─────────────────────────┬─────────────────────────────────────────┐
│ Hội thoại               │ Tên hiển thị · @ten_tai_khoan            │
│ [Tìm người…           ] │                                         │
│                         │ [Tải tin cũ hơn]                        │
│ Tên hiển thị            │                                         │
│ @ten_tai_khoan          │ Người kia · thời gian                   │
│                         │ Nội dung tin                            │
│ Hội thoại gần đây       │                                         │
│ • Người A               │                 Nội dung tin của tôi    │
│ • Người B               │                 Đã gửi · Đã sửa [⋯]    │
│                         │                                         │
│                         │                 Tin lỗi [Thử lại]       │
│                         ├─────────────────────────────────────────┤
│                         │ [Nhập tin nhắn…                        ]│
│                         │ [                                      ]│
│                         │                         123/giới hạn [Gửi]│
└─────────────────────────┴─────────────────────────────────────────┘
```

Kết quả tìm kiếm luôn đi kèm tên tài khoản để phân biệt tên hiển thị
trùng nhau; API trả định danh ổn định của người được chọn. Không đưa
email vào kết quả. Đây là cách thể hiện đề xuất cho AC-ACC-03/AC-DM-01.

Giới hạn 2.000 ký tự theo DEC-053; bộ đếm dùng cùng quy tắc với máy chủ. Khi chưa chọn ai, vùng chính hướng dẫn tìm người để
bắt đầu. Hội thoại mới có lời nhắc gửi tin đầu tiên và không dùng tin giả.

## 4. Màn hình hẹp và thao tác tin

Màn hình hẹp chỉ hiện một vùng chính mỗi lần: danh sách/tìm người → hội
thoại. Có nút quay lại danh sách; quay lại không tự gửi hoặc tự xóa bản
nháp đang nhập. Chính sách giữ bản nháp khi tải lại/đăng xuất chưa chốt;
đề xuất chỉ giữ trong bộ nhớ của phiên giao diện.

```text
DM-S03 · Sửa tin                     DM-S04 · Xóa tin
┌─────────────────────────────┐     ┌─────────────────────────────┐
│ Sửa tin nhắn                │     │ Xóa tin nhắn này?           │
│ [Nội dung hiện tại        ] │     │ Cả hai người sẽ thấy        │
│ [                         ] │     │ “Tin nhắn đã bị xóa”.       │
│ [Hủy]               [Lưu]  │     │ [Hủy]                 [Xóa] │
└─────────────────────────────┘     └─────────────────────────────┘
```

Menu sửa/xóa chỉ có trên tin của mình; máy chủ vẫn kiểm tra tác giả.
Hủy sửa không thay nội dung đã lưu. Xóa thành công giữ vị trí và thời
gian của tin với dòng thay thế, bỏ nội dung cũ khỏi giao diện đang xem.
Tin đã xóa không có thao tác sửa. Các quy tắc này được chi tiết hóa trong
đặc tả DM; prototype không tự thêm khôi phục tin đã xóa.

## 5. Trạng thái giao diện gắn với tiêu chí chấp nhận

| Mã | Trạng thái | Hiển thị và thao tác đề xuất | Căn cứ |
|---|---|---|---|
| UX-DM-01 | Đang tìm/không có kết quả/lỗi tìm | Hiện tiến trình; “Không tìm thấy người phù hợp”; lỗi có nút thử lại; giữ từ khóa | AC-DM-01 |
| UX-DM-02 | Đang tải/lỗi lịch sử | Không xóa tin đang thấy; báo lỗi riêng cho phần tải thêm; giữ vị trí cuộn | AC-DM-03 |
| UX-DM-03 | Đang gửi | Giữ tin tạm và hiển thị “Đang gửi”; chưa hiện “Đã gửi” | AC-DM-02 |
| UX-DM-04 | Gửi lỗi hoặc chưa rõ kết quả | Giữ tin tạm với “Chưa gửi được” và “Thử lại”; không tự gửi khi có mạng | AC-DM-06, AC-DM-08 |
| UX-DM-05 | Thử lại thành công | Thay tin tạm bằng tin đã lưu; chỉ một tin xuất hiện | AC-DM-08 |
| UX-DM-06 | Mất mạng/kết nối trở lại | Thông báo gián đoạn; đồng bộ lịch sử; tin đang lỗi vẫn chờ người gửi bấm thử lại | AC-DM-07, AC-DM-08 |
| UX-DM-07 | Phiên hết hạn | Yêu cầu đăng nhập lại; chỉ tải lại hội thoại khi đã kiểm tra quyền; không gửi lại tin tự động | AC-ACC-04 |
| UX-DM-08 | Sửa/xóa thất bại | Giữ nội dung xác nhận gần nhất; lỗi không làm tin biến mất; cho tải bản hiện tại nếu xung đột | AC-DM-04, AC-DM-05 |
| UX-DM-09 | Chưa xác minh email | Không gửi tin; có hướng dẫn xác minh theo luồng tài khoản | AC-DM-09 |

Nút “Gửi” là thao tác chắc chắn gửi tin; phím tắt Enter/Shift+Enter cần
chốt trong rà soát UX. Không đưa đọc/đã nhận, file, chặn tài khoản hoặc
cuộc gọi vào wireframe của đợt DM văn bản.

## 6. Bàn giao và nội dung còn cần rà soát

Vg rà soát bố cục và trạng thái; Thái dựng prototype theo mã màn hình,
Sáng đối chiếu các phản hồi với hợp đồng API. Chưa có kết quả rà soát
hoặc kiểm thử khả dụng được ghi nhận trong tài liệu này.

Trước khi giao frontend cần xác nhận ma trận trình duyệt/kích thước,
quy tắc liên kết email, phép đếm ký tự, thao tác bàn phím và cách xử lý
xung đột. Wireframe cộng đồng là đầu ra riêng tại
[SCDC-UX-COM-001](03-community-wireframes.md).

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-10-03 | Bổ sung wireframe văn bản tài khoản/DM, bố cục hai kích thước và bảng trạng thái gắn tiêu chí chấp nhận. |

[Mục lục hồ sơ](../README.md)
