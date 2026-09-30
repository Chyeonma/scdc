# SCDC — Đặc tả tham gia cộng đồng và phòng theo chủ đề, đợt đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-COM-001 |
| Phiên bản | 0.6 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Bản nháp — chờ làm rõ quy tắc và xác nhận tiêu chí chấp nhận |
| Căn cứ | REQ-002, REQ-004, SCP-003, SCP-004 và [SCDC-DIS-001](../02-discovery/01-users-and-needs.md) |

## 1. Mục tiêu và phạm vi đợt đầu

Người dùng có thể tìm cộng đồng công khai hoặc dùng liên kết mời để tham
gia. Trong cộng đồng, các phòng theo chủ đề giúp tách cuộc trò chuyện;
thành viên chỉ nhìn thấy phòng mình được cấp quyền xem và nhắn tin văn bản
trong phòng mình được phép nhắn.

Đợt này tập trung vào cách tham gia, hiển thị phòng và nhắn tin văn bản.
Việc gửi file tài liệu trong phòng được để sang đợt sau theo
DEC-023. Các tính năng thoại/video và chia sẻ màn hình thuộc phạm vi
[Project Brief](../01-initiation/01-project-brief.md) nhưng chưa được đặc
tả trong tài liệu này.

## 2. Hành trình tham gia

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

## 3. Quy tắc đã xác định

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

Các quyết định DEC-* được ghi tại
[sổ quyết định](../01-initiation/03-discovery-and-decision-log.md).

## 4. Tiêu chí chấp nhận dự thảo

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

## 5. Quy tắc cần làm rõ trước khi xác nhận

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Lời mời | Thời hạn được chọn theo những giá trị nào; giới hạn số lần dùng; cách hiển thị lời mời đã thu hồi. | OQ-003, OQ-004 |
| Yêu cầu tham gia | Người được quyền duyệt có được từ chối; người gửi có xem trạng thái hoặc hủy yêu cầu được không. | OQ-003, OQ-004 |
| Cộng đồng riêng tư | Ai có quyền thêm trực tiếp; có cần người được thêm chấp nhận; cách thông báo cho người được thêm. | OQ-003, OQ-004 |
| Cấp quyền | Ai cấp/thu hồi quyền quản lý; cách chọn người được xem phòng và thứ tự ưu tiên khi các quyền xung đột. | OQ-004 |
| Nhắn tin phòng | Giới hạn nội dung và cơ chế tránh trùng; quyền truy cập lịch sử khi đã rời/mất quyền. | OQ-005 |
| Rời cộng đồng | Quy tắc tham gia lại, quyền xem lịch sử sau khi rời và xử lý nếu chủ sở hữu muốn rời. | OQ-003 |

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Tạo bản nháp hành trình tham gia, quyền xem/tạo phòng và tiêu chí chấp nhận. |
| 0.2 | 2026-09-30 | Ghi nhận quyền tạo mời/duyệt, quyền xem phòng mặc định và nhắn tin văn bản với sửa/xóa; bổ sung tiêu chí chấp nhận. |
| 0.3 | 2026-09-30 | Bổ sung việc xem lại tin phòng đã lưu sau lúc vắng mặt. |
| 0.4 | 2026-09-30 | Chốt lịch sử cho thành viên mới, quyền đổi danh sách xem/gửi và thử lại không trùng. |
| 0.5 | 2026-09-30 | Bổ sung cộng đồng riêng tư, thu hồi lời mời và tự rời cộng đồng. |
| 0.6 | 2026-09-30 | Bổ sung điều kiện xác minh email trước khi gửi tin trong phòng. |

[Mục lục hồ sơ](../README.md)
