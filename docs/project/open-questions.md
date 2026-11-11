# SCDC — Câu hỏi còn mở

Trang này liệt kê các câu hỏi ảnh hưởng nhiều tính năng mà nhóm chưa trả lời xong, kèm người theo dõi và kết luận đã có. Chi tiết thiết kế và phần chưa làm nằm trong tài liệu từng tính năng.

Một câu hỏi còn "mở" có thể chỉ còn việc thiết kế chi tiết, làm code hoặc kiểm thử; không có nghĩa mọi quy tắc trong đó chưa chốt.

<a id="v1-decisions"></a>

## Cần quyết định cho v1

Rà soát ngày 2026-11-01.

| Cần quyết định | Cụ thể | Hạn | Theo dõi tại |
|---|---|---|---|
| Phạm vi mobile, thông báo, gửi file | Nền tảng và chức năng mobile; loại và kênh thông báo; ngữ cảnh và giới hạn file; thuộc v1 hay đợt sau | Trước khi chốt gói, lịch và tiêu chí bàn giao | [OQ-014](#oq-014), [OQ-015](#oq-015), [OQ-016](#oq-016) |
| Nhóm người dùng đầu tiên và giá trị sản phẩm | Ai dùng trước, hành trình tiêu biểu, cách biết sản phẩm giải quyết được nhu cầu. Hiện ưu tiên DM, sau đó Community | Khi viết hành trình nghiệm thu và chuẩn bị giới thiệu sản phẩm | [OQ-001](#oq-001), [OQ-012](#oq-012), [nhu cầu](overview.md#needs) |
| Ngôn ngữ và khu vực | Ngôn ngữ giao diện, email, trợ giúp; khu vực phục vụ đầu tiên | Trước khi chốt nội dung và cấu hình phát hành công khai | [OQ-001](#oq-001), [tổng quan](overview.md#needs) |
| Môi trường cho bản công khai | Nơi chạy, domain, HTTPS, dịch vụ email, tài nguyên và chi phí. Máy Linux và Gmail hiện có chỉ dùng cho phát triển và demo | Trước khi dựng môi trường và nghiệm thu phát hành | [OQ-002](#oq-002), [OQ-008](#oq-008), [OQ-010](#oq-010), [môi trường](../system/environments.md#v1-available-infrastructure) |
| Cách kết luận chất lượng hình | Cách lấy mẫu, tổng hợp và tỷ lệ thời gian tối thiểu phải đạt độ phân giải và FPS. Mục tiêu và điều kiện mạng đã chốt | Trước khi chốt phép đo và nghiệm thu | [OQ-007](#oq-007), [chuẩn bị đo](../system/quality.md#measurement-readiness) |
| Tiêu chí khả năng truy cập | Tiêu chí ngoài các yêu cầu đã có (nhãn, bàn phím, focus, thông báo lỗi). Chưa chọn chuẩn cụ thể | Khi chốt tiêu chí giao diện của gói liên quan | [OQ-007](#oq-007), [giao diện DM](../features/messaging/direct-messaging.md#phạm-vi-bàn-giao), [quy ước giao diện](../experience/design-system.md) |
| Người nghiệm thu và vận hành | Ai có quyền nghiệm thu, mở công khai, chấp nhận lỗi còn tồn; ai trực vận hành, ai thay thế, lịch và quyền hạn | Trước nghiệm thu và bàn giao vận hành | [OQ-011](#oq-011), [DEC-111](../records/decisions/README.md#dec-111), [runbook](../guides/operations.md#inputs) |

Những điều **không** cần quyết định lại: chính sách tài khoản, thành viên, vai trò, ACL, gửi lại thủ công, danh sách trình duyệt, giới hạn media và các ngưỡng đã chốt. Giới hạn tần suất bổ sung và tự động gửi lại không thành yêu cầu mới của v1. Backup và khôi phục làm [sau v1](overview.md#backup-scope). Đợt này không khảo sát người dùng bên ngoài (DEC-030).

Phần kỹ thuật còn thiếu (hợp đồng giữa service, quyền, phiên, dữ liệu, migration, phục hồi, cấu hình demo, tích hợp nhà cung cấp) được thiết kế [theo từng gói](planning.md#v1-next-design). Gói nào đủ đầu vào thì bắt đầu theo [điều kiện READY-01 đến 08](workflow.md#readiness), không cần chờ đóng mọi câu hỏi.

<a id="open-questions"></a>

<a id="2-vấn-đề-còn-mở"></a>

## Danh sách câu hỏi

| Mã | Câu hỏi | Theo dõi | Trạng thái |
|---|---|---|---|
| <a id="oq-001"></a>OQ-001 | Ai là người dùng tiêu biểu, hành trình nào ưu tiên, ngôn ngữ và khu vực nào? | Khách hàng, BA/UX | **Mở.** Đã biết (2026-09-30): đối tượng là nhóm người bất kỳ, ưu tiên nhắn tin riêng rồi tới cộng đồng. Còn thiếu người dùng tiêu biểu cụ thể, ngôn ngữ và khu vực. |
| <a id="oq-002"></a>OQ-002 | Quy tắc đăng ký, xác thực, khôi phục tài khoản và quản lý phiên? | Khách hàng, BA, kỹ thuật | **Mở.** Đã chốt định danh, mật khẩu, khóa khi sai nhiều lần, thời hạn và gửi lại link, hồ sơ, kênh khôi phục (DEC-063 đến 068). Phát triển và demo gửi email qua Gmail SMTP ([D37](../features/accounts/email-links.md#v1-email-delivery-model), DEC-155). Còn: tích hợp worker email từ nhánh `feat/identity` ([hiện trạng](../features/accounts/email-links.md#status)), kiểm thử gửi thật, xoay khóa, và dịch vụ email cho bản công khai. |
| <a id="oq-003"></a>OQ-003 | Tạo, tham gia, rời server; lời mời; tìm cộng đồng; quyền riêng tư? | Khách hàng, BA | **Mở.** Luồng đã chốt (DEC-072 đến 077, 087, 093 đến 097); phạm vi MVP theo DEC-118. Còn: tin phòng và realtime; duyệt, lời mời, rời, chuyển chủ, đổi sang riêng tư ở dạng đầy đủ. Tiến độ xem [Community](../features/community/README.md) và [hồ sơ kiểm thử](../records/verification/community/README.md). |
| <a id="oq-004"></a>OQ-004 | Vai trò và quyền trong server và phòng? | Khách hàng, BA | **Mở.** Đã chốt quyền xem, chủ sở hữu, vai trò @everyone, tối đa 20 vai trò, quyền cộng gộp, cần quyền xem mới quản lý được (DEC-055 đến 058, 092, 098). API vai trò và ACL đã có bằng chứng. Còn: ghi tin và đăng ký realtime cho tin phòng, đo thời gian thu hồi quyền, chuyển chủ, rời server, Media. |
| <a id="oq-005"></a>OQ-005 | Quy tắc gửi, nhận, lưu lịch sử; mất mạng và gửi lại; loại nội dung? | Khách hàng, BA, UX, kỹ thuật | **Mở.** Đã chốt nội dung, cách truyền, bản nháp DM (DEC-068 đến 071, 081, 090, 091); không tự xóa tài khoản, khóa tài khoản, thời hạn lưu (DEC-103 đến 109); v1 ghi tin trực tiếp vào DB nhưng giữ khả năng chuyển sang hàng đợi ([DEC-125](../features/messaging/message-lifecycle.md#v1-message-intake)). Gửi lại vẫn thủ công (DEC-021). DM đã có đầy đủ trong nền ([hồ sơ](../records/verification/messaging/merge-2026-10-13.md)). Còn: tin phòng, tách service, thời hạn lưu khi online, các mục DATA-GAP. |
| <a id="oq-006"></a>OQ-006 | Số người mỗi phòng, chất lượng thoại và video, chia sẻ màn hình, mất kết nối? | Khách hàng, UX, kỹ sư media, QA | **Mở.** Đã chốt (DEC-046 đến 050, 078 đến 085, 099 đến 102): cuộc gọi riêng, phòng 10 người, tổng 20, 2 màn hình chia sẻ, kết nối lại, chỉ máy tính, chất lượng, tự host, ngắt khi mất quyền, nhiều thiết bị, chia sẻ màn hình không kèm âm thanh. Đã có thiết kế API 19 thao tác. Còn: review, giới hạn tần suất, chọn phiên bản LiveKit và các mục MEDIA-GAP-01 đến 07. |
| <a id="oq-007"></a>OQ-007 | Trình duyệt và thiết bị hỗ trợ, hiệu năng, khả năng truy cập, cách nghiệm thu? | Khách hàng, kỹ thuật, QA | **Mở.** Đã chốt danh sách thiết bị (DEC-082), ngưỡng chat (DEC-083), mục tiêu media (DEC-085), ngắt khi mất quyền (DEC-099), lỗi còn tồn (DEC-110). Đã có phương pháp đo, 10 điều kiện phát hành, 12 bước kiểm tra sau triển khai. Còn: [cách kết luận chất lượng hình](../system/quality.md#measurement-readiness), tiêu chí truy cập đầy đủ, thiết bị, build và kết quả đo thật. |
| <a id="oq-008"></a>OQ-008 | Ranh giới service, dữ liệu, giải pháp media, triển khai, thành phần dùng lại? | Vg chủ trì, Sáng và Thái theo phần mình | **Mở.** Đến 2026-10-18 đã xác nhận D01 đến D52 (DEC-119 đến 170), tập hợp tại [kiến trúc v1](../system/architecture.md#target). Code vẫn là nền `900e226`, chưa tách service. Còn thiết kế chi tiết: [hợp đồng và xác thực](../system/api-conventions.md), [phiên, quyền, thu hồi](../system/access-revocation.md), [dữ liệu và migration](../system/database.md), [tạo phòng](../features/community/channels.md#v1-channel-provisioning), [tổ chức code](../system/backend.md), [cấu hình demo](../system/environments.md), [giám sát](../system/observability.md), tích hợp Media. Chưa chọn nơi chạy và email cho bản công khai. |
| <a id="oq-009"></a>OQ-009 | Công sức thực tế, lịch và dự toán theo phân công mới? | Vg, Sáng, Thái | **Mở.** Phân công mới từ 2026-11-01 (DEC-171), Trello và họp tuần (DEC-172). Giữ cơ sở 3 đến 4 ngày/tuần, dùng 80% cho công việc và 20% dự phòng. Cần ước lượng lại phần bổ sung DEC-173 trước khi chốt lịch và chi phí. Chưa cam kết ngày phát hành. |
| <a id="oq-010"></a>OQ-010 | Mức dùng media, băng thông, lưu trữ và chi phí sau phát hành? | Khách hàng, kỹ thuật, vận hành | **Mở.** Có mô hình tượng trưng 5 triệu/tháng (2026-10-04). Chưa có số liệu dùng, báo giá hay cấu hình được xác nhận. |
| <a id="oq-011"></a>OQ-011 | Quản trị nền tảng, xử lý vi phạm, hỗ trợ, lưu giữ dữ liệu? | Khách hàng, BA, vận hành | **Mở.** Đã chốt vòng đời dữ liệu và khôi phục (DEC-086, 103 đến 109); backup hoãn [sau v1](overview.md#backup-scope) ([DEC-156](../records/decisions/README.md#dec-156)); quản trị qua quy trình kỹ thuật có ghi log, không có trang quản trị (DEC-112). Đã có [chính sách](../system/data-lifecycle.md) và [runbook](../guides/operations.md). Còn: bằng chứng và công cụ cho DATA-GAP, RLS-GAP; người duyệt (DEC-111); người trực, lịch trực và quyền khóa tài khoản. |
| <a id="oq-012"></a>OQ-012 | Vấn đề ưu tiên cần giải quyết, giá trị sản phẩm và cách đánh giá? | Khách hàng, BA/UX | **Mở.** Đại diện sản phẩm nêu vấn đề: nhiều chủ đề làm trôi tin nhắn và tài liệu (2026-09-30). Chưa có bằng chứng từ người dùng ngoài (DEC-030). |
| <a id="oq-013"></a>OQ-013 | Đầu mối, thẩm quyền, người tham gia và lịch khảo sát? | Vg | **Đã xử lý** 2026-09-30: đầu mối tại SCDC-ORG-001; không khảo sát bên ngoài trong đợt này (DEC-030). |
| <a id="oq-014"></a>OQ-014 | Nền tảng, chức năng và đợt bàn giao mobile? | Vg chủ trì; mỗi người theo mảng | **Mở.** Đã thống nhất làm (DEC-173). Chi tiết tại [mobile](../experience/mobile.md). |
| <a id="oq-015"></a>OQ-015 | Loại và kênh thông báo, thiết bị, hợp đồng, đợt bàn giao? | Sáng; Vg thống nhất | **Mở.** Đã có người phụ trách (DEC-171, 173). Chi tiết tại [thông báo](../features/notifications/README.md). |
| <a id="oq-016"></a>OQ-016 | Ngữ cảnh, giới hạn, lưu trữ và vòng đời file; đợt bàn giao? | Thái; Sáng và Vg phối hợp | **Mở.** Đã có người phụ trách (DEC-171, 173). Chi tiết tại [gửi file](../features/files/README.md). |

Khi đóng một câu hỏi, ghi kết luận, ngày và tài liệu chứa kết quả. Vai trò trong nhóm xem [SCDC-ORG-001](planning.md#team).
