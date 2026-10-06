# SCDC — Cộng đồng, phòng và phân quyền

Cập nhật: 2026-10-06. Phạm vi: REQ-002/004, SCP-003/004 và phần tin phòng của SCP-005. Quy tắc COM, ACL-06–20, use case UC-COM, tiêu chí AC-COM, màn hình COM-S và ca TC-COM/TC-ACL.

Quy tắc tham gia/quyền cốt lõi đã xác nhận; các luồng DEC-072–077/087 và bổ sung vai trò/tìm kiếm/tên/phạm vi/visibility/lời mời/quản lý phòng DEC-092–098 được cụ thể hóa bên dưới. Thiết kế dữ liệu/API là bản dự thảo để rà soát. Community/Messaging mới có nền module; wireframe và ca kiểm thử chưa phải kết quả triển khai hoặc nghiệm thu.

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Quyền và thu hồi](#permissions)
- [Use case Community](#use-cases)
- [Giao diện](#ux)
- [Thiết kế dữ liệu và API](#contracts)
- [Thiết kế chi tiết cộng đồng và quyền](#detailed-design)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Vấn đề còn mở](#gaps)

<a id="requirements"></a>

## 1. Phạm vi, hành trình và quy tắc

### Mục tiêu và phạm vi đợt đầu

Người dùng có thể tìm cộng đồng công khai hoặc dùng liên kết mời để tham
gia. Trong cộng đồng, các phòng theo chủ đề giúp tách cuộc trò chuyện;
thành viên chỉ nhìn thấy phòng mình được cấp quyền xem và nhắn tin văn bản
trong phòng mình được phép nhắn.

Đợt này tập trung vào cách tham gia, hiển thị phòng và nhắn tin văn bản.
Việc gửi file tài liệu trong phòng được để sang đợt sau theo
DEC-023. Các tính năng thoại/video và chia sẻ màn hình thuộc phạm vi
[Project Brief](../project.md#scope) nhưng chưa được đặc
tả trong tài liệu này.

### Hành trình tham gia

1. Người dùng tìm cộng đồng. Kết quả chỉ gồm cộng đồng công khai.
2. Nếu cộng đồng cho vào ngay, người dùng tham gia trực tiếp. Đây là chế
   độ mặc định của cộng đồng công khai mới tạo.
3. Nếu cộng đồng bật chế độ chờ duyệt, người dùng gửi yêu cầu tham gia.
   Chủ sở hữu hoặc người được cấp quyền có thể duyệt yêu cầu.
4. Người dùng có liên kết mời hợp lệ được vào ngay, kể cả khi cộng đồng
   đang bật chế độ chờ duyệt. Người tạo liên kết mời chọn thời hạn hiệu
   lực của liên kết. Chỉ chủ sở hữu hoặc người được cấp quyền tạo liên
   kết mời.
5. Sau khi tham gia, thành viên chỉ thấy những phòng mình được cấp quyền
   xem. Phòng mới mặc định cho mọi thành viên xem được, trừ khi giới hạn
   quyền. Chỉ chủ sở hữu hoặc người được cấp quyền có thể tạo phòng mới.
6. Mọi thành viên có quyền xem phòng đều gửi được tin văn bản trong đợt
   đầu; người gửi được sửa và xóa tin của mình như tin riêng. Thành viên
   mới được xem lịch sử cũ của phòng nếu còn quyền xem.
7. Cộng đồng riêng tư không xuất hiện trong tìm kiếm; người dùng có thể
   tham gia bằng liên kết mời hoặc được thêm trực tiếp. Thành viên thường
   có thể tự rời cộng đồng.

### Quy tắc đã xác định

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| COM-001 | Người dùng có thể tham gia qua tìm kiếm hoặc liên kết mời. | DEC-022 |
| COM-002 | Chỉ cộng đồng công khai xuất hiện trong kết quả tìm kiếm. | DEC-024 |
| COM-003 | Cộng đồng công khai mới tạo mặc định cho vào ngay; có thể cấu hình chờ duyệt. | DEC-025 |
| COM-004 | Liên kết mời hợp lệ cho vào ngay, kể cả khi cộng đồng bật chế độ chờ duyệt. | DEC-025 |
| COM-005 | Người tạo liên kết mời chọn thời hạn hiệu lực. | DEC-028 |
| COM-006 | Chủ sở hữu hoặc người được cấp quyền có thể đổi chế độ vào ngay/chờ duyệt. | DEC-029 |
| COM-007 | Chỉ chủ sở hữu hoặc người được cấp quyền có thể tạo phòng theo chủ đề. | DEC-026 |
| COM-008 | Thành viên chỉ thấy phòng mình được cấp quyền xem. | DEC-027 |
| COM-009 | Gửi file tài liệu trong phòng được xếp vào đợt sau. | DEC-023 |
| COM-010 | Chủ sở hữu hoặc người được cấp quyền có thể tạo liên kết mời. | DEC-031 |
| COM-011 | Chủ sở hữu hoặc người được cấp quyền có thể duyệt yêu cầu tham gia. | DEC-032 |
| COM-012 | Phòng mới mặc định cho mọi thành viên xem được, trừ khi giới hạn quyền. | DEC-033 |
| COM-013 | Tin văn bản trong phòng được người gửi sửa và xóa như tin riêng: sửa bất cứ lúc nào với dấu “Đã sửa”, xóa cho mọi người với dòng thay thế; trạng thái gửi và thử lại áp dụng tương tự. | DEC-034 |
| COM-014 | Tin đã được lưu trong phòng vẫn xem lại được khi thành viên có quyền mở phòng sau lúc vắng mặt. | DEC-035, SCP-005 |
| COM-015 | Thành viên mới vào cộng đồng được xem lịch sử cũ của phòng mình được phép xem. | DEC-038 |
| COM-016 | Chủ sở hữu và người được cấp quyền được thay đổi danh sách người có quyền xem phòng. | DEC-039 |
| COM-017 | Trong đợt đầu, mọi thành viên có quyền xem phòng đều được gửi tin văn bản trong phòng đó. | DEC-040 |
| COM-018 | Thử lại cùng thao tác gửi trong phòng không tạo tin trùng. | DEC-037 |
| COM-019 | Cộng đồng riêng tư có thể tham gia bằng liên kết mời hoặc được thêm trực tiếp. | DEC-043 |
| COM-020 | Người có quyền tạo liên kết mời có thể thu hồi liên kết trước hạn; liên kết đã thu hồi không còn dùng để tham gia. | DEC-044 |
| COM-021 | Thành viên thường có thể tự rời cộng đồng. | DEC-045 |
| COM-022 | Người gửi phải xác minh email trước khi gửi tin trong phòng. | DEC-041, SCDC-FR-ACC-001 |
| COM-023 | Tin đã sửa chỉ giữ nội dung mới nhất; không cung cấp lịch sử bản cũ. | DEC-052, nguyên tắc tương tự DM tại DEC-034 |
| COM-024 | Tin văn bản tối đa 2.000 đơn vị UTF-16 sau CRLF/CR → LF; cho xuống dòng/emoji; từ chối UTF-16 lỗi và tin rỗng/chỉ trắng hoặc vô hình. Dùng chung [quy tắc nội dung DM](direct-messaging.md#detailed-design). | DEC-053, DEC-068, DEC-090 |
| COM-025 | Quyền quản lý và xem phòng cấp qua vai trò; phòng có ngoại lệ cho từng thành viên. | DEC-055 |
| COM-026 | Chủ sở hữu luôn xem được mọi phòng trong cộng đồng của mình. | DEC-056 |
| COM-027 | Quyền xem giữa các vai trò có từ chối thì từ chối thắng; ngoại lệ cá nhân áp dụng sau cùng, trừ quyền chủ sở hữu. | DEC-057, DEC-056 |
| COM-028 | Chỉ chủ sở hữu được tạo/sửa vai trò và gán/thu hồi vai trò thành viên. | DEC-058 |
| COM-029 | Tài khoản đã xác minh được tạo cộng đồng; tên 2–100, mô tả tối đa 1.000; chọn công khai/riêng tư, mặc định công khai. Chủ sở hữu sửa các trường này. | DEC-072 |
| COM-030 | Người có quyền duyệt được từ chối; người gửi xem trạng thái/hủy pending, sau từ chối/hủy được gửi mới. | DEC-073 |
| COM-031 | Chủ sở hữu/người có quyền tạo mời gửi lời mời đích danh vào cộng đồng riêng tư; người nhận chấp nhận mới trở thành thành viên. | DEC-074 |
| COM-032 | Link mời chọn hạn 1 giờ/1 ngày/7 ngày/không hết hạn, mặc định 7 ngày; maxUses nguyên dương hoặc không giới hạn; người có quyền tạo mời được thu hồi. | DEC-075 |
| COM-033 | Chủ sở hữu chuyển ngay cho thành viên đã xác minh/active, không cần người nhận chấp nhận; chủ cũ vẫn là thành viên và chỉ được rời sau chuyển. | DEC-076 |
| COM-034 | Tên phòng 1–100, chủ đề tối đa 1.000; owner/người có quyền quản lý phòng tạo/sửa/xóa; xóa ngừng truy cập tin/media, chưa khôi phục trong MVP. | DEC-077 |
| COM-035 | Rejoin theo join mode hiện hành và vai trò mặc định, không phục hồi role cũ; có quyền xem lại lịch sử phòng hiện được phép xem. | DEC-087, DEC-038 |
| COM-036 | Mời đích danh hạn 7 ngày; đúng người nhận từ chối, người có quyền tạo mời hủy pending; sau trạng thái cuối không accept được. | DEC-087 |
| COM-037 | @everyone tự áp cho mọi thành viên, không xóa/không có quyền quản lý; tối đa 20 vai trò tự tạo, tên 1–64 UTF-16; quyền quản lý cộng dồn. | DEC-092, DEC-058 |
| COM-038 | Search chỉ cộng đồng công khai theo một phần tên, q 2–100 UTF-16; case-insensitive/accent-sensitive, tên khớp đúng trước; trang 20/tối đa 50. Trường tên/mô tả/chủ đề đếm UTF-16. | DEC-093 |
| COM-039 | Tên Unicode được trim và không trống/vô hình; tên server được trùng, tên phòng/vai trò unique trong server theo case-insensitive/accent-sensitive. MVP chưa có xóa server. | DEC-094/095 |
| COM-040 | Chuyển sang private hủy pending join requests và báo lý do; giữ member và lời mời hợp lệ. | DEC-096 |
| COM-041 | Lời mời đã tạo không tự vô hiệu khi creator rời/mất quyền; actor tạo/duyệt/hủy/thu hồi vẫn kiểm tra quyền hiện hành. | DEC-097 |
| COM-042 | Sửa/xóa/đổi cấu hình xem phòng có sẵn cần quyền xem phòng và đúng quyền quản lý; tạo mới chỉ cần quyền tạo/quản lý phòng. Quyền quản lý không mở phòng bị ẩn. | DEC-098 |

Ma trận theo thao tác, thuật toán quyền xem và các trường hợp thu hồi
được quản lý tại [SCDC-FR-ACL-001](community.md#permissions).

Các quyết định DEC-* được ghi tại
[sổ quyết định](../decisions.md#decisions).

<a id="permissions"></a>

## 2. Ma trận quyền, thuật toán và thu hồi

“Người được cấp quyền” là người có quyền đúng thao tác đang xét; quyền tạo lời mời không tự cho quyền duyệt thành viên hoặc quản lý vai trò. Actor, phiên và tư cách thành viên được kiểm tra ở máy chủ. Quyền Accounts/DM độc lập với vai trò cộng đồng.

### Cộng đồng và phòng văn bản

| Mã | Thao tác | Điều kiện được phép | Điều kiện bổ sung hoặc giới hạn | Căn cứ |
|---|---|---|---|---|
| ACL-06 | Thấy cộng đồng trong tìm kiếm | Cộng đồng công khai | Cộng đồng riêng tư không xuất hiện | DEC-024 |
| ACL-07 | Tham gia qua tìm kiếm | Cộng đồng công khai | Vào ngay hoặc chờ duyệt theo cấu hình; chưa duyệt chưa có tư cách thành viên | DEC-025 |
| ACL-08 | Tham gia bằng liên kết mời | Liên kết hợp lệ, còn hiệu lực | Bỏ qua chế độ chờ duyệt; liên kết hết hạn/thu hồi bị từ chối | DEC-025, DEC-044 |
| ACL-09 | Tạo/thu hồi lời mời | Chủ sở hữu hoặc người có vai trò cho quyền tạo lời mời | Hạn/lượt theo COM-032; kiểm tra thu hồi và giới hạn trong transaction join | DEC-028, DEC-031, DEC-044, DEC-055 |
| ACL-10 | Duyệt yêu cầu tham gia | Chủ sở hữu hoặc người được cấp quyền duyệt | Người có quyền duyệt được từ chối; người gửi hủy yêu cầu đang chờ theo DEC-073 | DEC-032 |
| ACL-11 | Đổi chế độ tham gia | Chủ sở hữu hoặc người được cấp quyền đổi chế độ | Không thay đổi nguyên tắc liên kết mời hợp lệ cho vào ngay | DEC-029 |
| ACL-12 | Tạo/sửa/xóa phòng | Chủ sở hữu hoặc người có quyền quản lý phòng | Sửa/xóa cần quyền xem phòng theo DEC-098; phòng mới mặc định mọi thành viên xem; xóa chặn HTTP/realtime/media, chưa có khôi phục | DEC-026, DEC-033, DEC-077/098 |
| ACL-13 | Xem phòng và lịch sử | Là thành viên và có quyền xem phòng; chủ sở hữu luôn xem được | Thành viên mới được xem tin cũ; thứ tự vai trò/ngoại lệ cá nhân ở [thuật toán quyền xem](#view-permissions) | DEC-027, DEC-038, DEC-055–057 |
| ACL-14 | Gửi tin trong phòng | Có quyền xem phòng, phiên hợp lệ và đã xác minh email | Đợt đầu không có quyền chỉ đọc riêng | DEC-040, DEC-041 |
| ACL-15 | Sửa/xóa tin trong phòng | Có quyền truy cập phòng và là tác giả tin | Không cấp quyền sửa/xóa tin của người khác trong đặc tả đợt đầu | DEC-034, AC-COM-13 |
| ACL-16 | Đổi cấu hình quyền xem phòng | Chủ sở hữu hoặc người có vai trò cho quyền quản lý danh sách xem | Cần quyền xem phòng đang cấu hình theo DEC-098; không đồng nghĩa được quản lý vai trò | DEC-039, DEC-055, DEC-058/098 |
| ACL-17 | Tự rời cộng đồng | Thành viên thường | Chủ sở hữu phải chuyển ngay cho thành viên active/đã xác minh trước khi rời — DEC-076 | DEC-045 |
| ACL-18 | Mời đích danh vào cộng đồng riêng tư | Chủ sở hữu/người có quyền tạo mời gửi; đúng người nhận chấp nhận | Chưa chấp nhận thì chưa có tư cách thành viên/quyền phòng | DEC-043, DEC-074 |
| ACL-19 | Tạo/sửa vai trò, gán/thu hồi vai trò thành viên | Chỉ chủ sở hữu cộng đồng | Không ủy quyền quản lý vai trò trong đợt đầu | DEC-058 |
| ACL-20 | Tạo/sửa cộng đồng | Tài khoản đã xác minh được tạo; chủ sở hữu mới được sửa tên/mô tả/công khai | Quyền đổi chế độ vào ngay/chờ duyệt vẫn là ACL-11 | DEC-072 |

Tư cách chủ sở hữu không tự cho quyền đọc DM, sửa tin của người khác hoặc
truy cập nội dung ngoài cộng đồng của mình. Chủ sở hữu luôn xem được
mọi phòng trong cộng đồng của mình (DEC-056), nhưng vẫn phải thỏa điều
kiện tài khoản/phiên. Vai trò được quản lý riêng theo ACL-19.

<a id="view-permissions"></a>

### Thứ tự tính quyền xem đã chốt

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

Thiết kế dữ liệu dùng ba trạng thái “kế thừa/cho phép/từ chối” cho cấu hình quyền xem. @everyone và giới hạn vai trò đã chốt DEC-092; cách lưu được mô tả ở [thiết kế chi tiết](#detailed-design). Quyền quản lý là hợp các quyền cho phép từ vai trò tự tạo; không có DENY quản lý hoặc ngoại lệ quản lý cá nhân trong MVP. DEC-057 quyết định xung đột **quyền xem phòng**.

| Mặc định/va chạm | Ngoại lệ cá nhân | Kết quả xem (thành viên thường) |
|---|---|---|
| Mọi thành viên xem; không cấu hình vai trò | Kế thừa | Cho phép |
| Vai trò cho phép và vai trò từ chối | Kế thừa | Từ chối |
| Vai trò từ chối | Cho phép | Cho phép |
| Vai trò cho phép | Từ chối | Từ chối |
| Phòng giới hạn; không có cấu hình cho phép | Kế thừa | Từ chối |

### Quyền thay đổi khi đang sử dụng

| Tình huống | Kết quả phải kiểm chứng | Tiêu chí liên quan |
|---|---|---|
| Thu hồi quyền xem phòng đang mở | Lần đọc/gửi tiếp theo bị từ chối; không nhận thêm nội dung phòng qua kết nối cũ trong ≤5 giây cho chat theo DEC-083; media ≤5 giây theo DEC-099, cần proof riêng | AC-COM-11, AC-COM-13; DEC-083, OQ-007 |
| Thành viên thường rời cộng đồng | Không còn truy cập nội dung dành cho thành viên; giao diện rời phòng đang mở | AC-COM-21 |
| Mất quyền quản lý | Các thao tác quản lý tiếp theo bị kiểm tra lại phía máy chủ | ACL-09 đến ACL-12, ACL-16 |
| Mất kết nối rồi mở lại | Kiểm tra lại phiên, tư cách thành viên và quyền trước khi tải lịch sử hoặc đăng ký nhận tin | AC-ACC-04, AC-COM-13 |
| Thu hồi quyền một phòng | Không tự thu hồi quyền ở phòng khác hoặc DM độc lập | Suy ra từ phạm vi quyền phòng; cần kiểm thử tích hợp |

Không hứa xóa được nội dung người dùng đã nhìn thấy hoặc tự sao chép.
Việc ngừng hiển thị dữ liệu đã tải trong giao diện, cách xử lý bộ nhớ đệm
và độ trễ thu hồi cần được cụ thể hóa trong thiết kế và OQ-007/OQ-011.


Quy tắc có thẩm quyền là DEC-055–058: giữa vai trò có DENY thì DENY thắng, cá nhân áp dụng cuối; chủ sở hữu được xem sau khi thỏa điều kiện nền. Cách mã hóa bằng bit mask 64-bit từng được đề xuất là lựa chọn biểu diễn dữ liệu; nó không được thay thứ tự ưu tiên đã chốt. Không suy “toàn quyền tuyệt đối” thành quyền sửa/xóa tin của người khác hoặc đọc DM.

<a id="use-cases"></a>

## 3. Use case Community

Bổ sung ngày 2026-10-06. Các UC tổng hợp hành vi mục tiêu từ [quy tắc COM](#requirements), [ma trận ACL](#permissions) và các quyết định đã dẫn chiếu; phần use case là bản dự thảo để rà soát trước triển khai. Community/Messaging vẫn ở Foundation, toàn bộ UC-COM chưa có implementation hoặc kết quả kiểm thử sản phẩm. Thuật toán, lỗi, giao dịch và schema kỹ thuật tiếp tục được quản lý tại [thiết kế chi tiết](#detailed-design).

Community thực hiện quản lý server, membership, phòng, lời mời và quyền. UC-COM-17 đọc lịch sử qua Messaging; UC-COM-23/24 do Messaging thực hiện trên quyền Community; UC-COM-25 phối hợp Identity/Community/Messaging và WebClient. Chức năng vào phòng thoại/gọi/video thuộc [đặc tả media](voice-video.md), không được coi đã triển khai khi tạo được metadata phòng voice.

### Điều kiện và ngoại lệ dùng chung

- Mọi actor dùng ứng dụng phải có tài khoản active, email đã xác minh và phiên hợp lệ theo DEC-051. Thao tác quản lý còn đòi membership active và quyền đúng thao tác ở thời điểm thực hiện; owner cũng chịu điều kiện nền. Hệ thống lấy actor từ phiên, không nhận actor tùy ý từ client.
- Luồng chính giả định dữ liệu hợp lệ và phụ thuộc sẵn sàng. Validation, trạng thái tài nguyên và quyền đều kiểm tra phía server theo COM/ACL. Private server không được biết hoặc phòng không được xem trả 404 theo hợp đồng mục tiêu; thiếu quyền quản lý trong scope được biết trả 403. Không trả metadata/nội dung bị ẩn trong lỗi hoặc thông báo.
- Các thao tác có version dùng version đã tải; ACL dùng accessVersion, leave/gán role/ngoại lệ dùng đúng membershipId. Xung đột trả 409 và yêu cầu đọc lại hiện trạng, không tự đổi version/epoch rồi ghi đè.
- Không tự phát lại mutation sau refresh, timeout hoặc mất kết nối. Khi kết quả không rõ, tải lại trạng thái trước khi người dùng quyết định tiếp. Riêng thao tác tạo giữ clientOperationId và payload ban đầu cho lần thử lại chủ động; gửi tin giữ clientMessageId theo thiết kế DM. Cùng khóa khác payload hoặc tài nguyên đã deleted/terminal không tạo lại tài nguyên bằng khóa cũ.
- Khi transaction rollback, không để lại dữ liệu nghiệp vụ dở dang hoặc phát cập nhật như đã thành công. Khi không xác nhận được quyền, dừng thao tác/phát nội dung theo hợp đồng; lỗi phụ thuộc không trở thành quyền truy cập.
- Thu hồi chat đo từ commit, deadline ≤5 giây theo DEC-083; HTTP tiếp theo kiểm tra quyền hiện hành. Media có proof riêng theo DEC-099. Vòng đời nội dung, cleanup và bảo vệ sau restore theo [data-lifecycle.md](../data-lifecycle.md).

### Danh mục use case

| Use case | Mục tiêu | Màn hình/thành phần |
|---|---|---|
| [UC-COM-01](#uc-com-01) | Tạo cộng đồng | COM-S10 |
| [UC-COM-02](#uc-com-02) | Tìm và xem cộng đồng công khai | COM-S01/02 |
| [UC-COM-03](#uc-com-03) | Xem cộng đồng đang tham gia và tư cách của mình | COM-S03, danh sách cộng đồng |
| [UC-COM-04](#uc-com-04) | Sửa thông tin và visibility cộng đồng | COM-S10 |
| [UC-COM-05](#uc-com-05) | Đổi chế độ tham gia | COM-S07 |
| [UC-COM-06](#uc-com-06) | Tham gia cộng đồng công khai vào ngay | COM-S02 |
| [UC-COM-07](#uc-com-07) | Gửi, xem và hủy yêu cầu tham gia | COM-S02/06 |
| [UC-COM-08](#uc-com-08) | Duyệt hoặc từ chối yêu cầu tham gia | COM-S06 |
| [UC-COM-09](#uc-com-09) | Tạo, xem và sao chép link mời | COM-S05 |
| [UC-COM-10](#uc-com-10) | Thu hồi link mời | COM-S05 |
| [UC-COM-11](#uc-com-11) | Xem trước và tham gia bằng link mời | COM-S02 |
| [UC-COM-12](#uc-com-12) | Gửi, xem và hủy lời mời đích danh | COM-S11, quản lý lời mời |
| [UC-COM-13](#uc-com-13) | Xem, chấp nhận hoặc từ chối lời mời đích danh | COM-S11, inbox người nhận |
| [UC-COM-14](#uc-com-14) | Chuyển chủ sở hữu | COM-S12 |
| [UC-COM-15](#uc-com-15) | Rời cộng đồng | COM-S03, menu cộng đồng |
| [UC-COM-16](#uc-com-16) | Tạo phòng | COM-S04 |
| [UC-COM-17](#uc-com-17) | Xem phòng được phép và lịch sử tin văn bản | COM-S03 |
| [UC-COM-18](#uc-com-18) | Sửa thông tin phòng | Quản lý phòng từ COM-S03 |
| [UC-COM-19](#uc-com-19) | Xóa phòng | Quản lý phòng từ COM-S03 |
| [UC-COM-20](#uc-com-20) | Tạo, sửa và xóa vai trò tự tạo | COM-S08 |
| [UC-COM-21](#uc-com-21) | Gán hoặc thu hồi vai trò thành viên | COM-S08 |
| [UC-COM-22](#uc-com-22) | Xem và thay cấu hình quyền xem phòng | COM-S09 |
| [UC-COM-23](#uc-com-23) | Gửi và chủ động thử lại tin văn bản | COM-S03, composer; Messaging |
| [UC-COM-24](#uc-com-24) | Sửa hoặc xóa tin của mình | COM-S03, menu tin; Messaging |
| [UC-COM-25](#uc-com-25) | Nhận cập nhật, kết nối lại và xử lý mất quyền | COM-S03, Hub chat và thông báo theo người nhận |

<a id="use-case-rules"></a>

### Truy vết quy tắc cộng đồng

Giới hạn và thứ tự ưu tiên giữ ở bảng COM/ACL; bảng này chỉ xác định UC áp dụng. COM-009 và phần scope của COM-039 được ghi thành giới hạn, không tạo UC chức năng ngoài MVP.

| Quy tắc | Use case áp dụng | Điểm cần đối chiếu |
|---|---|---|
| COM-001 | UC-COM-02/06/07/11 | Tìm kiếm hoặc lời mời dẫn tới đúng nhánh tham gia |
| COM-002 | UC-COM-02/04 | Chỉ public xuất hiện trong tìm kiếm |
| COM-003 | UC-COM-01/05/06/07 | Mặc định vào ngay; approval tạo pending |
| COM-004 | UC-COM-11 | Link hợp lệ bỏ qua chờ duyệt |
| COM-005 | UC-COM-09 | Người tạo chọn hạn link |
| COM-006 | UC-COM-05 | Đúng quyền đổi join mode |
| COM-007 | UC-COM-16 | Đúng quyền tạo phòng |
| COM-008 | UC-COM-17/25 | Danh sách, nội dung và cập nhật đều theo view |
| COM-009 | UC-COM-23 | Composer chỉ gửi văn bản; file trong phòng ở đợt sau |
| COM-010 | UC-COM-09 | Đúng quyền tạo lời mời |
| COM-011 | UC-COM-08 | Đúng quyền duyệt |
| COM-012 | UC-COM-16/22 | Phòng mới mặc định cho mọi thành viên xem |
| COM-013 | UC-COM-23/24 | Gửi, sửa, xóa và trạng thái tương tự DM |
| COM-014 | UC-COM-17/25 | Tin lưu bền đọc lại khi còn quyền |
| COM-015 | UC-COM-17 | Thành viên mới được xem lịch sử cũ theo view |
| COM-016 | UC-COM-22 | Đúng quyền thay ACL |
| COM-017 | UC-COM-17/23 | Có view thì gửi text khi đủ điều kiện tài khoản |
| COM-018 | UC-COM-23 | Thử lại cùng thao tác không tạo tin trùng |
| COM-019 | UC-COM-11/12/13 | Private tham gia bằng lời mời hợp lệ |
| COM-020 | UC-COM-10/11 | Link thu hồi không cấp membership |
| COM-021 | UC-COM-15 | Thành viên thường tự rời |
| COM-022 | UC-COM-23 | Chưa xác minh không được dùng ứng dụng/gửi tin |
| COM-023 | UC-COM-24 | Sửa chỉ giữ nội dung hiện hành |
| COM-024 | UC-COM-23/24 | Validation nội dung dùng chung DM |
| COM-025 | UC-COM-20/21/22 | Vai trò quản lý và ngoại lệ view cá nhân |
| COM-026 | UC-COM-17/22 | Owner luôn view sau điều kiện nền |
| COM-027 | UC-COM-17/22/25 | Role deny thắng, cá nhân sau cùng |
| COM-028 | UC-COM-20/21 | Chỉ owner quản lý vai trò/assignment |
| COM-029 | UC-COM-01/04 | Tạo bởi account đã xác minh, metadata chỉ owner sửa |
| COM-030 | UC-COM-07/08 | Hủy/từ chối và gửi yêu cầu mới |
| COM-031 | UC-COM-12/13 | Nhận mời đích danh mới tạo membership |
| COM-032 | UC-COM-09/10/11 | Hạn/lượt và thu hồi link |
| COM-033 | UC-COM-14/15 | Chuyển ngay; owner phải chuyển trước rời |
| COM-034 | UC-COM-16/18/19 | Tạo/sửa/xóa phòng và vòng đời |
| COM-035 | UC-COM-06/07/11/13/15 | Rejoin dùng epoch mới và role mặc định |
| COM-036 | UC-COM-12/13 | Mời đích danh hết hạn hoặc terminal không accept được |
| COM-037 | UC-COM-01/20/21 | @everyone và giới hạn custom role, union management |
| COM-038 | UC-COM-02 | Search/UTF-16/khớp/phân trang |
| COM-039 | UC-COM-01/04/16/18/20 | Tên Unicode; xóa toàn server ngoài MVP |
| COM-040 | UC-COM-04/07/08 | Private switch kết thúc pending nguyên tử |
| COM-041 | UC-COM-09/10/11/12/13 | Hiệu lực mời độc lập creator; actor thao tác xét quyền hiện hành |
| COM-042 | UC-COM-16/17/18/19/22 | Quản lý phòng có sẵn cần view |

<a id="uc-com-01"></a>

### UC-COM-01 — Tạo cộng đồng

**Tác nhân:** Người dùng đủ điều kiện ứng dụng.

**Điều kiện trước:** Phiên hợp lệ; người dùng chưa cần là thành viên cộng đồng nào.

**Kích hoạt:** Người dùng chọn tạo cộng đồng tại COM-S10.

**Luồng chính:**

1. Người dùng nhập tên, mô tả tùy chọn và chọn public/private theo COM-029/039.
2. Hệ thống kiểm tra dữ liệu và quyền, tạo server, membership owner và @everyone trong cùng transaction; ghi khóa thao tác tạo theo thiết kế retry.
3. Sau commit, giao diện mở cộng đồng vừa tạo và tải thông tin/tư cách hiện hành.

**Ngoại lệ:** Dữ liệu không hợp lệ trả lỗi trường. Create đồng thời hoặc thử lại cùng khóa/payload chỉ có một server. Khi mất response, người dùng thử lại bằng khóa cũ; xung đột payload không tạo server khác. Không để lại server thiếu owner hoặc @everyone khi rollback.

**Kết quả sau cùng:** Người tạo là owner và thành viên active; public mới mặc định vào ngay. Use case tạo server không yêu cầu tự tạo phòng đầu tiên; tạo phòng thực hiện bằng UC-COM-16.

<a id="uc-com-02"></a>

### UC-COM-02 — Tìm và xem cộng đồng công khai

**Tác nhân:** Người dùng đủ điều kiện ứng dụng, gồm người chưa tham gia cộng đồng đích.

**Điều kiện trước:** Phiên hợp lệ; người dùng mở khu vực khám phá.

**Kích hoạt:** Người dùng tìm theo tên hoặc mở kết quả công khai tại COM-S01/02.

**Luồng chính:**

1. Người dùng nhập từ khóa; hệ thống kiểm tra và tìm literal theo COM-038, lọc public/active trên từng trang.
2. Giao diện hiển thị summary công khai, ưu tiên tên khớp đúng và cho tải trang tiếp theo.
3. Người dùng mở summary, xem join mode hiện hành rồi chọn UC-COM-06 hoặc UC-COM-07.

**Ngoại lệ:** Query/cursor không hợp lệ trả lỗi; không có kết quả hiển thị trạng thái rỗng. Server đã đổi private không xuất hiện từ cursor cũ, người ngoài mở trực tiếp nhận 404. Preview lời mời hợp lệ dùng UC-COM-11/13.

**Kết quả sau cùng:** Chỉ xem thông tin được công khai; chưa có membership, phòng hoặc nội dung tin.

<a id="uc-com-03"></a>

### UC-COM-03 — Xem cộng đồng đang tham gia và tư cách của mình

**Tác nhân:** Người dùng đủ điều kiện ứng dụng.

**Điều kiện trước:** Phiên hợp lệ; quyền đối với server đích được kiểm tra khi đọc.

**Kích hoạt:** Người dùng mở danh sách cộng đồng, chọn một server hoặc kiểm tra trạng thái sau thao tác có kết quả không rõ.

**Luồng chính:**

1. Hệ thống trả danh sách server người dùng đang là thành viên active, có phân trang.
2. Khi chọn server, hệ thống trả member detail, membershipId/version và quyền quản lý hiệu lực theo tư cách hiện hành.
3. Giao diện hiển thị thao tác phù hợp và tải các phòng qua UC-COM-17; khi cần đối soát join/leave, người dùng đọc tư cách của chính mình.

**Ngoại lệ:** Chưa tham gia server nào hiển thị danh sách rỗng. Sau leave, server không còn trong danh sách active nhưng người dùng vẫn đọc được membership đã left của chính mình theo contract. Quyền đọc status không cấp lại member detail/private content. Danh sách thay đổi được dedup ID và tải lại trang đầu.

**Kết quả sau cùng:** Giao diện có trạng thái tham gia và quyền hiện hành, không tạo hoặc phục hồi membership bằng thao tác đọc.

<a id="uc-com-04"></a>

### UC-COM-04 — Sửa thông tin và visibility cộng đồng

**Tác nhân:** Owner hiện hành.

**Điều kiện trước:** Điều kiện nền hợp lệ; đã tải server và version. Quyền owner kiểm tra lại khi ghi.

**Kích hoạt:** Owner mở COM-S10 và lưu tên, mô tả hoặc visibility mới.

**Luồng chính:**

1. Owner chỉnh các trường theo COM-029/039 và gửi kèm expectedVersion.
2. Hệ thống kiểm tra quyền/dữ liệu/version rồi cập nhật. Nếu public→private, đồng thời chuyển mọi join request còn pending sang cancelled với reason server_private theo COM-040.
3. Sau commit, trả server hiện hành; thông báo cho sender về request bị hủy và yêu cầu các client đủ quyền tải lại metadata.

**Ngoại lệ:** Actor mất ownership hoặc version cũ không được ghi đè. Approve và private switch tranh nhau có một thứ tự commit: membership đã tạo trước switch được giữ, request đã bị hủy không approve được. Private→public không tự mở lại request terminal.

**Kết quả sau cùng:** Metadata/visibility và trạng thái request nhất quán; thành viên hiện có và lời mời hợp lệ được giữ. Xóa toàn bộ server nằm ngoài MVP theo COM-039.

<a id="uc-com-05"></a>

### UC-COM-05 — Đổi chế độ tham gia

**Tác nhân:** Owner hoặc thành viên có manage_join_mode.

**Điều kiện trước:** Server/membership active, phiên hợp lệ; đã tải version và join mode.

**Kích hoạt:** Actor chọn vào ngay/chờ duyệt tại COM-S07.

**Luồng chính:**

1. Actor chọn join mode mới và gửi expectedVersion.
2. Hệ thống kiểm tra manage_join_mode/version và cập nhật server.
3. Sau commit, giao diện tải lại chế độ; lần join công khai tiếp theo xét mode hiện hành.

**Ngoại lệ:** Mất quyền hoặc version cũ từ chối thao tác. Quyền đổi join mode không cấp quyền sửa tên/mô tả/visibility. Đổi mode không tự duyệt request cũ; review thực hiện qua UC-COM-08 theo trạng thái pending.

**Kết quả sau cùng:** Join công khai áp dụng mode mới; link hợp lệ vẫn cho vào ngay theo COM-004. Server private vẫn yêu cầu đường tham gia bằng lời mời.

<a id="uc-com-06"></a>

### UC-COM-06 — Tham gia cộng đồng công khai vào ngay

**Tác nhân:** Người dùng đủ điều kiện ứng dụng chưa là thành viên active của server đích.

**Điều kiện trước:** Server public/active và join mode vào ngay tại thời điểm join.

**Kích hoạt:** Người dùng bấm Tham gia tại COM-S02.

**Luồng chính:**

1. Hệ thống kiểm tra lại visibility, join mode, actor và tư cách hiện hành dưới cùng transaction.
2. Tạo hoặc kích hoạt lại membership với membershipId mới; chỉ áp vai trò mặc định. Kết thúc pending cũ bằng reason joined_elsewhere theo thiết kế transition.
3. Sau commit, trả membership và mở các phòng được phép qua UC-COM-17.

**Ngoại lệ:** Join lặp/đồng thời của người đang active trả tư cách hiện hành, không nhân membership. Nếu mode đã đổi sang chờ duyệt thì xử lý UC-COM-07; nếu đã private thì đường join công khai bị chặn. Mất response được đối soát qua UC-COM-03 trước thao tác tiếp.

**Kết quả sau cùng:** Có một membership active; lần rejoin không phục hồi role/ngoại lệ cá nhân cũ. Lịch sử phòng đọc theo view hiện hành.

<a id="uc-com-07"></a>

### UC-COM-07 — Gửi, xem và hủy yêu cầu tham gia

**Tác nhân:** Người dùng đủ điều kiện ứng dụng chưa là thành viên active; chỉ sender quản lý yêu cầu của mình.

**Điều kiện trước:** Gửi mới vào server public/active có join mode chờ duyệt. Xem/hủy cần quyền đối với request của chính mình, không đòi đã là thành viên.

**Kích hoạt:** Người dùng gửi yêu cầu, mở trạng thái hoặc chọn hủy pending tại COM-S02/06.

**Luồng chính:**

1. Khi gửi yêu cầu, hệ thống kiểm tra join mode/visibility và trả request pending duy nhất của cặp server/user; nếu chưa có thì tạo mới.
2. Sender xem trạng thái mới nhất; pending chưa cấp quyền phòng. Nếu chưa từng có request thì trạng thái trả null.
3. Khi sender hủy, hệ thống kiểm tra expectedVersion và chuyển pending sang cancelled; giao diện hiển thị trạng thái cuối. Sau rejected/cancelled, sender có thể gửi request mới nếu server còn cho phép.

**Ngoại lệ:** Gửi lặp trả cùng pending. Hủy tranh approve/reject/private switch chỉ một transition thắng. Sau server chuyển private, sender vẫn đọc được cancelled/server_private, nhưng không gửi mới qua đường công khai. Join qua đường khác đóng pending với reason joined_elsewhere; UI hiển thị đường tham gia đó.

**Kết quả sau cùng:** Request và trạng thái của chính sender có thể theo dõi; pending/rejected/cancelled không tự tạo membership. Request mới có ID mới, không mở lại request terminal.

<a id="uc-com-08"></a>

### UC-COM-08 — Duyệt hoặc từ chối yêu cầu tham gia

**Tác nhân:** Owner hoặc thành viên có review_join_requests.

**Điều kiện trước:** Phiên/server/membership hợp lệ; request còn pending và actor còn quyền review tại thời điểm xử lý.

**Kích hoạt:** Reviewer mở COM-S06, chọn approve hoặc reject.

**Luồng chính:**

1. Hệ thống chỉ trả danh sách request cho reviewer đủ quyền.
2. Reviewer chọn request và gửi quyết định cùng expectedVersion; hệ thống đọc lại trạng thái và quyền.
3. Approve chuyển trạng thái và tạo/kích hoạt membership cùng transaction; reject chỉ chuyển request sang rejected. Sau commit, sender và reviewer đủ quyền nhận trạng thái mới.

**Ngoại lệ:** Request đã cancelled/rejected/resolved không chuyển lần nữa; xung đột tải lại trạng thái. Target phải đủ điều kiện khi approve. Nếu target đã joined bằng đường khác, đóng request theo joined_elsewhere và giữ membership hiện hành. Private switch và sender cancel không thể cùng thắng với approve.

**Kết quả sau cùng:** Approve có một membership active với role mặc định; reject không cấp quyền thành viên. Request cuối trạng thái được giữ để sender theo dõi.

<a id="uc-com-09"></a>

### UC-COM-09 — Tạo, xem và sao chép link mời

**Tác nhân:** Owner hoặc thành viên có manage_invites.

**Điều kiện trước:** Actor có phiên/membership hợp lệ và quyền mời hiện hành trong server.

**Kích hoạt:** Actor mở COM-S05 để tạo link hoặc sao chép lại link còn dùng được.

**Luồng chính:**

1. Actor chọn hạn/lượt theo COM-032; hệ thống kiểm tra quyền/dữ liệu và ghi invite cùng operation tạo.
2. Sau commit, trả metadata và URL mời; danh sách quản lý chỉ chứa metadata, không chứa token.
3. Khi cần sao chép lại, hệ thống kiểm tra quyền hiện hành và hiệu lực invite, đọc secret được bảo vệ rồi trả đúng URL theo thiết kế.

**Ngoại lệ:** Tạo lặp cùng khóa/payload không tạo link khác. Link hết hạn/thu hồi/hết lượt không được cấp lại URL sử dụng. Key thiếu trả lỗi phụ thuộc, không đổi token âm thầm. Creator rời/mất manage_invites không tự vô hiệu link, nhưng không còn quyền lấy link hoặc thu hồi chỉ vì từng tạo.

**Kết quả sau cùng:** Link đã tạo có hạn/lượt đã chọn, có thể chia sẻ để người nhận dùng UC-COM-11. Việc tạo/copy link chưa tạo membership và không cấp custom role qua link.

<a id="uc-com-10"></a>

### UC-COM-10 — Thu hồi link mời

**Tác nhân:** Owner hoặc thành viên hiện có manage_invites, không cần là creator.

**Điều kiện trước:** Phiên/membership/quyền hợp lệ; actor biết invite thuộc server và version đã tải.

**Kích hoạt:** Actor chọn thu hồi tại COM-S05.

**Luồng chính:**

1. Actor xác nhận invite cần thu hồi và gửi expectedVersion.
2. Hệ thống kiểm tra quyền/version, đánh dấu revoked và xử lý secret theo thiết kế trong transaction.
3. Sau commit, danh sách hiển thị invite đã thu hồi; preview/join tiếp theo không sử dụng được link đó.

**Ngoại lệ:** Version cũ hoặc actor đã mất quyền từ chối. Revoke tranh join tuân thứ tự commit: join đã commit trước được giữ; revoke thắng trước chặn join. Mất response đọc lại metadata, không tự phát lại mutation.

**Kết quả sau cùng:** Link không cấp membership mới. Thành viên đã tham gia bằng link không bị rời server vì thao tác thu hồi link.

<a id="uc-com-11"></a>

### UC-COM-11 — Xem trước và tham gia bằng link mời

**Tác nhân:** Người dùng đủ điều kiện ứng dụng đang giữ link mời.

**Điều kiện trước:** Phiên hợp lệ; hiệu lực token/server được kiểm tra riêng khi preview và khi join.

**Kích hoạt:** Người dùng mở link và chọn tham gia tại COM-S02.

**Luồng chính:**

1. Giao diện lấy token theo thiết kế link; preview kiểm tra hiệu lực và trả summary server cùng hạn/lượt còn lại, không trả phòng/tin.
2. Người dùng xác nhận join; hệ thống kiểm tra lại hạn/thu hồi/lượt dưới khóa, tạo/kích hoạt membership và tăng lượt trong cùng transaction.
3. Kết thúc pending join request hoặc mời đích danh đang chờ theo joined_elsewhere. Sau commit, mở server và phòng được phép, bỏ qua join mode chờ duyệt.

**Ngoại lệ:** Preview không tiêu lượt hoặc giữ chỗ. Token sai/hết hạn/thu hồi không cấp membership hoặc lộ nội dung private. Hai người tranh lượt cuối chỉ một người vào; rollback không tiêu lượt. Người đang active join bằng link còn hợp lệ không tiêu thêm lượt. Creator đã rời/mất quyền không làm link mất hiệu lực.

**Kết quả sau cùng:** Có một membership active với role mặc định; rejoin dùng epoch mới. Link không còn hợp lệ không trở thành cách phục hồi membership đã left.

<a id="uc-com-12"></a>

### UC-COM-12 — Gửi, xem và hủy lời mời đích danh

**Tác nhân:** Owner hoặc thành viên có manage_invites.

**Điều kiện trước:** Phiên/membership/quyền hợp lệ; tạo mời đích danh vào server private, recipient chưa là thành viên active.

**Kích hoạt:** Actor mở quản lý lời mời tại COM-S11, gửi mời hoặc hủy mời pending.

**Luồng chính:**

1. Actor chọn recipient theo ID; hệ thống kiểm tra điều kiện và tạo mời pending hạn 7 ngày, ghi operation tạo.
2. Actor xem danh sách mời theo quyền; recipient nhận thông báo để mở UC-COM-13. Chưa accept chưa tạo membership.
3. Khi actor có quyền hiện hành chọn hủy pending, hệ thống kiểm tra version/trạng thái rồi chuyển cancelled và thông báo đúng recipient sau commit.

**Ngoại lệ:** Recipient đã active nhận xung đột. Pending còn hạn được trả lại khi tạo lặp, không kéo dài hạn; pending quá hạn được kết thúc trước khi tạo mời mới theo thiết kế. Cancel tranh accept/reject chỉ một transition thắng. Lời mời không tự vô hiệu khi creator rời/mất quyền; creator đó không được hủy nếu mất manage_invites.

**Kết quả sau cùng:** Mời pending hoặc cancelled có trạng thái theo dõi được; tạo/hủy mời không cấp quyền đọc phòng. Trạng thái terminal của khóa tạo cũ không bị mở lại.

<a id="uc-com-13"></a>

### UC-COM-13 — Xem, chấp nhận hoặc từ chối lời mời đích danh

**Tác nhân:** Đúng recipient của lời mời.

**Điều kiện trước:** Recipient đủ điều kiện ứng dụng; đọc inbox của mình. Accept/reject kiểm tra pending còn hạn và server/actor hiện hành.

**Kích hoạt:** Recipient mở inbox/COM-S11 rồi chọn accept hoặc reject.

**Luồng chính:**

1. Hệ thống chỉ trả lời mời của recipient cùng summary server được phép biết; chưa nhận không tải phòng/tin.
2. Khi accept, kiểm tra recipient/expiry/trạng thái rồi chuyển accepted và tạo/kích hoạt membership với role mặc định cùng transaction, không cần reviewer duyệt thêm.
3. Khi reject, kiểm tra expectedVersion rồi chuyển rejected. Sau commit, giao diện hiển thị trạng thái cuối; accept mở phòng được xem qua UC-COM-17.

**Ngoại lệ:** Người khác nhận thay bị từ chối. Expired/cancelled/rejected không accept được; accept/reject/cancel đồng thời chỉ một transition thắng. Accept lặp chỉ trả membership còn đúng epoch đã tạo, không rejoin người đã rời. Joined bằng đường khác kết thúc pending mời theo joined_elsewhere. Creator mất quyền không ảnh hưởng hiệu lực mời còn hợp lệ.

**Kết quả sau cùng:** Accept có một membership active và mời accepted; reject/expiry không tạo membership. Recipient vẫn xem được trạng thái lời mời của mình theo contract.

<a id="uc-com-14"></a>

### UC-COM-14 — Chuyển chủ sở hữu

**Tác nhân:** Owner hiện hành; target là thành viên active/đã xác minh.

**Điều kiện trước:** Actor và target đủ điều kiện, target còn membership active; owner đã tải server version.

**Kích hoạt:** Owner chọn người nhận và xác nhận chuyển ngay tại COM-S12.

**Luồng chính:**

1. Owner chọn target theo ID từ roster được phép biết và gửi expectedVersion.
2. Hệ thống kiểm tra lại actor/target/membership/version dưới cùng transaction rồi đổi owner và phiên bản quyền.
3. Sau commit, trả server hiện hành; giao diện hai bên tải lại quyền. Chủ cũ vẫn là thành viên và có thể thực hiện UC-COM-15.

**Ngoại lệ:** Target đã rời hoặc không đủ điều kiện thì chuyển thất bại. Target leave và transfer được tuần tự hóa: leave trước chặn transfer, transfer trước biến target thành owner không được leave. Actor mất ownership/version cũ phải tải lại; không tự thử lại sau kết quả không rõ.

**Kết quả sau cùng:** Luôn đúng một owner, chuyển có hiệu lực ngay và không cần target accept. Chủ cũ giữ role hiện có, không tự được gán role quản lý để thay quyền owner.

<a id="uc-com-15"></a>

### UC-COM-15 — Rời cộng đồng

**Tác nhân:** Thành viên thường rời tư cách của chính mình.

**Điều kiện trước:** Phiên/tài khoản hợp lệ, có membershipId của lần tham gia muốn rời; actor hiện không phải owner.

**Kích hoạt:** Thành viên chọn rời và xác nhận từ COM-S03.

**Luồng chính:**

1. Hệ thống kiểm tra lại membershipId và tư cách owner hiện hành.
2. Chuyển membership sang left, dọn assignment/ngoại lệ epoch hiện tại và ghi thay đổi quyền trong cùng transaction.
3. Sau commit, chặn truy cập member content, thu hồi subscription theo UC-COM-25 và đóng vùng nội dung/draft của scope trên giao diện.

**Ngoại lệ:** Owner phải hoàn tất UC-COM-14 trước khi rời. Leave lặp cùng epoch đã left trả thành công; leave cũ sau rejoin nhận MEMBERSHIP_CHANGED, không rời epoch mới. HTTP/send và leave tranh nhau được xử lý bằng guard/giao dịch theo thiết kế.

**Kết quả sau cùng:** Mất quyền nội dung server, DM độc lập vẫn dùng được và tin đã viết được giữ. Muốn rejoin phải đi UC-COM-06/07/11/13 theo điều kiện hiện hành; dùng membershipId mới và role mặc định.

<a id="uc-com-16"></a>

### UC-COM-16 — Tạo phòng

**Tác nhân:** Owner hoặc thành viên có manage_channels.

**Điều kiện trước:** Server/membership/phiên hợp lệ; tạo mới chỉ cần manage_channels, không đòi xem một phòng khác.

**Kích hoạt:** Actor mở COM-S04, nhập tên/chủ đề và chọn kind text/voice.

**Luồng chính:**

1. Actor nhập dữ liệu theo COM-034/039 và gửi thao tác tạo.
2. Hệ thống kiểm tra quyền, tên unique trong server và kind; tạo metadata cùng chat space qua hợp đồng lifecycle trong cùng transaction theo thiết kế. Phòng mới mặc định mọi thành viên view.
3. Sau commit, trả phòng vừa tạo và tải lại danh sách theo quyền; voice gọi lifecycle Media theo thiết kế riêng khi tích hợp.

**Ngoại lệ:** Tên sai/trùng hoặc actor mất quyền không tạo phòng dở dang. Retry cùng khóa/payload không tạo thêm space/phòng. Phụ thuộc lifecycle không đáp ứng phải rollback; tạo metadata voice chưa chứng minh gọi/media hoạt động.

**Kết quả sau cùng:** Có một phòng active đúng kind; text có thể dùng UC-COM-17/23. Phòng read-only và text bên trong voice nằm ngoài hợp đồng MVP.

<a id="uc-com-17"></a>

### UC-COM-17 — Xem phòng được phép và lịch sử tin văn bản

**Tác nhân:** Thành viên active; owner vẫn phải thỏa điều kiện nền.

**Điều kiện trước:** Actor/server/membership hợp lệ; phòng còn active và actor có view tại mỗi lần đọc.

**Kích hoạt:** Thành viên chọn server, mở phòng hoặc tải lịch sử cũ tại COM-S03.

**Luồng chính:**

1. Community tính quyền theo thuật toán view và chỉ trả các phòng được xem trên từng trang.
2. Khi người dùng chọn phòng, kiểm tra lại quyền rồi trả metadata. Với text, Messaging tải lịch sử có phân trang qua guard Community.
3. Giao diện hiển thị tin hiện hành, dấu đã sửa/tombstone; thành viên mới được xem lịch sử cũ khi còn view. Voice chuyển sang hành trình Media.

**Ngoại lệ:** Không có phòng được xem hoặc chưa có tin hiển thị trạng thái rỗng. Hidden/deleted channel trả 404 kể cả biết ID hoặc có management permission; owner không vượt điều kiện nền. Tác giả cũ inactive không làm mất trang lịch sử của người đọc đủ quyền, projection theo thiết kế vòng đời dữ liệu. Mất view khi đang đọc chuyển UC-COM-25.

**Kết quả sau cùng:** Có danh sách/nội dung đúng quyền hiện hành; đọc lịch sử không phục hồi phòng đã deleted và không cấp quyền mới.

<a id="uc-com-18"></a>

### UC-COM-18 — Sửa thông tin phòng

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải metadata và version.

**Kích hoạt:** Actor sửa tên/chủ đề từ phần quản lý phòng.

**Luồng chính:**

1. Actor chỉnh tên/chủ đề theo COM-034/039 và gửi expectedVersion.
2. Hệ thống kiểm tra quyền quản lý cùng view, dữ liệu/version và tên unique rồi cập nhật metadata.
3. Sau commit, trả phòng hiện hành và thông báo tải lại thông tin cho các client đủ quyền.

**Ngoại lệ:** Hidden/deleted channel không lộ metadata; thiếu manage_channels trong phòng được biết từ chối. Tên trùng/version cũ không ghi đè, lỗi giữ form để người dùng sửa. Kind không thay đổi qua PATCH trong MVP; ACL dùng UC-COM-22.

**Kết quả sau cùng:** Metadata phòng được cập nhật; lịch sử/kind không bị đổi bởi thao tác sửa tên/chủ đề.

<a id="uc-com-19"></a>

### UC-COM-19 — Xóa phòng

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải version và được biết phòng.

**Kích hoạt:** Actor chọn xóa và xác nhận phòng cần xóa.

**Luồng chính:**

1. Hệ thống kiểm tra lại view/manage_channels/version.
2. Chuyển channel và Messaging space sang deleted, tăng version quyền và ghi sự kiện thu hồi cùng transaction qua hợp đồng lifecycle; voice phối hợp lifecycle Media theo thiết kế.
3. Sau commit, phòng biến mất khỏi danh sách, chặn đọc/gửi/join/resume; thu hồi kết nối và giao diện đóng nội dung theo UC-COM-25.

**Ngoại lệ:** Quyền/version cũ hoặc deleted không biến thành phòng mới. Delete tranh send/media admission tuân guard và thứ tự commit; nội dung đã commit trước vẫn giữ theo retention. Lỗi lifecycle rollback toàn bộ; proof cutoff media cần thực hiện riêng.

**Kết quả sau cùng:** Phòng không truy cập/khôi phục trong MVP, giữ ID và nội dung theo DEC-105. Thao tác này không xóa toàn server hoặc purge backup.

<a id="uc-com-20"></a>

### UC-COM-20 — Tạo, sửa và xóa vai trò tự tạo

**Tác nhân:** Chỉ owner hiện hành.

**Điều kiện trước:** Actor/server/membership hợp lệ; role sửa/xóa thuộc server và là custom role, đã tải version.

**Kích hoạt:** Owner mở COM-S08 để quản lý định nghĩa vai trò.

**Luồng chính:**

1. Owner đọc danh mục, nhập tên và chọn management permission của custom role theo COM-037.
2. Hệ thống kiểm tra owner/dữ liệu/giới hạn; tạo role theo operation key hoặc sửa role với expectedVersion.
3. Khi xóa, hệ thống bỏ role cùng assignment/role override, cập nhật phiên bản quyền trong cùng transaction; sau commit, tính lại quyền và thông báo tải lại.

**Ngoại lệ:** Chỉ một create thắng khi tranh slot thứ 20. Tên trùng, permission sai/trùng, version cũ hoặc actor mất ownership bị từ chối. @everyone không sửa/xóa/gán tay/cấp management. Xóa role deny có thể mở view, xóa role allow có thể mất view; phải tính lại kết quả thực.

**Kết quả sau cùng:** Danh mục custom role hợp lệ; management là hợp quyền allow, không có DENY quản lý/hierarchy vượt owner. Mất view do thay đổi role phải thu hồi theo UC-COM-25.

<a id="uc-com-21"></a>

### UC-COM-21 — Gán hoặc thu hồi vai trò thành viên

**Tác nhân:** Chỉ owner hiện hành.

**Điều kiện trước:** Actor/target đủ điều kiện liên quan; target còn membership active của server. Owner đã tải membershipId/version và tập role hiện tại.

**Kích hoạt:** Owner chọn thành viên tại COM-S08 và lưu tập custom role.

**Luồng chính:**

1. Owner đọc roster và tập vai trò của target; roster chỉ có user summary và membership, không có email.
2. Owner thêm/bỏ role rồi gửi toàn bộ tập custom role cùng membershipId/expectedVersion.
3. Hệ thống kiểm tra owner, epoch/version và role cùng server rồi thay assignment nguyên tử, cập nhật quyền; sau commit, target tải lại quyền/subscription theo UC-COM-25.

**Ngoại lệ:** Target đã leave/rejoin, role khác server/trùng hoặc system role không được gán. Actor có manage_channels/manage_channel_access vẫn không được thay assignment. Membership/version cũ không áp vào lần tham gia mới.

**Kết quả sau cùng:** Tập role của đúng epoch được lưu; @everyone tự áp theo membership active. Management permission không tự mở view của phòng bị ẩn.

<a id="uc-com-22"></a>

### UC-COM-22 — Xem và thay cấu hình quyền xem phòng

**Tác nhân:** Owner hoặc thành viên có manage_channel_access và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; actor đã tải ACL snapshot/accessVersion. Member override chỉ dùng membership active đúng epoch.

**Kích hoạt:** Actor mở COM-S09 và lưu mặc định phòng, role override hoặc member override.

**Luồng chính:**

1. Hệ thống kiểm tra quyền đọc ACL, trả cấu hình và danh mục role/member phục vụ chọn ngoại lệ trong scope được phép.
2. Actor chỉnh snapshot và gửi expectedAccessVersion; hệ thống kiểm tra view/manage_channel_access cùng role/member/epoch hợp lệ.
3. Thay snapshot nguyên tử, tăng accessVersion và tính lại view theo default → role deny thắng → cá nhân sau cùng; owner theo COM-026. Sau commit, thu hồi scope mất view theo UC-COM-25.

**Ngoại lệ:** Role/member duplicate/khác server/epoch cũ hoặc accessVersion cũ rollback toàn cấu hình. Management permission không cho đọc ACL của hidden channel. Actor tự mất view qua cấu hình hợp lệ vẫn nhận kết quả commit, nhưng request/subscription tiếp theo bị chặn. DENY owner không vượt quyền owner sau điều kiện nền.

**Kết quả sau cùng:** ACL nhất quán và áp dụng cho đọc/gửi/cập nhật; cấu hình ACL không cấp quyền quản lý role hoặc quyền sửa/xóa tin người khác.

<a id="uc-com-23"></a>

### UC-COM-23 — Gửi và chủ động thử lại tin văn bản

**Tác nhân:** Thành viên active có view phòng text, tài khoản đã xác minh và phiên hợp lệ.

**Điều kiện trước:** Phòng text active; điều kiện Identity/Community được giữ tới commit Messaging theo thiết kế guard.

**Kích hoạt:** Người dùng bấm gửi từ composer hoặc chủ động thử lại thao tác gửi chưa rõ kết quả.

**Luồng chính:**

1. Client giữ payload và clientMessageId của thao tác; Messaging chuẩn hóa/kiểm tra nội dung theo COM-024 và thiết kế DM.
2. Messaging kiểm tra quyền, xử lý operation chống trùng rồi lưu tin, sequence theo space và outbox trong cùng transaction.
3. Sau commit, trả tin hiện hành và hiển thị đã gửi. Khi người dùng thử lại cùng khóa/payload, trả cùng tin hiện hành, không tạo tin/outbox mới.

**Ngoại lệ:** Chưa xác minh/mất phiên/mất view/deleted/voice channel bị chặn. Cùng clientMessageId khác payload trả xung đột; thiếu fingerprint key dừng thao tác. Response mất giữ trạng thái chưa rõ/lỗi để người dùng đối soát hoặc thử lại chủ động. Revoke tranh send không chen giữa kiểm tra quyền và commit.

**Kết quả sau cùng:** Một tin cho mỗi thao tác hợp lệ, lưu bền trước trạng thái đã gửi. Nội dung và retry dùng cùng cơ chế DM; file và quyền chỉ đọc độc lập chưa thuộc tin phòng MVP.

<a id="uc-com-24"></a>

### UC-COM-24 — Sửa hoặc xóa tin của mình

**Tác nhân:** Tác giả tin, còn view phòng text và đủ điều kiện ứng dụng.

**Điều kiện trước:** Phòng active, tin thuộc phòng; tác giả đã tải message version. Quyền tác giả và view kiểm tra lại khi thực hiện.

**Kích hoạt:** Tác giả chọn sửa hoặc xóa từ menu tin tại COM-S03.

**Luồng chính:**

1. Khi sửa, tác giả gửi nội dung mới và expectedVersion; Messaging kiểm tra validation dùng chung DM và quyền tác giả.
2. Khi xóa, tác giả gửi expectedVersion; Messaging bỏ content và giữ tombstone/ID/sequence cùng operation chống trùng.
3. Thay đổi tin/version/outbox commit nguyên tử; client đủ quyền nhận nội dung hiện hành cùng dấu đã sửa hoặc dòng thay thế tin đã xóa.

**Ngoại lệ:** Owner/manager không sửa/xóa tin người khác. Tin đã xóa không được sửa, stale version hoặc mất view yêu cầu đọc lại/chặn thao tác. Sửa không hợp lệ giữ form; response không rõ không tự replay mutation. Nội dung cũ không được trả lại từ receipt của thao tác gửi.

**Kết quả sau cùng:** Sửa chỉ giữ nội dung mới nhất; xóa không làm mất khóa chống trùng hoặc phục hồi tin khi retry send. Lịch sử bản sửa cũ không được cung cấp.

<a id="uc-com-25"></a>

### UC-COM-25 — Nhận cập nhật, kết nối lại và xử lý mất quyền

**Tác nhân:** Người dùng đang sử dụng Community; client và hệ thống realtime hỗ trợ đồng bộ/thu hồi.

**Điều kiện trước:** Subscribe phòng cần actor/membership/view hiện hành. Thông báo request/invitation định tuyến theo đúng user/session và manager còn quyền, không đòi người ngoài subscribe server.

**Kích hoạt:** Mở phòng, có sự kiện đã commit, kết nối lại hoặc thay đổi quyền/phiên/membership/phòng.

**Luồng chính:**

1. Client subscribe phòng text qua Hub chat; server kiểm tra điều kiện và gắn subscription với session, server/channel, membershipId và accessVersion.
2. Dispatcher phát bản tin hiện hành cho connection còn quyền; thông báo CommunityChanged chỉ yêu cầu tải lại metadata/quyền. Request/invitation/membership gửi đúng đối tượng được biết.
3. Khi reconnect, kiểm tra quyền và subscribe lại rồi REST bù tin, tải lại trang cũ để nhận edit/delete; merge theo ID/version và chỉ tiến resume cursor sau khi merge đủ trang.
4. Khi quyền bị thu hồi, server tự gỡ subscription/chặn nội dung mới; client đóng nội dung/composer và dọn cache/draft của scope. Thu hồi chat được kiểm chứng trong ≤5 giây từ commit.

**Ngoại lệ:** Mất quyền/phiên không được reconnect vào scope cũ; kiểm tra quyền lỗi thì dừng phát. Sự kiện trùng/đảo thứ tự không nhân tin hoặc hạ version. Revoke membershipId cũ không xóa cache epoch rejoin mới. Client không hợp tác vẫn bị server ngừng phát; không broadcast roster/ACL/request/invitation cho toàn server.

**Kết quả sau cùng:** Client đủ quyền hội tụ về trạng thái đã lưu, scope mất quyền không tiếp tục nhận nội dung; DM và phòng khác vẫn theo quyền riêng. Cutoff media được kiểm chứng riêng theo đặc tả Media.

<a id="use-case-coverage"></a>

### Đối chiếu use case với API và kiểm thử

API trong bảng là hợp đồng mục tiêu tại [community.openapi.json](../contracts/community.openapi.json), chưa phải endpoint hoạt động. Các đường dẫn dùng prefix `/api/v1`; `{id}` là serverId, `{channelId}` là phòng đích. AC và TC dẫn tới [tiêu chí chấp nhận](#acceptance) và [ca kiểm thử](#tests) hiện có. Mọi UC-COM đang ở trạng thái **chưa triển khai/chưa chạy**; bảng là kế hoạch truy vết, không phải bằng chứng đạt.

| Use case | API/thành phần mục tiêu | AC-COM | TC hiện có và phụ thuộc |
|---|---|---|---|
| UC-COM-01 | POST /servers | 29, 36, 38 | TC-COM-02/19/23/26; account guard, server/owner/@everyone/operation nguyên tử |
| UC-COM-02 | GET /servers/search; GET /servers/{id} | 01, 19, 37 | TC-COM-01/18; search key/cursor/visibility |
| UC-COM-03 | GET /servers; GET /servers/{id}; GET /servers/{id}/membership/me | 06, 19, 21, 35 | TC-COM-11/16/22; cần bổ sung assertion list/detail/own status sau left |
| UC-COM-04 | PATCH /servers/{id} | 29, 38, 39, 40 | TC-COM-02/19/20/27; private switch tranh approve/cancel |
| UC-COM-05 | PATCH /servers/{id}/join-mode | 08 | TC-COM-07; quyền/version và ảnh hưởng request pending cần assertion riêng |
| UC-COM-06 | POST /servers/{id}/join → membership | 02, 35 | TC-COM-03/16/22; membership epoch/unique/role mặc định |
| UC-COM-07 | POST /servers/{id}/join → pending; GET .../join-requests/me; DELETE .../join-requests/{requestId} | 03, 30, 40 | TC-COM-04/05/20; pending unique/transition |
| UC-COM-08 | GET .../join-requests; POST .../{requestId}/approve hoặc /reject | 10, 30, 40 | TC-COM-04/05/20; guard actor/target và membership cùng commit |
| UC-COM-09 | GET/POST .../invites; GET .../invites/{inviteId}/link | 09, 32, 41 | TC-COM-07/21/23/24; secret/key ring/operation |
| UC-COM-10 | DELETE .../invites/{inviteId} | 20, 32, 41 | TC-COM-06/07/21/24; revoke tranh join |
| UC-COM-11 | POST /invites/preview; POST /invites/join | 04, 05, 19, 20, 32, 35, 41 | TC-COM-06/13/16/21/24; lượt cuối/rollback, pending joined_elsewhere |
| UC-COM-12 | GET/POST .../member-invitations; DELETE .../{invitationId} | 19, 31, 35, 41 | TC-COM-08/17/21/23; unique pending/expiry/operation |
| UC-COM-13 | GET /member-invitations; POST .../{invitationId}/accept hoặc /reject | 19, 31, 35, 41 | TC-COM-08/17/21; đúng recipient/terminal/epoch |
| UC-COM-14 | POST /servers/{id}/ownership-transfer; GET .../members | 33 | TC-COM-14; Identity→server→membership lock order |
| UC-COM-15 | DELETE /servers/{id}/members/me | 21, 35 | TC-COM-11/14/16/22; epoch/clear role/override/revocation |
| UC-COM-16 | POST /servers/{id}/channels | 07, 11, 38, 42 | TC-ACL-01/06/08; TC-COM-19/23/26/27; lifecycle Messaging/Media |
| UC-COM-17 | GET .../channels; GET .../channels/{channelId}; GET .../messages | 06, 11, 13, 14, 15, 25, 26, 27, 42 | TC-ACL-01–05/08/12; TC-COM-09/12/25/27; lịch sử theo guard/projection |
| UC-COM-18 | PATCH .../channels/{channelId} | 34, 38, 42 | TC-ACL-12; TC-COM-19; cần assertion sửa tên/topic/version riêng |
| UC-COM-19 | DELETE .../channels/{channelId} | 34, 39, 42 | TC-ACL-12; TC-COM-15/25/27; lifecycle/retention, cutoff media riêng |
| UC-COM-20 | GET/POST .../roles; PATCH/DELETE .../roles/{roleId} | 28, 36, 38 | TC-ACL-06/08/09/11; TC-COM-19/23/26; limit/system role/version |
| UC-COM-21 | GET .../members; GET/PUT .../members/{userId}/roles | 28, 35, 36 | TC-ACL-06/08; TC-COM-16/22/25; đúng epoch/union quyền |
| UC-COM-22 | GET/PUT .../channels/{channelId}/access; GET .../roles; GET .../members | 11, 16, 25, 26, 27, 42 | TC-ACL-02–05/08/10/12; TC-COM-22/25; ACL snapshot/guard |
| UC-COM-23 | POST .../channels/{channelId}/messages | 12, 17, 18, 22, 24 | TC-COM-10/12/25/27; TC-TEXT ở DM, HMAC/sequence/outbox |
| UC-COM-24 | PATCH/DELETE .../messages/{messageId} | 12, 13, 23, 24 | TC-COM-10; TC-TEXT ở DM, tác giả/version/tombstone |
| UC-COM-25 | /hubs/chat; REST bù lịch sử và đọc lại resource | 06, 11, 13, 14, 18, 21, 25, 26, 34, 35, 42 | TC-ACL-07/11; TC-COM-09/11/15/22/25; cần proof dispatch/reconnect/notification routing và DEC-083 |

42 AC-COM và 42 quy tắc COM đều được truy vết; COM-009/039 có phần giới hạn scope. Các TC hiện có cần bổ sung assertion cho từng endpoint/nhánh khi triển khai, đặc biệt UC-COM-03/05/18 và định tuyến thông báo UC-COM-25. TC-COM-23/26 cùng fixtures operation/quyền kiểm chứng cơ chế dùng chung; fixture khớp không chứng minh UC/API đã đạt. Phương pháp ghi kết quả và đo tải/thu hồi theo [nghiệm thu](../release-operations.md#testing).

<a id="use-case-delivery"></a>

### Thứ tự rà soát và triển khai theo use case

Áp dụng [quy trình dự án](../project.md#process) cho từng nhóm UC: rà soát luồng/ngoại lệ và AC/TC → đối chiếu UX, API, dữ liệu/giao dịch → xác định phụ thuộc và gói việc → triển khai cùng kiểm thử → tích hợp và ghi bằng chứng. Các bước được lặp theo nhóm chức năng; không đợi hoàn tất mọi module mới kiểm thử luồng đầu tiên.

| Nhóm | Use case | Đầu vào kỹ thuật cần có | Đầu ra cần kiểm chứng |
|---|---|---|---|
| 1. Server và tư cách | UC-COM-01–04/06 | Account guard, shared transaction, migration server/membership/@everyone/version/operation và search | Tạo nguyên tử, join trực tiếp, list/detail đúng quyền và tên Unicode; private switch và đóng pending của UC-COM-04/06 hoàn thiện cùng request ở nhóm 3 |
| 2. Vai trò và phòng | UC-COM-16–22 | Permission evaluator/catalog, migration role/ACL/epoch, channel guard và Messaging lifecycle | Role limit, assignment/ACL nguyên tử, hidden channel, create/delete và race quyền; voice cần Media lifecycle riêng |
| 3. Tham gia và lời mời | UC-COM-05–15 | Role/quyền từ nhóm 2, request/invitation migration, token protection/key ring, guard actor/target | Pending/approve/cancel/private switch, lượt cuối, accept/expiry, transfer/leave/rejoin và retry |
| 4. Tin phòng và cập nhật | UC-COM-17/23–25 | Messaging writer/history dùng chung DM, outbox/Hub/registry/revoker và frontend API | Lưu bền/chống trùng/tác giả, reconnect/routing, thu hồi chat ≤5 giây, scope cache đúng epoch |

Các nhóm gồm API và trạng thái frontend tương ứng, có kiểm thử quyền/đồng thời ngay trong gói. Hiện các guard/lifecycle/shared transaction và migrations còn cần triển khai theo COM-SQL-01–10; scope room deleted/restore/media và các đầu vào chưa chốt tiếp tục được theo dõi ở [vấn đề còn mở](#gaps), không đánh dấu đã nghiệm thu từ danh mục UC.

<a id="ux"></a>

## 4. Giao diện và trạng thái

### Khám phá và tham gia

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

### Cộng đồng và phòng văn bản

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
sửa/xóa, lỗi và chủ động thử lại giống [wireframe DM](direct-messaging.md#ux).

Trạng thái riêng: chưa có phòng được xem, phòng chưa có tin, tải lỗi,
lời mời không dùng được, yêu cầu chờ duyệt, mất quyền khi đang mở và
rời cộng đồng. Sau mất quyền/rời, đóng vùng nội dung phòng và tải lại
danh sách theo quyền; không tiếp tục gửi hoặc nhận tin qua kết nối cũ.

### Quản lý theo quyền

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S04 Tạo phòng | Chủ sở hữu/người có quyền tạo phòng | Tên 1–100/chủ đề tối đa 1.000; mặc định mọi thành viên xem; lưu lỗi giữ dữ liệu, tạo thành công về phòng mới |
| COM-S05 Lời mời | Chủ sở hữu/người có quyền tạo lời mời | Tạo, chọn 1 giờ/1 ngày/7 ngày/không hết hạn, đặt lượt nguyên dương hoặc không giới hạn, sao chép, xem và thu hồi |
| COM-S06 Yêu cầu tham gia | Chủ sở hữu/người có quyền duyệt | Danh sách và thao tác duyệt/từ chối; người gửi xem trạng thái/hủy pending; sau từ chối/hủy được gửi mới |
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

### Trạng thái theo quyết định mới

COM-S10 Tạo/sửa cộng đồng theo DEC-072: thu tên/mô tả/công khai; chủ sở hữu mới được sửa. COM-S11 Lời mời đích danh theo DEC-074: người nhận xem đúng cộng đồng, chấp nhận hoặc từ chối; người mời được hủy khi còn chờ theo DEC-087; chưa nhận không tải phòng/tin. COM-S12 Chuyển chủ sở hữu: chọn thành viên active/đã xác minh, xác nhận chuyển ngay; chủ cũ vẫn là thành viên và được rời sau đó. Phòng bị xóa đóng vùng nội dung và cuộc gọi theo DEC-077. Bộ đếm tên/mô tả/chủ đề dùng UTF-16 theo DEC-092/093 để thống nhất API; giới hạn và validation theo COM-029/034/037–039.

Đề xuất màn hình hẹp dùng lần lượt danh sách cộng đồng → danh sách
phòng → hội thoại; quản lý mở thành trang riêng. DEC-082 đã chốt desktop
Chrome/Edge/Firefox/Safari và Chrome Android/Safari iOS cho Community;
phiên bản/OS/thiết bị/build và bằng chứng khả dụng còn cần khóa tại OQ-007.
Media MVP cam kết trên desktop, điện thoại ở đợt sau.

Vg rà soát wireframe; Thái dựng prototype và trạng thái; Sáng đối chiếu
quyền/API. Chưa có prototype, kết quả rà soát hoặc kiểm thử khả dụng
được ghi nhận trong tài liệu này.

### Luồng yêu cầu tham gia và mời đích danh

Thiết kế trạng thái để thực hiện DEC-073/074/087; giao dịch/schema chưa triển khai:

| Đối tượng/trạng thái | Thao tác | Kết quả và quyền |
|---|---|---|
| Chưa có yêu cầu | Gửi vào cộng đồng công khai chờ duyệt | Pending; chưa có membership/quyền phòng |
| Pending | Người có quyền duyệt chấp nhận | Approved và tạo membership cùng transaction |
| Pending | Người có quyền duyệt từ chối | Rejected; không tạo membership |
| Pending | Chính người gửi hủy | Cancelled; không tạo membership |
| Rejected/Cancelled | Người dùng gửi lại | Tạo yêu cầu mới, không phục hồi pending cũ |
| Mời đích danh pending | Đúng người nhận chấp nhận, lời mời còn hợp lệ | Accepted và tạo membership cùng transaction; không thêm bước duyệt |
| Mời đích danh pending | Người nhận từ chối / người có quyền tạo mời hủy / đủ 7 ngày | Rejected / Cancelled / Expired; không tạo membership, không chấp nhận sau trạng thái cuối |

Máy chủ kiểm tra quyền ở thời điểm commit. Thiết kế unique một pending mỗi cặp user/cộng đồng, unique membership và cập nhật có điều kiện để hai thao tác duyệt/hủy không cùng thắng. Gọi lặp join/accept của người đã là thành viên trả tư cách hiện hành, không nhân đôi. Dùng lời mời link hợp lệ khi đang pending join phải kết thúc pending trong cùng giao dịch tạo membership; chi tiết lịch sử và retention thuộc OQ-011. Link có hạn/lượt theo DEC-075; mời đích danh hạn 7 ngày theo DEC-087. Khi link hết lượt, request join mới bị từ chối; người đã là thành viên không tiêu tốn lượt mới.

<a id="contracts"></a>

## 5. Thiết kế dữ liệu và API

Repo có schema `community`/`messaging` và seed, nhưng chưa có controller/service hoặc Swagger runtime nghiệp vụ Community. OpenAPI mục tiêu và thiết kế dưới đây đã được bổ sung; wireframe/schema không tự chứng minh endpoint hoạt động.

| Nhóm | Dữ liệu/hành vi tối thiểu | Phụ thuộc |
|---|---|---|
| Cộng đồng | Tạo/sửa, công khai/riêng tư, chủ sở hữu, vòng đời | DEC-072/094/096; chưa có xóa toàn bộ cộng đồng trong MVP |
| Tìm/tham gia | Chỉ tìm công khai, vào ngay/chờ duyệt, trạng thái yêu cầu và tư cách thành viên | COM-001–004, DEC-073; cần transaction chống request lặp |
| Lời mời | Tạo, thời hạn, kiểm tra, tiêu thụ và thu hồi; ghi thành viên nhất quán khi gọi lặp | COM-005/010/020/032; cần tiêu thụ lượt nhất quán |
| Phòng | Tạo, danh sách theo quyền, chủ đề và cấu hình xem | COM-007/008/012; trường/quyền theo DEC-077; kiểm tra xóa và thu hồi đồng thời |
| Vai trò/quyền | Chủ sở hữu quản lý vai trò; mặc định, kế thừa/cho phép/từ chối, ngoại lệ cá nhân và version cấu hình | DEC-055–058, ACL-O1/O2 |
| Tin phòng | Lưu/lịch sử/sửa/xóa/thử lại; chỉ người có quyền, tác giả sửa/xóa | COM-013–018/022–024; tham chiếu cơ chế tin DM |
| Thu hồi | Mất quyền hoặc rời phải chặn lần đọc/gửi tiếp và kết nối cập nhật theo ngưỡng đã chốt | ACL-O5, OQ-007 |

### Hợp đồng HTTP đề xuất

Prefix `/api/v1`; chưa có endpoint nghiệp vụ Community chạy được. Actor lấy từ phiên, ID server UUIDv7 theo DEC-081; time UTC; request cập nhật dùng `expectedVersion` để tránh ghi đè. POST tạo server/channel/role/link/mời đích danh thêm `clientOperationId` UUIDv4; field đầy đủ ở OpenAPI. `ServerSummary` công khai gồm `id,name,description,visibility,joinMode,version`; chỉ thành viên được nhận chi tiết phòng/thành viên theo quyền.

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers` | `{name,description?,visibility}` | 201 server; account active/verified; tạo server và membership owner cùng transaction |
| `PATCH /servers/{id}` | `{name?,description?,visibility?,expectedVersion}` | 200 server; chỉ owner; private không xuất hiện trong tìm kiếm |
| `GET /servers/search` | `q,cursor,limit` | 200 trang server công khai; q/khớp/phân trang theo DEC-093; không trả nội dung/phòng riêng tư |
| `POST /servers/{id}/join` | Không có body | 200 membership nếu vào ngay; 202 yêu cầu pending nếu chờ duyệt; không join server private từ tìm kiếm |
| `GET /servers/{id}/join-requests/me` | Phiên người yêu cầu | 200 `{request: JoinRequest|null}`; chưa có trả null; không trả request người khác |
| `POST /servers/{id}/join-requests/{requestId}/approve` hoặc `/reject` | `{expectedVersion}` | 200 trạng thái cuối; đúng quyền duyệt; chỉ transition từ pending |
| `DELETE /servers/{id}/join-requests/{requestId}` | `expectedVersion` | 200 cancelled; chỉ chính người gửi hủy pending |
| `PATCH /servers/{id}/join-mode` | `{joinMode,expectedVersion}` | 200 server; đúng quyền đổi chế độ, không vô hiệu nguyên tắc link cho vào ngay |
| `POST /servers/{id}/invites` | `{expiresInSeconds,maxUses?}` | 201 metadata/link; expiresInSeconds là 3600/86400/604800/null, bỏ trường dùng mặc định 604800; quyền tạo mời |
| `DELETE /servers/{id}/invites/{inviteId}` | ID link và expectedVersion | 204 thu hồi; đúng quyền, join sau thu hồi bị từ chối |
| `POST /invites/preview` | `{token}` | 200 summary khi link hợp lệ; không tạo membership/tiêu lượt; không trả phòng/tin |
| `POST /invites/join` | `{token}` | 200 membership; token trong body, kiểm tra hạn/thu hồi/lượt dưới khóa; không ghi body/token vào log |
| `POST /servers/{id}/member-invitations` | `{recipientUserId}` | 201 mời pending; owner/quyền tạo mời; chưa tạo membership |
| `POST /member-invitations/{invitationId}/accept` | Không có body | 200 membership; đúng người nhận, mời còn hợp lệ; không thêm bước duyệt |
| `POST /member-invitations/{invitationId}/reject` | `{expectedVersion}` | 200 rejected; đúng người nhận; chỉ pending còn hạn |
| `DELETE /servers/{id}/member-invitations/{invitationId}` | `expectedVersion` | 200 cancelled; quyền tạo mời; chỉ pending |
| `POST /servers/{id}/ownership-transfer` | `{newOwnerUserId,expectedVersion}` | 200 server; owner hiện tại, target là thành viên active/verified; chuyển ngay, không tạo pending |
| `DELETE /servers/{id}/members/me?membershipId=...` | Epoch đang tham gia | 204 tự rời; owner chưa chuyển nhận 409; epoch cũ không làm rời epoch mới; chặn HTTP/realtime/media theo ngưỡng |
| `POST /servers/{id}/channels` | `{name,topic?,kind}` | 201 phòng text/voice; đúng quyền quản lý phòng; mặc định mọi thành viên xem |
| `PATCH /servers/{id}/channels/{channelId}` | `{name?,topic?,expectedVersion}` | 200 phòng; đúng quyền quản lý phòng |
| `DELETE /servers/{id}/channels/{channelId}` | `expectedVersion` | 204 xóa logic, dừng truy cập/kết nối; không tự xóa vật lý tin/backup |
| `GET /servers/{id}/channels` | Phiên/thành viên | 200 chỉ phòng được xem; phòng deleted không được trả |

Danh mục quyền quản lý tối thiểu: quản lý phòng, tạo/thu hồi mời, duyệt/từ chối yêu cầu, đổi join mode và cấu hình quyền xem phòng. Chỉ owner quản lý vai trò/chuyển ownership/sửa metadata server. @everyone không có quyền quản lý, tối đa 20 vai trò tự tạo theo DEC-092. Tên mã permission và phiên bản cấu hình cụ thể hóa bên dưới; không thêm quyền xóa tin người khác.

Unique membership `(serverId,userId)`, unique pending request và cập nhật có điều kiện bảo vệ join/approve/accept lặp. Chuyển owner khóa server và hai membership; target phải vẫn là thành viên active/verified tại commit. Mời có giới hạn khóa record, tạo membership và tăng lượt cùng transaction; rollback không tiêu lượt. Xóa phòng kiểm tra lại quyền và version, chuyển trạng thái deleted rồi phát sự kiện thu hồi sau commit; giữ tombstone để API cũ không phục hồi phòng.

Lỗi đề xuất: validation 400; thiếu phiên 401; trái quyền quản lý 403; tài nguyên không được biết 404; request/version/owner conflict 409; vượt limiter 429; phụ thuộc tạm lỗi 503. [OpenAPI cộng đồng](../contracts/community.openapi.json) bổ sung schema, role/access API và tin phòng; vẫn là hợp đồng mục tiêu, chưa có endpoint chạy được.

Mỗi endpoint/sự kiện cần schema request/response, actor/quyền, lỗi theo [ProblemDetails](../architecture.md#contracts), giao dịch, version và retry. Community sở hữu metadata/thành viên/quyền; Messaging sở hữu tin. Messaging kiểm tra quyền qua `IChannelAccessChecker`, không đọc/JOIN schema Community.

Cơ chế tin dùng chung được thiết kế tại [DM](direct-messaging.md#contracts). Dữ liệu biên TC-TEXT áp dụng cả gửi/sửa tin phòng; không chép một phiên bản quy tắc ký tự hoặc chống trùng khác ở đây.

<a id="detailed-design"></a>

### Thiết kế chi tiết cộng đồng và phân quyền

Phương án ngày 2026-10-04; chưa sửa source/SQL hoặc có mock/proof. [OpenAPI](../contracts/community.openapi.json), [schema realtime](../contracts/community-realtime.schema.json), [fixture quyền](../fixtures/community-permissions.json) và [fixture fingerprint](../fixtures/community-operations.json) là artefact bàn giao thiết kế. Các lựa chọn sản phẩm đã xác nhận dẫn tới DEC; những thuật toán/interface/migration dưới đây còn cần rà soát kỹ thuật.

#### Danh tính, tên và phiên bản

Server/channel/role/request/invitation dùng UUIDv7; actor lấy từ phiên. Tên được trim, kiểm tra Unicode hợp lệ, độ dài UTF-16 và không chỉ trắng/vô hình theo bảng [text-policy](../fixtures/text-policy.json). Giữ cách viết/emoji của tên; không dùng tên làm định danh. Khóa so sánh đề xuất là NFC + ToLowerInvariant của tên đã trim, so sánh ordinal/DB collation cố định; tên cộng đồng không có unique constraint, tên phòng/vai trò unique trong server theo DEC-095. Nội dung tin vẫn không NFC/trim.

Giới hạn sau trim: server 2–100, channel 1–100, role 1–64 UTF-16; description/topic tối đa 1.000 UTF-16 theo DEC-072/077/093. Đề xuất tên là một dòng, không control/NUL; description/topic là văn bản thuần, rỗng thành null. Validation client/server dùng cùng thứ tự chuẩn hóa. `slug` hiện bắt buộc trong SQL chỉ là cột nội bộ: writer có thể sinh từ UUID, không thêm trường slug do người dùng nhập.

`version` của server/channel/role/membership/request/invitation truyền chuỗi số nguyên dương. Server có `accessVersion` tăng khi membership, role/assignment, ACL hoặc trạng thái truy cập đổi. Channel có `accessVersion` riêng cho cấu hình xem; PUT ACL dùng expectedAccessVersion, không lẫn version metadata. Client gửi đúng version đã tải, 409 thì đọc lại; không tự thay version rồi ghi đè.

Membership giữ unique `(serverId,userId)` nhưng mỗi lần tham gia tạo `membershipId` UUIDv7 mới và tăng version; trạng thái chỉ active/left thuộc MVP. Gán role/ngoại lệ cá nhân gắn membershipId để bản cấu hình cũ không áp nhầm khi người dùng rời rồi vào lại. Kicked/banned/timeout/nickname trong schema chưa là chức năng MVP được đặc tả.

#### Vai trò và cấu hình quyền

| Mã quyền kỹ thuật | Thao tác được cấp | Không tự cấp |
|---|---|---|
| `manage_channels` | Tạo/sửa/xóa phòng | Quyền xem tin của mọi phòng, đổi ACL hoặc quản lý role |
| `manage_invites` | Tạo/thu hồi link và gửi/hủy mời đích danh | Duyệt yêu cầu hoặc gán role |
| `review_join_requests` | Xem danh sách pending, duyệt/từ chối | Tạo mời hoặc quản lý role |
| `manage_join_mode` | Đổi vào ngay/chờ duyệt | Đổi visibility/name/description |
| `manage_channel_access` | Đọc/sửa cấu hình xem phòng và chọn thành viên cho ngoại lệ | Quyền đọc tin bị giới hạn hoặc quản lý role |

Owner có các quyền quản lý trên và các thao tác riêng ACL-19/20/chuyển ownership sau kiểm tra nền; không có quyền sửa/xóa tin người khác. @everyone là role hệ thống duy nhất của server, tự áp cho mọi membership active, không cần insert member_roles cho từng người. Không đổi tên/xóa/gán tay role này hoặc cấp management permission. Role tự tạo có tập permission cho phép; effective management là hợp tập này, không phụ thuộc role position và không có hierarchy vượt owner. Giới hạn 20 role tự tạo kiểm tra dưới khóa server, không tính @everyone.

API đề xuất:

| Route bổ sung | Quyền và kết quả |
|---|---|
| `GET /servers/{id}/roles` | Owner/manage_channel_access đọc danh mục để chọn role; chỉ owner được thay role/assignment |
| `POST /servers/{id}/roles` | Owner; `{clientOperationId,name,permissions}`; 201 role |
| `PATCH /servers/{id}/roles/{roleId}` | Owner; `{name?,permissions?,expectedVersion}`; 200 role; system role không sửa |
| `DELETE /servers/{id}/roles/{roleId}?expectedVersion=...` | Owner; xóa custom role và assignment/override cùng transaction; 204 |
| `GET /servers/{id}/members` | Owner hoặc manage_channel_access; trang user summary/membership để chọn role/ngoại lệ, không có email |
| `GET /servers/{id}/members/{userId}/roles` | Owner; danh sách custom role, membershipId/version |
| `PUT /servers/{id}/members/{userId}/roles` | Owner; `{membershipId,expectedVersion,roleIds}` thay tập custom role đầy đủ, không gán role server khác |
| `GET /servers/{id}/channels/{channelId}/access` | Owner/manage_channel_access và còn quyền xem phòng; base + override/accessVersion, không trả tin |
| `PUT /servers/{id}/channels/{channelId}/access` | Cùng điều kiện; thay cấu hình xem có expectedAccessVersion; một lần cập nhật nguyên tử |

ACL snapshot có `defaultView: allow|deny`, `roleOverrides:[{roleId,effect:allow|deny}]`, `memberOverrides:[{userId,membershipId,effect:allow|deny}]`. Không có entry nghĩa inherit; không dùng số bit mask trên wire. @everyone tham gia bước role như mọi role khác; role/user duplicate hoặc thuộc server khác nhận 400. Snapshot chỉ nhận membership active đúng epoch. Owner luôn xem được theo DEC-056; UI không diễn giải override deny owner thành thu hồi quyền owner.

Xóa custom role bỏ role assignment/role override và tăng accessVersion; kết quả có thể mở hoặc đóng quyền xem tùy role đã allow/deny, phải tính lại thay vì giả mọi xóa role là thu hồi. Rời server xóa assignment/ngoại lệ của epoch hiện tại; rejoin chỉ @everyone theo DEC-087. Chuyển owner không tự gán role quản lý cho chủ cũ; quyền chủ cũ trở lại theo role hiện có.

#### Giao dịch và thu hồi

Thứ tự khóa: Identity user rows theo UUID network order → server → membership/role/request/invite/channel → Messaging space/message/operation → outbox. Lookup ban đầu chỉ định tuyến; đọc lại trạng thái/permission sau khi có khóa. `IAccountAccessGuard` đã đề xuất ở DM kiểm tra actor session và tài khoản target khi cần; không JOIN Identity từ Community.

Phương án MVP tuần tự hóa mutation Community bằng `FOR UPDATE` trên server. `IChannelAccessGuard` mới giữ `FOR SHARE` trên server và channel tới commit Messaging, nên thay role/ACL/rời/xóa không chen giữa kiểm tra và lưu tin. Cơ chế [row lock PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS) hỗ trợ xung đột share/write này; đây là suy luận thiết kế cần proof, không là kết quả đo. `IChannelAccessChecker.CheckAsync` hiện chỉ trả bool, không giữ transaction/lease nên chưa đủ chống race.

- Tạo server: Identity guard → server + owner membership + @everyone + operation + outbox một transaction; không có khoảng thời gian server thiếu owner. Hai create cùng clientOperationId chưa có server để khóa: unique operation làm một transaction thắng; bên thua rollback toàn bộ server/membership/role/outbox rồi đọc operation đã commit trong transaction mới, không chỉ bỏ lỗi insert operation và giữ server trùng.
- Join công khai: khóa server, xét visibility/joinMode hiện hành, rồi membership/request. Đã active trả membership hiện hành. Vào ngay tạo/reactivate membership và kết thúc pending cũ; chờ duyệt có tối đa một pending/server/user, gọi lặp trả cùng pending. Dùng [partial unique index](https://www.postgresql.org/docs/18/indexes-partial.html) cho pending; service vẫn khóa để transition nguyên tử. Join qua đường khác kết thúc request pending bằng approved/reason joined_elsewhere và membershipId hiện hành; UI hiển thị đã tham gia bằng đường khác, không giả reviewer đã duyệt.
- Approve/reject/cancel: kiểm tra actor/target và version; chỉ một transition pending thắng. Approve tạo membership cùng commit; đã joined bằng link/đường khác thì đóng pending với lý do joined_elsewhere, không tạo membership thứ hai. Request mới sau rejected/cancelled có ID mới, không sửa lại lịch sử request cũ.
- Public → private: update visibility và chuyển mọi pending request sang cancelled với reason server_private trong cùng transaction theo DEC-096; không xóa lịch sử request. Sender vẫn đọc được trạng thái của chính mình và nhận thông báo lý do. Existing members/invites giữ nguyên; join request cũ không được approve sau commit private.
- Mời đích danh: chỉ vào private theo DEC-074; unique pending/server/recipient, hạn 7 ngày, accept đúng recipient active/verified. Đang active thì create mời nhận 409 ALREADY_MEMBER; pending còn hạn thì POST lặp trả cùng mời, không kéo dài hạn. Accept và membership cùng transaction; accepted lặp chỉ trả membership nếu còn đúng membershipId đã tạo, không rejoin sau người nhận đã rời. Join bằng đường khác hủy mời pending với reason joined_elsewhere; sau trạng thái cuối hoặc expiry, lời mời cũ không được mở lại.
- Chuyển owner: khóa actor/target theo thứ tự rồi server/membership; target còn active/verified và là member tại commit. Owner field là nguồn chuẩn duy nhất; target rời trước thì transfer thất bại, transfer trước thì target thành owner không được rời. Chủ cũ chỉ rời sau chuyển đã commit.
- Xóa phòng: Community chuyển deleted/version/accessVersion, Messaging đánh dấu space deleted qua hợp đồng lifecycle, outbox thu hồi cùng transaction. Không có hai commit độc lập khiến phòng đã deleted vẫn nhận tin; không purge tin/backup ở đây.

Shared connection/transaction do BuildingBlocks quản lý, mỗi module chỉ đọc/ghi schema mình sở hữu. Hợp đồng cần bổ sung `IChannelAccessGuard`, `IChatSpaceLifecycle`, `ICommunityAccessReader` và định tuyến thu hồi server/channel/membership; tất cả chưa có implementation. Server lock đơn giản hóa correctness MVP nhưng cần đo contention ở workload đã chốt; không tự suy là đủ cho mọi tải.

#### Link mời và retry

Link token kỹ thuật đề xuất là 32 byte ngẫu nhiên base64url, DB tra bằng SHA-256. Route SPA `/invite#token=…` lấy token vào RAM rồi bỏ fragment khỏi URL; API preview/join nhận token trong body. Điều chỉnh route draft cũ `/invites/{token}/join` để secret không nằm trong request path; không ghi body/token/URL mời trong log, audit hoặc outbox.

Theo DEC-097, hiệu lực link/mời đích danh không phụ thuộc creator còn là member/còn manage_invites. Revoke/cancel vẫn cần actor hiện có quyền; creator đã mất quyền không tự được hủy chỉ vì từng tạo. Không tự refresh hạn 7 ngày khi gọi lại POST mời đích danh. Pending quá hạn được chuyển expired dưới khóa trước khi tạo mời mới; partial index chỉ dùng trạng thái pending, không đặt `now()` trong predicate.

Preview chỉ trả server summary khi token còn hợp lệ, không có phòng/tin, không tiêu lượt và không giữ chỗ. Join khóa server/invite và kiểm tra `expiresAt > now`, revokedAt null, số lượt trước tạo membership; chỉ tăng lượt cho membership mới/reactivate. Rollback không tiêu lượt. Thành viên đang active gọi lại không tiêu lượt; link sai/không còn hợp lệ không cấp quyền mới. Người tranh lượt cuối chỉ một người join thành công; không hứa preview thành công là join chắc chắn còn lượt.

Để người có quyền sao chép lại link khi response tạo bị mất, đề xuất giữ token trong envelope Data Protection `Community.InviteSecret.v1`, ngoài hash tra cứu. Metadata/list không có token; `GET /servers/{id}/invites/{inviteId}/link` trả URL chỉ cho manage_invites khi link còn dùng được, `Cache-Control:no-store`. Purge envelope khi thu hồi/hết hạn/hết lượt; link không hết hạn cần giữ key ring tương ứng và backup liên quan. Key thiếu trả 503, không đổi token/link âm thầm.

Các POST tạo server/channel/role/link/mời đích danh có `clientOperationId` UUIDv4, copy payload một lần. Unique operation theo actor + kind + scope + client ID; fingerprint HMAC trên payload chuẩn hóa, không chứa token thô trong DB/audit. Retry cùng ID/payload trả resource hiện hành sau kiểm tra quyền; khác payload 409 OPERATION_CONFLICT; resource đã deleted/terminal không tạo lại bằng khóa cũ. Lưu operation metadata, không lưu toàn request có bí mật. Rotation/HMAC/key retention theo cơ chế ở DM.

Input fingerprint kỹ thuật v1: domain ASCII `SCDC.Community.Write.v1` + byte 0, operation kind ASCII + byte 0, UUID actor/scope/client theo network order, UInt32 big-endian độ dài canonical body byte, rồi body. Create-server dùng scope UUID toàn byte 0. Body lấy DTO đã normalize/default, không có clientOperationId và không serialize JSON. String là UInt32 big-endian độ dài UTF-8 + byte UTF-8; nullable có byte 0 cho null hoặc byte 1 + giá trị. Nullable số là marker rồi UInt32 big-endian. Name giữ cách viết sau trim; description/topic rỗng thành null.

| Operation kind | Thứ tự field trong canonical body |
|---|---|
| `create_server` | name string, description nullable string, visibility byte (public=1/private=2) |
| `create_channel` | name string, topic nullable string, kind byte (text=1/voice=2) |
| `create_role` | name string, permission mask một byte; bit 0–4 theo thứ tự 5 code trong bảng danh mục quyền ở trên |
| `create_invite` | expiresInSeconds nullable UInt32 (default 604800), maxUses nullable UInt32 |
| `create_member_invitation` | recipientUserId UUID 16 byte network order |

Mask chỉ biểu diễn fingerprint; wire vẫn là permission array, không thay thuật toán quyền. Duplicate/unknown permission bị validation trước fingerprint. [community-operations.json](../fixtures/community-operations.json) có byte/hash kỳ vọng và key giả, gồm Unicode/emoji, đổi thứ tự permission và default/null. Cần đối chiếu writer .NET với fixture khi triển khai; chưa coi mô tả/fixture là proof API.

Join/transition/transfer/leave/PUT/PATCH/DELETE không tự replay sau 401/timeout. Client dùng `retry:false` của api.js cho mutation; response không rõ thì tải trạng thái hiện hành, người dùng quyết định tiếp. GET có thể retry sau refresh. Leave lặp trả 204 khi đã left, nhưng phải có membershipId của lần tham gia đang rời để một request cũ không làm người dùng rời lần rejoin mới.

#### Search, danh sách và quyền được biết

Search theo DEC-093 dùng key tên trim/NFC/ToLowerInvariant, exact trước rồi normalizedName/UUID để ổn định. Escape `%`, `_`, backslash cho LIKE literal; collation không bỏ dấu. Cursor Data Protection purpose `Community.Search.v1`, actor/query/limit/position/expiry 24 giờ; mỗi trang lọc lại visibility/status. Đổi public→private phải biến mất cả khi dùng cursor cũ, không dựa vào snapshot đã cấp quyền public trước đó.

`GET /servers` chỉ server người gọi đang là member; `GET /servers/{id}` trả public summary hoặc member detail theo quyền hiện hành, private nonmember nhận 404 trừ preview bằng lời mời hợp lệ. `GET /servers/{id}/channels` chỉ phòng được xem; không có endpoint quản lý lộ metadata phòng bị ẩn theo DEC-098. Sửa/xóa/đọc hoặc thay ACL cần view hiện hành và management permission; actor tự làm mất view qua PUT thì nhận kết quả commit nhưng các request tiếp theo bị chặn. Roster phục vụ role/ngoại lệ chỉ trả user summary/membership cho owner/manage_channel_access. Danh sách pending/mời/role chỉ đúng actor/quyền, không broadcast toàn server.

Phân trang collection quản lý đề xuất mặc định 20/tối đa 50 theo cursor riêng actor/server/filter; inbox mời đích danh của người nhận và status yêu cầu của chính người gửi còn đọc được sau thay đổi visibility để giải thích trạng thái. Các danh sách đang thay đổi có dedup ID/refresh từ đầu; không hứa snapshot bất biến. GET/read cần một transaction snapshot với kiểm tra quyền Community nhất quán; auth session hiện hành vẫn kiểm tra từ Identity.

#### Tin phòng và realtime

REST tin text dùng `/servers/{id}/channels/{channelId}/messages` với GET/POST/PATCH/DELETE tương tự DM. Message wire dùng `channelId` thay `conversationId`; `sequence` theo Messaging space, fingerprint/send operation/tombstone/version và cursor/resume dùng chung thiết kế DM. Voice channel chỉ có media, chưa mở luồng text bên trong voice; kind không đổi bằng PATCH trong MVP. Quyền quản lý/owner không thay kiểm tra tác giả khi sửa/xóa.

Hub vẫn `/hubs/chat`; schema riêng [community-realtime.schema.json](../contracts/community-realtime.schema.json) bổ sung SubscribeChannel/UnsubscribeChannel và ChannelMessageChanged. Subscribe kiểm tra phiên/membership/quyền hiện hành; registry gắn user/session/server/channel/membershipId/accessVersion. Dispatcher chỉ phát nội dung cho connection còn đủ điều kiện, reconciliation ≤1 giây và deadline chat ≤5 giây từ commit thu hồi theo DEC-083. Lỗi kiểm tra quyền dừng phát.

Thông báo `CommunityChanged` chỉ chứa eventId/serverId/accessVersion và yêu cầu UI tải lại metadata/quyền; không mang tên phòng bị ẩn, danh sách thành viên/role hay nội dung tin. `MembershipChanged`, `JoinRequestChanged`, `MemberInvitationChanged` định tuyến đến đúng người và manager còn quyền theo user/session, không cần người ngoài subscribe server. Server tự gỡ subscription khi mất quyền, không trông chờ client xử lý thông báo. Client merge version theo từng resource; sự kiện thu hồi có membershipId cũ không được xóa cache của epoch rejoin mới. Reconnect subscribe lại rồi REST bù trang/tải lại tin cũ để nhận edit/delete.

Mất quyền/rời/xóa phòng xóa cache/tin tạm của scope tương ứng và đóng composer; không ảnh hưởng DM. Draft tin phòng đề xuất dùng RAM tab cùng cơ chế DM, không lưu nội dung xuống browser storage; chưa có quyết định riêng nếu muốn lưu bền. Media nhận revocation từ cùng commit, thu hồi ≤5 giây/fail-close theo DEC-099; admission/lease/quota gate nằm trong [thiết kế media](voice-video.md#detailed-design), không tự coi chat proof là media proof. Voice channel đề xuất gọi IMediaRoomLifecycle tạo room DB rỗng cùng transaction, đánh dấu closing khi xóa; interface/migration chưa có, SFU provision/dừng sau commit.

#### Đối chiếu schema và migration

| Mã | SQL/source hiện tại | Mapping/đầu việc thiết kế |
|---|---|---|
| COM-SQL-01 | servers chưa có visibility/join_mode; description varchar(500), slug bắt buộc | Thêm visibility/join_mode/access_version, description 1.000; slug nội bộ sinh từ ID; backfill visibility/join mode được rà soát theo dữ liệu thực |
| COM-SQL-02 | channel name regex slug, tối thiểu 2 ASCII; topic 500; visibility có read-only | Tên Unicode 1–100 UTF-16, topic 1.000; kind text/voice/default_view/deleted_at/version/access_version riêng; unique tên phòng đang active, ID đã deleted không khôi phục; không đưa read-only vào MVP |
| COM-SQL-03 | roles tên 50, unique generated lower; default index chỉ “tối đa một” | Role tên 64, key chuẩn hóa cùng service; đúng một @everyone tạo cùng server; system restrictions và giới hạn 20 dưới server lock |
| COM-SQL-04 | member_roles/user_overrides gắn user nhưng chưa có epoch | Thêm membership_id/version, FK theo epoch; clear assignment/override khi leave; chống ABA rejoin |
| COM-SQL-05 | Không có join_requests hoặc member_invitations | Thêm trạng thái/version/lý do/expiry, unique pending có điều kiện, membershipId đã tạo; transition có khóa và CAS |
| COM-SQL-06 | invites giữ hash/lượt/hạn, default_role_id có thể khác default | Thêm version/protected_token; không cấp custom role qua link; use_count/membership cùng transaction |
| COM-SQL-07 | channels FK sang Messaging space; chưa có nghiệp vụ writer | Create/delete qua IChatSpaceLifecycle trong shared transaction; Community không đọc/ghi bảng Messaging trực tiếp |
| COM-SQL-08 | Trigger servers/spaces tự tăng version; channels/roles chỉ touch timestamp | Một nguồn tăng version cho mỗi resource, tránh trigger và service cùng tăng; wire chuỗi số, accessVersion tăng đúng khi quyền đổi |
| COM-SQL-09 | Permission catalog/role overrides có thể chứa quyền ngoài MVP | Migrate danh mục 5 management codes + channel_view; chỉ channel_view nhận override allow/deny; không suy seed permissions thành tính năng |
| COM-SQL-10 | Không có operation dedup hoặc realtime dispatcher Community | Thêm operation key `(actor,kind,scope,client_id)` với scope không null (create-server dùng UUID zero), fingerprint/key/version/resource ID; outbox metadata/lease, revoker/guards; key ring bền và thử restart |

Nguồn: [schema.sql](../../database/postgres/schema.sql), [CommunityModule](../../services/Modules/Community/CommunityModule.cs), [IChannelAccessChecker](../../services/SCDC.Contracts/Community/IChannelAccessChecker.cs). Không sửa schema/seed trong đợt tài liệu này. Trước bật unique key mới, dò collision Unicode/case và mapping tên legacy; không tự đổi tên/xóa dữ liệu thật. Giữ tin/phòng deleted theo [vòng đời dữ liệu](../data-lifecycle.md#deletion): không tự hết hạn tin DEC-070, phòng deleted giữ nội dung chưa hạn purge DEC-105; backup tuổi tối đa 30 ngày DEC-109. Role/override epoch cũ dọn theo rule membership, không phục hồi từ backup; migration không tự xóa nội dung.

#### Lỗi và kiểm chứng

| HTTP / mã đề xuất | UI/kết quả |
|---|---|
| 400 VALIDATION_FAILED / NAME_INVALID / ACCESS_CONFIG_INVALID | Giữ form, chỉ trường sai; không cắt Unicode/đổi dữ liệu âm thầm |
| 404 RESOURCE_NOT_FOUND | Dùng cho private/nonmember/hidden channel; không trả metadata bị ẩn |
| 403 PERMISSION_DENIED | Biết scope nhưng thiếu quyền quản lý/tác giả; dừng thao tác |
| 409 VERSION_CONFLICT / MEMBERSHIP_CHANGED | Tải lại version/epoch và quyền; không tự replay |
| 409 ROLE_LIMIT_REACHED / NAME_CONFLICT / SYSTEM_ROLE_IMMUTABLE | Giải thích đúng giới hạn/quy tắc đã chốt; không sửa @everyone |
| 409 REQUEST_NOT_PENDING / INVITATION_NOT_PENDING / OWNER_MUST_TRANSFER | Hiện trạng thái hiện hành, không tạo lại transition cũ |
| 400 INVITE_INVALID hoặc 409 INVITE_EXHAUSTED | Không cấp membership/lộ metadata private; preview không bảo đảm lượt |
| 503 KEY_UNAVAILABLE / ACCESS_CHECK_UNAVAILABLE | Dừng thao tác/phát nội dung; không bỏ qua guard hoặc tạo lại link |

Review contract/schema/fixture với UX và AC/TC trước tích hợp. Proof cần bao gồm lượt cuối, approve/cancel/accept tranh nhau, 21 role tạo đồng thời, stale epoch sau rejoin, role deny bị xóa, revoke tranh gửi/subscribe, owner transfer/leave và migration Unicode. Các ca chưa có kết quả chạy trên sản phẩm; kiểm tra fixture chỉ đối chiếu giá trị thiết kế.

<a id="acceptance"></a>

## 6. Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-COM-01 | Một cộng đồng công khai và một cộng đồng riêng tư tồn tại; người dùng tìm cộng đồng. | Kết quả chỉ hiện cộng đồng công khai. |
| AC-COM-02 | Người dùng tham gia cộng đồng công khai mới tạo qua tìm kiếm. | Người dùng được vào ngay theo cấu hình mặc định. |
| AC-COM-03 | Người dùng yêu cầu tham gia qua tìm kiếm khi cộng đồng bật chờ duyệt. | Yêu cầu được ghi nhận; người dùng chưa là thành viên cho đến khi được duyệt. |
| AC-COM-04 | Người dùng mở liên kết mời còn hiệu lực của cộng đồng đang bật chờ duyệt. | Người dùng được vào ngay. |
| AC-COM-05 | Người dùng mở liên kết mời đã hết hạn. | Người dùng không thể dùng liên kết đó để tham gia. |
| AC-COM-06 | Một thành viên không có quyền xem phòng X đăng nhập. | Phòng X không hiện trong danh sách phòng của thành viên đó. |
| AC-COM-07 | Thành viên không có quyền tạo phòng thử tạo phòng. | Hệ thống từ chối thao tác; chủ sở hữu hoặc người được cấp quyền thực hiện được. |
| AC-COM-08 | Chủ sở hữu hoặc người được cấp quyền đổi chế độ tham gia. | Cách tham gia qua tìm kiếm áp dụng chế độ mới; liên kết mời hợp lệ vẫn cho vào ngay. |
| AC-COM-09 | Chủ sở hữu, người được cấp quyền và thành viên thường lần lượt thử tạo liên kết mời. | Hai nhóm đầu tạo được liên kết; thành viên thường bị từ chối. |
| AC-COM-10 | Cộng đồng chờ duyệt có một yêu cầu; ba nhóm người ở AC-COM-09 lần lượt thử duyệt. | Chủ sở hữu và người được cấp quyền duyệt được; thành viên thường bị từ chối. |
| AC-COM-11 | Tạo phòng mới không đặt giới hạn xem; sau đó giới hạn quyền xem của một thành viên. | Ban đầu mọi thành viên thấy phòng; sau thay đổi, thành viên bị giới hạn không còn thấy hoặc truy cập được phòng. |
| AC-COM-12 | Thành viên được phép nhắn gửi tin văn bản trong phòng, rồi sửa và xóa tin của mình. | Tin được lưu và thấy lại; sửa có dấu “Đã sửa”; xóa hiện “Tin nhắn đã bị xóa” cho các thành viên có quyền xem phòng. |
| AC-COM-13 | Thành viên cố sửa/xóa tin của người khác hoặc đọc/gửi tin ở phòng không có quyền. | Hệ thống từ chối, kể cả khi gọi trực tiếp API. |
| AC-COM-14 | Tin được lưu trong phòng khi thành viên đang vắng mặt; thành viên mở lại phòng sau đó và vẫn có quyền xem. | Thành viên thấy tin trong lịch sử phòng. |
| AC-COM-15 | Thành viên mới tham gia mở phòng có tin từ trước và mình được phép xem. | Thành viên thấy lịch sử cũ của phòng. |
| AC-COM-16 | Chủ sở hữu, người được cấp quyền và thành viên thường thử đổi danh sách người được xem phòng. | Hai nhóm đầu thực hiện được; thành viên thường bị từ chối ở máy chủ. |
| AC-COM-17 | Một thành viên có quyền xem phòng gửi tin văn bản; một người không có quyền xem thử gửi. | Người thứ nhất gửi được; người thứ hai bị từ chối ở máy chủ. |
| AC-COM-18 | Kết quả gửi tin phòng lần đầu không rõ, người gửi bấm thử lại cho chính tin đó. | Phòng chỉ có một tin tương ứng với thao tác gửi. |
| AC-COM-19 | Người dùng thử tìm cộng đồng riêng tư, rồi dùng liên kết mời hợp lệ hoặc được thêm trực tiếp. | Cộng đồng không xuất hiện trong tìm kiếm; hai cách còn lại cho phép trở thành thành viên theo điều kiện tương ứng. |
| AC-COM-20 | Người có quyền tạo lời mời thu hồi một liên kết còn hạn; người khác mở liên kết đó. | Hệ thống không cho tham gia bằng liên kết đã thu hồi. |
| AC-COM-21 | Thành viên thường chọn rời cộng đồng. | Tư cách thành viên chấm dứt; thành viên không còn quyền truy cập nội dung dành cho thành viên. |
| AC-COM-22 | Thành viên chưa xác minh email nhưng có quyền xem phòng thử gửi tin. | Hệ thống từ chối gửi và chỉ dẫn bước xác minh email. |
| AC-COM-23 | Thành viên sửa tin nhiều lần; người khác tải lại. | Chỉ nội dung hiện hành cùng dấu “Đã sửa”, không có lịch sử nội dung cũ. |
| AC-COM-24 | Thành viên gửi/sửa tin tại biên 2.000/2.001 UTF-16 sau chuẩn hóa xuống dòng; gửi tin chỉ trắng/vô hình hoặc Unicode lỗi. | Nhận đến 2.000 UTF-16, từ chối vượt giới hạn/tin trống/Unicode lỗi; giữ tiếng Việt, emoji và ZWJ trong tin có nội dung theo DEC-090. |
| AC-COM-25 | Thành viên có hai vai trò cho phép/từ chối xem cùng phòng, chưa đặt ngoại lệ cá nhân. | Thành viên không thấy, đọc, gửi hoặc nhận cập nhật phòng đó. |
| AC-COM-26 | Đặt ngoại lệ cho phép cá nhân ở ca AC-COM-25; sau đó đổi thành từ chối. | Cho phép cá nhân thắng từ chối vai trò; khi cá nhân bị từ chối thì không xem/gửi/nhận được. |
| AC-COM-27 | Chủ sở hữu mở phòng giới hạn có cấu hình từ chối vai trò/cá nhân. | Vẫn xem được trong cộng đồng của mình nếu tài khoản/phiên hợp lệ. |
| AC-COM-28 | Thành viên được quyền tạo phòng thử sửa hoặc gán vai trò; chủ sở hữu làm cùng thao tác. | Thành viên bị từ chối; chủ sở hữu thực hiện được. |
| AC-COM-29 | Tài khoản đã xác minh tạo cộng đồng, chủ sở hữu sửa tên/mô tả/công khai; thành viên gọi cùng API sửa | Tạo/sửa theo giới hạn DEC-072; thành viên không có quyền sửa; riêng tư không còn xuất hiện trong tìm kiếm |
| AC-COM-30 | Người có quyền duyệt từ chối; người gửi hủy pending hoặc gửi lại sau từ chối/hủy | Trạng thái đúng, chưa duyệt chưa có membership; một pending hoạt động mỗi cặp; thao tác đồng thời không cùng thắng |
| AC-COM-31 | Mời đích danh A vào cộng đồng riêng tư; B cố chấp nhận thay; A chưa nhận rồi nhận | B bị từ chối; A chưa nhận không có quyền phòng; sau nhận có một membership và không cần duyệt thêm |
| AC-COM-32 | Link ở biên hạn/lượt; join đồng thời và gọi lặp của thành viên | Không vượt maxUses, không tăng lượt cho thành viên hiện hành; hết hạn/thu hồi không join được |
| AC-COM-33 | Owner chuyển cho thành viên active/verified trong lúc target/chủ cũ thử rời | Chuyển ngay, không cần nhận chấp nhận; luôn đúng một owner; chủ cũ được rời sau chuyển thành công |
| AC-COM-34 | Người đúng/sai quyền sửa/xóa phòng đang có tin/cuộc gọi | Deleted không đọc/gửi/nhận/tiếp tục gọi được; chặn race writer; không có khôi phục MVP |
| AC-COM-35 | Thành viên rời tham gia lại; mời đích danh hết 7 ngày/đã hủy/từ chối được dùng lại | Rejoin theo join mode hiện hành, role mặc định; lịch sử theo quyền; mời cuối trạng thái không accept được |
| AC-COM-36 | Tạo role thứ 20/21, sửa/xóa @everyone, hai role cấp quyền quản lý khác nhau | Tối đa 20 custom role; @everyone không sửa/xóa/cấp management; quyền quản lý là hợp theo DEC-092 |
| AC-COM-37 | Tìm tên Unicode có/không dấu, biên q và phân trang; đổi server public sang private giữa các trang | Đúng q 2–100/trang 20–50/exact-first; case-insensitive/accent-sensitive; private không xuất hiện từ cursor cũ theo DEC-093 |
| AC-COM-38 | Tên có emoji/tiếng Việt, khoảng trắng, trống/vô hình, hai tên chỉ khác case/dấu | Đếm UTF-16/trim; server được trùng; active channel/role unique case-insensitive/accent-sensitive theo DEC-095 |
| AC-COM-39 | Owner tìm thao tác xóa toàn server | Không có thao tác/API xóa server trong MVP theo DEC-094; vẫn có xóa phòng/chuyển owner |
| AC-COM-40 | Public có pending đổi sang private trong lúc reviewer duyệt | Một thứ tự commit xác định; pending còn lại cancelled/server_private, sender biết lý do; members/invites được giữ theo DEC-096 |
| AC-COM-41 | Creator của link/mời đích danh rời hoặc mất manage_invites | Mời đã phát hành còn hiệu lực tới hạn/lượt/thu hồi; creator không còn quyền tự hủy, đúng actor hiện hành mới xử lý theo DEC-097 |
| AC-COM-42 | Manager có manage_channels/manage_channel_access nhưng không view phòng | Không thấy metadata/phòng/tin hoặc sửa/xóa/ACL phòng đó; owner vẫn quản lý, tạo mới theo management permission theo DEC-098 |

Các tiêu chí mới dẫn tới DEC-072–077/087/092–098; toàn bộ AC-COM chưa có kết quả thực thi được ghi nhận. Ca chat thu hồi đo ≤5 giây theo DEC-083; media thu hồi ≤5 giây/fail-close theo DEC-099, chưa có kết quả SFU.

<a id="tests"></a>

## 7. Ca kiểm thử

Dữ liệu: O là chủ sở hữu; M được cấp một quyền quản lý cụ thể; N là thành viên thường. Chuẩn bị phòng mở/phòng giới hạn, vai trò R-allow/R-deny, người ngoài cộng đồng, lời mời hợp lệ/hết hạn/thu hồi và yêu cầu chờ duyệt. Tài khoản thử phải có trạng thái xác minh/phiên được kiểm soát.
| Mã ca | Tình huống | Kết quả | Dẫn chiếu |
|---|---|---|---|
| TC-ACL-01 | Phòng mới chưa giới hạn; N đã xác minh | N xem và gửi được; thấy lịch sử cũ | AC-COM-11, AC-COM-15, AC-COM-17 |
| TC-ACL-02 | N có cả R-allow và R-deny, không có ngoại lệ cá nhân | Không xem, không gửi hoặc nhận nội dung phòng | DEC-057 |
| TC-ACL-03 | Như TC-ACL-02 nhưng cá nhân N được cho phép | N xem/gửi được nếu thỏa điều kiện tài khoản/thành viên | DEC-057 |
| TC-ACL-04 | Vai trò cho phép, ngoại lệ cá nhân từ chối | N không xem/gửi được | DEC-057 |
| TC-ACL-05 | O mở phòng giới hạn, gồm cả cấu hình từ chối theo vai trò/cá nhân | O vẫn xem được trong cộng đồng của mình; không vượt điều kiện phiên/tài khoản | DEC-056 |
| TC-ACL-06 | M được quyền tạo phòng nhưng thử tạo/sửa/gán/thu hồi vai trò | Máy chủ từ chối; O thực hiện được | DEC-058 |
| TC-ACL-07 | N đang mở phòng; O thu hồi quyền hoặc N rời cộng đồng | API từ chối; ngừng nhận nội dung phòng theo ngưỡng thu hồi; DM độc lập vẫn hoạt động | AC-COM-11, AC-COM-21 |
| TC-ACL-08 | Chạy 18 fixture nền/owner/role/cá nhân/quyền quản lý/kind | Khớp [community-permissions.json](../fixtures/community-permissions.json); không coi fixture đã khớp là API đã đạt | AC-COM-06/17/36/42 |
| TC-ACL-09 | O tạo hai custom role tranh slot thứ 20; gọi sửa/xóa/gán @everyone | Một role mới, role còn lại ROLE_LIMIT_REACHED; system role bị bảo vệ | AC-COM-36 |
| TC-ACL-10 | PUT ACL với role/member khác server, duplicate, epoch cũ; actor làm mất view của mình | Cấu hình sai rollback toàn bộ; cấu hình hợp lệ commit, request/subscription tiếp theo bị chặn | AC-COM-16/42 |
| TC-ACL-11 | Xóa role deny hoặc allow khi user đang xem phòng | Tính lại kết quả thực, không suy mọi xóa là deny; view mất thì thu hồi ≤5 giây | DEC-057/083 |
| TC-ACL-12 | M biết ID phòng bị ẩn và gọi GET/PATCH/DELETE/GET-ACL/PUT-ACL | 404 không metadata; đúng management permission vẫn cần view; owner không vượt điều kiện nền | AC-COM-42 |

### Ca cộng đồng và tin phòng

Mọi ca dưới đây ở trạng thái Chưa chạy; ca phụ thuộc quy tắc hoặc implementation thiếu ghi điều kiện còn thiếu. Dùng cộng đồng công khai vào ngay/chờ duyệt, cộng đồng riêng tư, người ngoài P và đúng quyền từng thao tác.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-COM-01 | Tìm công khai/riêng tư; đổi cộng đồng sang riêng tư rồi tìm lại | Chỉ kết quả công khai, không lộ phòng/tin cho người ngoài | AC-COM-01/19/29 |
| TC-COM-02 | Tạo cộng đồng với biên tên/mô tả; N sửa metadata của O qua API | Giới hạn theo DEC-072; chỉ O sửa được; không để lại membership owner dở dang | AC-COM-29 |
| TC-COM-03 | P join cộng đồng vào ngay; gọi join lặp/đồng thời | Một membership, có quyền mặc định sau commit | AC-COM-02 |
| TC-COM-04 | P gửi yêu cầu chờ duyệt, xem trạng thái; N/M thử duyệt/từ chối | Chỉ đúng quyền xử lý; pending chưa đọc phòng được | AC-COM-03/10/30 |
| TC-COM-05 | P hủy pending đồng thời với M duyệt/từ chối; gửi yêu cầu mới sau trạng thái cuối | Một thao tác thắng; trạng thái/membership nhất quán; cho yêu cầu mới sau hủy/từ chối | AC-COM-30 |
| TC-COM-06 | Link hợp lệ/hết hạn/thu hồi; dùng link trong cộng đồng chờ duyệt | Link hợp lệ tạo membership ngay; các link khác bị từ chối; pending cũ kết thúc cùng giao dịch | AC-COM-04/05/20 |
| TC-COM-07 | N/M tạo/thu hồi link và đổi join mode | Chỉ quyền tương ứng; link hợp lệ vẫn bỏ qua chờ duyệt | AC-COM-08/09/20 |
| TC-COM-08 | M mời đích danh P; N nhận thay; P nhận hai lần/đồng thời | N bị từ chối; trước nhận không là thành viên; nhận tạo một membership | AC-COM-19/31 |
| TC-COM-09 | Thành viên mới/mất mạng mở lại phòng có tin cũ | Lịch sử đầy đủ nếu còn quyền xem | AC-COM-14/15 |
| TC-COM-10 | Gửi/sửa/xóa tin phòng; N sửa tin O bằng API; gửi lại khi mất response | Kiểm tra tác giả, tombstone, một tin mỗi thao tác; dữ liệu UTF-16 dùng chung DM | AC-COM-12/13/18/23/24 |
| TC-COM-11 | N tự rời trong khi đọc/gửi/nhận cập nhật | Chat thu hồi trong ≤5 giây theo DEC-083; DM độc lập còn dùng được | AC-COM-21 |
| TC-COM-12 | U chưa xác minh giả membership và gọi API đọc/gửi | Chặn truy cập ứng dụng phía server, không suy U được đọc phòng | AC-COM-22, DEC-051 |
| TC-COM-13 | Dùng link đúng lúc hết hạn/hết lượt; nhiều request tranh lượt cuối; thành viên join lặp | Không vượt lượt, không nhân membership; rollback không tiêu lượt | AC-COM-32 |
| TC-COM-14 | Chuyển owner đồng thời với target rời/chủ cũ rời; target mất trạng thái active | Luôn một owner; target đủ điều kiện tại chuyển; chủ cũ chỉ rời sau chuyển thành công | AC-COM-33 |
| TC-COM-15 | Xóa phòng cùng lúc gửi/join media; thử ID phòng cũ sau xóa | Không phục hồi phòng, không đọc/nhận/gọi sau thu hồi; tin đã commit giữ theo retention | AC-COM-34 |
| TC-COM-16 | Thành viên có role riêng rời rồi vào lại theo từng join mode | Role mặc định, không phục hồi role cũ; lịch sử hiện theo quyền mới | AC-COM-35 |
| TC-COM-17 | Mời đích danh accept/cancel/reject đồng thời, ở cutoff 7 ngày và gọi sau trạng thái cuối | Một transition thắng; một membership nếu accept thắng; hết hạn không accept được | AC-COM-31/35 |
| TC-COM-18 | Search q biên, tiếng Việt/case/dấu, ký tự %/_/backslash; private switch với cursor cũ | Tìm literal/exact rank/phân trang đúng; không lộ private | AC-COM-37 |
| TC-COM-19 | Tên 1/2/100/101 UTF-16, emoji/combining; role 64/65; collision key sau trim/case/NFC | Unicode hợp lệ, đúng unique theo DEC-095; không để lại dữ liệu dở dang | AC-COM-38 |
| TC-COM-20 | Public→private tranh approve/cancel; sender đọc status sau chuyển | Pending còn lại cancelled/server_private cùng commit; membership đã commit trước được giữ | AC-COM-40 |
| TC-COM-21 | Creator leave/mất quyền; recipient accept hoặc join link; creator cố cancel/revoke | Mời vẫn dùng nếu hợp lệ; kiểm tra quyền actor hiện hành | AC-COM-41 |
| TC-COM-22 | Leave/rejoin, gửi lại PUT role/ACL/leave và event revoke của epoch cũ | Role/override cũ không khôi phục, epoch cũ bị chặn; event cũ không xóa cache epoch mới | AC-COM-35/42 |
| TC-COM-23 | POST create response mất; manual retry cùng/khác payload; resource sau đó deleted | Một resource cùng operation, payload khác conflict; không phục hồi resource deleted bằng retry | Hợp đồng operation |
| TC-COM-24 | Preview link rồi tranh lượt cuối; copy link thiếu key, revoke/expiry đúng cutoff | Preview không giữ chỗ; một membership/lượt; thiếu key không tạo token khác | AC-COM-32 |
| TC-COM-25 | Channel guard tranh role/ACL/leave/delete và send/subscribe/dispatch | Guard giữ tới commit; thu hồi chat ≤5 giây, không mất/trùng/trái quyền | AC-COM-11/18/34/42, DEC-083 |
| TC-COM-26 | Migration tên legacy/collision, system role/permission catalog/trigger version | Không đổi/xóa dữ liệu thật tự động; một nguồn tăng version và schema phù hợp writer | COM-SQL-01–10 |
| TC-COM-27 | Tìm UI/API xóa server hoặc text message trong voice channel | Không mở scope ngoài DEC-094; voice/text kind theo contract, media kiểm chứng riêng | AC-COM-39, hợp đồng kind |

Các ca này bao phủ quyền đã chốt; cần bổ sung ca cho từng AC-COM khi hoàn thiện backend/API. Chạy trực tiếp API để kiểm tra quyền, không chỉ nhìn nút UI. Ca nội dung tin dùng [TC-TEXT](direct-messaging.md#tests). Ca gửi lại/đồng thời cũng cần chạy cho tin phòng sau khi chọn hợp đồng.

Ca AC-COM-22 kiểm tra điều kiện chưa xác minh phía máy chủ; DEC-051 vẫn chặn truy cập ứng dụng từ trước, không suy rằng người chưa xác minh được đọc phòng. Chat thu hồi đã chốt DEC-083; ca media theo DEC-099 và MEDIA-GAP ở đặc tả media vẫn “Chưa chạy”, không đánh dấu đạt. Kết quả theo [mẫu nghiệm thu](../release-operations.md#testing).

<a id="gaps"></a>

## 8. Vấn đề còn mở

### Quy tắc cần làm rõ trước khi xác nhận

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Lời mời | Hạn/lượt/issuer theo DEC-075/087/097; đã có schema/secret/transaction, còn proof và migration/key store. | OQ-003, OQ-004, OQ-008 |
| Yêu cầu tham gia | DEC-073/096 đã chốt; có transition/schema, còn kiểm chứng approve/cancel/private switch; limiter bổ sung chưa chốt. | OQ-003, OQ-004 |
| Cộng đồng riêng tư | Đã chốt DEC-074/087/096; schema/inbox/notification có thiết kế; còn review/proof. Không xóa server MVP theo DEC-094. | OQ-003, OQ-008 |
| Cấp quyền | Mô hình/danh mục/giới hạn và quản lý phòng theo DEC-092/098 đã có thiết kế; còn review/migration/guard/đo thu hồi. | OQ-004, OQ-007, OQ-008 |
| Nhắn tin phòng | Dùng chung text/HMAC/sequence/cursor DM; REST/realtime đã có schema, chưa có writer/mock/proof; mất quyền không xóa nội dung, room deleted giữ chưa đặt hạn theo DEC-105; restore placeholder dùng chung schema DEC-108, còn DATA-GAP proof. | OQ-005, OQ-011 |
| Rời cộng đồng | Epoch/clear role/override/transfer có thiết kế; mất quyền dọn cache/role/override epoch cũ, giữ tin; dấu revoke/access floor bảo vệ restore theo [vòng đời](../data-lifecycle.md#restore), metadata khác còn review. | OQ-003, OQ-011 |

### Đầu việc rà soát và kiểm chứng phân quyền

| Mã theo dõi | Thiết kế đã có và việc còn lại | Phần bị ảnh hưởng | Đầu mối dự kiến |
|---|---|---|---|
| ACL-O1 | DEC-092 đã chốt @everyone/20 role/union; có API/catalog/fixture, còn review/migration/proof | Tạo phòng, mời, duyệt, đổi cấu hình/quyền xem; quản lý role theo DEC-058 | Vg; Sáng đánh giá thiết kế |
| ACL-O2 | Có snapshot/accessVersion/epoch và nguyên tử; cần prototype/guard proof, giữ DEC-057/098 | Danh sách phòng, lịch sử, gửi, thời gian thực | Vg, Sáng |
| ACL-O3 | Chuyển ngay DEC-076 có lock order/transaction; còn proof transfer/leave | Một owner, quyền chủ cũ/target, không xóa server MVP | Vg |
| ACL-O4 | Có schema/inbox/expiry/issuer theo DEC-074/087/097; còn review và concurrency proof | Cộng đồng riêng tư | Vg |
| ACL-O5 | Có registry/guard/outbox/epoch; cần đo chat ≤5 giây, media deadline DEC-099 cần proof SFU | Thiết kế đồng bộ quyền, kiểm thử chất lượng | Vg, Sáng, Thái |

Các mục trên tiếp tục thuộc OQ-003/OQ-004/OQ-007. DM hai người không phụ
thuộc mô hình vai trò cộng đồng; có thể rà soát phần ACL-01 đến ACL-05
riêng trong gói bàn giao DM.
