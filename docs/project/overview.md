<a id="scdc--mục-tiêu-yêu-cầu-và-phạm-vi"></a>

# SCDC — Mục tiêu, yêu cầu và phạm vi

Nguồn chuẩn cho nhu cầu, thuật ngữ và mã REQ/SCP/SUC của dự án. **v1 là sản phẩm hoàn thiện đầu tiên và là mục tiêu của bộ tài liệu hiện hành.** Hồ sơ bàn giao tại [v1](../releases/v1.md); nền phát triển nội bộ và phần việc chuyển tiếp được ghi tại [MVP](../releases/mvp.md).

## Mục lục

| Nội dung | Tài liệu |
|---|---|
| Nhân sự, công suất, ngân sách và lịch | [Kế hoạch](planning.md) |
| Điều kiện triển khai | [Sẵn sàng phát triển](workflow.md#readiness) |
| Trách nhiệm, đầu ra và quản lý thay đổi | [Quy trình](workflow.md) |

Phạm vi sản phẩm đã được xác định ở mức tổng thể. Đầu vào còn thiếu được theo dõi tại [vấn đề còn mở](open-questions.md) và từng chủ đề; có code trong repo chưa đồng nghĩa đã nghiệm thu hoặc sẵn sàng phát hành.

<a id="needs"></a>

<a id="nhu-cầu-và-người-sử-dụng"></a>

## Nhu cầu và người sử dụng

Xây dựng ứng dụng giao tiếp trực tuyến dành cho nhóm bạn và cộng đồng không
giới hạn chủ đề, phục vụ ba nhu cầu sử dụng:

- Kết nối với một cộng đồng hiện có và giao tiếp với các thành viên.
- Tạo một cộng đồng và tổ chức không gian giao tiếp chung.
- Nhắn tin riêng giữa hai người.

Người sử dụng có thể nhắn tin, tham gia phòng thoại, gọi video và chia sẻ
màn hình. Người quản lý cộng đồng có thể tổ chức phòng, quản lý thành viên
và cấp quyền cơ bản.

Ngày 2026-09-30, đại diện sản phẩm xác định đối tượng là một nhóm người bất
kỳ và nêu vấn đề của nhóm chat một luồng: nhiều người trao đổi khác chủ đề
khiến tin nhắn bị trôi, khó theo dõi liên tục; file tài liệu cũng gặp vấn
đề tương tự. Ưu tiên trước mắt là nhắn tin và tham gia cộng đồng, vì chức
năng nhắn tin cơ bản là nền tảng để phát triển thêm tính năng. Nhắn tin
riêng giữa hai người được ưu tiên trước; cộng đồng được tổ chức bằng nhiều
phòng theo chủ đề trước. Các quyết định về cách tìm người để nhắn riêng,
tin nhắn văn bản và cách tham gia cộng đồng được ghi tại
[SCDC-DIS-001](#needs). Đặc tả hành vi
nhắn tin riêng đợt đầu được quản lý tại
[SCDC-FR-DM-001](../features/messaging/direct-messaging.md#requirements).

Đợt hiện tại không khảo sát người dùng bên ngoài theo
[DEC-030](../records/decisions/README.md#decisions). Đầu vào từ đại diện sản phẩm
được dùng để đặc tả yêu cầu và vẫn được đánh dấu là chưa kiểm chứng với
người dùng bên ngoài.

Discord là sản phẩm tham chiếu về trải nghiệm giao tiếp và cách tổ chức
cộng đồng. Danh sách yêu cầu dưới đây xác định phạm vi cần phát triển.

| Nhóm | Nhu cầu sử dụng |
|---|---|
| Nhóm bạn | Giao tiếp chung trong các phòng của server hoặc riêng giữa hai người; sử dụng thoại/video và chia sẻ màn hình. |
| Người nhắn tin riêng | Trao đổi trực tiếp với một người khác qua hội thoại riêng. |
| Thành viên cộng đồng | Tham gia các phòng để trao đổi và giao tiếp theo chủ đề. |
| Người tạo và quản lý cộng đồng | Tạo không gian chung, tổ chức phòng, mời và quản lý thành viên, thiết lập quyền. |
<a id="thuật-ngữ"></a>

### Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| Server | Không gian cộng đồng trên ứng dụng, có thành viên và các phòng; phân biệt với máy chủ hạ tầng. |
| Channel/phòng | Khu vực giao tiếp bên trong một server. |
| Tin nhắn riêng (DM) | Hội thoại riêng giữa hai người. |
| Phòng thoại | Phòng trong server mà thành viên có thể vào và rời để giao tiếp trực tiếp. |

Phạm vi bàn giao được tổng hợp tại [Project Brief](#scope).
Những quy tắc nghiệp vụ và giới hạn cần xác định được theo dõi tại
[vấn đề còn mở](open-questions.md).


Chưa xác định nhóm người dùng đầu tiên, ngôn ngữ/khu vực và thước đo giá trị tại OQ-001/OQ-012. DM là nền chức năng ưu tiên; giá trị tách nhiều chủ đề cần tiếp tục kiểm chứng ở cộng đồng. Bộ câu hỏi phỏng vấn cũ được lưu dự phòng trong archive, không phải hoạt động khảo sát đã thực hiện.

<a id="requirements"></a>

<a id="yêu-cầu-cấp-cao"></a>

## Yêu cầu cấp cao

| Mã | Nội dung |
|---|---|
| REQ-001 | Phục vụ nhóm bạn và cộng đồng, không giới hạn chủ đề. |
| REQ-002 | Có không gian cộng đồng (server), phòng, quản lý thành viên và quyền cơ bản. |
| REQ-003 | Bản hoàn thiện v1 hoạt động trên trình duyệt web. |
| REQ-004 | Cho phép nhắn tin trong phòng thuộc server. |
| REQ-005 | Cho phép nhắn tin riêng giữa hai người. |
| REQ-007 | Có phòng thoại trong server, cho phép thành viên vào và rời phòng. |
| REQ-008 | Hỗ trợ gọi riêng giữa hai người. |
| REQ-009 | Hỗ trợ camera/video và chia sẻ màn hình trong phòng thoại của server và cuộc gọi riêng giữa hai người. |
| REQ-010 | Phát hành công khai, cho phép người dùng tự đăng ký. |
| REQ-011 | Thời gian thực hiện mục tiêu khoảng 3 tháng. |
| REQ-012 | MVP giữ một API host Modular Monolith; v1 đáp ứng yêu cầu microservice của môn học theo DEC-116. Chưa bắt buộc số service, Gateway, broker hoặc Kubernetes; ranh giới/dữ liệu/hợp đồng và chuyển đổi được thiết kế, kiểm chứng trong v1. |
| REQ-013 | Bên phát triển lập dự toán trên cơ sở phạm vi và nguồn lực cần thiết. |

REQ-006 về nhóm chat riêng ngoài server đã được loại khỏi phạm vi từ phiên
bản 1.3. Mã yêu cầu này được giữ trong lịch sử và không sử dụng lại.

<a id="scope"></a>

<a id="hai-mốc-bàn-giao-phạm-vi-v1-và-ràng-buộc"></a>

## Mốc nội bộ MVP và sản phẩm v1

MVP là mốc làm việc và tích hợp nội bộ, ghi nhận nền đã có và công việc bàn giao sang v1. v1 là sản phẩm hoàn thiện đầu tiên, phát triển tiếp từ nền này. Phần việc MVP còn thiếu được xếp vào các gói v1; gói có đủ đầu vào trực tiếp bắt đầu theo [quy trình](workflow.md#readiness), không cần hoàn tất một vòng nghiệm thu toàn MVP riêng.

Theo [DEC-114](../records/decisions/README.md#dec-114), mốc MVP được bổ sung để triển khai Identity, Community và DM trước media. Phạm vi đầy đủ trước đây mang tên MVP được gọi là v1; yêu cầu, mã truy vết và quy tắc nghiệp vụ được giữ. DEC-116 chốt nền MVP một API host và microservice ở v1; DEC-117 cập nhật phân công kế hoạch.

| Mốc | Vai trò và phạm vi | Hồ sơ sử dụng |
|---|---|---|
| MVP — nội bộ | Snapshot nền Identity, Community và Direct Messaging trong một API host; ghi bằng chứng, giới hạn và phần việc chuyển tiếp | [Nền MVP và bàn giao sang v1](../releases/mvp.md) |
| v1 — sản phẩm hoàn thiện đầu tiên | Chuyển microservice và hoàn thiện toàn bộ phạm vi ở bảng SCP bên dưới, gồm gọi riêng/phòng thoại, video, chia sẻ màn hình và điều kiện phát hành công khai | [Hồ sơ v1](../releases/v1.md), đặc tả tính năng và điều kiện nghiệm thu/phát hành |

REQ/SCP/SUC dưới đây mô tả sản phẩm mục tiêu v1, dùng chung với đặc tả chủ đề và thiết kế hệ thống trong bộ tài liệu dự án. Bằng chứng nền MVP giữ phạm vi và revision đã ghi nhận; nghiệm thu sản phẩm và mở công khai áp dụng [điều kiện v1](../releases/acceptance.md#v1-completion).

<a id="phạm-vi-bản-hoàn-thiện-v1"></a>

### Phạm vi bản hoàn thiện v1

v1 kế thừa nền [MVP](../releases/mvp.md) và các mã requirement, scope, quy tắc, use case, tiêu chí đã có. Yêu cầu kiến trúc REQ-012/SUC-005 chuyển từ một API host sang bốn service nghiệp vụ, dữ liệu và triển khai độc lập theo [kiến trúc mục tiêu](../system/architecture.md#target); các gói chuyển đổi nằm tại [kế hoạch v1](planning.md#v1-architecture-packages).

| Mã | Hạng mục | Nội dung bàn giao |
|---|---|---|
| SCP-001 | Nền tảng | Ứng dụng chạy trên trình duyệt web. |
| SCP-002 | Tài khoản | Hoàn thiện đăng ký/xác minh, đăng nhập, hồ sơ, mật khẩu, email và phiên theo UC/AC tại [Accounts](../features/accounts/README.md). |
| SCP-003 | Cộng đồng | Hoàn thiện server, membership, invitation, channel và vòng đời quản lý theo [Community](../features/community/README.md). |
| SCP-004 | Phân quyền | Vai trò, ACL và quyền đối với server/phòng theo [Access control](../features/community/access-control.md), gồm phối hợp quyền/thu hồi với Messaging và Media. |
| SCP-005 | Nhắn tin | DM và tin phòng: gửi/nhận, lịch sử, sửa/xóa, retry chống trùng, đồng thời, reconnect và thu hồi quyền/phiên theo [Messaging](../features/messaging/README.md). |
| SCP-006 | Thoại | Gọi riêng hai người với nhận/từ chối/hủy/kết thúc; phòng thoại với vào/rời và điều khiển micro theo [Media](../features/media/README.md). |
| SCP-007 | Video và chia sẻ màn hình | Camera/chia sẻ màn hình trong gọi riêng và phòng thoại, gồm quyền thiết bị, nhiều nguồn, dừng nguồn và reconnect theo [Media](../features/media/README.md). |
| SCP-008 | Phát hành | Kiểm thử bản đầy đủ, migration, rollback ứng dụng, giám sát, hỗ trợ và bàn giao vận hành trước mở đăng ký công khai theo [nghiệm thu](../releases/acceptance.md#v1-completion) và [runbook](../guides/operations.md). |

v1 [giữ dữ liệu MVP](../system/database.md#v1-mvp-data-transition) theo DEC-169 và [chuyển offline](../system/database.md#v1-mvp-data-cutover) theo DEC-170. Các đợt cập nhật hiện tại theo [chính sách offline](../system/environments.md#v1-offline-updates); thiết kế và hiện thực tiếp tục [theo từng gói việc](planning.md#package-design).

Ngoài phạm vi bản hoàn thiện v1:

- Nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này.
- Ứng dụng cài đặt riêng trên desktop và thiết bị di động.
- Gửi file và các khả năng xử lý DM không mong muốn đã được để sang đợt sau.
- Triển khai và nghiệm thu backup PostgreSQL/WAL, PITR và khôi phục DB từ backup; chuyển sang sau v1 theo [phạm vi backup](#backup-scope), DEC-156.

“Bản hoàn thiện” nghĩa là hoàn thành phạm vi đã chọn. Các chức năng bổ sung được đánh giá theo quy trình quản lý thay đổi và cần xác nhận trước khi lập đặc tả, ước lượng.

<a id="backup-scope"></a>

### Phạm vi backup sau v1

Theo [DEC-156](../records/decisions/README.md#dec-156), các phần sau thuộc giai đoạn sau v1, không là điều kiện hoàn tất v1:

- Tạo base backup, lưu WAL liên tục và chuyển bản sao ra kho ngoài máy.
- Thực thi thời hạn lưu backup, phục hồi theo thời điểm (PITR) và bảo vệ dữ liệu khi restore, gồm sổ bảo vệ độc lập phục vụ restore.
- Diễn tập restore toàn hệ thống, đo và xác nhận RPO/RTO theo DEC-086/108/109.

Các chính sách, AC/TC và runbook backup/restore được giữ làm thiết kế cho giai đoạn đó; chưa có bằng chứng triển khai hoặc đạt ngưỡng. Thiết kế tại [vòng đời dữ liệu](../system/data-lifecycle.md#restore); hướng lưu trữ Google Drive hiện có tại [môi trường](../system/environments.md#v1-postgres-backup-storage).

v1 vẫn phải lưu dữ liệu bền vững qua restart, thực thi retention online, giữ khóa cần thiết, quyền/thu hồi, dedup và deny marker. Migration phải giữ toàn vẹn và có phương án rollback ứng dụng phù hợp schema. Phạm vi này không tạo cam kết khôi phục dữ liệu sau mất máy/đĩa.

<a id="ràng-buộc-và-cơ-sở-lập-kế-hoạch"></a>

### Ràng buộc và cơ sở lập kế hoạch

| Nội dung | Giá trị |
|---|---|
| Kiến trúc | MVP một API host Modular Monolith; microservice thuộc v1 theo DEC-116. Ranh giới tại [kiến trúc](../system/architecture.md#service-boundaries) đã chốt theo DEC-119; thiết kế dữ liệu/giao tiếp/triển khai còn mở, chưa chuyển đổi code. |
| Thời gian mục tiêu | Khoảng 3 tháng từ ngày khởi động. Ngày bắt đầu dự kiến 2026-10-05 theo DEC-088; lịch còn là đề xuất, chưa cam kết phát hành. |
| Ngân sách minh họa | 250.000.000 VNĐ từ phương án 16 tuần trước khi tách MVP/v1; giữ làm cơ sở dự toán cũ theo DEC-088, không là báo giá/trần được duyệt. Mốc 500 triệu giữ trong lịch sử. |
| Nhân sự hiện có | Vg, Sáng và Thái dự kiến mỗi người 3–4 ngày/tuần, đủ ngày làm việc, từ 2026-10-05 theo DEC-088. Theo DEC-117, Vg giữ quyết định quan trọng và Identity/Community; Sáng sở hữu DM/nền tin phòng; Thái sở hữu email worker/môi trường/công cụ, sau đó provider/SDK media. Chi tiết tại [phân công](planning.md#team) và [công suất](planning.md#capacity). |
| Quy mô dự trù | 1.000 tài khoản, 100 người trực tuyến đồng thời và 20 người tham gia gọi đồng thời trên toàn hệ thống. |

1.000 tài khoản và 100 người online là giả định tải phục vụ dự toán/kiểm thử; 20 người gọi đồng thời là giới hạn sản phẩm đã chốt DEC-079. Cơ cấu nhân sự hiện có khác với giả
định ba kỹ sư có kinh nghiệm ban đầu; cần tính công sức kiêm nhiệm và hướng
dẫn khi đánh giá tiến độ, chi phí. Xem [nhân sự và phương án làm rõ nhu cầu](planning.md#team)
và [dự toán và giả định](planning.md#budget).

Ảnh hưởng của việc thu hẹp phạm vi nhắn tin và cuộc gọi đến công sức, chi
phí và tiến độ cần được lượng hóa khi cập nhật kế hoạch chi tiết.

<a id="kết-quả-bàn-giao"></a>

### Kết quả bàn giao

- Ứng dụng web và các dịch vụ đáp ứng phạm vi chức năng được đặc tả.
- Hồ sơ yêu cầu, thiết kế trải nghiệm và thiết kế kỹ thuật.
- Kết quả kiểm thử và hồ sơ nghiệm thu.
- Hướng dẫn triển khai, cấu hình, vận hành và bàn giao hệ thống.

Nội dung chi tiết và điều kiện chấp nhận của từng kết quả bàn giao được xác
định trong đặc tả yêu cầu và kế hoạch thực hiện.

<a id="success"></a>

<a id="tiêu-chí-thành-công-và-chất-lượng"></a>

## Tiêu chí thành công và chất lượng

| Mã | Kết quả cần đạt | Cơ sở đánh giá |
|---|---|---|
| SUC-001 | Người dùng có thể đăng ký và sử dụng các hình thức giao tiếp trong phạm vi. | Kịch bản sử dụng và nghiệm thu trên môi trường phát hành. |
| SUC-002 | Người quản lý cộng đồng có thể tổ chức phòng, mời thành viên và áp dụng quyền cơ bản. | Ma trận quyền và các tình huống cho phép/từ chối truy cập. |
| SUC-003 | Tin nhắn được lưu và có thể xem lại. | Kiểm thử gửi, nhận và truy xuất lịch sử theo quy tắc nghiệp vụ. |
| SUC-004 | Thoại, video và chia sẻ màn hình hoạt động trên trình duyệt được hỗ trợ. | Kiểm thử theo quy mô phòng, điều kiện mạng và chất lượng đã xác định. |
| SUC-005 | v1 đáp ứng kiến trúc microservice theo DEC-116, giữ quyền sở hữu dữ liệu và khả năng triển khai độc lập; MVP giữ ranh giới module trong một API host. | v1 có hồ sơ ranh giới service, hợp đồng, dữ liệu và bằng chứng chạy/triển khai các API độc lập, cùng hồi quy luồng MVP; source hiện tại chưa chuyển đổi. |
| SUC-006 | Sản phẩm sẵn sàng cho phát hành công khai. | Kết quả kiểm thử, nghiệm thu và xác nhận khả năng vận hành. |

Chỉ tiêu định lượng đã chốt tại DEC-082/083/085; chính sách dữ liệu ở DEC-103–109; điều kiện lỗi tồn ở DEC-110. DEC-156 hoãn triển khai và nghiệm thu backup/khôi phục DB cùng các mục tiêu DEC-086/109 sang sau v1, không hoãn retention online, quyền truy cập, chống trùng hoặc rollback ứng dụng. Môi trường, dữ liệu tải, kết quả chạy và người duyệt DEC-111 cần hoàn tất trước nghiệm thu/phát hành thực tế.

Các quyết định dưới đây là đầu vào cho triển khai và nghiệm thu. Phần còn lại gồm chuẩn bị môi trường/phép đo, triển khai và thu thập bằng chứng; quy tắc kết luận chất lượng hình ảnh và phạm vi tiêu chí tiếp cận đầy đủ còn cần chốt tại [OQ-007](open-questions.md#oq-007).

| Nội dung | Cơ sở đã chốt | Phần còn lại | Theo dõi |
|---|---|---|---|
| Trình duyệt/thiết bị và khả năng tiếp cận | DEC-059/082 chốt [ma trận hỗ trợ](../system/quality.md#quality-targets): Accounts/DM/Community trên desktop và trình duyệt điện thoại đã chọn; media v1 trên desktop | Ghi phiên bản browser, OS, thiết bị, viewport, build và ngày khóa ma trận; chạy các hành trình và ca bàn phím/IME, focus, thông báo lỗi, cuộn và trạng thái thiết bị; làm rõ phạm vi tiêu chí tiếp cận đầy đủ còn mở | OQ-007 |
| Hiệu năng và tải | DEC-083 chốt mức tải, thời lượng, percentile độ trễ, tỷ lệ lỗi và tính đúng tại [mục tiêu chất lượng](../system/quality.md#quality-targets) | Khóa dataset/workload chi tiết, cấu hình môi trường và instrumentation; hiệu chỉnh đồng hồ, chạy tải và lưu kết quả theo build | OQ-007 |
| Chất lượng media | DEC-079/080/085 chốt giới hạn người/nguồn, reconnect, điều kiện mạng và mục tiêu kết nối/độ trễ/hình ảnh tại [mục tiêu chất lượng](../system/quality.md#quality-targets) | Chuẩn bị thiết bị, mạng, bộ đo và bằng chứng; chốt [quy tắc đánh giá hình ảnh](../system/quality.md#measurement-readiness), gồm tỷ lệ thời gian đạt resolution/FPS | OQ-006/OQ-007 |
| Thu hồi quyền/phiên | DEC-083/099 chốt thời hạn chat/media đang mở và dừng media khi không xác nhận được quyền; HTTP mới kiểm tra trạng thái hiện hành | Triển khai giao thức liên service; đo commit→cutoff trên kết nối đang mở, cả phát/nhận RTP và chặn vào lại; kiểm chứng khi dependency lỗi | OQ-007/OQ-008 |
| Lưu giữ online | DEC-103–109 chốt [chính sách dữ liệu](../system/data-lifecycle.md); retention online thuộc v1 | Triển khai và kiểm chứng retention, xóa nội dung, chống trùng và quyền theo build | OQ-011 |
| Backup và khôi phục DB — sau v1 | DEC-086/109 giữ mục tiêu RPO ≤15 phút/RTO ≤4 giờ và tuổi mỗi backup/WAL ≤30 ngày; [DEC-156 hoãn sang sau v1](overview.md#backup-scope) | Đợt sau v1 chọn công cụ, cấu hình cửa sổ PITR và thu thập bằng chứng restore thực tế | OQ-011 |

1.000 tài khoản và 100 người trực tuyến là giả định dự toán, không phải giới hạn đăng ký/online. Ngày 2026-10-04, 20 người gọi đồng thời trở thành giới hạn media v1 theo DEC-079; chưa có kết quả đo. Các mốc hiệu năng trong tài liệu kỹ thuật cũ chưa có bằng chứng và chưa được xác nhận làm ngưỡng nghiệm thu. Không tuyên bố truy vấn luôn dưới 10ms.
