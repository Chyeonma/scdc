<a id="scdc--kế-hoạch-và-nguồn-lực"></a>

# SCDC — Kế hoạch và nguồn lực

Trang này là nơi duy nhất ghi: ai phụ trách gì, công suất của nhóm, ngân sách và giả định, rủi ro, các gói công việc và thứ tự làm. Phạm vi sản phẩm xem [tổng quan](overview.md); phạm vi từng mốc xem [MVP](../releases/mvp.md) và [v1](../releases/v1.md).

<a id="team"></a>

<a id="nhân-sự-và-phối-hợp"></a>

## Nhân sự

<a id="phân-công-thực-hiện-hiện-hành"></a>

### Phân công (từ 2026-11-01)

Mỗi người phụ trách trọn một hoặc vài service: phân tích yêu cầu, thiết kế giao diện, dữ liệu, backend, frontend, kiểm thử, tích hợp và tài liệu. Nhóm dùng AI hỗ trợ; mỗi người phải hiểu và giải thích được kết quả của mình.

| Thành viên | Phụ trách | Trách nhiệm chung | Thời gian |
|---|---|---|---|
| Vg | Identity và Community | Trưởng nhóm, đại diện sản phẩm: mục tiêu, phạm vi, kiến trúc tổng thể, quyết định quan trọng, kế hoạch, điều phối, triển khai hệ thống | 3–4 ngày/tuần |
| Sáng | Messaging và thông báo | Hợp đồng tin nhắn và thông báo, tích hợp với service khác, chuẩn bị triển khai phần mình | 3–4 ngày/tuần |
| Thái | Media control và LiveKit; gửi file; gửi email | Vòng đời cuộc gọi và phòng thoại, thiết bị, hợp đồng file và email, chuẩn bị triển khai phần mình | 3–4 ngày/tuần |

- Vg quyết định phạm vi và thiết kế tổng thể. Mỗi người tự thiết kế chi tiết và tạo nhiệm vụ Trello trong phạm vi đã thống nhất. Thay đổi ảnh hưởng phạm vi, kiến trúc hay hợp đồng chung phải thống nhất với Vg.
- Phân công phát triển chưa chọn người ký nghiệm thu hay người quyết định mở công khai (DEC-111, OQ-011).
- Phân công trong [MVP](../releases/mvp.md) giữ lại làm lịch sử.

<a id="ranh-giới-phối-hợp"></a>

### Phối hợp

| Việc | Người chịu trách nhiệm | Phối hợp |
|---|---|---|
| Sản phẩm, kiến trúc, kế hoạch chung | Vg | Sáng và Thái cung cấp ước lượng, tiến độ, phụ thuộc, đề xuất |
| Identity và Community | Vg | Công bố hợp đồng tài khoản, phiên, thành viên, quyền, vòng đời phòng |
| Messaging và thông báo | Sáng | Vg cung cấp quyền và phiên; Thái cung cấp thông tin file và sự kiện Media cần thông báo |
| Media control và LiveKit | Thái | Làm trọn vòng đời, giao diện, LiveKit, thiết bị; dùng hợp đồng quyền của Identity và Community; phối hợp thông báo với Sáng |
| Gửi file | Thái | Tải lên, lưu, tải xuống và giao diện. Sáng gắn file vào tin nhắn; Vg phối hợp quyền |
| Email tài khoản | Vg (nghiệp vụ) và Thái (gửi thư) | Identity cấp và dùng token, quyết định link còn hiệu lực. Thái làm mẫu thư, worker, gửi lại và kết quả gửi ([email](../features/accounts/email-links.md)) |
| Giao diện web và mobile | Người phụ trách từng mảng | Vg thống nhất trải nghiệm và bố cục chung ([mobile](../experience/mobile.md)) |
| Triển khai và môi trường chung | Vg chủ trì | Mỗi người cung cấp cấu hình, migration, đóng gói, cách chạy, kiểm tra sau triển khai cho phần mình |
| Kiểm thử và tích hợp | Người làm phần đó | Tự chuẩn bị dữ liệu và công cụ; một người khác kiểm tra lại, ghi tên trên thẻ Trello |

