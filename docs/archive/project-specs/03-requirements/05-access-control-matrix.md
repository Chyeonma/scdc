# SCDC — Ma trận quyền cốt lõi cho tài khoản, DM và phòng

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-ACL-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Đã chốt mô hình vai trò và thứ tự quyền; cần rà soát thiết kế/kiểm thử |
| Căn cứ | [Tài khoản](03-accounts.md), [DM](01-direct-messaging.md), [cộng đồng](02-community-join-and-channels.md), [sổ quyết định](../01-initiation/03-discovery-and-decision-log.md) |

## 1. Cách sử dụng

Ma trận dùng chung cho đặc tả API, giao diện và kiểm thử. “Người được cấp
quyền” luôn là người có quyền **đúng thao tác đang xét**; quyền tạo lời mời
không tự bao gồm quyền duyệt thành viên hoặc đổi quyền phòng.

Các dòng có căn cứ DEC-* tổng hợp quyết định hiện hành. Phần chưa có quyết
định được ghi riêng ở mục 5; không suy ra một cơ chế vai trò đã được duyệt.
Máy chủ kiểm tra quyền cả khi gọi API trực tiếp, tải lịch sử và nhận cập
nhật thời gian thực. Ẩn nút hoặc phòng trên giao diện không đủ để bảo vệ dữ liệu.

## 2. Tài khoản và DM

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-01 | Đọc/sửa hồ sơ riêng, quản lý phiên của mình | Đã xác thực; đối tượng thuộc chính tài khoản | Dùng định danh của tài khoản khác | ACC-003; chi tiết trường và phiên chờ OQ-002 |
| ACL-02 | Tìm người và mở DM | Đã đăng nhập sau xác minh; chọn đúng định danh người nhận | Chưa đăng nhập hoặc chưa xác minh | DEC-016, DEC-019, DEC-051 |
| ACL-03 | Đọc lịch sử DM | Là một trong hai người của hội thoại và phiên còn hợp lệ | Người thứ ba, kể cả người quản lý một cộng đồng mà hai bên tham gia | REQ-005, AC-ACC-04; suy ra từ phạm vi hội thoại riêng |
| ACL-04 | Gửi tin DM | Là người tham gia hội thoại, phiên hợp lệ, đã xác minh email | Chưa xác minh hoặc không thuộc hội thoại | DM-002, DM-010 |
| ACL-05 | Sửa/xóa tin DM | Có quyền truy cập hội thoại và là tác giả tin | Người nhận sửa/xóa tin của người gửi; người thứ ba | DEC-020 |

Quan hệ kết bạn và việc cùng cộng đồng không phải điều kiện mở/gửi DM.
Không bổ sung chức năng chặn người gửi trong đợt này (DEC-018).

## 3. Cộng đồng và phòng văn bản

