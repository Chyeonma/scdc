# SCDC — Phác thảo giải pháp kỹ thuật

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-ARC-001 |
| Phiên bản | 0.5 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Đề xuất để đánh giá — chưa chọn công nghệ, chưa duyệt thiết kế |
| Căn cứ | DEC-007, [đặc tả DM](../03-requirements/01-direct-messaging.md), [đặc tả cộng đồng](../03-requirements/02-community-join-and-channels.md), [khoảng trống yêu cầu](../03-requirements/00-requirements-coverage.md) |

## 1. Nguyên tắc và ranh giới dự kiến

Kiến trúc microservices là quyết định đã có; ranh giới dưới đây là phương
án để Vg và Sáng đánh giá. Mỗi dịch vụ sở hữu dữ liệu nghiệp vụ của mình;
dịch vụ khác truy cập qua hợp đồng API hoặc sự kiện. Không chọn framework,
nhà cung cấp hoặc số lượng dịch vụ triển khai cuối cùng khi chưa thử nghiệm
và ước lượng công sức vận hành.

| Thành phần dự kiến | Trách nhiệm | Dữ liệu sở hữu dự kiến |
|---|---|---|
| Web client | Điều hướng, hiển thị hội thoại/phòng, trạng thái gửi/lỗi và các thao tác của người dùng. | Dữ liệu phiên và trạng thái giao diện tạm thời. |
| Điểm vào API và kết nối thời gian thực | Xác thực yêu cầu, định tuyến, giới hạn truy cập và kết nối nhận cập nhật. | Trạng thái kết nối ngắn hạn; không là nguồn dữ liệu tin nhắn. |
| Tài khoản | Đăng ký/đăng nhập, hồ sơ, phiên và thông tin người dùng cho tìm kiếm. | Tài khoản, hồ sơ, phiên. |
| Cộng đồng | Cộng đồng, thành viên, lời mời, yêu cầu tham gia, phòng và quyền. | Cộng đồng, thành viên, phòng, vai trò và quyền. |
| Nhắn tin | Hội thoại riêng, tin trong phòng, sửa/xóa, lịch sử và kết quả gửi. | Hội thoại, tin nhắn, dấu sửa/xóa, khóa chống trùng. |
| Media | Báo hiệu cuộc gọi, trạng thái phòng thoại và tích hợp giải pháp thoại/video/chia sẻ màn hình. | Phiên gọi và cấu hình media; chi tiết chờ thử nghiệm. |

Các thành phần có thể được gom hoặc tách khác đi sau đánh giá OQ-008;
microservices vẫn là ràng buộc của phiên bản đầu. Đối với nhóm ba người,
cần đánh giá chi phí triển khai, giám sát và kiểm thử tích hợp của từng
ranh giới trước khi xác nhận.

Hợp đồng và dữ liệu chi tiết cho gói tài khoản/DM đã được đề xuất tại
[SCDC-API-DM-001](02-account-dm-contracts.md). Tài liệu đó cụ thể hóa API,
lỗi, khóa chống trùng, phiên bản tin và đồng bộ lịch sử; chưa phải thiết
kế được duyệt hoặc bằng chứng triển khai. Ma trận quyền dùng
[SCDC-FR-ACL-001](../03-requirements/05-access-control-matrix.md).

## 2. Các luồng dữ liệu trọng tâm

**Gửi tin riêng:** web client gửi yêu cầu có định danh thao tác; điểm vào
chuyển đến dịch vụ nhắn tin; dịch vụ kiểm tra người gửi, lưu tin rồi trả
kết quả. Chỉ sau khi lưu thành công, giao diện hiện “Đã gửi”. Cập nhật
thời gian thực giúp người đang mở ứng dụng thấy tin mới; người vắng mặt
đọc lịch sử đã lưu khi mở lại. Theo DEC-037, thử lại cùng thao tác phải
chỉ tạo một tin dù kết quả lần đầu không rõ. Định danh thao tác được giữ
ổn định giữa các lần thử; cơ chế và vòng đời khóa đã có phương án ở
SCDC-API-DM-001, cần rà soát. Theo DEC-051, tài khoản chưa xác minh chưa
có quyền vào ứng dụng;
dịch vụ vẫn kiểm tra ở máy chủ, không chỉ ẩn vùng nhập. Sửa tin chỉ giữ
bản mới nhất và nội dung có giới hạn theo DEC-052/053; không dùng
outbox/log để lưu lại nội dung cũ.

**Gửi tin trong phòng:** dịch vụ nhắn tin phải kiểm tra tư cách thành viên
và quyền xem/gửi của phòng theo thông tin từ dịch vụ cộng đồng. Quyền phải
được kiểm tra lại khi đọc lịch sử và khi nhận tin qua kết nối thời gian
thực; ẩn nút trên giao diện không thay thế kiểm tra phía máy chủ. Cần
xác định cách cập nhật quyền gần thời gian thực và hành vi khi quyền thay
đổi trong lúc người dùng đang mở phòng. Thành viên mới có quyền xem được
đọc lịch sử cũ; mọi thành viên có quyền xem được gửi tin trong đợt đầu
(DEC-038, DEC-040). Chỉ chủ sở hữu hoặc người được cấp quyền đổi danh
sách xem (DEC-039).

**Tham gia cộng đồng:** dịch vụ cộng đồng quyết định vào ngay, chờ duyệt
hoặc dùng lời mời còn hiệu lực. Việc tiêu thụ lời mời và ghi thành viên
cần có kết quả nhất quán để không tạo nhiều bản ghi thành viên khi người
dùng nhấn nhiều lần. Quyền của người tạo mời/người duyệt được kiểm tra ở
máy chủ. Sau khi thành viên được thêm, danh sách phòng trả về theo quyền.

