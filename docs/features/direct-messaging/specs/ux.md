# SCDC — Giao diện DM

Màn hình và hành vi UI mục tiêu; cơ chế state/merge dùng chung tại [Messaging](../../../shared/messaging/text.md).

<a id="ux"></a>

## Giao diện và trạng thái

### Phạm vi bàn giao

Các phác thảo dưới đây xác định vùng giao diện, thao tác và phản hồi để
thảo luận với frontend và QA. Chưa chốt màu sắc, kiểu chữ hoặc thiết kế
tương tác trực quan. Kích thước tham chiếu đề xuất: màn hình rộng 1280 ×
800 và màn hình hẹp 390 × 844; đây chưa phải ma trận thiết bị hỗ trợ.

DEC-059 xác nhận mục tiêu bố cục thích ứng cho desktop và trình duyệt
điện thoại của tài khoản/DM. Mỗi màn hình phải có nhãn trường, thứ tự đi bằng bàn phím hợp lý, focus
nhìn thấy được và thông báo lỗi bằng chữ. Trạng thái gửi không chỉ dựa
vào màu. Tiêu chí tiếp cận đầy đủ tiếp tục thuộc OQ-007.

### Tìm người và hội thoại trên màn hình rộng

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

Điều chỉnh theo yêu cầu người dùng ngày08/10/2026: modal tìm người cho chọn nhiều recipient theo ID, giữ danh sách đã chọn khi đổi từ khóa/tải thêm/lỗi tìm, có thẻ tên hiển thị và username, bỏ riêng hoặc bỏ tất cả. Draft selection chỉ trong bộ nhớ modal; đóng/mở lại hoặc đổi tài khoản sẽ dọn. Đây là lựa chọn UI, contract DM vẫn gồm đúng hai participant; không tự tạo group conversation.

Mục **Người vừa nhắn tin** lấy peer khác actor từ hội thoại mà actor là member, `lastActivityAt` khác null, theo thứ tự API (DESC rồi conversation ID ASC), dedup peer ID. Tên trùng luôn có username; chọn/bỏ chọn dùng chung selection với search. Empty/error/loading/retry riêng; không lấy lịch sử lượt chọn hoặc mock để thay dữ liệu hội thoại. UI/loader được giao P1-T03; thứ tự người vừa nhắn chỉ nghiệm thu bằng tin commit thật sau writer P2-T01. Các task này chưa triển khai ở bản search P1-T01.

Giới hạn 2.000 ký tự theo DEC-053; bộ đếm dùng cùng quy tắc với máy chủ. Khi chưa chọn ai, vùng chính hướng dẫn tìm người để
bắt đầu. Hội thoại mới có lời nhắc gửi tin đầu tiên và không dùng tin giả.

### Màn hình hẹp và thao tác tin

Màn hình hẹp chỉ hiện một vùng chính mỗi lần: danh sách/tìm người → hội
thoại. Có nút quay lại danh sách; quay lại không tự gửi hoặc tự xóa bản
nháp đang nhập. Bản nháp theo DEC-091: giữ theo tài khoản/hội thoại trong bộ nhớ tab; tải lại/đóng tab/logout mất bản nháp, không lưu nội dung vào localStorage/IndexedDB.

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

### Trạng thái giao diện gắn với tiêu chí chấp nhận

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
| UX-DM-10 | Peer không khả dụng do account bị khóa | Đọc lịch sử vẫn được, composer/call mới bị chặn; không hiển thị lý do khóa/email | DEC-104 |
| UX-DM-11 | Tin unavailable sau restore | Hiển thị “Nội dung chưa khôi phục được”, giữ vị trí/tác giả; author có quyền được viết lại body mới hoặc xóa, không tải bản cũ | DEC-108 |

Nút “Gửi” gửi tin trên mọi thiết bị. Desktop dùng Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng theo DEC-071. Không gửi trong khi IME đang composition; hành vi bàn phím ảo phải kiểm thử trên ma trận thiết bị. Không đưa đọc/đã nhận, file, chặn tài khoản hoặc
cuộc gọi vào wireframe của đợt DM văn bản.

### Bàn giao và nội dung còn cần rà soát

Sáng sở hữu UI DM và phản hồi API theo gói fullstack; Vg rà soát bố cục, trạng thái và quyền. Thái cung cấp dataset/bộ chạy kiểm tra theo [DEC-117](../../../project/planning.md#team). Chưa có kết quả rà soát hoặc kiểm thử khả dụng được ghi nhận trong tài liệu này.

Trước khi giao frontend cần xác nhận ma trận trình duyệt/kích thước,
thiết kế liên kết email, fixture UTF-16, hành vi bàn phím/IME và cách xử lý
xung đột. Chính sách đếm/tìm kiếm và phím gửi đã chốt tại DEC-068/069/071. Wireframe cộng đồng là đầu ra riêng tại
[SCDC-UX-COM-001](../../community/specs/integration.md#ux).