| Mã | Thao tác | Điều kiện được phép | Điều kiện bổ sung hoặc giới hạn | Căn cứ |
|---|---|---|---|---|
| ACL-06 | Thấy cộng đồng trong tìm kiếm | Cộng đồng công khai | Cộng đồng riêng tư không xuất hiện | DEC-024 |
| ACL-07 | Tham gia qua tìm kiếm | Cộng đồng công khai | Vào ngay hoặc chờ duyệt theo cấu hình; chưa duyệt chưa có tư cách thành viên | DEC-025 |
| ACL-08 | Tham gia bằng liên kết mời | Liên kết hợp lệ, còn hiệu lực | Bỏ qua chế độ chờ duyệt; liên kết hết hạn/thu hồi bị từ chối | DEC-025, DEC-044 |
| ACL-09 | Tạo/thu hồi lời mời | Chủ sở hữu hoặc người có vai trò cho quyền tạo lời mời | Các giá trị thời hạn và phạm vi thu hồi cần đặc tả | DEC-028, DEC-031, DEC-044, DEC-055 |
| ACL-10 | Duyệt yêu cầu tham gia | Chủ sở hữu hoặc người được cấp quyền duyệt | Quyền từ chối/hủy yêu cầu chưa được chốt | DEC-032 |
| ACL-11 | Đổi chế độ tham gia | Chủ sở hữu hoặc người được cấp quyền đổi chế độ | Không thay đổi nguyên tắc liên kết mời hợp lệ cho vào ngay | DEC-029 |
| ACL-12 | Tạo phòng theo chủ đề | Chủ sở hữu hoặc người được cấp quyền tạo phòng | Phòng mới mặc định mọi thành viên xem được | DEC-026, DEC-033 |
| ACL-13 | Xem phòng và lịch sử | Là thành viên và có quyền xem phòng; chủ sở hữu luôn xem được | Thành viên mới được xem tin cũ; thứ tự vai trò/ngoại lệ cá nhân ở mục 4 | DEC-027, DEC-038, DEC-055–057 |
| ACL-14 | Gửi tin trong phòng | Có quyền xem phòng, phiên hợp lệ và đã xác minh email | Đợt đầu không có quyền chỉ đọc riêng | DEC-040, DEC-041 |
| ACL-15 | Sửa/xóa tin trong phòng | Có quyền truy cập phòng và là tác giả tin | Không cấp quyền sửa/xóa tin của người khác trong đặc tả đợt đầu | DEC-034, AC-COM-13 |
| ACL-16 | Đổi cấu hình quyền xem phòng | Chủ sở hữu hoặc người có vai trò cho quyền quản lý danh sách xem | Áp dụng vai trò và ngoại lệ cá nhân; không đồng nghĩa được quản lý vai trò | DEC-039, DEC-055, DEC-058 |
| ACL-17 | Tự rời cộng đồng | Thành viên thường | Chủ sở hữu rời/chuyển quyền sở hữu chưa được chốt | DEC-045 |
| ACL-18 | Thêm trực tiếp vào cộng đồng riêng tư | Có hình thức tham gia này | Chưa xác định ai được thêm và người được thêm có phải chấp nhận không | DEC-043 |
| ACL-19 | Tạo/sửa vai trò, gán/thu hồi vai trò thành viên | Chỉ chủ sở hữu cộng đồng | Không ủy quyền quản lý vai trò trong đợt đầu | DEC-058 |

Tư cách chủ sở hữu không tự cho quyền đọc DM, sửa tin của người khác hoặc
truy cập nội dung ngoài cộng đồng của mình. Chủ sở hữu luôn xem được
mọi phòng trong cộng đồng của mình (DEC-056), nhưng vẫn phải thỏa điều
kiện tài khoản/phiên. Vai trò được quản lý riêng theo ACL-19.

## 4. Thứ tự tính quyền xem đã chốt

1. Kiểm tra phiên hợp lệ, tài khoản đủ điều kiện, cộng đồng/phòng còn
   tồn tại và người dùng còn là thành viên. Không có điều kiện nền thì
   từ chối trước khi xét quyền.
2. Nếu là chủ sở hữu của chính cộng đồng này, cho xem (DEC-056).
3. Lấy quyền xem mặc định của phòng: phòng mới cho mọi thành viên xem
   trừ khi được đặt giới hạn (DEC-033).
4. Áp cấu hình quyền xem theo các vai trò người dùng có trong phòng:
   có từ chối thì từ chối; nếu không có từ chối nhưng có cho phép thì
   cho phép; không có cấu hình riêng thì giữ mặc định (DEC-055/057).
5. Áp ngoại lệ cá nhân sau cùng: cho phép hoặc từ chối thay kết quả
   vai trò; không đặt ngoại lệ thì giữ kết quả bước 4 (DEC-057).
6. Đợt đầu, có quyền xem và đã xác minh email thì được gửi tin; không
   thêm quyền chỉ đọc độc lập (DEC-040/041).

Thiết kế dữ liệu đề xuất dùng ba trạng thái “kế thừa/cho phép/từ chối”
cho cấu hình quyền xem. Mặc định phòng và cách biểu diễn vai trò mặc
định cần được Vg/Sáng rà soát khi thiết kế dữ liệu. Quyền quản lý qua
vai trò là một tập quyền theo thao tác; DEC-057 chỉ quyết định xung đột
**quyền xem phòng**, không tự đặt quy tắc từ chối cho mọi quyền quản lý.

