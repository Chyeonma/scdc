# SCDC — Giao diện tài khoản

Màn hình và trạng thái mục tiêu; đối chiếu giao diện đang có tại [status](../status.md).

<a id="ux"></a>

## Giao diện và trạng thái

```text
ACC-S01 · Đăng ký
┌────────────────────────────────┐
│ Tạo tài khoản                  │
│ Email             [          ] │
│ Tên tài khoản     [          ] │
│ Tên hiển thị      [          ] │
│ Mật khẩu          [          ] │
│ [Tạo tài khoản]                 │
│ Đã có tài khoản? [Đăng nhập]    │
└────────────────────────────────┘

ACC-S02 · Đăng nhập
┌────────────────────────────────┐
│ Email hoặc tên tài khoản       │
│ [                            ] │
│ Mật khẩu          [          ] │
│ [Đăng nhập]                    │
│ [Quên mật khẩu] / [Đăng ký]    │
└────────────────────────────────┘
```

Tên trường đăng ký/đăng nhập đã xác nhận tại DEC-054, bổ sung trường tên hiển thị theo DEC-062. Không hiển thị
token kỹ thuật lên màn hình sản phẩm.

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S01 Đăng ký | Đang nhập, dữ liệu sai, đang gửi, thất bại | Giữ email/tên tài khoản/tên hiển thị đã nhập; lỗi tại trường phù hợp; tránh bấm gửi lặp trong khi chờ |
| ACC-S03 Xác minh email | Đã tiếp nhận yêu cầu, mở liên kết, đang xác minh, thành công, liên kết không dùng được | Hướng dẫn kiểm tra email; có đường gửi lại, tối thiểu 60 giây giữa hai yêu cầu cùng mục đích; liên kết mới vô hiệu liên kết cũ; không hứa email đã giao khi mới tiếp nhận |
| ACC-S02 Đăng nhập | Sai thông tin, chưa xác minh, hết phiên, lỗi dịch vụ | Không mất ngữ cảnh lý do cần đăng nhập; chỉ về hội thoại sau khi xác thực thành công và kiểm tra quyền |
| ACC-S04 Quên mật khẩu | Nhập email, đang gửi, đã tiếp nhận | Phản hồi không tiết lộ email có tài khoản hay không; không hứa email đã được giao nếu mới tiếp nhận yêu cầu |
| ACC-S05 Đặt lại mật khẩu | Liên kết hợp lệ/không dùng được; mật khẩu mới; hoàn tất | Không hiển thị token; thành công dẫn đến đăng nhập, mọi phiên cũ bị thu hồi; nếu chưa xác minh thì vẫn phải xác minh |
| ACC-S06 Hồ sơ | Tải/lưu/lỗi; dữ liệu không hợp lệ | Giữ bản đang sửa khi lưu lỗi; tên tài khoản/email chỉ đọc; các trường sửa theo ACC-013 |
| ACC-S07 Đổi mật khẩu | Đang nhập/đang lưu/sai mật khẩu hiện tại/thành công | Khi thành công xóa phiên giao diện và về đăng nhập; mọi thiết bị phải đăng nhập lại |
| ACC-S08 Phiên/thiết bị | Tải danh sách, thu hồi một phiên, đăng xuất mọi thiết bị | Đánh dấu phiên hiện tại; thu hồi phiên khác không đăng xuất phiên hiện tại; thu hồi phiên hiện tại/đăng xuất tất cả về đăng nhập |

Theo DEC-051, ACC-S03 là điểm dừng của tài khoản chưa xác minh: chỉ
có xác minh và đường khôi phục mật khẩu, chưa vào ứng dụng. Đặt lại
mật khẩu không tự bỏ qua bước xác minh.

Hồ sơ hiện tại cho sửa `displayName`, `bio`, `locale`, `timezone`; username/email không có endpoint đổi. Cần thể hiện tải/lưu/lỗi và trạng thái phiên bị thu hồi sau đổi mật khẩu. Tài khoản hỗ trợ bố cục desktop/trình duyệt điện thoại theo DEC-059; ma trận trình duyệt đã chốt DEC-082; phiên bản cụ thể/thiết bị/kích thước và tiêu chí tiếp cận còn OQ-007. Thiết kế thị giác và prototype vẫn cần rà soát; form hiện tại nằm trong [AuthScreen](../../../../clients/WebClient/src/components/AuthScreen.jsx).
