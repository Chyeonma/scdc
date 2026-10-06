# SCDC — Mục tiêu, yêu cầu, phạm vi và kế hoạch

Cập nhật: 2026-10-05. Nguồn chuẩn cho REQ, SCP, SUC, COST, AS, RSK, READY và PREP.

Phạm vi sản phẩm đã được xác định ở mức tổng thể. Đặc tả và bằng chứng còn thiếu được theo dõi bên dưới; có code trong repo chưa đồng nghĩa đã nghiệm thu hoặc sẵn sàng phát hành.

## Mục lục

- [Nhu cầu và người sử dụng](#needs)
- [Yêu cầu cấp cao](#requirements)
- [Phạm vi MVP](#scope)
- [Tiêu chí thành công và chất lượng](#success)
- [Nhân sự](#team)
- [Ngân sách, giả định, rủi ro](#budget)
- [Độ phủ đặc tả](#coverage)
- [Kế hoạch bàn giao](#delivery)
- [Sẵn sàng phát triển](#readiness)
- [Quy trình và thay đổi](#process)

<a id="needs"></a>

## 1. Nhu cầu và người sử dụng

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
[SCDC-DIS-001](project.md#needs). Đặc tả hành vi
nhắn tin riêng đợt đầu được quản lý tại
[SCDC-FR-DM-001](features/direct-messaging.md#requirements).

Đợt hiện tại không khảo sát người dùng bên ngoài theo
[DEC-030](decisions.md#decisions). Đầu vào từ đại diện sản phẩm
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
### Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| Server | Không gian cộng đồng trên ứng dụng, có thành viên và các phòng; phân biệt với máy chủ hạ tầng. |
| Channel/phòng | Khu vực giao tiếp bên trong một server. |
| Tin nhắn riêng (DM) | Hội thoại riêng giữa hai người. |
| Phòng thoại | Phòng trong server mà thành viên có thể vào và rời để giao tiếp trực tiếp. |

Phạm vi bàn giao được tổng hợp tại [Project Brief](project.md#scope).
Những quy tắc nghiệp vụ và giới hạn cần xác định được theo dõi tại
[sổ vấn đề cần làm rõ](decisions.md#decisions).


Đầu vào hiện tại là từ đại diện sản phẩm theo DEC-030. Chưa xác định nhóm người dùng đầu tiên, ngôn ngữ/khu vực và thước đo giá trị tại OQ-001/OQ-012. DM là nền chức năng ưu tiên; giá trị tách nhiều chủ đề cần tiếp tục kiểm chứng ở cộng đồng. Bộ câu hỏi phỏng vấn cũ được lưu dự phòng trong archive, không phải hoạt động khảo sát đã thực hiện.

<a id="requirements"></a>

## 2. Yêu cầu cấp cao

| Mã | Nội dung |
|---|---|
| REQ-001 | Phục vụ nhóm bạn và cộng đồng, không giới hạn chủ đề. |
| REQ-002 | Có không gian cộng đồng (server), phòng, quản lý thành viên và quyền cơ bản. |
| REQ-003 | Phiên bản đầu hoạt động trên trình duyệt web. |
| REQ-004 | Cho phép nhắn tin trong phòng thuộc server. |
| REQ-005 | Cho phép nhắn tin riêng giữa hai người. |
| REQ-007 | Có phòng thoại trong server, cho phép thành viên vào và rời phòng. |
| REQ-008 | Hỗ trợ gọi riêng giữa hai người. |
| REQ-009 | Hỗ trợ camera/video và chia sẻ màn hình trong phòng thoại của server và cuộc gọi riêng giữa hai người. |
| REQ-010 | Phát hành công khai, cho phép người dùng tự đăng ký. |
| REQ-011 | Thời gian thực hiện mục tiêu khoảng 3 tháng. |
| REQ-012 | Phát hành MVP bằng Modular Monolith, giữ ranh giới module; tách microservices ở đợt sau theo DEC-060 (thay yêu cầu microservices cho phiên bản đầu). |
| REQ-013 | Bên phát triển lập dự toán trên cơ sở phạm vi và nguồn lực cần thiết. |

REQ-006 về nhóm chat riêng ngoài server đã được loại khỏi phạm vi từ phiên
bản 1.3. Mã yêu cầu này được giữ trong lịch sử và không sử dụng lại.

<a id="scope"></a>

## 3. Phạm vi MVP và ràng buộc

### Phạm vi phiên bản đầu

| Mã | Hạng mục | Nội dung bàn giao |
|---|---|---|
| SCP-001 | Nền tảng | Ứng dụng chạy trên trình duyệt web. |
| SCP-002 | Tài khoản | Đăng ký, đăng nhập và hồ sơ cá nhân. |
| SCP-003 | Cộng đồng | Tạo và quản lý server; tổ chức phòng; mời và quản lý thành viên. |
| SCP-004 | Phân quyền | Vai trò và quyền cơ bản đối với server và phòng. |
| SCP-005 | Nhắn tin | Gửi, nhận và xem lịch sử tin nhắn trong phòng thuộc server và hội thoại riêng giữa hai người. |
| SCP-006 | Thoại | Phòng thoại trong server cho nhiều thành viên và cuộc gọi riêng giữa hai người. |
| SCP-007 | Video và chia sẻ màn hình | Sử dụng camera và chia sẻ màn hình trong phòng thoại của server và cuộc gọi riêng giữa hai người. |
| SCP-008 | Phát hành | Mở đăng ký công khai sau khi hoàn thành kiểm thử, nghiệm thu và chuẩn bị vận hành. |

Ngoài phạm vi phiên bản đầu:

- Nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này.
- Ứng dụng cài đặt riêng trên desktop và thiết bị di động.

Các chức năng bổ sung được đánh giá theo quy trình quản lý thay đổi.

### Ràng buộc và cơ sở lập kế hoạch

| Nội dung | Giá trị |
|---|---|
| Kiến trúc | Modular Monolith cho MVP theo DEC-060; microservices ở đợt sau, cần quyết định thời điểm và kế hoạch riêng. |
| Thời gian mục tiêu | Khoảng 3 tháng từ ngày khởi động. Ngày bắt đầu dự kiến 2026-10-05 theo DEC-088; lịch còn là đề xuất, chưa cam kết phát hành. |
| Ngân sách minh họa | 250.000.000 VNĐ cho phương án 16 tuần đề xuất; ngân sách tượng trưng theo DEC-088, không là báo giá/trần được duyệt. Mốc 500 triệu giữ trong lịch sử. |
| Nhân sự hiện có | Vg, Sáng và Thái dự kiến mỗi người 3–4 ngày/tuần, đủ ngày làm việc, từ 2026-10-05 theo DEC-088. Vg là trưởng nhóm, phụ trách kiến trúc tổng thể, lựa chọn công nghệ, UX/UI, trực tiếp lập trình và hướng dẫn Thái; Sáng phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg; Thái phụ trách frontend và kiểm thử, cần đào tạo và hướng dẫn. |
| Quy mô dự trù | 1.000 tài khoản, 100 người trực tuyến đồng thời và 20 người tham gia gọi đồng thời trên toàn hệ thống. |

1.000 tài khoản và 100 người online là giả định tải phục vụ dự toán/kiểm thử; 20 người gọi đồng thời là giới hạn sản phẩm đã chốt DEC-079. Cơ cấu nhân sự hiện có khác với giả
định ba kỹ sư có kinh nghiệm ban đầu; cần tính công sức kiêm nhiệm và hướng
dẫn khi đánh giá tiến độ, chi phí. Xem [nhân sự và phương án làm rõ nhu cầu](project.md#team)
và [dự toán và giả định](project.md#budget).

Ảnh hưởng của việc thu hẹp phạm vi nhắn tin và cuộc gọi đến công sức, chi
phí và tiến độ cần được lượng hóa khi cập nhật kế hoạch chi tiết.

### Kết quả bàn giao

- Ứng dụng web và các dịch vụ đáp ứng phạm vi chức năng được đặc tả.
- Hồ sơ yêu cầu, thiết kế trải nghiệm và thiết kế kỹ thuật.
- Kết quả kiểm thử và hồ sơ nghiệm thu.
- Hướng dẫn triển khai, cấu hình, vận hành và bàn giao hệ thống.

Nội dung chi tiết và điều kiện chấp nhận của từng kết quả bàn giao được xác
định trong đặc tả yêu cầu và kế hoạch thực hiện.

<a id="success"></a>

## 4. Tiêu chí thành công và chất lượng

| Mã | Kết quả cần đạt | Cơ sở đánh giá |
|---|---|---|
| SUC-001 | Người dùng có thể đăng ký và sử dụng các hình thức giao tiếp trong phạm vi. | Kịch bản sử dụng và nghiệm thu trên môi trường phát hành. |
| SUC-002 | Người quản lý cộng đồng có thể tổ chức phòng, mời thành viên và áp dụng quyền cơ bản. | Ma trận quyền và các tình huống cho phép/từ chối truy cập. |
| SUC-003 | Tin nhắn được lưu và có thể xem lại. | Kiểm thử gửi, nhận và truy xuất lịch sử theo quy tắc nghiệp vụ. |
| SUC-004 | Thoại, video và chia sẻ màn hình hoạt động trên trình duyệt được hỗ trợ. | Kiểm thử theo quy mô phòng, điều kiện mạng và chất lượng đã xác định. |
| SUC-005 | MVP vận hành bằng Modular Monolith với ranh giới module rõ theo DEC-060. | Hồ sơ kiến trúc hiện tại, kiểm tra phụ thuộc module và kết quả triển khai/kiểm thử MVP. |
| SUC-006 | Sản phẩm sẵn sàng cho phát hành công khai. | Kết quả kiểm thử, nghiệm thu và xác nhận khả năng vận hành. |

Chỉ tiêu định lượng đã chốt tại DEC-082/083/085/086; chính sách dữ liệu và cách áp dụng tuổi backup ở DEC-103–109; điều kiện lỗi tồn ở DEC-110. Môi trường, dữ liệu tải, kết quả chạy và người duyệt DEC-111 cần hoàn tất trước nghiệm thu/phát hành thực tế.

| Nội dung cần chốt | Đầu ra cần có | Theo dõi |
|---|---|---|
| Trình duyệt/thiết bị và khả năng tiếp cận | Danh sách phiên bản, thiết bị, kích thước và hành trình bắt buộc; DEC-059 đã chốt desktop/trình duyệt điện thoại cho tài khoản và DM; DEC-082 bổ sung Community/desktop-media | OQ-007 |
| Hiệu năng và tải | Độ trễ API/gửi–nhận theo percentile, tỷ lệ lỗi, dataset, thời lượng tải và cấu hình đo | OQ-007 |
| Chất lượng media | Độ trễ, âm thanh/hình ảnh, số người/luồng, mạng và thời gian reconnect | OQ-006/OQ-007 |
| Thu hồi quyền/phiên | Thời hạn hiệu lực trên HTTP và kết nối đang mở, cách đo | OQ-007/OQ-008 |
| Lưu giữ và khôi phục | [Chính sách dữ liệu](data-lifecycle.md) DEC-103–109, RPO ≤15 phút/RTO ≤4 giờ và tuổi mỗi backup/WAL ≤30 ngày đã chốt; còn công cụ, cửa sổ PITR thực tế và bằng chứng restore | OQ-011 |

1.000 tài khoản và 100 người trực tuyến là giả định dự toán, không phải giới hạn đăng ký/online. Ngày 2026-10-04, 20 người gọi đồng thời trở thành giới hạn media MVP theo DEC-079; chưa có kết quả đo. Các mốc hiệu năng trong tài liệu kỹ thuật cũ chưa có bằng chứng và chưa được xác nhận làm ngưỡng nghiệm thu. Không tuyên bố truy vấn luôn dưới 10ms.

<a id="team"></a>

## 5. Nhân sự và phối hợp

### Nhân sự đã xác định

| Thành viên | Vai trò đã xác định | Khả năng tham gia | Cơ sở phân công |
|---|---|---|---|
| Vg | Trưởng nhóm, đại diện sản phẩm; phụ trách kiến trúc tổng thể, lựa chọn công nghệ, thiết kế UX/UI và trực tiếp lập trình; hướng dẫn Thái. | 3–4 ngày/tuần theo DEC-088. | Thiết kế hệ thống và UX/UI, lựa chọn công nghệ, phát triển phần mềm cùng Sáng; điều phối công việc, xác nhận yêu cầu và phạm vi, hướng dẫn thành viên mới. |
| Sáng | Phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg. | 3–4 ngày/tuần theo DEC-088. | Thiết kế chi tiết và phát triển backend theo kiến trúc tổng thể; phối hợp với Vg trong các quyết định kỹ thuật. |
| Thái | Phụ trách frontend và kiểm thử; thành viên mới được Vg hướng dẫn. | 3–4 ngày/tuần theo DEC-088. | Phát triển giao diện theo thiết kế UX/UI của Vg và thực hiện kiểm thử; cần đào tạo, chia nhỏ công việc và rà soát kết quả. |

Cơ sở mới từ 2026-10-04 là 3–4 ngày/tuần mỗi người; 8 giờ/ngày và 3,5 ngày/tuần dùng làm baseline giả định. Công sức thực hiện cần
tính riêng cho điều phối, phân tích, thiết kế, đào tạo, phát triển và kiểm thử.
Nhóm hiện tại chưa đáp ứng giả định ba kỹ sư có kinh nghiệm trong dự toán ban đầu.

### Phương án phối hợp đề xuất

| Hạng mục | Phụ trách đề xuất | Cách phối hợp |
|---|---|---|
| Làm rõ yêu cầu với đại diện sản phẩm | Vg | Sáng góp ý tính khả thi; Thái hỗ trợ ghi nhận, đối chiếu với giao diện và tiêu chí kiểm thử. |
| Kiến trúc tổng thể và lựa chọn công nghệ | Vg | Sáng góp ý khả năng triển khai, các ràng buộc backend và tích hợp. |
| Thiết kế chi tiết backend | Sáng | Phối hợp với Vg để bảo đảm phù hợp với kiến trúc tổng thể và công nghệ đã lựa chọn. |
| Lập trình và tích hợp | Vg, Sáng và Thái | Vg và Sáng trực tiếp lập trình; Thái triển khai frontend dưới sự hướng dẫn của Vg. Phân chia công việc cụ thể khi có thiết kế và phối hợp tích hợp, rà soát mã nguồn. |
| Đào tạo thành viên mới | Vg | Chia công việc nhỏ, mô tả đầu ra và hướng dẫn cách tự kiểm tra; Sáng hỗ trợ rà soát phần kỹ thuật liên quan. |
| Thiết kế UX/UI | Vg | Xác định luồng sử dụng, màn hình và trạng thái giao diện để Thái triển khai frontend. |
| Frontend | Thái | Triển khai theo thiết kế của Vg; Vg hướng dẫn và rà soát, Sáng hỗ trợ tích hợp backend. |
| Kiểm thử | Thái | Thực hiện theo tiêu chí chấp nhận và kịch bản kiểm thử, ghi nhận lỗi và kiểm tra lại sau sửa; Vg hoặc Sáng rà soát kịch bản và kết quả, đặc biệt với phần frontend do Thái phát triển. |

Đã xác định Vg phụ trách UX/UI, Thái phụ trách frontend và kiểm thử. Cách
phối hợp trong bảng là đề xuất để lập kế hoạch chi tiết. Công sức học,
hướng dẫn, phát triển, kiểm thử và rà soát cần được phân bổ cụ thể; sản lượng
độc lập của Thái chưa được dùng làm cơ sở cam kết tiến độ.

### Công việc làm rõ nhu cầu tiếp theo

| Công việc | Kết quả cần có | Điều kiện thực hiện |
|---|---|---|
| Làm rõ quy tắc còn mở với đại diện sản phẩm | Kết luận hoặc giả định có đầu mối tại SCDC-LOG-001. | Các câu hỏi ảnh hưởng đến hành trình và tiêu chí chấp nhận đã được xác định. |
| Rà soát bản nháp đặc tả | Hành vi, ngoại lệ và tiêu chí chấp nhận nhất quán với phạm vi đã thống nhất. | Có đầu vào trong SCDC-DIS-001 và quyết định tương ứng. |
| Lập kế hoạch kiểm thử và nghiệm thu | Kịch bản kiểm tra chức năng, phân quyền, mất kết nối và kết quả mong đợi. | Quy tắc đủ rõ để QA viết kịch bản. |

Các nhận định nội bộ được quản lý như giả định sản phẩm cho đến khi có bằng
chứng kiểm chứng. OQ-013 được xử lý cho đợt này bằng DEC-030; OQ-001 và
OQ-012 vẫn mở về người dùng đại diện và cách đánh giá giá trị sản phẩm.

### Ảnh hưởng đến kế hoạch

- Cập nhật AS-001 theo cơ cấu nhân sự hiện có.
- Tính thời gian hướng dẫn, học và rà soát vào công sức của nhóm.
- Phân bổ công sức UX/UI của Vg, frontend và kiểm thử của Thái; làm rõ công
  việc phân tích nghiệp vụ khi lập lịch chi tiết.
- Đánh giá lại mốc ba tháng và dự toán; chi tiết tại
  [SCDC-EST-001](project.md#budget).

<a id="budget"></a>

## 6. Ngân sách, giả định và rủi ro

### Mô hình ngân sách minh họa cập nhật 2026-10-04

Theo DEC-088, ngân sách mang tính tượng trưng và được phép điều chỉnh để trình bày rõ. Mô hình **250.000.000 VNĐ** dưới đây do agent ước lượng cho phương án **16 tuần**; đơn giá chưa được các thành viên cung cấp, không phải mức lương thị trường/báo giá hoặc nghĩa vụ thanh toán. Mốc 500 triệu trước đây giữ làm lịch sử, không tiếp tục làm trần. Lịch 16 tuần cần người dùng xác nhận; không đổi phạm vi MVP hoặc tự loại media.

| Hạng mục | Cơ sở minh họa | Số tiền |
|---|---|---:|
| Vg | 4 tháng × 20 triệu/tháng; gồm sản phẩm, kiến trúc, UX, code và hướng dẫn | 80.000.000 VNĐ |
| Sáng | 4 tháng × 20 triệu/tháng; gồm backend, tích hợp, thử nghiệm và review | 80.000.000 VNĐ |
| Thái | 4 tháng × 10 triệu/tháng; gồm frontend, học, kiểm thử và sửa lỗi | 40.000.000 VNĐ |
| Hạ tầng/công cụ | Khoản minh họa cho môi trường, LiveKit tự host/TURN, email, backup và giám sát | 20.000.000 VNĐ |
| Dự phòng | Khoản minh họa cho biến động công sức, tích hợp và vận hành | 30.000.000 VNĐ |
| **Tổng** | Không cộng thêm BA/UX/QA như nhân sự độc lập vì đang kiêm nhiệm | **250.000.000 VNĐ** |

Mức tháng giả định đã ứng với sự tham gia 3–4 ngày/tuần, không nhân thêm tỷ lệ công suất rồi tính trùng. Khi có đơn giá thực tế, thay từng giả định và tính lại thời gian/chi phí. Chi phí hạ tầng thật cần topology, số giờ camera/share, lưu lượng TURN, lưu trữ và báo giá; 20 triệu chưa chứng minh đủ. Chưa bao gồm thuế, marketing hoặc vận hành dài hạn ngoài kỳ minh họa.

### Cơ cấu dự toán ban đầu

| Mã | Hạng mục | Cơ sở tính | Thành tiền |
|---|---|---|---:|
| COST-001 | Phát triển và kỹ thuật | 3 kỹ sư × 3 tháng × 35.000.000 VNĐ/người/tháng | 315.000.000 VNĐ |
| COST-002 | QA/kiểm thử | 2 tháng công × 25.000.000 VNĐ/tháng công | 50.000.000 VNĐ |
| COST-003 | Phân tích nghiệp vụ và UX/UI | 1 tháng công × 30.000.000 VNĐ/tháng công | 30.000.000 VNĐ |
| COST-004 | Hạ tầng và công cụ | Khoản dự trù trong thời gian phát triển, kiểm thử và phát hành ban đầu | 20.000.000 VNĐ |
| COST-005 | Dự phòng | Công việc phát sinh, tích hợp, kiểm thử và chênh lệch chi phí | 85.000.000 VNĐ |
| | **Tổng trước dự phòng** | | **415.000.000 VNĐ** |
| | **Tổng dự toán** | | **500.000.000 VNĐ** |

Các đơn giá và tháng công trong bảng thuộc phương án ước tính ban đầu, chưa
phản ánh chi phí của từng thành viên hiện tại. Đơn giá thực tế và phương án
tính công sức kiêm nhiệm cần được xác định khi cập nhật dự toán.

### Nguồn lực và công sức cần ước lượng lại

- Ba thành viên dự kiến 3–4 ngày/tuần mỗi người; cần tính quỹ thời gian thực tế theo DEC-088.
- Vg phụ trách kiến trúc tổng thể, lựa chọn công nghệ, thiết kế UX/UI và trực
  tiếp lập trình, đồng thời điều phối, đại diện sản phẩm và hướng dẫn Thái;
  cần phân bổ thời gian cho từng trách nhiệm trong quỹ thời gian của một người.
- Sáng phụ trách kỹ thuật backend và trực tiếp lập trình cùng Vg; cần tính
  cả thiết kế chi tiết, tích hợp và rà soát bên cạnh phát triển tính năng.
- Thái phụ trách frontend và kiểm thử, cần đào tạo và hướng dẫn; công sức
  cần bao gồm học, phát triển giao diện, chuẩn bị và thực hiện kiểm thử,
  sửa lỗi, kiểm tra lại và thời gian rà soát cùng Vg hoặc Sáng.
- QA 2 tháng công và BA/UX/UI 1 tháng công là giả định khối lượng từ dự toán
  ban đầu; cần ước lượng lại theo phân công kiêm nhiệm hiện có và làm rõ
  công việc phân tích nghiệp vụ.

Khi lập lịch, tổng phân bổ của mỗi người phải bao gồm công việc kiêm nhiệm,
học, hướng dẫn và rà soát; tránh tính cùng một khoảng thời gian cho nhiều vai trò.

### Cơ sở ước lượng hạ tầng

Khoản COST-004 bao gồm tài nguyên chạy ứng dụng, cơ sở dữ liệu, xử lý
thoại/video, email hệ thống, lưu trữ, sao lưu, giám sát và công cụ phát triển.
Nhà cung cấp và cấu hình chưa được lựa chọn.

Để xác lập chi phí chi tiết cần có:

- Số môi trường và tài nguyên tính toán cho từng môi trường.
- Tổng thời lượng tham gia gọi, tỷ lệ sử dụng video/chia sẻ màn hình và
  lưu lượng truyền tải hằng tháng.
- Dung lượng dữ liệu, thời hạn lưu trữ và chính sách sao lưu.
- Đơn giá dịch vụ, hạn mức đi kèm, chi phí vượt hạn mức và tỷ giá thanh toán.

Khoản 20.000.000 VNĐ là dự trù ban đầu, cần kiểm tra lại bằng cấu hình và mức
sử dụng cụ thể trước khi lựa chọn dịch vụ.

### Mô hình chi phí vận hành minh họa

Để khớp khoản hạ tầng 20 triệu của kế hoạch, dùng **5 triệu/tháng × 4 tháng** làm khoản phân bổ tượng trưng: Web/API/worker 0,8 triệu; DB 1,2 triệu; SFU/TURN 1,5 triệu; backup 0,5 triệu; email/quan sát 0,4 triệu; dự phòng hạ tầng 0,6 triệu. Đây là số chia ngân sách, không phải giá dịch vụ hoặc bằng chứng cấu hình đủ tải. Không tính lại phần này bên ngoài tổng 250 triệu.

Chi phí thật mỗi tháng = tài nguyên cố định + GB truyền ra × đơn giá vượt hạn + GB-tháng DB/backup × đơn giá + số email × đơn giá + khoản khác được xác nhận. Ghi currency/tỷ giá, lưu lượng bao gồm trong gói, cách tính TURN và thuế khi có báo giá; không cộng cùng một lưu lượng hai lần chỉ vì đã đi qua cả SFU và TURN.

Ví dụ kỹ thuật để nhìn độ nhạy, **chưa phải mức dùng được xác nhận**: 2.000 giờ-người/tháng, trung bình 4 người/phòng, mọi người bật camera, thời lượng share bằng 20% giờ-người; bitrate giả định camera 1,5 Mbps, audio 0,064 Mbps, share 1 Mbps. Giả định mọi người đăng ký nhận mọi nguồn của người khác: giờ-luồng = giờ-người × (4−1); 1 Mbps truyền liên tục một giờ = 0,45 GB theo đơn vị thập phân. Camera khoảng 4.050 GB, audio 172,8 GB, share 540 GB, tổng 4.762,8 GB; cộng giả định 20% overhead khoảng **5.715 GB/tháng**. Simulcast/chọn nguồn/FPS/tỷ lệ bật camera/TURN làm số thực tế khác; cần đo GB thật thay hệ số minh họa.

Ở workload media 10 + 8 + 2 người, số lượt nhận camera từ người khác = 10×9 + 8×7 + 2×1 = 148; với bitrate giả định trên và hai nguồn share trong mỗi phòng/call, lưu lượng ra minh họa khoảng 265 Mbps, khoảng 319 Mbps nếu cộng 20%. Con số này không suy ra chất lượng đã đạt hoặc thay kiểm thử mạng từng máy. Ghi chỉ số SFU/NIC và hóa đơn để điều chỉnh model.

Dữ liệu cần thu thập cho OQ-010: giờ-người, tỷ lệ camera/share, số người/phòng, tỷ lệ qua TURN, bitrate/GB ra, tốc độ tăng DB/WAL, GB backup 30 ngày và email được provider nhận. Trước mở công khai phải chốt nhà cung cấp/cấu hình/hạn mức và cơ chế cảnh báo chi phí. Không tự mua dịch vụ trong quá trình hoàn thiện tài liệu.

### Giả định lập kế hoạch

AS-001 đã được đối chiếu với cơ cấu nhân sự hiện tại và không còn phù hợp.
Các giả định còn lại đang chờ kiểm chứng. Chúng là đầu vào ước tính công sức
và chi phí, chưa phải cam kết năng lực hoặc hạn mức đăng ký của sản phẩm.

| Mã | Giả định | Cách kiểm chứng | Đầu mối dự kiến |
|---|---|---|---|
| AS-001 | Giả định ban đầu: có 3 kỹ sư có kinh nghiệm làm toàn thời gian trong 3 tháng. | Không còn phù hợp: đội ngũ 3 thành viên, mỗi người 3–4 ngày/tuần, có 1 thành viên mới cần hướng dẫn; phải ước lượng lại theo phân công và năng lực. | Vg, Sáng |
| AS-002 | QA 2 tháng công và BA/UX/UI 1 tháng công đáp ứng khối lượng công việc. | Ước lượng theo yêu cầu, màn hình, kế hoạch kiểm thử và năng lực kiêm nhiệm của nhóm; làm rõ công việc phân tích nghiệp vụ. | Vg, Thái; Sáng hỗ trợ đánh giá kỹ thuật |
| AS-003 | Tích hợp nền tảng media có sẵn cho thoại/video và chia sẻ màn hình. | Đánh giá tính phù hợp, khả năng tích hợp, chi phí và vận hành. | Tech Lead, kỹ sư media |
| AS-004 | Quy mô dự trù 1.000 tài khoản. | Xác định nhóm người dùng ban đầu và nhu cầu lưu trữ. | Khách hàng, BA |
| AS-005 | Có khoảng 100 người trực tuyến đồng thời trên toàn hệ thống. | Xác định nhu cầu tải dự kiến; kiểm chứng khả năng đáp ứng bằng kiểm thử tải. | Khách hàng, Tech Lead, QA |
| AS-006 | Dự trù ban đầu 20 người gọi đồng thời; ngày 2026-10-04 thành giới hạn MVP theo DEC-079. | Đã chốt quy tắc 10/phòng, 20/toàn hệ thống, 2 share/phòng; khả năng đáp ứng vẫn cần kiểm thử. | Khách hàng, kỹ sư media, QA |
| AS-007 | Hạ tầng và công cụ cần khoảng 20.000.000 VNĐ trong thời gian dự án. | Lập mô hình chi phí theo cấu hình, mức sử dụng và báo giá. | Phụ trách vận hành |
| AS-008 | Phạm vi có thể hoàn thành trong khoảng 3 tháng. | Ước lượng công việc, phụ thuộc và lịch nguồn lực sau phân tích, thiết kế. | Quản lý dự án, nhóm kỹ thuật |

Khả năng tiết kiệm từ tái sử dụng mã nguồn được tính vào dự toán sau khi có
kết quả đánh giá kỹ thuật.

### Các khoản chưa bao gồm

- Marketing và thu hút người dùng.
- Thuế và phí phát sinh theo điều kiện hợp đồng.
- Vận hành, hỗ trợ và bảo trì dài hạn sau thời gian dự án.
- Tính năng và nền tảng ngoài phạm vi phiên bản đầu.

### Rủi ro

| Mã | Rủi ro | Ảnh hưởng | Biện pháp xử lý | Đầu mối dự kiến |
|---|---|---|---|---|
| RSK-001 | Phòng thoại trong server và gọi riêng hai người có video/chia sẻ màn hình; yêu cầu chất lượng chưa đầy đủ. | Tăng công sức tích hợp, kiểm thử và kéo dài tiến độ. | Làm rõ ma trận chức năng, giới hạn và điều kiện nghiệm thu trước khi ước lượng chi tiết. | Tech Lead, QA |
| RSK-002 | Giới hạn phòng/media đã chốt DEC-079; lượng sử dụng, bitrate thực tế và giá hạ tầng còn thiếu. | Chi phí hạ tầng vượt dự trù. | Tính chi phí theo thời lượng, dữ liệu truyền tải và theo dõi mức sử dụng. | Phụ trách vận hành |
| RSK-003 | Ranh giới module, tích hợp MVP và việc tách microservices ở đợt sau có thể làm tăng công sức. | Tăng công sức triển khai, kiểm thử tích hợp và vận hành. | Xác định ranh giới module, trách nhiệm dữ liệu và công việc vận hành MVP; ước lượng tách dịch vụ ở kế hoạch riêng. | Tech Lead |
| RSK-004 | Khối lượng kiêm nhiệm frontend, UX/UI và kiểm thử chưa được ước lượng; trình duyệt đã chốt DEC-082 nhưng cấu hình nghiệm thu và trách nhiệm quản trị nền tảng công khai còn thiếu. | Phải điều chỉnh tiến độ hoặc phạm vi bàn giao. | Ước lượng công sức theo phân công hiện có; khóa cấu hình nghiệm thu và trách nhiệm quản trị trước phát hành. | Vg, Sáng, Thái |
| RSK-005 | Thời gian đào tạo, hướng dẫn và rà soát của thành viên mới chưa được lượng hóa. | Giảm thời gian dành cho các công việc khác và tăng độ bất định của tiến độ. | Chia công việc nhỏ, bố trí thời gian hướng dẫn và đánh giá công sức theo kết quả thực hiện. | Vg, Sáng |

### Điều kiện cập nhật dự toán

Cập nhật khi có thay đổi phạm vi, thời gian, nguồn lực, giải pháp kỹ thuật,
mức sử dụng hoặc đơn giá dịch vụ. Mỗi điều chỉnh cần thể hiện cơ sở tính,
chênh lệch và tác động đến tiến độ; khách hàng xác nhận các thay đổi làm tăng
ngân sách hoặc thay đổi phạm vi bàn giao.

<a id="coverage"></a>

## 7. Độ phủ đặc tả

| Phạm vi | Đầu ra hiện có | Khoảng trống chính | Trạng thái |
|---|---|---|---|
| SCP-001 Web | DEC-059; [wireframe tài khoản/DM](features/direct-messaging.md#ux) | Đã chốt ma trận trình duyệt cho Accounts/DM/Community và media desktop (DEC-082), ngưỡng chat/thu hồi (DEC-083); còn OS/thiết bị/build, tiếp cận và kết quả đo (OQ-007). | Phạm vi/ngưỡng đã chốt; cần kiểm chứng |
| SCP-002 Tài khoản | [SCDC-FR-ACC-001](features/accounts.md#requirements) | Chốt định danh, mật khẩu/lockout, phiên/thời hạn, gửi lại, hồ sơ và kênh khôi phục tại DEC-063–068; có trạng thái/dữ liệu và ACC-GAP-01–07. Email/resend/limiter đã có phương án; còn khóa schema/ngưỡng/provider/key store và kiểm chứng các chênh lệch source (OQ-002/OQ-008). | Nghiệp vụ đã chi tiết hóa; kỹ thuật còn mở |
| SCP-003 Cộng đồng | [đặc tả](features/community.md#requirements), [5 thành phần và UC](features/community.md#organization), [thiết kế tích hợp](features/community/integration.md#contracts) | Luồng DEC-072–077/087 và search/tên/phạm vi/private switch/issuer DEC-093–097 đã chốt; có OpenAPI/realtime/transaction/migration, còn review/mock/proof (OQ-003/OQ-008). | Có thiết kế chi tiết; chưa triển khai/kiểm chứng |
| SCP-004 Phân quyền | [ma trận](features/community/permissions.md#permissions), [thiết kế Permissions](features/community/permissions.md#detailed-design) | @everyone/20 custom role/union DEC-092 và quản lý cần view DEC-098 đã chốt; có role/ACL API, epoch/accessVersion/guard/fixture; còn review/migration/đo thu hồi (OQ-004/OQ-007). | Có thiết kế chi tiết; chưa triển khai/kiểm chứng |
| SCP-005 Nhắn tin | [DM](features/direct-messaging.md#requirements), [cộng đồng](features/community.md#requirements), [hợp đồng DM](features/direct-messaging.md#contracts), [vòng đời](data-lifecycle.md) | DEC-068–071/081/090/091 chốt nội dung/tìm/transport/draft; DEC-103–109 chốt phạm vi account/retention/restore. Có HMAC/SQL/Hub/cursor-resume/placeholder/fixture; còn review/migration/shared guard, kho sổ/worker và proof (OQ-005/008/011). | Đã chi tiết hóa; còn rà soát và thử nghiệm |
| SCP-006 Thoại | [Đặc tả](features/voice-video.md#requirements), [thiết kế media](features/voice-video.md#detailed-design) | DEC-078–085/099–102 chốt điều kiện, 10/20/2, ring/reconnect, desktop/chất lượng/cutoff/multi-device/thiết bị. Có OpenAPI 16 công khai +3 nội bộ, realtime/fixture/coordinator/lease; còn review và proof SFU/migration (OQ-006/007/008). | Có thiết kế chi tiết; chưa triển khai/kiểm chứng |
| SCP-007 Video/chia sẻ màn hình | [Thiết kế media](features/voice-video.md#detailed-design) | DEC-079/101/102 chốt nguồn/người, 2 share/phòng, ban đầu tắt, screen chỉ hình. Có source permit/quota gate/layout/AC/TC; SDK/extension/build và capture matrix thực tế còn cần review/proof. | Có thiết kế chi tiết; chưa tích hợp/kiểm chứng |
| SCP-008 Phát hành | [nghiệm thu/gate](release-operations.md#release-gates), [runbook](operations-runbook.md), [mẫu hồ sơ](templates/release-record.md) | DEC-110 chốt lỗi tồn, DEC-112 chốt quản trị kỹ thuật/no admin UI; người duyệt DEC-111 chưa chọn. Còn người trực/topology/tool và bằng chứng RLS-GAP-01–08. | Đã soạn gate/smoke/runbook/mẫu; chưa triển khai/diễn tập |

Đầu ra chuẩn bị và điều kiện còn thiếu theo gói tài khoản/DM được theo
dõi tại [SCDC-READY-001](project.md#readiness).
Repo có bộ test tự động cho Identity/response; lần chỉnh docs này chưa chạy hoặc nghiệm thu sản phẩm. Không đánh dấu yêu cầu đạt chỉ vì có test.

### Tiến độ hoàn thiện tài liệu ngày 2026-10-04

Bảng này đánh giá nội dung đã viết và quyết định còn cần, không đánh giá phần mềm đã hoàn thành. “Có bản thiết kế” chưa có nghĩa đã chạy thử hoặc được nghiệm thu.

| Phần | Nội dung đã hoàn thiện trong đợt này | Còn cần để khóa tài liệu |
|---|---|---|
| Mục tiêu/phạm vi | REQ/SCP/SUC, luồng ưu tiên và ranh giới MVP nhất quán | Nhóm sử dụng đầu tiên/ngôn ngữ và cách đánh giá nội bộ đang được hỏi |
| Tài khoản | Quy tắc, UX, [12 use case và đối chiếu source/test](features/accounts.md#use-cases), AC/TC; hành vi mật khẩu trùng DEC-113; thiết kế chi tiết token/consume/EmailDelivery, recovery OpenAPI 4 thao tác | Limiter bổ sung hoãn DEC-089; provider/key store và kiểm chứng ACC-GAP/API/UI thuộc triển khai |
| DM | Quy tắc/UX, OpenAPI 7 thao tác, schema SignalR, HMAC/mapping SQL/guard/cursor-resume; validation/draft và fixture; account lock/restore placeholder DEC-104/108 | Review/mock, migration/projection/sổ bảo vệ và proof concurrency/thu hồi/restore khi triển khai |
| Cộng đồng/quyền | Quy tắc DEC-092–098; role/ACL/epoch, OpenAPI 45 thao tác, realtime 9 loại, fingerprint/fixture, AC/TC; room retention/restore DEC-105/108 | Review/mock, migration/guards và proof concurrency/thu hồi/restore; metadata nghiệp vụ khác chưa đặt TTL riêng |
| Vòng đời dữ liệu | Policy/matrix DEC-103–109, schema sổ bảo vệ, placeholder/availability, 49 vector +10 schema cases +6 kịch bản, AC/TC và DATA-GAP-01–06 | Review/migration/worker, kho sổ/key/checkpoint và proof hai kho/restore; scope quyền/thẩm quyền vận hành còn mở |
| Media | Quy tắc DEC-099–102, OpenAPI 19 thao tác, realtime 8 loại, room/call/participation/capacity/source/lease, 30 vector +8 transition +4 HMAC, AC/TC và MEDIA-GAP-01–07; TTL terminal DEC-107 | Review/migration, chọn/pin SDK/server và đánh giá extension/fork, proof quota/admission/cutoff/load/cleanup; host/key store và vận hành còn mở |
| Kế hoạch/dự toán | Ngày đầu 05/10/2026, công suất 3–4 ngày/tuần; phương án 16 tuần/148 ngày công, dự phòng 20 ngày; ngân sách tượng trưng 250 triệu | Chọn lịch; nhóm rà soát ngày công/lịch nghỉ và số liệu sử dụng thực tế |
| Nghiệm thu/vận hành | Quy trình/lỗi tồn DEC-110, 10 gate/12 smoke; [runbook](operations-runbook.md) 8 tình huống, lịch/bàn giao; mẫu phát hành/sự cố; quản trị kỹ thuật DEC-112 | Người duyệt chưa chọn DEC-111; người trực/thay thế/lịch, provider/domain/key store/tool và lệnh production còn mở; kết quả diễn tập thuộc triển khai |

Các đầu vào sản phẩm còn mở: lịch 16 tuần, đối tượng/ngôn ngữ, người duyệt và phân công/lịch trực. Phạm vi quản trị kỹ thuật/no admin UI đã chốt DEC-112, không còn gộp với việc chọn tên người phụ trách. Các gói technical evidence/PREP/RLS-GAP vẫn cần thực thi trong kế hoạch phát triển, không phải ca đã chạy trong lần hoàn thiện tài liệu.

<a id="documentation-remaining"></a>

### Phần tài liệu còn cần hoàn thiện

Rà soát ngày 2026-10-04. Các mục dưới đây tách công việc viết/rà soát thiết kế khỏi quyết định sản phẩm và bằng chứng cần tạo khi triển khai. Không mở lại các quy tắc đã chốt tới DEC-112; DEC-089 hoãn lựa chọn limiter, DEC-111 giữ người duyệt chưa được chọn.

| Nhóm | Nội dung tài liệu còn thiếu hoặc chưa khóa | Việc có thể tiếp tục soạn | Quyết định/đầu vào còn cần |
|---|---|---|---|
| Nhu cầu và giá trị sản phẩm | Người dùng đầu tiên, ngôn ngữ/khu vực, kết quả mong muốn và cách đánh giá nội bộ | Kịch bản đánh giá theo hành trình DM và tổ chức phòng; ghi rõ giả định chưa khảo sát | Chọn nhóm sử dụng/ngôn ngữ và cách đánh giá; OQ-001/OQ-012 |
| UX/UI | Có wireframe văn bản; chưa có hồ sơ tương tác/prototype được rà soát, tiêu chí tiếp cận và cấu hình thiết bị cụ thể | Chi tiết trạng thái màn hình, thao tác bàn phím/focus, lỗi/mất mạng/mất quyền, bố cục cuộc gọi và checklist rà soát | Nhóm dùng/ngôn ngữ; ghi browser/OS/thiết bị thực tế khi có build, giữ ma trận DEC-082 |
| Tài khoản | Có thiết kế chi tiết/request-response/consume/schema email; provider/key store và limiter còn mở | Rà soát contract recovery + schema migration/policy/delivery; ACC-GAP theo gói triển khai | Ngưỡng limiter được hoãn DEC-089; email provider/domain/key store; OQ-002/OQ-008 |
| DM | Validation/bản nháp đã chốt; HMAC/SQL/Hub/cursor-resume và fixture đã chi tiết hóa, chưa có mock/proof | Review đồng bộ OpenAPI/schema và UI, migration legacy/transaction guard; mock/proof thuộc triển khai | DEC-103/104/108 chốt no self-delete/account lock/restore placeholder; projection/guard/schema và DATA-GAP proof còn triển khai |
| Cộng đồng và quyền | Có thiết kế chi tiết role/ACL/epoch, OpenAPI/realtime/fixture và mapping migration; chưa có review/mock/proof | Rà soát contract/UX, migration Unicode và guards/outbox/key ring; bằng chứng transaction/thu hồi thuộc triển khai | Vai trò/search/tên/private switch/issuer/quản lý phòng đã chốt DEC-092–098; limiter bổ sung còn mở, không xóa server MVP; retention chính DEC-105–109 đã chốt, còn proof/migration |
| Media | Có thiết kế chi tiết/OpenAPI/realtime/fixture/coordinator/lease/quota gate; chưa có implementation hoặc proof | Review schema/UX/fingerprint, migration/shared guards; thử quota gate và fail-close trên build LiveKit tự host được pin | Quy tắc bổ sung đã chốt DEC-099–102; limiter media, host/domain/key store, extension/server/SDK và kho sổ/worker còn OQ-006/007/008/010/011; retention DEC-106/107 đã chốt |
| Kế hoạch và dự toán | Có phương án 16 tuần/148 ngày công, dự phòng 20 ngày và ngân sách tượng trưng 250 triệu; lịch chưa được chọn | Phân rã gói việc, đầu ra/người rà soát/phụ thuộc, cập nhật tác động khi chọn lịch | Chọn 16 tuần hay giữ mục tiêu 3 tháng; nhóm rà soát ngày công/lịch nghỉ; OQ-009 |
| Nghiệm thu và vận hành | Có [quy trình/10 gate/12 smoke](release-operations.md), [runbook 8 tình huống](operations-runbook.md), [mẫu phát hành](templates/release-record.md)/[sự cố](templates/incident-record.md); DEC-110/112 chốt lỗi tồn/quản trị | Review và bổ sung manifest/lệnh thật khi chọn công cụ, khóa ma trận đo/thiết bị, diễn tập và điền hồ sơ theo RLS-GAP-01–08 | Người duyệt chưa chọn DEC-111; người trực/thay thế/lịch, provider/domain/key store/tool; OQ-007/008/010/011 |
| Vòng đời dữ liệu | Có [policy/matrix/thiết kế](data-lifecycle.md), DEC-103–109, schema sổ bảo vệ, contentState/availability, fixture và AC-DATA/TC-DATA | Review/migration/guard/worker, chọn kho sổ/key/tool và proof hai kho/restore/retention | Phạm vi/TTL/restore đã chốt; người vận hành/thẩm quyền và công cụ còn OQ-008/011 |

**Bằng chứng thuộc gói triển khai:** mock/prototype chạy được, kiểm thử ACC-GAP, lưu bền/chống trùng/đồng thời/reconnect/thu hồi chat, admission LiveKit, tải/chất lượng media và diễn tập restore/rollback. Cần kế hoạch và mẫu ghi nhận trong docs; kết quả chỉ điền sau khi thực thi trên build/môi trường cụ thể. Không dùng việc chưa chạy các ca này để kết luận mọi quy tắc nghiệp vụ đều chưa được chốt.

Tài khoản/DM, cộng đồng/quyền, media và vòng đời dữ liệu đã có bản thiết kế chi tiết; nghiệm thu/vận hành đã có checklist, runbook và mẫu hồ sơ. Các bản này còn review và đầu vào ở bảng trên. Phần có thể tiếp tục soạn là UX/UI; nhu cầu/ngôn ngữ, lịch và tên người vận hành/người duyệt được cập nhật khi người dùng chọn. Không ghi các đề xuất phân công hoặc kết quả diễn tập là đã xác nhận.

<a id="delivery"></a>

## 8. Kế hoạch bàn giao và ước lượng

### Cơ sở và nguyên tắc lập lịch

Ba người Vg, Sáng và Thái dự kiến mỗi người 3–4 ngày/tuần, đủ ngày làm việc, từ 2026-10-05 theo DEC-088. Vg kiêm điều
phối, đại diện sản phẩm, kiến trúc, UX/UI, lập trình và hướng dẫn Thái;
Sáng phụ trách backend và tích hợp; Thái phụ trách frontend và kiểm thử.
Thời gian kiêm nhiệm phải lấy từ quỹ thời gian của chính từng người,
không tính như một vị trí bổ sung.

Ngày bắt đầu dự kiến 2026-10-05; công suất 3–4 ngày/tuần theo DEC-088. Mục tiêu ba tháng cũ cần đánh giá lại; đề xuất 16 tuần và mô hình 250 triệu chỉ để lập kế hoạch, chưa phải cam kết/chi phí thật. Giới hạn media đã chốt DEC-079; năng suất và đơn giá thực tế còn thiếu. Các đợt dưới đây
có thứ tự phụ thuộc, có thể chồng lấp ở phần việc đã đủ đầu vào.

### Đợt bàn giao và điều kiện chuyển tiếp

| Đợt | Kết quả cần có | Chủ trì dự kiến | Phụ thuộc và điều kiện chuyển |
|---|---|---|---|
| 0. Chốt nền | Quy tắc tài khoản tối thiểu, ngoại lệ DM, ma trận quyền cốt lõi; wireframe hai hành trình; hợp đồng API và kiểm thử thử nghiệm lưu/nhận tin. | Vg chủ trì sản phẩm/kiến trúc/UX; Sáng đánh giá backend; Thái rà soát giao diện/kiểm thử. | OQ-002, OQ-004, OQ-005; chưa bắt đầu phát triển tính năng phụ thuộc vào quy tắc còn mở. |
| 1. Nhắn tin riêng | Đăng nhập đủ dùng, tìm người, hội thoại riêng, gửi/nhận/lịch sử, sửa/xóa, lỗi và thử lại; kiểm thử tích hợp và tiêu chí AC-DM. | Sáng và Vg phát triển; Thái frontend/kiểm thử. | Đợt 0 đủ thiết kế; kiểm tra tin đã lưu khi người nhận mở lại, quyền sửa/xóa và gửi lại không trùng. |
| 2. Cộng đồng và phòng | Tạo/tham gia cộng đồng, tìm kiếm/lời mời/chờ duyệt, phòng theo chủ đề, quyền và nhắn tin phòng; tiêu chí AC-COM. | Vg chốt quy tắc/quyền; Sáng backend; Thái frontend/kiểm thử. | Đợt 1 ổn định phần nhắn tin dùng chung; OQ-003/OQ-004 được chốt cho các luồng triển khai. |
| 3. Thoại/video/chia sẻ màn hình | Gọi riêng, phòng thoại, camera và chia sẻ màn hình theo phạm vi SCP-006/007; kiểm thử chất lượng và chi phí. | Vg chủ trì giải pháp; Sáng tích hợp; Thái giao diện/kiểm thử. | OQ-006/OQ-008 và thử nghiệm media; chốt giới hạn/thiết bị trước khi cam kết công sức. |
| 4. Ổn định và phát hành | Kiểm thử hệ thống, phân quyền, tải, sao lưu/khôi phục, xử lý lỗi, tài liệu vận hành và nghiệm thu. | Thái điều phối kiểm thử với Vg; Vg quyết định kỹ thuật/sản phẩm; Sáng sửa lỗi/vận hành. | Đạt điều kiện ở [kế hoạch kiểm thử](release-operations.md#testing); có người trực vận hành và quyết định mở công khai. |

Đợt 1 và 2 là ưu tiên trải nghiệm; đợt 3 vẫn nằm trong phạm vi phiên bản
đầu. Nếu ước lượng vượt mốc ba tháng hoặc ngân sách, Vg phải trình phương
án điều chỉnh thời gian, nguồn lực hoặc phạm vi cho đại diện khách hàng
xác nhận theo quy trình thay đổi. Không tự loại media khỏi phiên bản đầu.

Tình trạng từng đầu ra Đợt 0, gói việc chuẩn bị và bằng chứng còn thiếu
được theo dõi tại [SCDC-READY-001](project.md#readiness). Ngày
2026-10-03 đã bổ sung quyết định sản phẩm, ma trận quyền, wireframe văn
bản, hợp đồng DM và ca kiểm thử; chưa có kết quả thử nghiệm hoặc xác
nhận hoàn tất Đợt 0.

### Phương án 16 tuần để xem xét

Ngày đầu 05/10/2026; kết thúc mục tiêu 24/01/2027 nếu đủ công suất và các điều kiện chuyển đợt. Baseline 3,5 ngày/tuần × 16 tuần = 56 ngày/người, tổng 168 ngày; dải 3–4 ngày là 144–192 ngày. Các ngày dưới đây là ước lượng agent dựa trên scope/source hiện tại, gồm hướng dẫn/review/kiểm thử, chưa có dữ liệu năng suất để cam kết.

| Đợt | Khoảng lịch đề xuất | Vg (ngày) | Sáng (ngày) | Thái (ngày) |
|---|---|---:|---:|---:|
| 0. Chốt nền | Tuần 1–2, 05/10/2026–18/10/2026 | 6 | 6 | 5 |
| 1. DM | Tuần 3–6, 19/10/2026–15/11/2026 | 11 | 13 | 12 |
| 2. Cộng đồng/phòng | Tuần 7–10, 16/11/2026–13/12/2026 | 11 | 12 | 11 |
| 3. Media | Tuần 9–13, 30/11/2026–03/01/2027 | 12 | 12 | 11 |
| 4. Ổn định/nghiệm thu | Tuần 14–16, 04/01/2027–24/01/2027 | 8 | 8 | 10 |
| **Tổng công việc baseline** | Gồm kiêm nhiệm và đào tạo | **48** | **51** | **49** |
| **Quỹ còn lại ở 3,5 ngày/tuần** | Dự phòng trong công suất 56 ngày/người | **8** | **5** | **7** |

Cộng đồng/media chồng tuần 9–10 nhưng không cộng quá quỹ thời gian: tuần 7–8 ưu tiên cộng đồng (7 ngày/người); tuần 9–10 phân bổ phần cộng đồng còn lại Vg 4/Sáng 5/Thái 4 ngày và media Vg 3/Sáng 2/Thái 3 ngày. Tuần 11–13 media còn Vg 9/Sáng 10/Thái 8 ngày, trong 10,5 ngày/người baseline. Không giả định Thái tự làm phần media backend.

Ở mức 3 ngày/tuần, Sáng cần 51 ngày trong khi chỉ có 48 ngày, Thái 49; phải kéo lịch hoặc điều chỉnh phân công/công sức. Lịch chưa trừ ngày nghỉ/lễ; nếu giữ mốc ba tháng cần giảm công sức bằng bằng chứng, tăng công suất hoặc được duyệt thay đổi. Đánh giá lại sau đợt 0 và thử nghiệm self-host media; không tự giảm scope. Phương án này cần người dùng chọn trước khi ghi thành lịch đã xác nhận.

### Bảng công việc để ước lượng

Ước lượng mỗi dòng bằng ngày công còn lại, tách thiết kế, thực hiện, rà
soát, kiểm thử và sửa lỗi. Ghi người làm, người rà soát và phụ thuộc; cập
nhật sau mỗi đợt. Baseline minh họa ở phương án trên; bảng gói cần được nhóm rà soát thành ước lượng thực tế.

| Nhóm việc | Đầu ra cần ước lượng | Người dự kiến | Dữ liệu còn thiếu |
|---|---|---|---|
| Sản phẩm/UX | Chốt quy tắc, luồng, wireframe và đánh giá nội bộ. | Vg, Thái hỗ trợ | OQ-002 đến OQ-007 |
| Tài khoản | Đăng ký, đăng nhập, hồ sơ, phiên, bảo vệ tài khoản và kiểm thử. | Sáng, Vg, Thái | OQ-002 |
| Nhắn tin | Dữ liệu, API, cập nhật thời gian thực, UI, xử lý lỗi và kiểm thử. | Sáng, Vg, Thái | OQ-005 |
| Cộng đồng/quyền | Thành viên, mời/duyệt, phòng, phân quyền và kiểm thử. | Sáng, Vg, Thái | OQ-003, OQ-004 |
| Media | Thử nghiệm, tích hợp, UI và kiểm thử chất lượng. | Vg, Sáng, Thái | OQ-006, OQ-008, OQ-010 |
| Nền tảng/vận hành | Môi trường, triển khai, giám sát, sao lưu và khôi phục. | Vg, Sáng | OQ-007, OQ-010, OQ-011 |
| Chất lượng/phát hành | Bộ kiểm thử, sửa lỗi, nghiệm thu và bàn giao. | Thái, Vg, Sáng | Tiêu chí chất lượng và ma trận trình duyệt |
| Hướng dẫn/đào tạo | Hướng dẫn Thái, rà soát code và tài liệu bàn giao. | Vg, Sáng, Thái | Năng lực hiện tại và số vòng rà soát |

### Cách khóa lịch và chi phí

1. Chốt ngày bắt đầu, số ngày làm việc/tháng, thời gian nghỉ và tỷ lệ
   phân bổ thực tế cho từng người. Tổng tỷ lệ công việc của mỗi người
   không vượt quá 100% trong cùng kỳ.
2. Ước lượng từng nhóm việc sau khi có quy tắc/thiết kế đủ rõ. Ghi khoảng
   ước lượng và rủi ro; giữ riêng công sức hướng dẫn, tích hợp, sửa lỗi,
   kiểm thử và chuẩn bị vận hành.
3. Xếp phụ thuộc thành lịch, xác định đường găng; so với mốc khoảng ba
   tháng và công suất ba người. Xem xét riêng rủi ro media.
4. Tính chi phí nhân sự từ ngày công và đơn giá đã xác nhận, chi phí hạ
   tầng từ cấu hình/mức dùng dự kiến, cộng dự phòng minh bạch. Đối chiếu
   với mốc 500 triệu đồng tại [SCDC-EST-001](project.md#budget).
5. Khi có đủ số liệu, lập lịch ngày cụ thể, dự toán điều chỉnh, người
   duyệt và ngưỡng xử lý sai lệch. Cho đến lúc đó, tài liệu này là khung
   triển khai, không phải cam kết ngày phát hành.

### Theo dõi và điều chỉnh

Mỗi đợt ghi phần đã hoàn thành theo tiêu chí chấp nhận, lỗi còn mở, ngày
công thực tế, công việc còn lại và thay đổi phạm vi. Vg tổng hợp tác động
đến mốc bàn giao; các thay đổi ảnh hưởng phạm vi, thời gian hoặc ngân
sách theo [SCDC-PRC-001](project.md#process).

MVP không có điều kiện bắt buộc tách microservices. Quyết định về thời điểm tách, YARP/gRPC/broker và các cổng dịch vụ dành cho đợt sau; chưa có lịch xác nhận. Lộ trình 5 tuần ở hướng dẫn kỹ thuật cũ đã được thay bằng kế hoạch theo đợt này.

<a id="readiness"></a>

## 9. Sẵn sàng phát triển và gói chuẩn bị

Trạng thái của bảng này là mức sẵn sàng đặc tả và bằng chứng bàn giao. Identity đã có implementation; Messaging/Community mới có nền module. Bộ ca TC trong docs chưa có kết quả chạy được ghi nhận. Các test tự động có trong repo được liệt kê ở [hướng dẫn phát triển](development.md#testing).

### Bảng điều kiện sẵn sàng

| Mã | Điều kiện/đầu ra | Tình trạng và bằng chứng | Việc còn lại/đầu mối dự kiến |
|---|---|---|---|
| READY-01 | Tài khoản tối thiểu | Đã chốt ACC-P01–05 (DEC-063–068) và mật khẩu trùng DEC-113; có thiết kế token/email/consume, OpenAPI recovery 4 thao tác, AC-ACC-01–22 và [12 use case/coverage source-test](features/accounts.md#use-case-coverage); chưa chạy kiểm thử trong bước tài liệu | Rà soát provider/key store; limiter bổ sung được hoãn DEC-089; giải quyết ACC-GAP/API/UI và ghi bằng chứng khi triển khai; Vg/Sáng |
| READY-02 | DM và ngoại lệ | Đã chốt 2.000 UTF-16, tìm người, không tự hết hạn, cách gửi, validation và bản nháp (DEC-068–071/090/091); có AC-DM và ngoại lệ tại [đặc tả](features/direct-messaging.md#requirements) | Validation/bản nháp đã chốt DEC-090/091; có Unicode fixture, HMAC/mapping SQL/Hub/resume; còn review/mock, kiểm chứng đồng thời và no self-delete/account lock/restore đã chốt DEC-103/104/108, còn DATA-GAP proof; Vg/Sáng, Thái đối chiếu |
| READY-03 | Ma trận quyền và thiết kế Community | [5 thành phần và 25 UC](features/community.md#organization), [Permissions](features/community/permissions.md#detailed-design) và [tích hợp](features/community/integration.md#contracts) có role/ACL/epoch/45 REST/9 realtime/fixture, AC-COM-01–42; [gói đầu tiên](features/community.md#use-case-delivery) đã xác định | Review/mock và migration/guard/thu hồi cần proof; media deadline DEC-099 và retention chính DEC-105–109 đã chốt, còn worker/restore proof; DM vẫn độc lập role cộng đồng |
| READY-04 | UX hai hành trình | Có wireframe văn bản [tài khoản/DM](features/direct-messaging.md#ux) và [cộng đồng](features/community.md#ux) | Vg rà soát, Thái dựng prototype; ma trận trình duyệt đã chốt DEC-082; khóa OS/thiết bị/build và trạng thái còn mở |
| READY-05 | API/dữ liệu DM | Có [hợp đồng đề xuất](features/direct-messaging.md#contracts) với schema logic, lỗi, chống trùng, lịch sử và cập nhật | REST/SignalR, ID/cursor đã chọn DEC-081; Vg/Sáng rà soát schema/lỗi cùng Thái; Identity có OpenAPI từ code; DM có [OpenAPI dự thảo](contracts/direct-messaging.openapi.json), có schema realtime/fixture/thiết kế chi tiết; chưa có mock hoặc proof chạy được |
| READY-06 | Thử nghiệm kỹ thuật | Có kịch bản cần chứng minh tại [bằng chứng thử nghiệm](#technical-evidence); chưa chạy | Chốt thiết kế DM trên nền Modular Monolith và môi trường, chạy và ghi bằng chứng; Vg/Sáng |
| READY-07 | Kiểm thử | Có [ca kiểm thử](features/direct-messaging.md#tests) và dữ liệu dự kiến; tất cả chưa chạy | Thái chuẩn bị fixture, Vg/Sáng rà soát; ngưỡng chat đã chốt DEC-083; khóa fixture/môi trường, đo cả ngưỡng media đã chốt DEC-085 và ghi kết quả |
| READY-08 | Công việc/nguồn lực | Có các gói cụ thể tại [gói chuẩn bị](#preparation); có baseline ngày công/lịch 16 tuần đề xuất; chưa xác nhận | Vg/Sáng/Thái ước lượng trong quỹ thời gian thật, gồm hướng dẫn và kiểm thử |

Không tính phần trăm từ số dòng hoàn tất. Mỗi gói chỉ sẵn sàng khi có
yêu cầu, thiết kế, kiểm chứng và người phụ trách tương ứng. Quyết định
media, chi phí toàn dự án và vận hành vẫn phải hoàn thành ở các đợt
liên quan; không tự loại chúng khỏi phiên bản đầu để đánh dấu Đợt 0 xong.

<a id="preparation"></a>

### Gói việc đủ cụ thể để ước lượng

| Gói | Đầu ra | Phụ thuộc | Thực hiện / rà soát dự kiến | Điều kiện hoàn tất |
|---|---|---|---|---|
| PREP-01 | Hoàn thiện thiết kế tài khoản và fixture sau chính sách đã chốt | ACC-GAP-01–07, OQ-002 | Vg, Sáng / Thái đối chiếu kiểm thử | Quy tắc không còn chỗ diễn giải khác nhau ở luồng bàn giao; AC cập nhật |
| PREP-02 | Prototype tài khoản/DM và cộng đồng theo mã màn hình | Wireframe, quyết định liên quan | Vg thiết kế, Thái dựng / Sáng rà soát phản hồi API | Có trạng thái rỗng/lỗi/mất mạng/mất quyền; ghi kết quả rà soát |
| PREP-03 | Hợp đồng API có schema máy đọc được và mock | SCDC-API-DM-001, PREP-01 | Sáng, Vg / Thái dùng mock | Request/response/lỗi thống nhất; không trả dữ liệu ngoài quyền |
| PREP-04 | Thử nghiệm DM lưu bền/chống trùng/phân trang | PREP-03, lựa chọn công nghệ | Sáng / Vg, Thái quan sát | Các [kịch bản thử nghiệm](#technical-evidence) có bằng chứng, rủi ro được ghi nhận |
| PREP-05 | Thử nghiệm cập nhật, reconnect và thu hồi phiên | PREP-04, thiết kế phiên | Sáng, Vg / Thái | Không trùng/sót dữ liệu; kết nối bị thu hồi đúng ngưỡng đã chốt |
| PREP-06 | Ma trận thiết bị và fixture kiểm thử | DEC-059, AC và ca kiểm thử | Thái / Vg, Sáng | Danh sách trình duyệt/phiên bản, kích thước, dữ liệu và kết quả mong đợi rõ |
| PREP-07 | Ước lượng và lịch đợt tài khoản/DM | PREP-01–06 đủ rõ | Vg, Sáng, Thái | Ngày công, người làm/rà soát, thời gian hướng dẫn và phụ thuộc không trùng quỹ thời gian |

Đây là phân công dự kiến theo vai trò hiện có, chưa phải xác nhận các
thành viên đã nhận việc. Ngày công/lịch baseline ở phương án 16 tuần; từng gói vẫn cần nhóm rà soát và xác nhận trước cam kết. Gói cộng đồng chi tiết và media tiếp tục theo kế hoạch đợt 2/3.

<a id="technical-evidence"></a>

### Bằng chứng thử nghiệm kỹ thuật cần có

1. Hai người xác thực được, mở cùng một DM khi tạo đồng thời; người thứ
   ba không truy cập được API hoặc kênh cập nhật.
2. Gửi tin lưu bền; rollback không phát sự kiện; mất response sau commit
   và gửi lại đồng thời chỉ tạo một tin.
3. Phân trang không bỏ sót tin khi có giao dịch commit trễ; reconnect
   bù nhiều trang và cập nhật sửa/xóa tin cũ, không dùng cache hết hiệu lực.
4. Worker dừng rồi hoạt động lại; sự kiện lặp/đảo thứ tự không nhân đôi
   hoặc đưa giao diện về phiên bản tin cũ.
5. Thu hồi phiên chặn HTTP và kết nối chat đang mở trong ≤5 giây theo DEC-083; ghi cách đo, không tuyên bố thu hồi tức thời khi chưa chứng minh.

Mỗi bằng chứng ghi môi trường, bản thử, dữ liệu, thao tác, kết quả, hạn
chế và người rà soát. Viết kế hoạch hoặc sơ đồ không thay thế chạy thử.

### Thứ tự xử lý tiếp theo

Ngày 2026-10-04 đã bổ sung thiết kế chi tiết Accounts/DM, Community/quyền/tin phòng, Media/LiveKit tự host, [vòng đời dữ liệu](data-lifecycle.md) và [nghiệm thu/vận hành](release-operations.md) với gate/smoke/runbook/mẫu hồ sơ. Ngày 2026-10-05 bổ sung use case Identity và DEC-113 về mật khẩu trùng; chưa sửa mã hoặc chạy kiểm thử sản phẩm. Quyết định cập nhật tới DEC-113; limiter tài khoản hoãn DEC-089, người duyệt chưa chọn DEC-111. Các bản thiết kế còn cần rà soát; provider/key store/kho sổ, phân công và bằng chứng triển khai ở [phần còn cần hoàn thiện](#documentation-remaining). UX/UI là phần có thể soạn tiếp. Chỉ xác nhận gói phát triển khi các phụ thuộc trực tiếp được giải quyết; không cần chờ toàn bộ media để rà soát Accounts/DM/Community.

Các nội dung còn mở được tập trung tại OQ-002–OQ-011; bảng này dẫn chiếu
và theo dõi đầu ra, không tạo một nguồn quyết định sản phẩm khác.

<a id="process"></a>

## 10. Quy trình và quản lý thay đổi

Khung vòng đời và trách nhiệm dưới đây là đề xuất hợp nhất từ SCDC-PRC-001; cần xác nhận cách áp dụng theo từng gói bàn giao. Quy ước duy trì nguồn docs và lưu lịch sử được áp dụng trong lần tổ chức lại này.


### Phạm vi áp dụng

Quy trình xác định trách nhiệm, đầu ra và điều kiện bàn giao từ tiếp nhận
nhu cầu đến phát hành, vận hành và cải tiến SCDC. Phạm vi sản phẩm được quản
lý tại [Project Brief](project.md#scope).

Công việc được triển khai theo từng nhóm chức năng. Phân tích, thiết kế và
kiểm thử có thể diễn ra song song khi đủ đầu vào; kết quả đánh giá được phản
hồi để điều chỉnh yêu cầu và kế hoạch liên quan.

Đánh giá khả thi và lập kế hoạch bắt đầu từ giai đoạn khởi tạo, gồm nguồn
lực, thời gian, chi phí, rủi ro và phương án làm rõ nhu cầu sơ bộ. Sau khi yêu cầu và
thiết kế rõ hơn, kế hoạch được chi tiết hóa và cập nhật theo từng đợt bàn giao.

### Vai trò và trách nhiệm

| Vai trò | Trách nhiệm | Nội dung xác nhận |
|---|---|---|
| Đại diện khách hàng | Cung cấp mục tiêu và yêu cầu; quyết định ưu tiên, phạm vi, ngân sách và thay đổi. | Nghiệp vụ, phạm vi và nghiệm thu kết quả. |
| Người dùng đại diện | Cung cấp nhu cầu sử dụng; tham gia đánh giá luồng thao tác và bản thiết kế tương tác. | Phản hồi về khả năng sử dụng. |
| Quản lý dự án | Điều phối công việc; quản lý tiến độ, nguồn lực, phụ thuộc, chi phí và rủi ro. | Kế hoạch thực hiện và trạng thái bàn giao. |
| Phân tích nghiệp vụ (BA) | Làm rõ nhu cầu, quy tắc, ngoại lệ; đặc tả yêu cầu và theo dõi thay đổi. | Tính đầy đủ và nhất quán của đặc tả. |
| Thiết kế UX/UI | Thiết kế hành trình, luồng thao tác, giao diện và các trạng thái tương tác. | Hồ sơ trải nghiệm và giao diện. |
| Tech Lead và kỹ sư | Đánh giá khả thi; thiết kế kiến trúc, dữ liệu, tích hợp; phát triển và rà soát mã nguồn. | Giải pháp kỹ thuật và chất lượng triển khai. |
| QA/kiểm thử | Rà soát khả năng kiểm chứng yêu cầu; lập và thực hiện kế hoạch kiểm thử. | Kết quả kiểm thử và các vấn đề chất lượng còn tồn tại. |
| Vận hành/hỗ trợ | Chuẩn bị môi trường, phát hành, giám sát, sao lưu, khôi phục và tiếp nhận sự cố. | Khả năng vận hành và phương án hỗ trợ. |

Một nhân sự có thể đảm nhiệm nhiều vai trò. Kế hoạch nguồn lực phải xác định
người chịu trách nhiệm và thẩm quyền xác nhận cho từng đầu ra.

### Các giai đoạn công việc

| Giai đoạn | Chủ trì | Đầu ra | Điều kiện hoàn tất |
|---|---|---|---|
| 1. Khởi tạo | Khách hàng, BA, quản lý dự án | Yêu cầu ban đầu, Project Brief, đầu mối xác nhận, đánh giá khả thi và dự toán sơ bộ, phương án làm rõ nhu cầu, quyết định và vấn đề cần làm rõ. | Thống nhất định hướng, phạm vi sơ bộ, ràng buộc, đầu mối và cách làm rõ nhu cầu. |
| 2. Làm rõ nhu cầu | BA, UX | Mô tả người dùng dự kiến, vấn đề cần giải quyết, giá trị mong muốn, hành trình ưu tiên, giả định và mức độ kiểm chứng. | Có đầu vào để xác định nhu cầu ưu tiên và nội dung cần phân tích; giả định chưa kiểm chứng và cách đánh giá giá trị được ghi rõ. |
| 3. Đặc tả yêu cầu | BA, phối hợp khách hàng và QA | Quy tắc nghiệp vụ, luồng chính/ngoại lệ, ma trận quyền, yêu cầu chất lượng và tiêu chí chấp nhận. | Các yêu cầu có phạm vi rõ, kiểm chứng được và được khách hàng xác nhận. |
| 4. Thiết kế UX/UI | UX/UI, phối hợp BA và kỹ sư | Sơ đồ màn hình, luồng thao tác, bản thiết kế tương tác và đặc tả giao diện. | Luồng sử dụng và trạng thái giao diện đáp ứng yêu cầu; các vấn đề qua đánh giá được xử lý. |
| 5. Thiết kế kỹ thuật | Tech Lead, kỹ sư, vận hành | Kiến trúc Modular Monolith cho MVP, dữ liệu, hợp đồng API/sự kiện, thiết kế chi tiết và phương án vận hành. | Giải pháp được rà soát về khả thi, tích hợp, hiệu năng và chi phí. |
| 6. Hoàn thiện kế hoạch thực hiện | Quản lý dự án, toàn nhóm | Danh sách công việc theo ưu tiên, phân công, phụ thuộc, lịch bàn giao, dự toán cập nhật và kế hoạch kiểm thử. | Kế hoạch ban đầu được chi tiết hóa theo yêu cầu và thiết kế; có nguồn lực thực hiện và các điều chỉnh cần thiết được xác nhận. |
| 7. Phát triển và tích hợp | Kỹ sư, Tech Lead | Chức năng đã triển khai, mã nguồn được rà soát, kết quả kiểm thử kỹ thuật và bản tích hợp. | Đáp ứng tiêu chí chấp nhận của phần công việc và sẵn sàng kiểm thử hệ thống. |
| 8. Kiểm thử và nghiệm thu | QA, khách hàng | Kết quả kiểm thử chức năng, tích hợp, phân quyền, hiệu năng; danh sách lỗi và kết quả nghiệm thu. | Đạt điều kiện nghiệm thu; lỗi còn tồn tại được đánh giá và thống nhất cách xử lý. |
| 9. Phát hành và bàn giao | Vận hành, Tech Lead, quản lý dự án | Bản phát hành, hướng dẫn triển khai/vận hành, phương án khôi phục và đầu mối hỗ trợ. | Kiểm tra sau triển khai đạt yêu cầu; có người tiếp nhận vận hành và quyết định mở sử dụng công khai. |
| 10. Vận hành và cải tiến | Vận hành/hỗ trợ, khách hàng, nhóm sản phẩm | Báo cáo sử dụng, chất lượng, sự cố, chi phí và danh sách cải tiến ưu tiên. | Sự cố được xử lý theo trách nhiệm; yêu cầu mới được đánh giá trước khi đưa vào kế hoạch tiếp theo. |

Các điều kiện hoàn tất áp dụng cho phần công việc được bàn giao. Những vấn
đề chưa ảnh hưởng đến phần đó có thể tiếp tục xử lý nếu đã xác định người
phụ trách, thời hạn và tác động.

Khảo sát người dùng bên ngoài là một cách làm rõ nhu cầu, không phải điều
kiện bắt buộc cho mọi đợt. Nếu bỏ qua, quyết định và giới hạn bằng chứng
phải được ghi tại sổ quyết định; đặc tả không được trình bày giả định của
nhóm như kết quả khảo sát. Đợt hiện tại áp dụng
[DEC-030](decisions.md#decisions).

### Nội dung hồ sơ thiết kế chi tiết

Mỗi chức năng cần liên kết được yêu cầu, thiết kế và phương pháp kiểm chứng.
Mức chi tiết được xác định theo độ phức tạp và rủi ro của chức năng.

| Thành phần | Nội dung cần có |
|---|---|
| Yêu cầu | Mục tiêu sử dụng, phạm vi và tiêu chí chấp nhận. |
| Nghiệp vụ | Đối tượng thực hiện, điều kiện trước/sau, quyền, luồng chính, ngoại lệ và giới hạn. |
| UX/UI | Màn hình, thao tác, trạng thái tải/rỗng/lỗi, mất mạng và mất quyền truy cập. |
| Dữ liệu | Thực thể, quan hệ, ràng buộc, chủ sở hữu dữ liệu và vòng đời. |
| API và sự kiện | Đầu vào/đầu ra, xác thực, phân quyền, lỗi, đối tượng nhận và quy tắc tương thích. |
| Tương tác hệ thống | Trình tự xử lý giữa giao diện và dịch vụ, gồm các nhánh lỗi và mất kết nối. |
| Độ tin cậy | Xử lý đồng thời, giao dịch, gửi lại, trùng lặp và khôi phục trạng thái. |
| Vận hành | Log, chỉ số giám sát, giới hạn tài nguyên, sao lưu và khôi phục. |
| Kiểm thử | Tình huống, dữ liệu, môi trường, điều kiện chấp nhận và cách đo chất lượng. |

### Điều kiện bàn giao cho phát triển

Một phần công việc được đưa vào triển khai khi:

- Yêu cầu và tiêu chí chấp nhận đủ rõ để triển khai và kiểm thử.
- Luồng giao diện, dữ liệu và hợp đồng tích hợp đã được rà soát giữa các bên liên quan.
- Các phụ thuộc và vấn đề cản trở đã được giải quyết hoặc có phương án xử lý.
- Có người phụ trách, ước lượng công sức và phạm vi bàn giao cụ thể.
- Phương pháp kiểm thử và điều kiện nghiệm thu đã được xác định.

BA chịu trách nhiệm đặc tả; UX/UI chịu trách nhiệm thiết kế tương tác;
Tech Lead chịu trách nhiệm giải pháp; QA chịu trách nhiệm phương pháp kiểm
chứng; quản lý dự án chịu trách nhiệm kế hoạch. Khách hàng xác nhận hành vi
sản phẩm và những thay đổi ảnh hưởng đến phạm vi, thời gian hoặc chi phí.

### Quản lý hồ sơ và thay đổi

Hồ sơ được lưu tập trung tại `docs/`, sử dụng Markdown cho bản nguồn
và PDF khi cần bản bàn giao. Phiên bản tài liệu được quản lý bằng Git; thay đổi quyết định được ghi ở decisions.md, bản đã thay thế được lưu trong archive. Không duy trì bản tóm tắt chép lại quy tắc ở file khác.

Các mã `REQ-*`, `SCP-*`, `SUC-*`, `DEC-*`, `COST-*`, `AS-*`, `RSK-*`, `OQ-*`
lần lượt nhận diện yêu cầu, phạm vi, tiêu chí thành công, quyết định, chi phí,
giả định, rủi ro và vấn đề cần làm rõ. Giữ mã ổn định khi cập nhật nội dung.

Đặc tả, thiết kế và kết quả kiểm thử phải dẫn chiếu đến yêu cầu tương ứng.
Trạng thái tài liệu phân biệt rõ bản dự thảo, bản đang xem xét, bản đã xác
nhận và bản đã được thay thế. Giả định có đầu mối và cách kiểm chứng riêng.

Khi có đề nghị thay đổi:

1. BA ghi mục tiêu, nội dung và phạm vi bị ảnh hưởng.
2. UX/UI, kỹ thuật và QA đánh giá tác động đến thiết kế, công sức và kiểm thử.
3. Quản lý dự án tổng hợp ảnh hưởng đến nguồn lực, tiến độ và chi phí.
4. Khách hàng quyết định đối với thay đổi phạm vi, thời gian hoặc ngân sách.
5. Nhóm cập nhật yêu cầu, thiết kế, kế hoạch và các tiêu chí kiểm chứng liên quan.

Kết quả giải quyết vấn đề và thay đổi quyết định được cập nhật tại
[sổ quyết định](decisions.md#decisions).