| Mặc định/va chạm | Ngoại lệ cá nhân | Kết quả xem (thành viên thường) |
|---|---|---|
| Mọi thành viên xem; không cấu hình vai trò | Kế thừa | Cho phép |
| Vai trò cho phép và vai trò từ chối | Kế thừa | Từ chối |
| Vai trò từ chối | Cho phép | Cho phép |
| Vai trò cho phép | Từ chối | Từ chối |
| Phòng giới hạn; không có cấu hình cho phép | Kế thừa | Từ chối |

## 5. Quyền thay đổi khi đang sử dụng

| Tình huống | Kết quả phải kiểm chứng | Tiêu chí liên quan |
|---|---|---|
| Thu hồi quyền xem phòng đang mở | Lần đọc/gửi tiếp theo bị từ chối; không nhận thêm nội dung phòng qua kết nối cũ theo thời hạn thu hồi cần chốt | AC-COM-11, AC-COM-13; OQ-007 |
| Thành viên thường rời cộng đồng | Không còn truy cập nội dung dành cho thành viên; giao diện rời phòng đang mở | AC-COM-21 |
| Mất quyền quản lý | Các thao tác quản lý tiếp theo bị kiểm tra lại phía máy chủ | ACL-09 đến ACL-12, ACL-16 |
| Mất kết nối rồi mở lại | Kiểm tra lại phiên, tư cách thành viên và quyền trước khi tải lịch sử hoặc đăng ký nhận tin | AC-ACC-04, AC-COM-13 |
| Thu hồi quyền một phòng | Không tự thu hồi quyền ở phòng khác hoặc DM độc lập | Suy ra từ phạm vi quyền phòng; cần kiểm thử tích hợp |

Không hứa xóa được nội dung người dùng đã nhìn thấy hoặc tự sao chép.
Việc ngừng hiển thị dữ liệu đã tải trong giao diện, cách xử lý bộ nhớ đệm
và độ trễ thu hồi cần được cụ thể hóa trong thiết kế và OQ-007/OQ-011.

## 6. Chi tiết còn thiếu để hoàn tất thiết kế phân quyền

| Mã theo dõi | Quyết định cần có | Phần bị ảnh hưởng | Đầu mối dự kiến |
|---|---|---|---|
| ACL-O1 | Mô hình dữ liệu vai trò mặc định, danh mục quyền quản lý; giới hạn số vai trò | Tạo phòng, mời, duyệt, đổi cấu hình/quyền xem; người quản lý vai trò đã chốt DEC-058 | Vg; Sáng đánh giá thiết kế |
| ACL-O2 | Màn hình đặt giới hạn, giao dịch cập nhật và version quyền | Danh sách phòng, lịch sử, gửi, thời gian thực; thứ tự đã chốt DEC-057 | Vg, Sáng |
| ACL-O3 | Chuyển chủ sở hữu và rời cộng đồng của chủ sở hữu | Quản lý vòng đời cộng đồng; quyền xem đã chốt DEC-056 | Vg |
| ACL-O4 | Ai thêm trực tiếp; có yêu cầu người được thêm đồng ý không | Cộng đồng riêng tư | Vg |
| ACL-O5 | Thời hạn thu hồi hiệu lực trên kết nối đang mở và khi thao tác đồng thời | Thiết kế đồng bộ quyền, kiểm thử chất lượng | Vg, Sáng, Thái |

Các mục trên tiếp tục thuộc OQ-003/OQ-004/OQ-007. DM hai người không phụ
thuộc mô hình vai trò cộng đồng; có thể rà soát phần ACL-01 đến ACL-05
riêng trong gói bàn giao DM.

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-10-03 | Tổng hợp quyền cốt lõi; ghi nhận DEC-055–058 về vai trò, ngoại lệ cá nhân, quyền chủ sở hữu và thứ tự tính quyền; giữ riêng chi tiết thiết kế còn mở. |

[Mục lục hồ sơ](../README.md)
