# SCDC — Kiểm thử nghiệm thu, phát hành và vận hành

Cập nhật: 2026-10-03. Phạm vi: SCP-008, SUC-006 và yêu cầu chất lượng xuyên tính năng.

Kế hoạch chuẩn bị; chưa có kết quả nghiệm thu/phát hành được ghi nhận. Compose hiện tại là cấu hình Development. Các điều kiện vận hành còn phải chốt tại OQ-007/OQ-010/OQ-011.

## Mục lục

- [Kiểm thử và nghiệm thu](#testing)
- [Chuẩn bị phát hành](#release)
- [Vận hành và khôi phục](#operations)
- [Thông tin cần chốt](#gaps)

<a id="testing"></a>

## 1. Kiểm thử và nghiệm thu

### Phạm vi kiểm chứng

Thái chuẩn bị và điều phối kiểm thử, Vg và Sáng rà soát tình huống liên
quan đến UX, backend và kiến trúc. Kết quả kiểm thử phải nối tới mã tiêu
chí chấp nhận, môi trường, dữ liệu thử, phiên bản sản phẩm và lỗi phát hiện.
Không coi tiêu chí dự thảo là đã đạt khi chưa có kết quả thực thi.
Bộ ca và dữ liệu chuẩn bị cho tài khoản/DM/quyền phòng tại
[SCDC-QA-DM-001](features/direct-messaging.md#tests); các ca TC trong đặc tả chưa có kết quả chạy được ghi nhận; repo có bộ test tự động được mô tả trong hướng dẫn phát triển.

| Nhóm | Tình huống cần kiểm chứng | Căn cứ |
|---|---|---|
| DM chức năng | Tìm theo hai trường, chọn đúng người, gửi/lưu/xem lại, sửa/xóa, lỗi gửi và chủ động thử lại; từ chối gửi khi email chưa xác minh. | AC-DM-01 đến AC-DM-15 |
| DM kết nối | A gửi khi B chưa mở ứng dụng; B thấy tin đã lưu khi mở lại. Kết quả lần đầu không rõ, thử lại cùng thao tác chỉ một tin. | AC-DM-07, AC-DM-08 |
| Cộng đồng | Chỉ tìm được cộng đồng công khai; vào ngay/chờ duyệt; mời hợp lệ/hết hạn/thu hồi; quyền tạo mời, duyệt, tạo phòng; vào cộng đồng riêng tư và tự rời. | AC-COM-01 đến AC-COM-11, AC-COM-19 đến AC-COM-21 |
| Phòng/tin | Quyền xem mặc định, thành viên mới thấy lịch sử, người có quyền xem được gửi sau xác minh email, chủ sở hữu/người được cấp quyền đổi danh sách xem, gửi lại không trùng và từ chối thao tác trái quyền ở API. | AC-COM-11 đến AC-COM-18, AC-COM-22 đến AC-COM-28 |
| Tài khoản | Đăng ký với email/tên tài khoản/tên hiển thị/mật khẩu, đăng nhập bằng email hoặc tên tài khoản; chưa xác minh bị từ chối gửi, đặt lại mật khẩu qua email. Phiên và bảo vệ tài khoản còn chờ đặc tả. | AC-ACC-01 đến AC-ACC-10, OQ-002 |
| Media | Gọi riêng cần chấp nhận, phòng thoại vào theo quyền xem, nhiều nguồn chia sẻ đồng thời, tự kết nối lại; quyền thiết bị và tải dự kiến. Chỉ có tiêu chí chức năng khung, chưa có ngưỡng chất lượng. | AC-MEDIA-01 đến AC-MEDIA-09; chờ OQ-006/OQ-007 |
| Vận hành | Cài đặt/triển khai, giám sát, sao lưu/khôi phục, lỗi dịch vụ và mở đăng ký công khai. | Chờ OQ-007/OQ-010/OQ-011 và SCP-008 |

### Các cấp kiểm thử

| Cấp | Mục đích | Điều kiện ghi kết quả |
|---|---|---|
| Thành phần | Quy tắc quyền theo vai trò/ngoại lệ cá nhân, nội dung 2.000 ký tự, chuyển trạng thái tin, xử lý mời/yêu cầu và ngoại lệ dữ liệu. | Có trường hợp đúng/sai, dữ liệu biên và kết quả rõ. |
| Tích hợp | Web–API–dịch vụ–dữ liệu; cập nhật thời gian thực và tải lại lịch sử; quyền đổi trong phiên. | Có môi trường tích hợp và định danh bản chạy. |
| Hệ thống | Hành trình người dùng đầu đến cuối trên trình duyệt được hỗ trợ. | Tài khoản/DM cần desktop và trình duyệt điện thoại theo DEC-059; danh sách cụ thể ở OQ-007 phải được xác nhận. |
| Chất lượng/vận hành | Tải, lỗi mạng, sao lưu/khôi phục, giám sát và chất lượng cuộc gọi. | Có ngưỡng đo và mức tải được xác nhận, không dùng giả định quy mô làm tiêu chí đạt. |
| Nghiệm thu | Đại diện khách hàng thực hiện hoặc xác nhận kịch bản trọng yếu. | Ghi rõ đạt/chưa đạt, lỗi tồn, người và ngày xác nhận. |

### Bộ tình huống ưu tiên để chuẩn bị dữ liệu

1. Hai tài khoản không cùng cộng đồng: tìm người, mở DM, gửi tin, đăng
   xuất người nhận, gửi tiếp, đăng nhập lại và đọc lịch sử.
2. Gửi thất bại, người gửi bấm thử lại; xác nhận không có hai bản tin khi
   kết quả lần gửi đầu không rõ, cả trong DM và phòng.
3. Sửa/xóa tin của mình trong DM và trong phòng; tài khoản khác thử cùng
   thao tác bằng API. Kiểm tra nội dung hiển thị trên hai phiên.
4. Hai cộng đồng có chế độ tham gia khác nhau; liên kết mời hợp lệ, hết
   hạn; chủ sở hữu, người có quyền và thành viên thường thử tạo mời/duyệt.
5. Phòng mới hiện cho mọi thành viên; thành viên mới đọc được lịch sử cũ;
   người có quyền xem gửi được tin. Sau khi giới hạn quyền, thành viên
   mất quyền không đọc được lịch sử hay nhận cập nhật qua kết nối mở.
6. Khi media được đặc tả, thử quyền micro/camera, cuộc gọi riêng, phòng
   nhiều người, chia sẻ màn hình, ngắt mạng và khôi phục.

### Điều kiện nghiệm thu dự kiến

Trước mỗi đợt bàn giao cần có: tiêu chí chấp nhận đã được xác nhận, môi
trường và dữ liệu thử, kết quả chạy, danh sách lỗi với mức độ và quyết
định xử lý. Trước phát hành công khai cần thêm kết quả phân quyền, kiểm
thử tải/media theo ngưỡng đã chốt, khôi phục dữ liệu, tài liệu vận hành
và người nhận hỗ trợ. Ngưỡng lỗi cho phép, chất lượng dịch vụ, trình
duyệt hỗ trợ và người ký nghiệm thu còn phải xác định; chưa thể tuyên bố
đạt điều kiện phát hành.

Đợt hiện tại không khảo sát người dùng bên ngoài theo DEC-030. Việc đó
không thay thế kiểm thử khả dụng nội bộ, kiểm thử hệ thống hoặc nghiệm
thu của đại diện khách hàng.

### Mẫu ghi nhận kết quả

| Trường | Nội dung phải ghi |
|---|---|
| Mã tình huống và yêu cầu | AC-*, SCP-* hoặc OQ-* đã được giải quyết. |
| Bản chạy/môi trường | Phiên bản triển khai, trình duyệt, thiết bị và dữ liệu thử. |
| Kết quả | Đạt/chưa đạt/chưa chạy; bằng chứng và thời điểm. |
| Lỗi và xử lý | Mã lỗi, mức ảnh hưởng, người phụ trách và kết quả kiểm tra lại. |
| Xác nhận | Người thực hiện và người xác nhận theo cấp kiểm thử. |


Ca chi tiết được quản lý tại [Accounts](features/accounts.md#tests), [DM](features/direct-messaging.md#tests), [Community](features/community.md#tests) và [Media](features/voice-video.md#acceptance). Một ca phụ thuộc quy tắc chưa chốt phải ghi “Chờ chốt quy tắc”. Các trạng thái Đạt/Chưa đạt/Chưa chạy gắn với build và lần thực thi, không chỉ với sự tồn tại của file test.

<a id="release"></a>

## 2. Chuẩn bị phát hành

MVP triển khai Modular Monolith theo DEC-060. Nơi host, topology, domain, tài nguyên và người vận hành chưa được chốt. Các mục dưới đây là đầu ra cần chuẩn bị, không phải hướng dẫn deployment production đã được kiểm chứng.

| Đầu ra | Điều kiện cần có |
|---|---|
| Bản phát hành | Commit/build, artefact, cấu hình môi trường và phạm vi bàn giao |
| Cấu hình production | Connection string, signing key, CORS, HTTPS và token Development tắt; cách quản lý cấu hình/quyền truy cập |
| Database | Quy trình migration, sao lưu trước thay đổi, kiểm tra tương thích và cách phục hồi |
| Email | Provider/worker, token/liên kết gửi được, retry và trạng thái giao; kiểm thử verify/reset thật |
| Media | Kết quả thử, giới hạn/chất lượng/chi phí và ma trận trình duyệt đã xác nhận |
| Kiểm tra sau triển khai | Health, kết nối DB, đăng ký/xác minh/login, hành trình tin/quyền và media theo scope |
| Rollback | Điều kiện kích hoạt, người quyết định, phiên bản ứng dụng/dữ liệu có thể quay lại và bằng chứng thử |
| Bàn giao | Kết quả nghiệm thu, lỗi tồn được xử lý, hướng dẫn, người trực và quyết định mở công khai |

Không chạy production bằng cấu hình Development của compose hiện tại hoặc dùng script tạo lại schema như migration dữ liệu thật. Hướng dẫn local ở [development.md](development.md#setup).

<a id="operations"></a>

## 3. Vận hành và khôi phục

| Phạm vi | Nội dung cần thiết kế và thử |
|---|---|
| Quan sát | Độ trễ/tỷ lệ lỗi API, số kết nối, backlog outbox/email, lỗi media, tải và chi phí |
| Log | Request/trace ID, mã lỗi, thời lượng; không ghi mật khẩu/token/liên kết email/nội dung chat |
| Thu hồi | Phiên/quyền thay đổi chặn HTTP và kết nối cập nhật theo thời hạn được chốt |
| Sao lưu | Lịch, thời hạn giữ, quyền truy cập, bảo vệ bản sao và chính sách xóa |
| Khôi phục | Thử restore, đo mức mất dữ liệu/thời gian phục hồi; ghi môi trường, build, thao tác và bằng chứng |
| Sự cố | Đầu mối nhận, mức độ, ngưỡng cảnh báo, cách xử lý và ghi nhận nguyên nhân/hành động |
| Hỗ trợ/quản trị | Ai hỗ trợ tài khoản, xử lý vi phạm, xem dữ liệu vận hành và xác nhận thay đổi |

Health hiện tại không kiểm tra DB. Cần phép kiểm tra thực sự cho phụ thuộc và hành trình sau deploy. Độ trễ hoặc khả năng khôi phục chỉ được ghi thành kết quả khi có cấu hình đo và bằng chứng.

<a id="gaps"></a>

## 4. Thông tin cần chốt

- OQ-007: ngưỡng chất lượng, trình duyệt/thiết bị, mức lỗi nghiệm thu và thời hạn thu hồi.
- OQ-010: nơi triển khai, tài nguyên, tải media, chi phí và hạn mức theo mức dùng.
- OQ-011: lưu giữ/xóa dữ liệu, sao lưu/khôi phục, quản trị, hỗ trợ và trách nhiệm vận hành.
- OQ-008: migration, email worker, media provider và cơ chế realtime/thu hồi cần thiết kế.

Vg chủ trì xác nhận sản phẩm/kỹ thuật, Sáng đánh giá triển khai, Thái điều phối kiểm thử theo phân công hiện tại. Người trực vận hành, người ký nghiệm thu và thẩm quyền mở công khai cần xác định cụ thể trước phát hành; không tự coi phân công dự kiến là đã nhận việc.