## 3. Hợp đồng cần thiết trước khi phát triển

| Hợp đồng | Dữ liệu/hành vi tối thiểu cần thống nhất | Phụ thuộc |
|---|---|---|
| Tìm người | Truy vấn theo tên tài khoản/tên hiển thị, định danh duy nhất để chọn đúng người, phân trang và giới hạn truy vấn. | OQ-002, OQ-005 |
| Tin nhắn | Tạo, đọc lịch sử, sửa/xóa tin của mình, định danh thao tác, kết quả lỗi rõ ràng và phân trang lịch sử. | OQ-005 |
| Quyền phòng | Kiểm tra xem/gửi, tạo phòng, thay đổi quyền và phản hồi khi mất quyền. | OQ-004 |
| Tham gia | Tìm cộng đồng công khai, yêu cầu, duyệt, tạo/kiểm tra lời mời, thêm thành viên. | OQ-003, OQ-004 |
| Media | Báo hiệu, tham gia/rời, trạng thái thiết bị và xử lý ngắt kết nối. | OQ-006, OQ-008 |

Với mỗi hợp đồng cần ghi định dạng đầu vào/đầu ra, quyền truy cập, mã lỗi,
phiên bản, giới hạn tải và kịch bản thử lại. Không coi bảng này là đặc tả
API đã hoàn thành.

## 4. Tin cậy, bảo vệ dữ liệu và vận hành

- Dùng kết nối bảo mật, xác thực mọi yêu cầu, kiểm tra quyền tại ranh giới
  dịch vụ sở hữu dữ liệu; không tin quyền do client gửi lên.
- Không ghi nội dung tin nhắn, token đăng nhập hoặc liên kết mời vào log
  vận hành. Cần xác định thời gian giữ dữ liệu, chính sách xóa và quyền
  truy cập của người vận hành tại OQ-011.
- Có giám sát lỗi API, lỗi kết nối thời gian thực, độ trễ gửi/đọc tin,
  lượng phiên media và chi phí tài nguyên; cần chốt ngưỡng ở OQ-007/OQ-010.
- Thử khôi phục dữ liệu từ bản sao lưu trước khi mở đăng ký công khai.
  Mức mất dữ liệu và thời gian khôi phục chấp nhận được còn phải chốt.
- Đánh giá thử một luồng gọi riêng và một phòng thoại có video/chia sẻ
  màn hình, gồm quyền thiết bị, ngắt mạng và chi phí media, trước khi
  khóa giải pháp hoặc dự toán. Thử nhiều người chia sẻ đồng thời trong
  cùng phòng và tự kết nối lại theo DEC-049/050; đo số luồng và tải thực
  tế để đề xuất giới hạn sản phẩm.

## 5. Quyết định kỹ thuật còn mở

| Quyết định | Cách xác nhận | Đầu mối |
|---|---|---|
| Ranh giới triển khai thực tế, giao tiếp đồng bộ/sự kiện, cơ sở dữ liệu | Phác thảo dữ liệu, thử một lát cắt DM và phân quyền phòng; so sánh công sức vận hành. | Vg, Sáng |
| Công nghệ kết nối thời gian thực và cách đồng bộ sau mất mạng | Thử gửi/nhận, mở lại lịch sử và gửi lại không trùng trên môi trường thử. | Vg, Sáng, Thái |
| Giải pháp media, khả năng tái sử dụng và chi phí | Thử nghiệm theo OQ-006 và AS-003; đo chất lượng/chi phí theo tải dự trù. | Vg, Sáng |
| Môi trường triển khai, sao lưu, giám sát và giới hạn tải | Thiết kế vận hành và kiểm thử theo OQ-007/OQ-010. | Vg, Sáng, Thái |

## 6. Điều kiện chuyển sang thiết kế chi tiết

Đã có ma trận quyền và luồng tài khoản/DM chi tiết hơn; cần rà soát
các đề xuất và vấn đề còn mở ở [bảng sẵn sàng](../06-planning/02-development-readiness.md).
Điều kiện hoàn tất vẫn là: có quy tắc đủ cho gói bàn giao; thống nhất hợp
đồng chính và mô hình dữ liệu; chứng minh được đường gửi tin có lưu bền,
phát lại lịch sử và kiểm tra quyền. Riêng phần media cần kết quả thử
media và ước lượng chi phí vận hành trước khi khóa giải pháp; không
biến phần này thành điều kiện chờ cho thiết kế DM độc lập. Vg chủ trì
rà soát; việc xác nhận giải pháp cần theo
[quy trình dự án](../development-process.md).

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Đề xuất ranh giới dịch vụ, luồng dữ liệu, hợp đồng và nội dung cần thử nghiệm. |
| 0.2 | 2026-09-30 | Đồng bộ quyết định tránh tin trùng và quyền lịch sử/xem/gửi phòng. |
| 0.3 | 2026-09-30 | Bổ sung kiểm tra trạng thái xác minh email tại ranh giới gửi tin. |
| 0.4 | 2026-09-30 | Đưa chia sẻ nhiều nguồn và tự kết nối lại vào thử nghiệm media. |
| 0.5 | 2026-10-03 | Liên kết hợp đồng tài khoản/DM, ma trận quyền và điều kiện bàn giao; đồng bộ dữ liệu tin sửa và quyền trước xác minh. |

[Mục lục hồ sơ](../README.md)
