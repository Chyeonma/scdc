# SCDC — Cộng đồng, phòng và phân quyền

Cập nhật: 2026-10-04. Phạm vi: REQ-002/004, SCP-003/004 và phần tin phòng của SCP-005. Quy tắc COM, ACL-06–20, AC-COM, COM-S và TC-ACL.

Quy tắc tham gia/quyền cốt lõi đã xác nhận; tạo/sửa, duyệt/từ chối/hủy, chuyển chủ sở hữu, mời và tham gia lại đã chốt DEC-072–077/087; dữ liệu/API còn cần thiết kế. Community/Messaging mới có nền module; wireframe và ca kiểm thử chưa phải kết quả triển khai hoặc nghiệm thu.

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Quyền và thu hồi](#permissions)
- [Giao diện](#ux)
- [Dữ liệu/API cần thiết kế](#contracts)
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
| COM-024 | Tin văn bản tối đa 2.000 đơn vị UTF-16, cho xuống dòng/emoji; không nhận tin rỗng/chỉ khoảng trắng. | DEC-053, DEC-068 |
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
| ACL-12 | Tạo/sửa/xóa phòng | Chủ sở hữu hoặc người có quyền quản lý phòng | Phòng mới mặc định mọi thành viên xem; xóa chặn HTTP/realtime/media, chưa có khôi phục | DEC-026, DEC-033, DEC-077 |
| ACL-13 | Xem phòng và lịch sử | Là thành viên và có quyền xem phòng; chủ sở hữu luôn xem được | Thành viên mới được xem tin cũ; thứ tự vai trò/ngoại lệ cá nhân ở [thuật toán quyền xem](#view-permissions) | DEC-027, DEC-038, DEC-055–057 |
| ACL-14 | Gửi tin trong phòng | Có quyền xem phòng, phiên hợp lệ và đã xác minh email | Đợt đầu không có quyền chỉ đọc riêng | DEC-040, DEC-041 |
| ACL-15 | Sửa/xóa tin trong phòng | Có quyền truy cập phòng và là tác giả tin | Không cấp quyền sửa/xóa tin của người khác trong đặc tả đợt đầu | DEC-034, AC-COM-13 |
| ACL-16 | Đổi cấu hình quyền xem phòng | Chủ sở hữu hoặc người có vai trò cho quyền quản lý danh sách xem | Áp dụng vai trò và ngoại lệ cá nhân; không đồng nghĩa được quản lý vai trò | DEC-039, DEC-055, DEC-058 |
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

### Quyền thay đổi khi đang sử dụng

| Tình huống | Kết quả phải kiểm chứng | Tiêu chí liên quan |
|---|---|---|
| Thu hồi quyền xem phòng đang mở | Lần đọc/gửi tiếp theo bị từ chối; không nhận thêm nội dung phòng qua kết nối cũ trong ≤5 giây cho chat theo DEC-083; media cần xác nhận ngưỡng riêng | AC-COM-11, AC-COM-13; DEC-083, OQ-007 |
| Thành viên thường rời cộng đồng | Không còn truy cập nội dung dành cho thành viên; giao diện rời phòng đang mở | AC-COM-21 |
| Mất quyền quản lý | Các thao tác quản lý tiếp theo bị kiểm tra lại phía máy chủ | ACL-09 đến ACL-12, ACL-16 |
| Mất kết nối rồi mở lại | Kiểm tra lại phiên, tư cách thành viên và quyền trước khi tải lịch sử hoặc đăng ký nhận tin | AC-ACC-04, AC-COM-13 |
| Thu hồi quyền một phòng | Không tự thu hồi quyền ở phòng khác hoặc DM độc lập | Suy ra từ phạm vi quyền phòng; cần kiểm thử tích hợp |

Không hứa xóa được nội dung người dùng đã nhìn thấy hoặc tự sao chép.
Việc ngừng hiển thị dữ liệu đã tải trong giao diện, cách xử lý bộ nhớ đệm
và độ trễ thu hồi cần được cụ thể hóa trong thiết kế và OQ-007/OQ-011.


Quy tắc có thẩm quyền là DEC-055–058: giữa vai trò có DENY thì DENY thắng, cá nhân áp dụng cuối; chủ sở hữu được xem sau khi thỏa điều kiện nền. Cách mã hóa bằng bit mask 64-bit từng được đề xuất là lựa chọn biểu diễn dữ liệu; nó không được thay thứ tự ưu tiên đã chốt. Không suy “toàn quyền tuyệt đối” thành quyền sửa/xóa tin của người khác hoặc đọc DM.

<a id="ux"></a>

## 3. Giao diện và trạng thái

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

COM-S10 Tạo/sửa cộng đồng theo DEC-072: thu tên/mô tả/công khai; chủ sở hữu mới được sửa. COM-S11 Lời mời đích danh theo DEC-074: người nhận xem đúng cộng đồng, chấp nhận hoặc từ chối; người mời được hủy khi còn chờ theo DEC-087; chưa nhận không tải phòng/tin. COM-S12 Chuyển chủ sở hữu: chọn thành viên active/đã xác minh, xác nhận chuyển ngay; chủ cũ vẫn là thành viên và được rời sau đó. Phòng bị xóa đóng vùng nội dung và cuộc gọi theo DEC-077. Bộ đếm trường mới đề xuất dùng UTF-16 để thống nhất API; không suy DEC-068 là đã quyết định phép đếm mọi trường cộng đồng.

Đề xuất màn hình hẹp dùng lần lượt danh sách cộng đồng → danh sách
phòng → hội thoại; quản lý mở thành trang riêng. DEC-059 mới chốt mục
tiêu thiết bị cho tài khoản/DM; phạm vi thiết bị của cộng đồng và media
cần được rà soát riêng ở OQ-007.

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

## 4. Dữ liệu và API cần thiết kế

Repo có schema `community`/`messaging` và seed, nhưng chưa có controller, service hoặc OpenAPI nghiệp vụ Community. Wireframe không tự xác nhận schema endpoint. Hợp đồng cần được bổ sung sau khi các quy tắc tương ứng đủ rõ:

| Nhóm | Dữ liệu/hành vi tối thiểu | Phụ thuộc |
|---|---|---|
| Cộng đồng | Tạo/sửa, công khai/riêng tư, chủ sở hữu, vòng đời | DEC-072; vòng đời/xóa cộng đồng còn cần chốt |
| Tìm/tham gia | Chỉ tìm công khai, vào ngay/chờ duyệt, trạng thái yêu cầu và tư cách thành viên | COM-001–004, DEC-073; cần transaction chống request lặp |
| Lời mời | Tạo, thời hạn, kiểm tra, tiêu thụ và thu hồi; ghi thành viên nhất quán khi gọi lặp | COM-005/010/020/032; cần tiêu thụ lượt nhất quán |
| Phòng | Tạo, danh sách theo quyền, chủ đề và cấu hình xem | COM-007/008/012; trường/quyền theo DEC-077; kiểm tra xóa và thu hồi đồng thời |
| Vai trò/quyền | Chủ sở hữu quản lý vai trò; mặc định, kế thừa/cho phép/từ chối, ngoại lệ cá nhân và version cấu hình | DEC-055–058, ACL-O1/O2 |
| Tin phòng | Lưu/lịch sử/sửa/xóa/thử lại; chỉ người có quyền, tác giả sửa/xóa | COM-013–018/022–024; tham chiếu cơ chế tin DM |
| Thu hồi | Mất quyền hoặc rời phải chặn lần đọc/gửi tiếp và kết nối cập nhật theo ngưỡng đã chốt | ACL-O5, OQ-007 |

### Hợp đồng HTTP đề xuất

Prefix `/api/v1`; chưa có endpoint nghiệp vụ Community chạy được. Actor lấy từ phiên, ID server UUIDv7 theo DEC-081; time UTC; request thay đổi dùng `expectedVersion` để tránh ghi đè. `ServerSummary` công khai tối thiểu gồm `id,name,description,visibility`; chỉ thành viên được nhận chi tiết phòng/thành viên theo quyền.

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers` | `{name,description?,visibility}` | 201 server; account active/verified; tạo server và membership owner cùng transaction |
| `PATCH /servers/{id}` | `{name?,description?,visibility?,expectedVersion}` | 200 server; chỉ owner; private không xuất hiện trong tìm kiếm |
| `GET /servers/search` | `q,cursor,limit` | 200 trang server công khai; không trả nội dung/phòng riêng tư; giới hạn tìm kiếm còn thiết kế |
| `POST /servers/{id}/join` | Không có body | 200 membership nếu vào ngay; 202 yêu cầu pending nếu chờ duyệt; không join server private từ tìm kiếm |
| `GET /servers/{id}/join-requests/me` | Phiên người yêu cầu | 200 trạng thái của mình, chưa có trả rỗng; không trả request người khác |
| `POST /servers/{id}/join-requests/{requestId}/approve` hoặc `/reject` | `{expectedVersion}` | 200 trạng thái cuối; đúng quyền duyệt; chỉ transition từ pending |
| `DELETE /servers/{id}/join-requests/{requestId}` | `expectedVersion` | 200 cancelled; chỉ chính người gửi hủy pending |
| `PATCH /servers/{id}/join-mode` | `{joinMode,expectedVersion}` | 200 server; đúng quyền đổi chế độ, không vô hiệu nguyên tắc link cho vào ngay |
| `POST /servers/{id}/invites` | `{expiresInSeconds,maxUses?}` | 201 metadata/link; expiresInSeconds là 3600/86400/604800/null, bỏ trường dùng mặc định 604800; quyền tạo mời |
| `DELETE /servers/{id}/invites/{inviteId}` | ID link của server | 204 thu hồi; đúng quyền, join sau thu hồi bị từ chối |
| `POST /invites/{token}/join` | Không có body | 200 membership; kiểm tra hạn/thu hồi/lượt dưới khóa, không ghi token vào log |
| `POST /servers/{id}/member-invitations` | `{recipientUserId}` | 201 mời pending; owner/quyền tạo mời; chưa tạo membership |
| `POST /member-invitations/{invitationId}/accept` | Không có body | 200 membership; đúng người nhận, mời còn hợp lệ; không thêm bước duyệt |
| `POST /member-invitations/{invitationId}/reject` | `{expectedVersion}` | 200 rejected; đúng người nhận; chỉ pending còn hạn |
| `DELETE /servers/{id}/member-invitations/{invitationId}` | `expectedVersion` | 200 cancelled; quyền tạo mời; chỉ pending |
| `POST /servers/{id}/ownership-transfer` | `{newOwnerUserId,expectedVersion}` | 200 server; owner hiện tại, target là thành viên active/verified; chuyển ngay, không tạo pending |
| `DELETE /servers/{id}/members/me` | Không có body | 204 tự rời; owner chưa chuyển nhận 409; chặn HTTP/realtime/media theo ngưỡng |
| `POST /servers/{id}/channels` | `{name,topic?,kind}` | 201 phòng text/voice; đúng quyền quản lý phòng; mặc định mọi thành viên xem |
| `PATCH /servers/{id}/channels/{channelId}` | `{name?,topic?,expectedVersion}` | 200 phòng; đúng quyền quản lý phòng |
| `DELETE /servers/{id}/channels/{channelId}` | `expectedVersion` | 204 xóa logic, dừng truy cập/kết nối; không tự xóa vật lý tin/backup |
| `GET /servers/{id}/channels` | Phiên/thành viên | 200 chỉ phòng được xem; phòng deleted không được trả |

Thiết kế danh mục quyền tối thiểu: quản lý phòng, tạo/thu hồi mời, duyệt/từ chối yêu cầu, đổi join mode và cấu hình quyền xem phòng. Chỉ owner quản lý vai trò/chuyển ownership/sửa metadata server. Vai trò mặc định không tự có quyền quản lý. Tên mã permission, số vai trò và phiên bản cấu hình là thiết kế còn cần rà soát; không thêm quyền xóa tin người khác.

Unique membership `(serverId,userId)`, unique pending request và cập nhật có điều kiện bảo vệ join/approve/accept lặp. Chuyển owner khóa server và hai membership; target phải vẫn là thành viên active/verified tại commit. Mời có giới hạn khóa record, tạo membership và tăng lượt cùng transaction; rollback không tiêu lượt. Xóa phòng kiểm tra lại quyền và version, chuyển trạng thái deleted rồi phát sự kiện thu hồi sau commit; giữ tombstone để API cũ không phục hồi phòng.

Lỗi đề xuất: validation 400; thiếu phiên 401; trái quyền quản lý 403; tài nguyên không được biết 404; request/version/owner conflict 409; vượt limiter 429; phụ thuộc tạm lỗi 503. Tên errorCode và schema chi tiết cần khóa cùng frontend/mock trước triển khai. Role API/configuration và tin phòng sẽ được máy đọc hóa sau khi chốt mô hình quyền; bảng này không phải OpenAPI đã duyệt.

Mỗi endpoint/sự kiện cần schema request/response, actor/quyền, lỗi theo [ProblemDetails](../architecture.md#contracts), giao dịch, version và retry. Community sở hữu metadata/thành viên/quyền; Messaging sở hữu tin. Messaging kiểm tra quyền qua `IChannelAccessChecker`, không đọc/JOIN schema Community.

Cơ chế tin dùng chung được thiết kế tại [DM](direct-messaging.md#contracts). Dữ liệu biên TC-TEXT áp dụng cả gửi/sửa tin phòng; không chép một phiên bản quy tắc ký tự hoặc chống trùng khác ở đây.

<a id="acceptance"></a>

## 5. Tiêu chí chấp nhận

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
| AC-COM-24 | Thành viên gửi/sửa tin tại biên 2.000/2.001 ký tự hoặc tin chỉ có khoảng trắng. | Nhận đến 2.000 ký tự, từ chối vượt giới hạn và tin trống; cho xuống dòng và emoji. |
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

Các tiêu chí mới dẫn tới DEC-072–077/087; toàn bộ AC-COM chưa có kết quả thực thi được ghi nhận. Ca chat thu hồi đo ≤5 giây theo DEC-083; ngưỡng thu hồi media còn chờ quyết định.

<a id="tests"></a>

## 6. Ca kiểm thử

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

Các ca này bao phủ quyền đã chốt; cần bổ sung ca cho từng AC-COM khi hoàn thiện backend/API. Chạy trực tiếp API để kiểm tra quyền, không chỉ nhìn nút UI. Ca nội dung tin dùng [TC-TEXT](direct-messaging.md#tests). Ca gửi lại/đồng thời cũng cần chạy cho tin phòng sau khi chọn hợp đồng.

Ca AC-COM-22 kiểm tra điều kiện chưa xác minh phía máy chủ; DEC-051 vẫn chặn truy cập ứng dụng từ trước, không suy rằng người chưa xác minh được đọc phòng. Chat thu hồi đã chốt DEC-083; ca media phụ thuộc ngưỡng chưa chốt ghi “Chờ chốt quy tắc”, không đánh dấu đạt. Kết quả theo [mẫu nghiệm thu](../release-operations.md#testing).

<a id="gaps"></a>

## 7. Vấn đề còn mở

### Quy tắc cần làm rõ trước khi xác nhận

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Lời mời | Link theo DEC-075, mời đích danh theo DEC-087; còn schema/transaction tiêu thụ lượt và chống race. | OQ-003, OQ-004 |
| Yêu cầu tham gia | Đã chốt DEC-073; còn đồng thời giữa duyệt/từ chối/hủy và rate limit gửi mới. | OQ-003, OQ-004 |
| Cộng đồng riêng tư | Đã chốt DEC-074; hạn 7 ngày/từ chối/hủy theo DEC-087; còn schema/thông báo và đồng thời. | OQ-003, OQ-004 |
| Cấp quyền | Đã chốt vai trò/ngoại lệ, người quản lý vai trò và thứ tự xung đột; còn mô hình dữ liệu, cấu hình, giới hạn và hành vi thu hồi đồng thời tại SCDC-FR-ACL-001. | OQ-004 |
| Nhắn tin phòng | Đã chốt UTF-16 và không tự hết hạn tin tại DEC-068/070; còn fixture, tránh trùng, backup và dữ liệu sau khi rời/mất quyền. | OQ-005, OQ-011 |
| Rời cộng đồng | Chuyển chủ sở hữu đã chốt DEC-076; tham gia lại theo join mode hiện hành/vai trò mặc định đã chốt DEC-087; retention/cache sau khi rời còn OQ-011. | OQ-003 |

### Chi tiết còn thiếu để hoàn tất thiết kế phân quyền

| Mã theo dõi | Quyết định cần có | Phần bị ảnh hưởng | Đầu mối dự kiến |
|---|---|---|---|
| ACL-O1 | Mô hình dữ liệu vai trò mặc định, danh mục quyền quản lý; giới hạn số vai trò | Tạo phòng, mời, duyệt, đổi cấu hình/quyền xem; người quản lý vai trò đã chốt DEC-058 | Vg; Sáng đánh giá thiết kế |
| ACL-O2 | Màn hình đặt giới hạn, giao dịch cập nhật và version quyền | Danh sách phòng, lịch sử, gửi, thời gian thực; thứ tự đã chốt DEC-057 | Vg, Sáng |
| ACL-O3 | Chuyển ngay chủ sở hữu theo DEC-076; cần giao dịch/kiểm thử đồng thời | Quản lý vòng đời cộng đồng; quyền xem đã chốt DEC-056 | Vg |
| ACL-O4 | Lời mời đích danh đã chốt DEC-074; hạn/transition theo DEC-087; còn schema và đồng thời | Cộng đồng riêng tư | Vg |
| ACL-O5 | Chat ≤5 giây đã chốt DEC-083; cần cơ chế, đo đồng thời và ngưỡng thu hồi media | Thiết kế đồng bộ quyền, kiểm thử chất lượng | Vg, Sáng, Thái |

Các mục trên tiếp tục thuộc OQ-003/OQ-004/OQ-007. DM hai người không phụ
thuộc mô hình vai trò cộng đồng; có thể rà soát phần ACL-01 đến ACL-05
riêng trong gói bàn giao DM.
