<a id="scdc--mục-tiêu-yêu-cầu-và-phạm-vi"></a>

# SCDC — Tổng quan dự án

SCDC là ứng dụng giao tiếp trên web cho nhóm bạn và cộng đồng. Trang này nói SCDC phục vụ ai, gồm những yêu cầu nào, phạm vi v1 là gì, và thế nào là thành công.

**Trạng thái:** v1 là sản phẩm hoàn thiện đầu tiên, đang phát triển. MVP là mốc nội bộ đã xong phần nền ([MVP](../releases/mvp.md)); hồ sơ bàn giao xem [v1](../releases/v1.md). Ngày 2026-11-01, mobile, thông báo và gửi file được thêm vào kế hoạch ([phần bổ sung](#extensions)).

Kế hoạch, nhân sự và ngân sách xem [kế hoạch](planning.md). Cách làm việc xem [quy trình](workflow.md). Câu hỏi chưa trả lời xem [câu hỏi còn mở](open-questions.md).

<a id="needs"></a>

<a id="nhu-cầu-và-người-sử-dụng"></a>

## Người dùng và nhu cầu

SCDC phục vụ ba nhu cầu, không giới hạn chủ đề:

- Tham gia một cộng đồng có sẵn và nói chuyện với các thành viên.
- Tạo cộng đồng và tổ chức không gian trò chuyện chung.
- Nhắn tin riêng giữa hai người.

Người dùng có thể nhắn tin, vào phòng thoại, gọi video và chia sẻ màn hình. Người quản lý cộng đồng có thể tạo phòng, quản lý thành viên và phân quyền cơ bản. Discord và Slack là sản phẩm tham khảo về trải nghiệm.

| Nhóm | Cần gì |
|---|---|
| Nhóm bạn | Trò chuyện chung trong các phòng của server hoặc riêng hai người; thoại, video, chia sẻ màn hình |
| Người nhắn tin riêng | Trao đổi trực tiếp với một người |
| Thành viên cộng đồng | Vào các phòng để trao đổi theo chủ đề |
| Người tạo và quản lý cộng đồng | Tạo không gian chung, tổ chức phòng, mời và quản lý thành viên, phân quyền |

<a id="thuật-ngữ"></a>

### Thuật ngữ

| Từ | Nghĩa |
|---|---|
| Server, cộng đồng | Một không gian cộng đồng trong ứng dụng, có thành viên và các phòng (không phải máy chủ vật lý) |
| Phòng (channel) | Khu vực trò chuyện trong một server |
| Tin nhắn riêng (DM) | Cuộc trò chuyện riêng giữa hai người |
| Phòng thoại | Phòng trong server mà thành viên vào và rời để nói chuyện trực tiếp |

Các từ kỹ thuật khác xem [thuật ngữ](../glossary.md).

**Chưa biết:** nhóm người dùng đầu tiên, ngôn ngữ, khu vực và cách đo giá trị (OQ-001, OQ-012). DM là chức năng ưu tiên; giá trị của việc chia nhiều chủ đề trong cộng đồng cần kiểm chứng thêm. Bộ câu hỏi phỏng vấn cũ trong archive chưa từng được dùng để khảo sát.

<a id="requirements"></a>

<a id="yêu-cầu-cấp-cao"></a>

## Yêu cầu

| Mã | Yêu cầu |
|---|---|
| REQ-001 | Phục vụ nhóm bạn và cộng đồng, không giới hạn chủ đề |
| REQ-002 | Có server, phòng, quản lý thành viên và quyền cơ bản |
| REQ-003 | v1 chạy trên trình duyệt web |
| REQ-004 | Nhắn tin trong phòng của server |
| REQ-005 | Nhắn tin riêng giữa hai người |
| REQ-007 | Phòng thoại trong server, thành viên vào và rời được |
| REQ-008 | Gọi riêng giữa hai người |
| REQ-009 | Camera và chia sẻ màn hình trong phòng thoại và cuộc gọi riêng |
| REQ-010 | Phát hành công khai, người dùng tự đăng ký |
| REQ-011 | Thời gian thực hiện mục tiêu khoảng 3 tháng |
| REQ-012 | MVP là một ứng dụng API duy nhất (modular monolith); v1 dùng microservice theo yêu cầu môn học (DEC-116). Môn học không bắt buộc số service, Gateway, broker hay Kubernetes |
| REQ-013 | Nhóm tự lập dự toán dựa trên phạm vi và nguồn lực |
| REQ-014 | Ứng dụng mobile; nền tảng, chức năng và đợt bàn giao cần xác định (DEC-173) |
| REQ-015 | Thông báo cho web và mobile; loại, kênh và đợt bàn giao cần xác định |
| REQ-016 | Gửi file; ngữ cảnh, giới hạn, vòng đời và đợt bàn giao cần xác định |

REQ-006 (nhóm chat riêng ngoài server) đã bị loại từ phiên bản 1.3. Mã này giữ trong lịch sử và không dùng lại.

<a id="scope"></a>

<a id="hai-mốc-bàn-giao-phạm-vi-v1-và-ràng-buộc"></a>

## MVP và v1

| Mốc | Nội dung | Hồ sơ |
|---|---|---|
| MVP (nội bộ) | Tài khoản, cộng đồng và nhắn tin riêng trong một ứng dụng API; ghi bằng chứng, giới hạn và việc chuyển sang v1 | [Nền MVP](../releases/mvp.md) |
| v1 (sản phẩm) | Tách microservice và hoàn thiện toàn bộ phạm vi SCP bên dưới, gồm gọi riêng, phòng thoại, video, chia sẻ màn hình và điều kiện phát hành | [Hồ sơ v1](../releases/v1.md) |

- MVP được thêm vào để làm tài khoản, cộng đồng, DM trước media (DEC-114). Phạm vi đầy đủ trước đây gọi là "MVP" nay gọi là v1; mã yêu cầu và quy tắc giữ nguyên.
- Việc MVP còn thiếu được xếp vào gói v1; gói nào đủ đầu vào thì [bắt đầu](workflow.md#readiness), không chờ nghiệm thu toàn MVP.
- Nghiệm thu v1 theo [điều kiện hoàn tất](../releases/acceptance.md#v1-completion).

<a id="phạm-vi-bản-hoàn-thiện-v1"></a>

### Phạm vi v1

| Mã | Hạng mục | Nội dung |
|---|---|---|
| SCP-001 | Nền tảng | Chạy trên trình duyệt web |
| SCP-002 | Tài khoản | Đăng ký, xác minh, đăng nhập, hồ sơ, mật khẩu, email, phiên ([Tài khoản](../features/accounts/README.md)) |
| SCP-003 | Cộng đồng | Server, thành viên, lời mời, phòng và quản lý ([Cộng đồng](../features/community/README.md)) |
| SCP-004 | Phân quyền | Vai trò, ACL, quyền trên server và phòng; phối hợp thu hồi với Messaging và Media ([Quyền](../features/community/access-control.md)) |
| SCP-005 | Nhắn tin | DM và tin phòng: gửi, nhận, lịch sử, sửa, xóa, gửi lại không trùng, đồng thời, kết nối lại, thu hồi quyền ([Nhắn tin](../features/messaging/README.md)) |
| SCP-006 | Thoại | Gọi riêng (nhận, từ chối, hủy, kết thúc); phòng thoại (vào, rời, micro) ([Media](../features/media/README.md)) |
| SCP-007 | Video và chia sẻ màn hình | Camera và chia sẻ màn hình trong gọi riêng và phòng thoại; quyền thiết bị, nhiều nguồn, dừng nguồn, kết nối lại ([Media](../features/media/README.md)) |
| SCP-008 | Phát hành | Kiểm thử bản đầy đủ, migration, rollback ứng dụng, giám sát, hỗ trợ, bàn giao vận hành trước khi mở đăng ký ([nghiệm thu](../releases/acceptance.md#v1-completion), [runbook](../guides/operations.md)) |

- Kiến trúc v1: bốn service, dữ liệu và triển khai độc lập ([kiến trúc](../system/architecture.md#target), [gói chuyển đổi](planning.md#v1-architecture-packages)).
- Dữ liệu MVP được [giữ lại](../system/database.md#v1-mvp-data-transition) và [chuyển offline](../system/database.md#v1-mvp-data-cutover); cập nhật theo [chính sách offline](../system/environments.md#v1-offline-updates). Thiết kế [theo từng gói](planning.md#package-design).

**Ngoài phạm vi v1:**

- Nhóm chat riêng ngoài server, và gọi trong nhóm đó.
- Ứng dụng cài đặt trên máy tính. (Mobile đã vào kế hoạch bổ sung, chưa chốt đợt.)
- Xử lý DM không mong muốn. (Gửi file đã vào kế hoạch bổ sung, chưa chốt đợt.)
- Backup PostgreSQL, WAL, khôi phục theo thời điểm và khôi phục từ backup: để [sau v1](#backup-scope).

"Hoàn thiện" nghĩa là làm xong phạm vi đã chọn. Chức năng thêm phải đi qua quản lý thay đổi và được xác nhận trước khi đặc tả và ước lượng.

<a id="extensions"></a>

### Phần bổ sung (2026-11-01)

Ba hạng mục đã thống nhất làm và có người phụ trách (DEC-173), nhưng chưa quyết định thuộc v1 hay đợt sau, chưa có code, chưa cam kết lịch hay chi phí.

| Mã | Hạng mục | Cần làm rõ |
|---|---|---|
| SCP-009 | Ứng dụng mobile | Nền tảng, phạm vi, khung chung (Vg); giao diện và chức năng từng mảng (mỗi người). [Mobile](../experience/mobile.md), REQ-014 |
| SCP-010 | Thông báo | Sự kiện, người nhận, trong app hay push, thiết bị, trạng thái (Sáng). [Thông báo](../features/notifications/README.md), REQ-015 |
| SCP-011 | Gửi file | Tải lên, lưu, tải xuống, quyền, giao diện, tích hợp Messaging (Thái). [Gửi file](../features/files/README.md), REQ-016 |

Các gói EXT-MOBILE, EXT-NOTIFY, EXT-FILE ([kế hoạch](planning.md#extension-packages)) phải chốt phạm vi, phụ thuộc và cách kiểm thử trước khi vào một mốc nghiệm thu. Tiêu chí trình duyệt và media hiện có không tự áp cho app mobile.

<a id="backup-scope"></a>

### Backup để sau v1

Theo [DEC-156](../records/decisions/README.md#dec-156), các việc sau **không** là điều kiện hoàn tất v1:

- Tạo base backup, lưu WAL liên tục, chép ra kho ngoài máy.
- Thời hạn lưu backup, khôi phục theo thời điểm (PITR), bảo vệ dữ liệu khi khôi phục (gồm sổ bảo vệ độc lập).
- Diễn tập khôi phục toàn hệ thống, đo RPO và RTO (DEC-086, 108, 109).

Thiết kế giữ lại cho giai đoạn sau, tại [vòng đời dữ liệu](../system/data-lifecycle.md#restore); hướng lưu ra Google Drive tại [môi trường](../system/environments.md#v1-postgres-backup-storage).

**v1 vẫn phải:** lưu dữ liệu bền qua khởi động lại; thực thi thời hạn lưu; giữ khóa cần thiết, quyền, thu hồi, chống trùng và dấu chặn; migration giữ đúng dữ liệu, có cách rollback ứng dụng phù hợp schema. v1 không cam kết khôi phục được khi mất máy hay hỏng đĩa.

<a id="ràng-buộc-và-cơ-sở-lập-kế-hoạch"></a>

### Ràng buộc

| Mục | Giá trị |
|---|---|
| Kiến trúc | MVP một ứng dụng; v1 microservice (DEC-116). Ranh giới service đã chốt ([kiến trúc](../system/architecture.md#service-boundaries), DEC-119); code chưa chuyển |
| Thời gian | Khoảng 3 tháng từ 2026-10-05 (DEC-088); lịch còn là đề xuất |
| Ngân sách minh họa | 250.000.000 VNĐ (phương án 16 tuần cũ, DEC-088); không phải báo giá hay trần được duyệt |
| Nhân sự | Ba người, mỗi người 3–4 ngày/tuần từ 2026-10-05 (DEC-088). Xem [phân công](planning.md#team) và [công suất](planning.md#capacity) |
| Quy mô dự trù | 1.000 tài khoản, 100 người online cùng lúc, 20 người gọi cùng lúc |

- 1.000 tài khoản và 100 người online là giả định để dự toán và kiểm thử, không phải giới hạn. 20 người gọi cùng lúc là giới hạn sản phẩm đã chốt (DEC-079).
- Nhóm hiện tại khác giả định ban đầu (ba kỹ sư có kinh nghiệm); cần tính thời gian kiêm nhiệm và hướng dẫn khi ước lượng ([dự toán](planning.md#budget)).
- Ảnh hưởng của việc thu hẹp phạm vi nhắn tin và gọi tới công sức, chi phí, tiến độ cần tính lại khi cập nhật kế hoạch.

<a id="kết-quả-bàn-giao"></a>

### Bàn giao gì

- Ứng dụng web và các service theo phạm vi đã đặc tả.
- Hồ sơ yêu cầu, thiết kế trải nghiệm, thiết kế kỹ thuật.
- Kết quả kiểm thử và hồ sơ nghiệm thu.
- Hướng dẫn triển khai, cấu hình, vận hành, bàn giao.

<a id="success"></a>

<a id="tiêu-chí-thành-công-và-chất-lượng"></a>

## Thế nào là thành công

| Mã | Kết quả | Đánh giá bằng |
|---|---|---|
| SUC-001 | Người dùng đăng ký và dùng được các hình thức giao tiếp trong phạm vi | Kịch bản sử dụng và nghiệm thu trên môi trường phát hành |
| SUC-002 | Người quản lý tổ chức phòng, mời thành viên, áp dụng quyền cơ bản | Ma trận quyền; các ca cho phép và từ chối |
| SUC-003 | Tin nhắn được lưu và xem lại được | Kiểm thử gửi, nhận, xem lịch sử |
| SUC-004 | Thoại, video, chia sẻ màn hình chạy trên trình duyệt hỗ trợ | Kiểm thử theo quy mô phòng, điều kiện mạng, chất lượng |
| SUC-005 | v1 là microservice: mỗi service sở hữu dữ liệu và triển khai độc lập | Hồ sơ ranh giới, hợp đồng, dữ liệu; bằng chứng chạy và triển khai độc lập; chạy lại các luồng MVP. Code hiện chưa chuyển |
| SUC-006 | Sẵn sàng phát hành công khai | Kết quả kiểm thử, nghiệm thu, xác nhận vận hành |

### Chất lượng

Các chỉ tiêu đã chốt (DEC-082, 083, 085), chính sách dữ liệu (DEC-103 đến 109) và lỗi còn tồn (DEC-110). Backup để sau v1 (DEC-156). Chi tiết xem [chất lượng](../system/quality.md).

| Nội dung | Đã chốt | Còn phải làm | Theo dõi |
|---|---|---|---|
| Trình duyệt, thiết bị, truy cập | Tài khoản, DM, Community trên máy tính và điện thoại; media chỉ trên máy tính (DEC-059, 082) | Ghi phiên bản, thiết bị, build; chạy các hành trình và ca bàn phím, bộ gõ, focus, lỗi, cuộn; làm rõ tiêu chí truy cập đầy đủ | OQ-007 |
| Hiệu năng và tải | Mức tải, thời lượng, độ trễ, tỷ lệ lỗi, tính đúng (DEC-083) | Chốt dữ liệu thử, môi trường, công cụ đo; chạy tải và lưu kết quả | OQ-007 |
| Chất lượng media | Giới hạn người và nguồn, kết nối lại, điều kiện mạng, mục tiêu kết nối, độ trễ, hình (DEC-079, 080, 085) | Chuẩn bị thiết bị, mạng, công cụ; chốt [cách kết luận chất lượng hình](../system/quality.md#measurement-readiness) | OQ-006, OQ-007 |
| Thu hồi quyền, phiên | Chat và media ngắt trong 5 giây; không xác nhận được quyền thì dừng media (DEC-083, 099) | Làm giao thức giữa service; đo từ commit tới lúc ngắt trên kết nối thật, kể cả khi phụ thuộc lỗi | OQ-007, OQ-008 |
| Thời hạn lưu | [Chính sách dữ liệu](../system/data-lifecycle.md) (DEC-103 đến 109); thuộc v1 | Làm và kiểm thử thời hạn lưu, xóa nội dung, chống trùng, quyền | OQ-011 |
| Backup (sau v1) | RPO ≤ 15 phút, RTO ≤ 4 giờ, backup giữ ≤ 30 ngày (DEC-086, 109); [hoãn](#backup-scope) | Chọn công cụ, cấu hình, đo khôi phục thật | OQ-011 |

Chưa có kết quả đo. Các con số hiệu năng trong tài liệu kỹ thuật cũ chưa có bằng chứng và không phải ngưỡng nghiệm thu; không tuyên bố truy vấn luôn dưới 10ms. Người duyệt nghiệm thu (DEC-111) cần được chọn trước khi phát hành.

---

Nguồn: DEC-079, DEC-088, [DEC-114](../records/decisions/README.md#dec-114), DEC-116, DEC-119, [DEC-156](../records/decisions/README.md#dec-156), DEC-169, DEC-170, [DEC-173](../records/decisions/README.md#dec-173).