- Phân công không tự tạo thêm service riêng cho thông báo, file hay email ([kiến trúc](../system/architecture.md#ownership)).
- Thống nhất hợp đồng chung trước khi làm phần phụ thuộc. Lỗi làm người khác dừng việc thì báo ngay, tổng hợp ở họp tuần.
- Công suất: 3–4 ngày/tuần (giả định 8 giờ/ngày, 3,5 ngày/tuần). Lập kế hoạch dùng 80%, giữ 20% dự phòng. Thiết kế, code, test, họp, review, học, triển khai đều tính trong quỹ của từng người. Không chia đều công sức theo số service; xem [công suất](#capacity).

<a id="công-việc-làm-rõ-nhu-cầu-tiếp-theo"></a>

### Làm rõ nhu cầu

| Việc | Kết quả | Điều kiện |
|---|---|---|
| Làm rõ quy tắc còn mở với đại diện sản phẩm | Có kết luận hoặc giả định có người chịu trách nhiệm (SCDC-LOG-001) | Đã xác định câu hỏi ảnh hưởng hành trình và tiêu chí |
| Rà soát đặc tả | Hành vi, ngoại lệ, tiêu chí nhất quán với phạm vi | Có đầu vào trong SCDC-DIS-001 và quyết định liên quan |
| Lập kế hoạch kiểm thử và nghiệm thu | Kịch bản chức năng, quyền, mất kết nối, kết quả mong đợi | Quy tắc đủ rõ để viết kịch bản |

Nhận định nội bộ được coi là giả định cho tới khi có bằng chứng. OQ-013 đã xử lý (DEC-030); OQ-001 và OQ-012 vẫn mở.

<a id="ảnh-hưởng-đến-kế-hoạch"></a>

### Ảnh hưởng tới kế hoạch

- Giả định AS-001 (ba kỹ sư toàn thời gian) không còn đúng; xem [giả định](#giả-định-lập-kế-hoạch).
- Tính thời gian hướng dẫn, học và rà soát vào công sức.
- Mỗi gói gồm yêu cầu, giao diện, DB, backend, frontend, kiểm thử, tài liệu của người phụ trách.
- Mobile, thông báo, gửi file phải chốt đợt và ước lượng trước khi cập nhật lịch và chi phí.
- Đánh giá lại mốc 3 tháng và dự toán ([ngân sách](#budget)).

<a id="budget"></a>

<a id="ngân-sách-giả-định-và-rủi-ro"></a>

## Ngân sách, giả định, rủi ro

<a id="mô-hình-ngân-sách-minh-họa-cập-nhật-2026-10-04"></a>

### Ngân sách minh họa (2026-10-04)

Ngân sách chỉ mang tính tượng trưng (DEC-088). Mô hình **250.000.000 VNĐ** dưới đây được ước lượng cho phương án 16 tuần, **trước** khi tách MVP và v1 và trước phân công hiện tại. Đơn giá không phải lương thị trường, báo giá hay nghĩa vụ thanh toán.

| Hạng mục | Cơ sở | Số tiền |
|---|---|---:|
| Vg | 4 tháng × 20 triệu: sản phẩm, kiến trúc, UX, code, hướng dẫn | 80.000.000 VNĐ |
| Sáng | 4 tháng × 20 triệu (theo phân công cũ) | 80.000.000 VNĐ |
| Thái | 4 tháng × 10 triệu (theo phân công cũ) | 40.000.000 VNĐ |
| Hạ tầng, công cụ | Môi trường, LiveKit, TURN, email, backup, giám sát | 20.000.000 VNĐ |
| Dự phòng | Biến động công sức, tích hợp, vận hành | 30.000.000 VNĐ |
| **Tổng** | BA, UX, QA kiêm nhiệm, không tính riêng | **250.000.000 VNĐ** |

- Mức tháng đã tương ứng 3–4 ngày/tuần; không nhân thêm tỷ lệ công suất.
- 20 triệu hạ tầng chưa chứng minh là đủ; cần topology, giờ camera và chia sẻ, lưu lượng TURN, lưu trữ, báo giá.
- Chưa gồm thuế, marketing, vận hành dài hạn.
- Có đơn giá thật thì thay từng giả định và tính lại.

<a id="cơ-cấu-dự-toán-ban-đầu"></a>

### Dự toán ban đầu 500 triệu (lịch sử)

Phương án đầu tiên, giả định ba kỹ sư toàn thời gian 3 tháng. **Không còn dùng**; giữ lại để đối chiếu.

| Mã | Hạng mục | Cơ sở | Thành tiền |
|---|---|---|---:|
| COST-001 | Phát triển | 3 kỹ sư × 3 tháng × 35 triệu | 315.000.000 VNĐ |
| COST-002 | QA, kiểm thử | 2 tháng công × 25 triệu | 50.000.000 VNĐ |
| COST-003 | BA, UX/UI | 1 tháng công × 30 triệu | 30.000.000 VNĐ |
| COST-004 | Hạ tầng, công cụ | Dự trù trong phát triển, kiểm thử, phát hành ban đầu | 20.000.000 VNĐ |
| COST-005 | Dự phòng | Phát sinh, tích hợp, kiểm thử, chênh lệch | 85.000.000 VNĐ |
| | **Trước dự phòng** | | **415.000.000 VNĐ** |
| | **Tổng** | | **500.000.000 VNĐ** |

<a id="nguồn-lực-và-công-sức-cần-ước-lượng-lại"></a>

### Cần ước lượng lại

- Ba thành viên, 3–4 ngày/tuần; chỉ ước lượng phần còn lại từ nền đã có.
- Mỗi gói gồm yêu cầu, giao diện, dữ liệu, code, kiểm thử, tích hợp, tài liệu, chuẩn bị triển khai.
- Mobile, thông báo, gửi file chưa có đợt, nền tảng, hợp đồng, công sức. Mô hình 250 triệu chưa bao phủ phạm vi mới.
- QA, BA, UX là kiêm nhiệm. Tính cả học, hướng dẫn, họp tuần, kiểm tra chéo.

Theo dõi thời gian thực tế để điều chỉnh lịch và dự toán.

<a id="cơ-sở-ước-lượng-hạ-tầng"></a>

### Hạ tầng (COST-004)

Gồm: chạy ứng dụng, database, thoại và video, email, lưu trữ, backup, giám sát, công cụ. Chưa chọn nhà cung cấp và cấu hình. Muốn tính chi tiết cần:

- Số môi trường và tài nguyên mỗi môi trường.
- Tổng giờ gọi, tỷ lệ bật video và chia sẻ màn hình, lưu lượng mỗi tháng.
- Dung lượng dữ liệu, thời hạn lưu, chính sách backup.
- Đơn giá, hạn mức đi kèm, phí vượt hạn mức, tỷ giá.

20 triệu là dự trù ban đầu; phải kiểm tra lại trước khi chọn dịch vụ.

<a id="mô-hình-chi-phí-vận-hành-minh-họa"></a>

### Mô hình chi phí vận hành (minh họa)

**Phân bổ tượng trưng:** 5 triệu/tháng × 4 tháng = 20 triệu. Mỗi tháng: Web, API, worker 0,8; DB 1,2; LiveKit và TURN 1,5; backup 0,5; email và giám sát 0,4; dự phòng 0,6 (triệu VNĐ). Đây chỉ là chia ngân sách, không phải giá dịch vụ. Backup để [sau v1](overview.md#backup-scope).

**Công thức chi phí thật mỗi tháng:** tài nguyên cố định + GB truyền ra × đơn giá vượt hạn + GB-tháng DB và backup × đơn giá + số email × đơn giá + khoản khác. Ghi tiền tệ, tỷ giá, lưu lượng có sẵn trong gói, cách tính TURN, thuế. Không tính một lưu lượng hai lần vì đi qua cả LiveKit và TURN.

**Ví dụ độ nhạy (không phải mức dùng thật):**

- Giả định: 2.000 giờ-người/tháng; trung bình 4 người/phòng; mọi người bật camera; chia sẻ màn hình 20% thời gian; camera 1,5 Mbps, âm thanh 0,064 Mbps, chia sẻ 1 Mbps; mỗi người nhận mọi nguồn của người khác (giờ-luồng = giờ-người × 3); 1 Mbps trong 1 giờ = 0,45 GB.
- Kết quả: camera khoảng 4.050 GB, âm thanh 172,8 GB, chia sẻ 540 GB; tổng 4.762,8 GB; cộng 20% là khoảng **5.715 GB/tháng**.
- Với tải media 10 + 8 + 2 người: 148 lượt nhận camera (10×9 + 8×7 + 2×1); thêm hai nguồn chia sẻ mỗi phòng thì khoảng 265 Mbps ra, khoảng 319 Mbps nếu cộng 20%.
- Simulcast, chọn nguồn, FPS, tỷ lệ bật camera, TURN đều làm số thật khác; phải đo GB thật.

**Cần thu thập (OQ-010):** giờ-người, tỷ lệ camera và chia sẻ, số người mỗi phòng, tỷ lệ qua TURN, bitrate và GB ra, tốc độ tăng DB, số email được nhận. Sau v1 thêm WAL và GB backup 30 ngày. Trước khi mở công khai phải chọn nhà cung cấp, cấu hình, hạn mức và cảnh báo chi phí. Không tự mua dịch vụ.

<a id="giả-định-lập-kế-hoạch"></a>

### Giả định

AS-001 không còn đúng. Các giả định khác chờ kiểm chứng; chúng dùng để ước lượng, không phải cam kết.

| Mã | Giả định | Cách kiểm chứng | Theo dõi |
|---|---|---|---|
| AS-001 | Ba kỹ sư có kinh nghiệm, toàn thời gian, 3 tháng | **Không còn đúng:** ba người, 3–4 ngày/tuần, một người mới cần hướng dẫn; phải ước lượng lại | Vg, Sáng |
| AS-002 | QA 2 tháng công, BA/UX/UI 1 tháng công là đủ | Ước lượng lại: người phụ trách tự làm UX và tự kiểm thử, kiểm tra chéo; không tính QA hay UX như người riêng | Vg, Sáng, Thái |
| AS-003 | Dùng nền tảng media có sẵn cho thoại, video, chia sẻ màn hình | Đánh giá phù hợp, tích hợp, chi phí, vận hành | Thái |
| AS-004 | 1.000 tài khoản | Xác định nhóm người dùng đầu và nhu cầu lưu trữ | Khách hàng, BA |
| AS-005 | 100 người online cùng lúc | Xác định tải; kiểm thử tải | Khách hàng, kỹ thuật, QA |
| AS-006 | 20 người gọi cùng lúc; từ 2026-10-04 là giới hạn v1 (DEC-079) | Đã chốt 10 mỗi phòng, 20 toàn hệ thống, 2 chia sẻ mỗi phòng; khả năng đáp ứng cần kiểm thử | Khách hàng, kỹ sư media, QA |
| AS-007 | Hạ tầng và công cụ khoảng 20 triệu | Lập mô hình chi phí theo cấu hình, mức dùng, báo giá | Vận hành |
| AS-008 | Làm xong phạm vi trong khoảng 3 tháng | Ước lượng việc, phụ thuộc, lịch sau phân tích thiết kế | Quản lý dự án, kỹ thuật |

Tiết kiệm từ tái sử dụng code được tính khi có đánh giá kỹ thuật.

<a id="các-khoản-chưa-bao-gồm"></a>

### Chưa tính

- Marketing, thu hút người dùng.
- Thuế và phí theo hợp đồng.
- Vận hành, hỗ trợ, bảo trì dài hạn.
- Tính năng và nền tảng ngoài phạm vi v1.

<a id="rủi-ro"></a>

### Rủi ro

| Mã | Rủi ro | Ảnh hưởng | Xử lý | Theo dõi |
|---|---|---|---|---|
| RSK-001 | Phòng thoại và gọi riêng có video, chia sẻ màn hình; yêu cầu chất lượng chưa đủ | Tăng công tích hợp, kiểm thử, chậm tiến độ | Làm rõ chức năng, giới hạn, điều kiện nghiệm thu trước khi ước lượng chi tiết | Thái, QA |
| RSK-002 | Giới hạn media đã chốt; lượng dùng, bitrate, giá hạ tầng còn thiếu | Chi phí vượt dự trù | Tính theo thời lượng, dữ liệu truyền; theo dõi mức dùng | Vận hành |
| RSK-003 | Chuyển MVP sang microservice có thể vượt ước lượng | Tăng công triển khai, kiểm thử tích hợp, vận hành | Chốt ranh giới, dữ liệu, cách thay transaction và guard; dành giai đoạn V1-0 riêng; chia việc cho cả ba; chạy lại luồng MVP | Vg chủ trì; Sáng, Thái phối hợp |
| RSK-004 | Gói fullstack, review, hướng dẫn, công cụ có công sức khác nhau; tỷ lệ kế hoạch chưa chứng minh | Một người quá tải hoặc chờ việc | Theo dõi ngày công, kết quả, thời gian chờ hằng tuần; mỗi người tối đa một gói chính; giữ 20% dự phòng | Vg, Sáng, Thái |
| RSK-005 | Thời gian học, hướng dẫn, sửa lại, rà soát của Thái chưa lượng hóa | Ít thời gian cho việc khác, tiến độ bất định | Giao gói có hợp đồng và AC rõ; tính học và review vào công suất; đánh giá bằng đầu ra chạy được | Vg, Sáng, Thái |

<a id="điều-kiện-cập-nhật-dự-toán"></a>

### Khi nào cập nhật dự toán

Cập nhật khi đổi phạm vi, thời gian, nguồn lực, giải pháp kỹ thuật, mức dùng hay đơn giá. Mỗi lần ghi cơ sở tính, chênh lệch, ảnh hưởng tiến độ. Khách hàng xác nhận các thay đổi làm tăng ngân sách hay đổi phạm vi. Mobile, thông báo, gửi file có thể làm tăng công sức và chi phí; mỗi người phụ trách làm rõ đầu ra và ước lượng, Vg thống nhất đợt trước khi đưa vào lịch.

<a id="delivery"></a>

<a id="kế-hoạch-bàn-giao-và-ước-lượng"></a>

## Bàn giao và ước lượng

MVP là một ứng dụng để nhóm bắt đầu sớm; microservice thuộc v1 ([DEC-116](../records/decisions/README.md#dec-116)). Mục tiêu 3 tháng và mô hình 16 tuần, 250 triệu chỉ là cơ sở minh họa cũ; chưa có lịch v1 hay số liệu năng suất để cam kết.

<a id="roadmap"></a>

<a id="v1-planning"></a>

<a id="v1-architecture-packages"></a>

### Lộ trình lên microservice

**Trạng thái:** đề xuất ngày 2026-11-11, **chờ nhóm xác nhận** ở họp tuần. Thay cho các gói V1-ARCH-01 đến 03 và các đợt V1-0 đến 2 trước đây; bản cũ xem lịch sử Git của trang này.

**Nguyên tắc:**

- **Giải quyết phần khó trong monolith trước.** Với nhóm làm microservice lần đầu, phần khó là dữ liệu và lỗi giữa các phần: không còn transaction chung, lời gọi có thể thất bại, dữ liệu có thể lệch. Làm phần này khi code vẫn là một ứng dụng (còn debugger, rollback dễ), rồi mới tách thành service.
- **Sau mỗi bước, hệ thống vẫn chạy và demo được.** Không có giai đoạn "đang tách dở".
- **Mỗi lần tách một service.**
- **Bước 1 cả nhóm làm chung** để mọi người cùng hiểu hạ tầng; từ bước 2 mỗi người theo service của mình ([phân công](#team)).
- **Trang này chỉ ghi bước, thứ tự, lý do và tiêu chí xong.** Việc cụ thể, người làm, hạn và trạng thái nằm trên Trello; mỗi thẻ ghi mã bước (STEP-1 đến 4). Thẻ thuộc lộ trình viết trong [docs/project/tasks](tasks/README.md) và tự đồng bộ lên Trello. Bước sau được viết trước dưới dạng nháp; việc làm được ngay, không phụ thuộc kiến trúc, nằm ở [việc làm sớm](tasks/step-4-early.md). Khi một bước đạt tiêu chí, ghi ngày và link bằng chứng vào cột "Kết quả".

| Bước | Mục tiêu | Phụ trách chính | Xong khi | Kết quả |
|---|---|---|---|---|
| [STEP-1](#step-1) | Khung chạy: gateway, Compose, CI, giám sát; thử LiveKit | Cả nhóm; Vg điều phối, Thái thử LiveKit | Ai cũng tự dựng được môi trường; lần theo được request lỗi; một phiên âm thanh thật | — |
| [STEP-2](#step-2) | Tách ranh giới dữ liệu và transaction ngay trong monolith | Vg (Identity, Community), Sáng (Messaging) | Không còn transaction, khóa ngoại, JOIN giữa module; test cũ vẫn qua | — |
| [STEP-3](#step-3) | Tách từng service: Identity, Media, Messaging; phần còn lại là Community | Vg, Thái, Sáng | Bốn service chạy và triển khai độc lập, mỗi service một database | — |
| [STEP-4](#step-4) | Tính năng mới trên kiến trúc mới: tin phòng, phòng thoại, hoàn thiện Community | Theo service | Đạt [điều kiện hoàn tất v1](../releases/acceptance.md#v1-completion) | — |

Không ước lượng cả lộ trình ngay từ đầu: làm xong STEP-1, đo thời gian thực tế rồi mới ước lượng các bước sau ([ước lượng](#ước-lượng-và-khóa-lịchchi-phí)). Mỗi bước kết thúc bằng một buổi demo.

<a id="step-1"></a>

#### STEP-1 — Khung chạy

Mục tiêu: hạ tầng chạy được trước khi đụng vào nghiệp vụ.

- Nginx làm cổng vào trước monolith hiện tại, phục vụ luôn WebClient ([D09, D10, D22](../system/api-conventions.md#v1-public-routing)); thêm một service mẫu chỉ có `/health` để thử định tuyến.
- Docker Compose dựng toàn bộ bằng một lệnh ([môi trường](../system/environments.md)); CI build và chạy test trên mỗi pull request.
- OpenTelemetry Collector và OpenObserve ([D33–D35](../system/observability.md)): log và trace của một request đi qua gateway và service.
- Khung thư viện dùng chung `SCDC.Shared.Core` ([D45](../system/backend.md#v1-code-layout)), bắt đầu bằng phần kiểm tra JWT.
- Song song: Thái thử LiveKit tự host, một phiên âm thanh thật giữa hai trình duyệt (V1-MEDIA-01).

**Xong khi:** mỗi người tự dựng lại môi trường từ máy chưa cài gì theo [hướng dẫn phát triển](../guides/development.md); lần theo được một request lỗi qua trace; CI chặn được pull request làm hỏng test; LiveKit chạy được một phiên âm thanh.

<a id="step-2"></a>

<a id="v1-next-design"></a>

#### STEP-2 — Tách ranh giới ngay trong monolith

Code vẫn là một ứng dụng, nhưng các module phải cư xử như các service riêng. Sau bước này, tách service chủ yếu là đóng gói.

- **Database:** mỗi module có database user và connection string riêng; user chỉ có quyền trên schema của mình ([D02](../system/database.md#v1-database-layout)).
- **Bỏ khóa ngoại, JOIN và transaction xuyên module.** Chỗ khó nhất: `IAccountAccessGuard` và `IChannelAccessGuard` đang giữ transaction của bên gọi ([kiến trúc](../system/architecture.md#boundaries)). Thay bằng hàng rào ghi ([D05–D07](../system/access-revocation.md#v1-authorization-coordination)) và outbox ([D27](../system/api-conventions.md#v1-integration-event-transport)).
- **Hợp đồng giữa module** (`SCDC.Contracts`) viết lại như lời gọi qua mạng: có timeout, có thể thất bại, gọi lặp không gây hại.
- **Xác thực giữa các phần:** JWT ký bất đối xứng ([D28, D29](../system/api-conventions.md#v1-service-authentication)), phiên qua Redis ([D30, D31](../system/access-revocation.md#v1-session-validation)). Bắt đầu với luồng DM, sau đó Community.
- **Test tình huống lỗi:** bên kia không trả lời, sự kiện đến hai lần, sai thứ tự, mất Redis; đo thời gian thu hồi quyền (yêu cầu 5 giây).

Vg làm Identity và Community, Sáng làm Messaging; mỗi phần có người kia review.

**Xong khi:** toàn bộ test DM và Community vẫn qua; không còn transaction, khóa ngoại hay JOIN xuyên module (kiểm chứng bằng quyền của database user); đo được thời gian thu hồi.

<a id="step-3"></a>

#### STEP-3 — Tách từng service

Theo thứ tự ít rủi ro trước:

| Thứ tự | Service | Phụ trách | Lý do |
|---|---|---|---|
| 3a | Identity | Vg | Service khác chỉ cần kiểm tra JWT và biết phiên còn hiệu lực. Ranh giới rõ nhất, dùng để học quy trình tách |
| 3b (song song) | Media control | Thái | Xây mới thành service riêng, không phải chuyển dữ liệu. Làm gọi riêng trước (V1-MEDIA-02); phòng thoại cần quyền Community nên để STEP-4 |
| 3c | Messaging | Sáng | Nhiều dữ liệu và realtime nhất; tách sau khi đã quen quy trình. Cần hợp đồng tạo phòng giữa Community và Messaging ([D14–D19](../features/community/channels.md#v1-channel-provisioning)) |
| — | Community | Vg | Phần còn lại của monolith trở thành service Community |

Mỗi lần tách gồm: chuyển dữ liệu offline ([D48–D52](../system/database.md#v1-migration-execution)), chạy lại toàn bộ test, đo thời gian thu hồi, và cách quay về bản trước nếu lỗi.

**Xong khi:** đạt mục 2 của [điều kiện hoàn tất v1](../releases/acceptance.md#v1-completion): bốn service build, chạy, khởi động lại và triển khai độc lập, mỗi service chỉ dùng database của mình, luồng cũ vẫn đúng.

<a id="step-4"></a>

#### STEP-4 — Tính năng trên kiến trúc mới

- Tin phòng và realtime Community: [COM-W01 đến 07](#community-work-items).
- Phòng thoại, camera, chia sẻ màn hình, kiểm soát Media: [V1-MEDIA-03 đến 05](#v1-media-packages).
- Hoàn thiện Community: [COM-W08 đến 13](#community-plan-công-việc-mở-rộng-sau-phạm-vi-mvp-đã-chọn).
- Mobile, thông báo, gửi file nếu được chốt vào v1: [gói bổ sung](#extension-packages).

**Vì sao làm sau khi tách:** nếu làm tin phòng trong monolith trước, phần quyền dựa trên transaction chung sẽ phải viết lại khi tách.

<a id="tech-deferral"></a>

### Đề xuất hoãn một số công nghệ

Các quyết định dưới đây đã chốt. Đề xuất chỉ **hoãn** để nhóm học từng thứ một, không bỏ. Nếu nhóm đồng ý, ghi DEC mới; nếu môn học bắt buộc thì giữ nguyên.

| Đã chốt | Đề xuất làm trước | Lý do | Chuyển sang bản chốt khi |
|---|---|---|---|
| gRPC giữa service ([D04](../system/api-conventions.md#v1-internal-protocol)) | HTTP/JSON nội bộ | Dễ debug bằng curl và log; đổi giao thức về sau rẻ nếu hợp đồng đã rõ | Hợp đồng giữa hai service ổn định |
| mTLS giữa service ([D20](../system/api-conventions.md#v1-service-authentication)) | Mạng Docker nội bộ và token dịch vụ | Quản lý chứng chỉ là một mảng riêng, dễ tốn nhiều tuần | Bốn service đã chạy (cuối STEP-3) |
| Redis cho phiên ([D30](../system/access-revocation.md#v1-session-validation)) | Giữ nguyên | Cần để đạt thu hồi trong 5 giây | — |

<a id="package-design"></a>

### Thiết kế trước khi code

Trước khi code một phần việc, làm rõ: đầu ra, tiêu chí nghiệm thu, phụ thuộc trực tiếp, và phần giao diện, dữ liệu, hợp đồng, tổ chức code, môi trường bị ảnh hưởng. Đủ thông tin thì bắt đầu; không chờ toàn bộ thiết kế v1. Việc còn chờ quyết định chỉ chờ đúng phần đó.

Thiết kế cập nhật vào trang tính năng; thẻ Trello chỉ giữ phạm vi, phụ thuộc và link.

<a id="v1-media-packages"></a>

### Việc Media

Thái phụ trách Media control và LiveKit. Vg cung cấp quyền Identity, Community; Sáng phối hợp thông báo cuộc gọi; một người khác review. Các mã dưới đây là tên phần việc, chưa phải cam kết.

| Mã | Bước | Đầu ra | Phụ thuộc và kiểm thử |
|---|---|---|---|
| V1-MEDIA-01 | STEP-1 | Chọn phiên bản LiveKit, SDK; một phiên âm thanh thật | Trình phương án để Vg duyệt; thử kết nối thật trước khi ước lượng |
| V1-MEDIA-02 | STEP-3 | Gọi riêng, từ backend tới giao diện | Hợp đồng Identity và thông báo; thử nhận, từ chối, hủy, hết giờ, bận, kết thúc |
| V1-MEDIA-03 | STEP-4 | Phòng thoại, từ backend tới giao diện | Quyền và vòng đời phòng Community; thử vào, rời, hạn mức, mất quyền, xóa phòng |
| V1-MEDIA-04 | STEP-4 | Micro, camera, chia sẻ màn hình; thiết bị; hiển thị nguồn | Thử quyền thiết bị, dừng nguồn từ trình duyệt, kết nối lại |
| V1-MEDIA-05 | STEP-4 | Kiểm soát vào, hạn mức, thu hồi, dừng khi lỗi | Hợp đồng phiên và quyền từ Vg; chứng minh ngắt, đồng thời, tải, lỗi phụ thuộc |

File và email cũng là việc của Thái; xếp chung quỹ thời gian, không làm tất cả cùng lúc.

<a id="extension-packages"></a>

### Gói bổ sung: mobile, thông báo, gửi file

Đã thống nhất làm ([DEC-173](../records/decisions/README.md#dec-173)); chưa chốt thuộc v1 hay đợt sau. Đây là đầu ra thiết kế ban đầu, chưa phải cam kết.

| Gói | Đầu ra | Phụ trách | Phụ thuộc |
|---|---|---|---|
| EXT-MOBILE-01 | Nền tảng, chức năng, hành trình, khung app, công nghệ, đợt bàn giao | Vg thống nhất; mỗi người thiết kế phần mình | [Mobile](../experience/mobile.md), chất lượng, công suất |
| EXT-NOTIFY-01 | Loại sự kiện, người nhận, trong app hay push, đăng ký thiết bị, giao diện, hợp đồng | Sáng | [Thông báo](../features/notifications/README.md), Identity, Community, Media, mobile |
| EXT-FILE-01 | Ngữ cảnh gửi, loại và kích thước file, quyền, lưu trữ, vòng đời, giao diện, hợp đồng đính kèm | Thái | [Gửi file](../features/files/README.md), Messaging, quyền |

Sau thiết kế, người phụ trách tự tạo nhiệm vụ, ước lượng, và đưa phụ thuộc cần quyết định vào họp tuần.

<a id="capacity"></a>

<a id="phân-bổ-công-suất-để-cân-tải"></a>

### Công suất

Cơ sở 3–4 ngày/tuần; dùng 80% cho công việc, 20% dự phòng. Với 3,5 ngày/tuần là 2,8 ngày công việc, 0,7 ngày dự phòng. Chưa có số liệu để chia tỷ lệ theo từng service.

| Người | Tính trong quỹ | Theo dõi |
|---|---|---|
| Vg | Identity, Community; phạm vi, kiến trúc, quyết định, điều phối, review, triển khai chung | Ghi cả việc tổng thể lẫn service; thống nhất ưu tiên ở họp tuần |
| Sáng | Messaging, thông báo, giao diện, kiểm thử, tài liệu, tích hợp, chuẩn bị triển khai | Ước lượng theo nhiệm vụ còn lại trên Trello |
| Thái | Media, LiveKit, file, email, giao diện, kiểm thử, tài liệu, chuẩn bị triển khai | Thử Media sớm; tính cả học, tích hợp, sửa lại |

Mỗi người giữ tối đa một gói chính và một gói hỗ trợ nhỏ. Mobile, thông báo, gửi file chỉ vào lịch khi phạm vi và công sức đã rõ.

<a id="điều-chỉnh-theo-kết-quả-thực-tế"></a>

### Điều chỉnh theo thực tế

- Hằng tuần mỗi người báo: đầu ra chạy được, ngày công dự kiến và thực tế, lỗi, phụ thuộc, thời gian review và hỗ trợ, kế hoạch tiếp ([họp tuần](workflow.md#weekly-meeting)).
- Vg tổng hợp và quyết định điều chỉnh ưu tiên, phạm vi, lịch.
- Ước lượng vượt quỹ thì giảm việc, đổi thứ tự hoặc dời lịch. Đổi người phụ trách phải thống nhất và ghi trên thẻ.
- Ai xong sớm thì lấy việc tiếp theo trong phạm vi mình; kiểm tra chéo để chia sẻ kiến thức.

<a id="ước-lượng-và-khóa-lịchchi-phí"></a>

### Ước lượng và chốt lịch, chi phí

- Ước lượng **ngày công còn lại** cho từng gói: thiết kế, code, học, hướng dẫn, review, test, tích hợp, sửa lỗi. Phần Identity chỉ tính phần còn lại theo [hiện trạng](../features/accounts/README.md).
- AI là công cụ của cả nhóm; không tự gán hệ số tăng năng suất, không coi viết code xong là gói xong.
- Phương án 16 tuần chỉ còn là cơ sở ngân sách minh họa. Lịch mới cần kết quả MVP, kết quả thử chuyển service và Media, ngày nghỉ, công suất thực.
- Các quyết định kiến trúc (D01 đến D52) tổng hợp ở [kiến trúc v1](../system/architecture.md#target). Nơi chạy và email cho bản công khai còn mở. Backup để [sau v1](overview.md#backup-scope).

<a id="preparation"></a>

## Gói chuẩn bị (giai đoạn MVP)

Các gói PREP thuộc giai đoạn chuẩn bị MVP, giữ lại để tham khảo; lộ trình v1 xem [STEP-1 đến 4](#roadmap). PREP nằm trong quỹ của gói tính năng tương ứng, không phải việc toàn thời gian thêm. Chỉ làm phần còn thiếu cho gói v1 sắp làm; không làm lại đầu ra đã có ([nền MVP](../releases/mvp.md)).

| Gói | Đầu ra | Phụ thuộc | Làm, rà soát | Xong khi |
|---|---|---|---|---|
| PREP-01 | Thiết kế tài khoản, job email, fixture | ACC-GAP-01 đến 07, OQ-002 | Vg giữ hợp đồng; Thái chứng minh gửi thư; Sáng kiểm phần DM dùng | Quy tắc, token, job không còn cách hiểu khác nhau; AC cập nhật |
| PREP-02 | Giao diện tài khoản, DM, cộng đồng theo mã màn hình | Wireframe, quyết định | Vg: Identity, Community; Sáng: DM; review chéo | Có trạng thái trống, lỗi, mất mạng, mất quyền |
| PREP-03 | Hợp đồng API có schema máy đọc và mock | SCDC-API-DM-001, PREP-01 | Sáng; Vg review quyền | Request, response, lỗi thống nhất; không trả dữ liệu ngoài quyền |
| PREP-04 | Thử DM lưu bền, chống trùng, phân trang | PREP-03 | Sáng; Vg review | Các [tình huống kỹ thuật](workflow.md#technical-evidence) có bằng chứng |
| PREP-05 | Thử cập nhật, kết nối lại, thu hồi phiên | PREP-04 | Sáng: realtime; Vg: phiên | Không trùng, không sót; thu hồi đúng hạn |
| PREP-06 | Danh sách thiết bị, fixture kiểm thử | DEC-059, AC | Vg thống nhất; mỗi người chuẩn bị phần mình | Trình duyệt, phiên bản, kích thước, dữ liệu, kết quả mong đợi rõ |
| PREP-07 | Ước lượng và lịch cho đợt tài khoản và DM | PREP-01 đến 06 | Vg, Sáng, Thái | Ngày công, người làm và rà soát, thời gian hướng dẫn, phụ thuộc không chồng quỹ |

<a id="community-work-items"></a>

## Công việc Community

**Trạng thái:** danh sách phần việc theo ca sử dụng, làm ở [STEP-4](#step-4); thẻ Trello tương ứng ở [step-4.md](tasks/step-4.md) (cùng mã), trạng thái theo dõi trên Trello. Cột "Phụ thuộc" và "Đầu ra" còn viết theo cơ chế transaction chung của MVP; phải cập nhật theo thiết kế của [STEP-2](#step-2) trước khi làm.

COM-W01 đến 07 là phần tin phòng và realtime còn thiếu, chuyển từ [MVP](../releases/mvp.md#community-scope) sang v1. COM-W08 đến 13 hoàn thiện các nhánh khác. Mã COM-W chỉ là tên công việc, không thay mã UC, AC, TC. Trạng thái thật xem [Community](../features/community/README.md) và [tin phòng](../features/messaging/channel-messaging.md). Người phụ trách tự chia nhiệm vụ Trello và làm khi đủ [điều kiện](workflow.md#readiness).

| Mã | Ca sử dụng | Phụ thuộc | Phụ trách | Đầu ra và điều kiện xong |
|---|---|---|---|---|
| COM-W01 | UC-COM-17, nền tin dùng chung | Guard Identity và phòng, tạo phòng đã có; hợp đồng [Messaging](../features/messaging/README.md) | Sáng; Vg rà quyền | Migration và model tin, thứ tự theo không gian, cursor, đọc lịch sử có guard. Thử phân trang, phòng ẩn, khác server, phiên sai, epoch cũ trên DB thật |
| COM-W02 | UC-COM-23 gửi, gửi lại | COM-W01; [transaction](../features/community/mechanics.md#transactions) | Sáng; Vg rà Community | Ghi tin có guard tới commit; chống trùng; tin, sequence, outbox nguyên tử. Thử gửi lại, đồng thời, khởi động lại, đổi quyền tranh commit |
| COM-W03 | UC-COM-17, 23 giao diện | COM-W01, 02 | Sáng; Vg ghép khung | Mở phòng → lịch sử → gửi → tải lại với API thật; trạng thái đã lưu, lỗi rõ; gửi lại do người dùng bấm |
| COM-W04 | UC-COM-25 nhận tin | COM-W02; hợp đồng realtime Messaging | Sáng | Worker outbox, Hub, đăng ký có kiểm soát; hai tài khoản nhận tin; loại trùng; đối chiếu sau kết nối lại; ghi bằng chứng mất kết nối |
| COM-W05 | UC-COM-25 mất quyền; UC-COM-20, 21, 22 | COM-W04 | Sáng + Vg | HTTP, gửi, đăng ký, bù tin dùng quyền hiện tại; đo kết nối ngừng trong 5 giây từ commit (DEC-083) cho cả phiên và vai trò, ACL; có test tranh chấp và dừng khi lỗi |
| COM-W06 | Toàn luồng UC-COM-03, 06, 16, 17, 23, 25 | COM-W03, 04, 05 | Vg + Sáng | Tạo → tìm → tham gia → phòng chữ → lịch sử, gửi, nhận → mất quyền trên DB, API, Hub thật; hồi quy các chức năng cũ; ghi commit, môi trường, kết quả |
| COM-W07 | Tích hợp và kiểm thử | COM-W06 | Vg điều phối; Sáng kiểm Messaging | Chạy lại theo [hướng dẫn](../guides/development.md#community); hồ sơ đối chiếu [tiêu chí chuyển tiếp](../releases/mvp.md#community-acceptance) |

- Trước COM-W01, đối chiếu phần DM đã có và ranh giới service để chỉ làm phần còn thiếu.
- COM-W03 và 04 làm song song được khi hợp đồng COM-W02 ổn định.
- COM-W05 kiểm thử từ khi có Hub tin phòng, rồi lặp trong COM-W06. Guard hiện có chưa chứng minh thời hạn ngắt realtime hay giao thức giữa service.

<a id="community-plan-công-việc-mở-rộng-sau-phạm-vi-mvp-đã-chọn"></a>

### Công việc mở rộng

| Mã | Ca sử dụng | Điều kiện xong | Phụ trách |
|---|---|---|---|
| COM-W08 | UC-COM-19 xóa phòng | Xóa phòng và không gian cùng transaction; chống trùng, outbox; chặn HTTP và Hub; chứng minh tranh gửi và xóa ([phòng](../features/community/channels.md)) | Vg + Sáng |
| COM-W09 | UC-COM-04, 05, 07, 08 thông tin server, cách tham gia, duyệt | Ghi server và yêu cầu nguyên tử; chuyển sang riêng tư kết thúc yêu cầu đang chờ; quyền người duyệt; chứng minh CAS, duyệt, hủy, tham gia đồng thời | Vg |
| COM-W10 | UC-COM-09 đến 13 lời mời | Token, lượt dùng, hiệu lực theo người tạo, mời đích danh, epoch; chứng minh lượt cuối, thu hồi và nhận đồng thời | Vg |
| COM-W11 | UC-COM-14, 15 chuyển chủ và rời | Chủ, tư cách, epoch, dọn ACL nguyên tử; chủ không tự rời; epoch cũ; thu hồi HTTP và realtime | Vg + Sáng |
| COM-W12 | UC-COM-24 sửa, xóa tin | Ghi tin, quyền tác giả, placeholder, version, outbox; chứng minh CAS, tranh chấp, quyền hiện tại | Sáng; Vg phối hợp |
| COM-W13 | Phần thoại của UC-COM-16, 17, 25 | Module Media, kiểm soát vào, LiveKit, ngắt ([Media](../features/media/README.md)) | Thái: Media; Vg: vòng đời và quyền phòng |

UC-COM-02 còn nhánh gửi yêu cầu duyệt; UC-COM-06 còn nhánh chờ duyệt; UC-COM-03 còn đối chiếu khi rời và tham gia lại. Kết quả tìm kiếm và tham gia hiện tại không đóng được toàn bộ các ca này.

Câu hỏi mở của từng phần: [Servers](../features/community/servers.md#gaps), [Memberships](../features/community/memberships.md#gaps), [Invitations](../features/community/invitations.md#gaps), [Channels](../features/community/channels.md#gaps), [Permissions](../features/community/access-control.md#gaps), [tin phòng](../features/messaging/channel-messaging.md#gaps).

---

Nguồn: DEC-030, DEC-059, DEC-079, DEC-083, DEC-088, DEC-111, DEC-116, DEC-117, DEC-118, [DEC-171](../records/decisions/README.md#dec-171), DEC-172, [DEC-173](../records/decisions/README.md#dec-173).
