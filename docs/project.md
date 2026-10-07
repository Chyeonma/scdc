# SCDC — Mục tiêu, yêu cầu, phạm vi và kế hoạch

Cập nhật: 2026-10-06. Nguồn chuẩn cho REQ, SCP, SUC, COST, AS, RSK, READY và PREP. Phần bàn giao theo mốc ở [MVP](releases/mvp.md) và [v1](releases/v1.md); cách tổ chức ở [lộ trình](roadmap.md).

Phạm vi sản phẩm đã được xác định ở mức tổng thể. Đặc tả và bằng chứng còn thiếu được theo dõi bên dưới; có code trong repo chưa đồng nghĩa đã nghiệm thu hoặc sẵn sàng phát hành.

## Mục lục

- [Nhu cầu và người sử dụng](#needs)
- [Yêu cầu cấp cao](#requirements)
- [Phạm vi v1](#scope)
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
[SCDC-DIS-001](#needs). Đặc tả hành vi
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

Phạm vi bàn giao được tổng hợp tại [Project Brief](#scope).
Những quy tắc nghiệp vụ và giới hạn cần xác định được theo dõi tại
[sổ vấn đề cần làm rõ](decisions.md#decisions).


Đầu vào hiện tại là từ đại diện sản phẩm theo DEC-030. Chưa xác định nhóm người dùng đầu tiên, ngôn ngữ/khu vực và thước đo giá trị tại OQ-001/OQ-012. DM là nền chức năng ưu tiên; giá trị tách nhiều chủ đề cần tiếp tục kiểm chứng ở cộng đồng. Bộ câu hỏi phỏng vấn cũ được lưu dự phòng trong archive, không phải hoạt động khảo sát đã thực hiện.

<a id="requirements"></a>

## 2. Yêu cầu cấp cao

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

## 3. Hai mốc bàn giao, phạm vi v1 và ràng buộc

Theo [DEC-114](decisions.md#dec-114), thêm MVP trước bản đầy đủ đã đặc tả để nhóm dễ bắt đầu triển khai. Phạm vi đầy đủ trước đây được gọi là MVP nay mang tên **v1**; yêu cầu, mã truy vết và các quy tắc nghiệp vụ được giữ. DEC-116 chốt MVP một API host và microservice ở v1; DEC-117 cập nhật phân công kế hoạch.

| Mốc | Phạm vi đã xác nhận | Hồ sơ sử dụng |
|---|---|---|
| MVP | Identity, Community và Direct Messaging; chọn luồng tối thiểu theo từng gói, chạy trong một API host | [Phạm vi, gói việc và điều kiện MVP](releases/mvp.md); phân công theo DEC-117, chi tiết UC/AC còn cần khóa |
| v1 | Chuyển microservice và hoàn thiện toàn bộ phạm vi ở bảng SCP bên dưới, gồm gọi riêng/phòng thoại, video và chia sẻ màn hình | [Hồ sơ v1](releases/v1.md), đặc tả tính năng và điều kiện phát hành hiện có |

REQ/SCP/SUC dưới đây mô tả sản phẩm mục tiêu v1. Hồ sơ từng mốc xác định phần bàn giao và các tiêu chí áp dụng; hoàn tất MVP không suy ra đã đạt toàn bộ SUC hoặc có thể mở công khai.

### Phạm vi bản hoàn thiện v1

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

Ngoài phạm vi bản hoàn thiện v1:

- Nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này.
- Ứng dụng cài đặt riêng trên desktop và thiết bị di động.

Các chức năng bổ sung được đánh giá theo quy trình quản lý thay đổi.

### Ràng buộc và cơ sở lập kế hoạch

| Nội dung | Giá trị |
|---|---|
| Kiến trúc | MVP một API host Modular Monolith; microservice thuộc v1 theo DEC-116. Phương án ranh giới tại [kiến trúc](architecture.md#target) còn cần chốt, chưa có chuyển đổi code. |
| Thời gian mục tiêu | Khoảng 3 tháng từ ngày khởi động. Ngày bắt đầu dự kiến 2026-10-05 theo DEC-088; lịch còn là đề xuất, chưa cam kết phát hành. |
| Ngân sách minh họa | 250.000.000 VNĐ từ phương án 16 tuần trước khi tách MVP/v1; giữ làm cơ sở dự toán cũ theo DEC-088, không là báo giá/trần được duyệt. Mốc 500 triệu giữ trong lịch sử. |
| Nhân sự hiện có | Vg, Sáng và Thái dự kiến mỗi người 3–4 ngày/tuần, đủ ngày làm việc, từ 2026-10-05 theo DEC-088. Theo DEC-117, Vg giữ quyết định quan trọng và Identity/Community; Sáng sở hữu DM/nền tin phòng; Thái sở hữu email worker/môi trường/công cụ, sau đó provider/SDK media. Chi tiết tại [phân công](#team) và [công suất](#capacity). |
| Quy mô dự trù | 1.000 tài khoản, 100 người trực tuyến đồng thời và 20 người tham gia gọi đồng thời trên toàn hệ thống. |

1.000 tài khoản và 100 người online là giả định tải phục vụ dự toán/kiểm thử; 20 người gọi đồng thời là giới hạn sản phẩm đã chốt DEC-079. Cơ cấu nhân sự hiện có khác với giả
định ba kỹ sư có kinh nghiệm ban đầu; cần tính công sức kiêm nhiệm và hướng
dẫn khi đánh giá tiến độ, chi phí. Xem [nhân sự và phương án làm rõ nhu cầu](#team)
và [dự toán và giả định](#budget).

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
| SUC-005 | v1 đáp ứng kiến trúc microservice theo DEC-116, giữ quyền sở hữu dữ liệu và khả năng triển khai độc lập; MVP giữ ranh giới module trong một API host. | v1 có hồ sơ ranh giới service, hợp đồng, dữ liệu và bằng chứng chạy/triển khai các API độc lập, cùng hồi quy luồng MVP; source hiện tại chưa chuyển đổi. |
| SUC-006 | Sản phẩm sẵn sàng cho phát hành công khai. | Kết quả kiểm thử, nghiệm thu và xác nhận khả năng vận hành. |

Chỉ tiêu định lượng đã chốt tại DEC-082/083/085/086; chính sách dữ liệu và cách áp dụng tuổi backup ở DEC-103–109; điều kiện lỗi tồn ở DEC-110. Môi trường, dữ liệu tải, kết quả chạy và người duyệt DEC-111 cần hoàn tất trước nghiệm thu/phát hành thực tế.

| Nội dung cần chốt | Đầu ra cần có | Theo dõi |
|---|---|---|
| Trình duyệt/thiết bị và khả năng tiếp cận | Danh sách phiên bản, thiết bị, kích thước và hành trình bắt buộc; DEC-059 đã chốt desktop/trình duyệt điện thoại cho tài khoản và DM; DEC-082 bổ sung Community/desktop-media | OQ-007 |
| Hiệu năng và tải | Độ trễ API/gửi–nhận theo percentile, tỷ lệ lỗi, dataset, thời lượng tải và cấu hình đo | OQ-007 |
| Chất lượng media | Độ trễ, âm thanh/hình ảnh, số người/luồng, mạng và thời gian reconnect | OQ-006/OQ-007 |
| Thu hồi quyền/phiên | Thời hạn hiệu lực trên HTTP và kết nối đang mở, cách đo | OQ-007/OQ-008 |
| Lưu giữ và khôi phục | [Chính sách dữ liệu](data-lifecycle.md) DEC-103–109, RPO ≤15 phút/RTO ≤4 giờ và tuổi mỗi backup/WAL ≤30 ngày đã chốt; còn công cụ, cửa sổ PITR thực tế và bằng chứng restore | OQ-011 |

1.000 tài khoản và 100 người trực tuyến là giả định dự toán, không phải giới hạn đăng ký/online. Ngày 2026-10-04, 20 người gọi đồng thời trở thành giới hạn media v1 theo DEC-079; chưa có kết quả đo. Các mốc hiệu năng trong tài liệu kỹ thuật cũ chưa có bằng chứng và chưa được xác nhận làm ngưỡng nghiệm thu. Không tuyên bố truy vấn luôn dưới 10ms.

<a id="team"></a>

## 5. Nhân sự và phối hợp

### Phân công thực hiện hiện hành

Theo [DEC-117](decisions.md#dec-117), phân công được cập nhật để mỗi người có đầu ra trọn vẹn và cân tải theo từng giai đoạn. Nhóm dùng AI hỗ trợ; gói việc bao gồm DB, backend, frontend, kiểm thử và tích hợp cần thiết. Vai trò frontend/QA riêng cho Thái và backend chung cho Sáng trước đây được thay bằng phân công dưới đây.

| Thành viên | MVP — một host Modular Monolith | v1 — microservice và bản hoàn thiện | Khả năng tham gia |
|---|---|---|---|
| Vg | Trưởng nhóm/đại diện sản phẩm; chốt quyết định quan trọng, hoàn thiện Identity gần xong và phụ trách Community từ DB tới frontend | Chủ trì ranh giới service và chính sách quyền/phiên/media; chuyển Identity, hoàn thiện Community và review phần rủi ro cao | 3–4 ngày/tuần theo DEC-088 |
| Sáng | Phụ trách Direct Messaging từ DB tới frontend; nền lưu/gửi/nhận/lịch sử dùng chung cho tin phòng | Chuyển Messaging và các hợp đồng liên quan; hoàn thiện nhắn tin, backend và frontend điều khiển lifecycle cuộc gọi | 3–4 ngày/tuần theo DEC-088 |
| Thái | Phụ trách Email Worker; bộ chạy Compose/config/CI, dữ liệu demo và công cụ kiểm tra tích hợp | Hiện thực môi trường nhiều service, định tuyến/CI; tích hợp provider/SDK media, thiết bị và hiển thị nguồn theo hợp đồng đã chốt; hoàn thiện worker/công cụ vận hành | 3–4 ngày/tuần theo DEC-088 |

Vg giữ thẩm quyền chốt phạm vi và quyết định kỹ thuật quan trọng. Phân công phát triển không tự chọn người ký nghiệm thu/mở công khai hoặc cấp quyền production; các nội dung này vẫn thuộc DEC-111/OQ-011.

### Ranh giới phối hợp

| Hạng mục | Người chịu trách nhiệm | Cách phối hợp để tránh dồn việc |
|---|---|---|
| Sản phẩm và kiến trúc | Vg | Chốt quy tắc, hợp đồng và lựa chọn quan trọng; Sáng/Thái tự xử lý chi tiết trong gói đã thống nhất, chỉ đưa lên các thay đổi ảnh hưởng phạm vi/quyền/dữ liệu/tích hợp |
| Identity và Community | Vg | Sáng cung cấp nền Messaging cho tin phòng; Thái cung cấp email worker và môi trường thử. Tận dụng code/UI đã có, tính công sức review/hướng dẫn trong quỹ của Vg |
| DM và tin phòng | Sáng | Sở hữu nền Messaging và UI DM; Vg cung cấp quyền/lifecycle phòng và làm UI Community. Không xây hai cơ chế nhắn tin khác nhau |
| Email và công cụ chạy hệ thống | Thái | Identity giữ logic cấp/consume token, hiệu lực link và dữ liệu job; Thái hiện thực delivery/retry và công cụ theo hợp đồng/policy Vg chốt |
| Microservice ở v1 | Cả ba, Vg chủ trì thiết kế | Vg: ranh giới/Identity/quyền; Sáng: Messaging/hợp đồng/dữ liệu; Thái: container/config/định tuyến/CI. Việc chuyển Community được xếp trong quỹ Vg hoặc điều chuyển một gói triển khai đã thiết kế rõ khi quá tải |
| Media ở v1 | Chia theo trách nhiệm | Vg: quyền/admission/quota/thu hồi và review; Sáng: lifecycle gọi riêng/phòng và trạng thái điều khiển; Thái: adapter provider/SDK, mic/camera/share, hiển thị nguồn và môi trường. Chốt hợp đồng trước khi làm; mỗi phần tự kiểm thử |
| Kiểm thử và hồi quy | Người sở hữu gói; người khác kiểm tra lại | Vg thử Identity/Community, Sáng thử Messaging/lifecycle, Thái thử worker/adapter/môi trường và duy trì bộ chạy. Vg/Sáng viết kỳ vọng quyền/đồng thời; Thái chuẩn bị dữ liệu và tổng hợp kết quả, không chịu một mình chất lượng cả sản phẩm |

Thái nhận gói có đầu vào/đầu ra và cách kiểm chứng rõ; thời gian học và sửa lại được tính trong công suất. Không giao cho Thái tự quyết định nghiệp vụ quyền hoặc lifecycle cuộc gọi chỉ vì dùng AI. Vg review các thay đổi nhạy cảm, Sáng kiểm tra luồng tích hợp liên quan; mỗi người phải chạy và giải thích kết quả của gói mình làm.

Cơ sở nguồn lực giữ 3–4 ngày/tuần; 8 giờ/ngày và 3,5 ngày/tuần chỉ là baseline giả định. Phân bổ 80% công suất cho công việc đã xếp, giữ 20% dự phòng; xem [cân tải theo giai đoạn](#capacity). Tính thiết kế, học/hướng dẫn, kiểm thử, review và sửa lỗi trong gói tương ứng, không cộng thêm ngoài quỹ thời gian.

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
- Tính DB/backend/frontend và tự kiểm thử trong gói tính năng của Vg/Sáng;
  Thái sở hữu worker/công cụ/provider và tự kiểm chứng các đầu ra đó.
- Chỉ phân bổ 80% công suất theo giai đoạn; giữ 20% dự phòng, tính cả
  review/hướng dẫn của Vg và thời gian học/sửa lại của Thái.
- Đánh giá lại mốc ba tháng và dự toán; chi tiết tại
  [SCDC-EST-001](#budget).

<a id="budget"></a>

## 6. Ngân sách, giả định và rủi ro

### Mô hình ngân sách minh họa cập nhật 2026-10-04

Theo DEC-088, ngân sách mang tính tượng trưng và được phép điều chỉnh để trình bày rõ. Mô hình **250.000.000 VNĐ** dưới đây do agent ước lượng cho phương án **16 tuần** trước khi tách MVP/v1 và cân lại phân công; chỉ giữ làm cơ sở dự toán cũ. Đơn giá chưa được các thành viên cung cấp, không phải mức lương thị trường/báo giá hoặc nghĩa vụ thanh toán. Mốc 500 triệu giữ làm lịch sử. Lịch mới cần ước lượng theo giai đoạn và kết quả thực tế tại [kế hoạch](#delivery); không đổi phạm vi v1 hoặc tự loại media.

| Hạng mục | Cơ sở minh họa | Số tiền |
|---|---|---:|
| Vg | 4 tháng × 20 triệu/tháng; gồm sản phẩm, kiến trúc, UX, code và hướng dẫn | 80.000.000 VNĐ |
| Sáng | 4 tháng × 20 triệu/tháng; gồm DM/nền tin phòng, lifecycle media, tích hợp, tự kiểm thử và review | 80.000.000 VNĐ |
| Thái | 4 tháng × 10 triệu/tháng; gồm worker/môi trường/provider, học, tự kiểm thử và sửa lỗi | 40.000.000 VNĐ |
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
- Vg làm Identity còn lại/Community, giữ quyết định quan trọng, điều phối
  và review/hướng dẫn; chuyển service và media được xếp ở giai đoạn v1.
- Sáng làm DM/nền tin phòng từ DB tới frontend, sau đó chuyển Messaging
  và lifecycle/UI điều khiển media; thiết kế, tích hợp và tự kiểm thử nằm trong gói.
- Thái làm email worker/môi trường/công cụ, sau đó provider/SDK/thiết bị
  media; tính cả học, tự kiểm thử, sửa lại và review với Vg/Sáng trong quỹ thời gian.
- QA 2 tháng công và BA/UX/UI 1 tháng công là giả định khối lượng từ dự toán
  ban đầu; cần ước lượng lại theo phân công kiêm nhiệm hiện có và làm rõ
  công việc phân tích nghiệp vụ.

Khi lập lịch, tổng phân bổ của mỗi người phải bao gồm công việc kiêm nhiệm,
học, hướng dẫn và rà soát; tránh tính cùng một khoảng thời gian cho nhiều vai trò.
[Bảng công suất](#capacity) là cơ sở hiện hành; tỷ lệ cần điều chỉnh sau khi đo kết quả thực tế.

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
| AS-002 | QA 2 tháng công và BA/UX/UI 1 tháng công đáp ứng khối lượng công việc. | Cơ sở dự toán cũ cần ước lượng lại: người sở hữu tính năng làm UX/tự kiểm thử, Vg chốt nghiệp vụ và review, Thái cung cấp dữ liệu/bộ chạy; không tính QA/UX như công suất riêng ngoài ba người. | Vg, Sáng, Thái theo phần sở hữu |
| AS-003 | Tích hợp nền tảng media có sẵn cho thoại/video và chia sẻ màn hình. | Đánh giá tính phù hợp, khả năng tích hợp, chi phí và vận hành. | Tech Lead, kỹ sư media |
| AS-004 | Quy mô dự trù 1.000 tài khoản. | Xác định nhóm người dùng ban đầu và nhu cầu lưu trữ. | Khách hàng, BA |
| AS-005 | Có khoảng 100 người trực tuyến đồng thời trên toàn hệ thống. | Xác định nhu cầu tải dự kiến; kiểm chứng khả năng đáp ứng bằng kiểm thử tải. | Khách hàng, Tech Lead, QA |
| AS-006 | Dự trù ban đầu 20 người gọi đồng thời; ngày 2026-10-04 thành giới hạn v1 theo DEC-079. | Đã chốt quy tắc 10/phòng, 20/toàn hệ thống, 2 share/phòng; khả năng đáp ứng vẫn cần kiểm thử. | Khách hàng, kỹ sư media, QA |
| AS-007 | Hạ tầng và công cụ cần khoảng 20.000.000 VNĐ trong thời gian dự án. | Lập mô hình chi phí theo cấu hình, mức sử dụng và báo giá. | Phụ trách vận hành |
| AS-008 | Phạm vi có thể hoàn thành trong khoảng 3 tháng. | Ước lượng công việc, phụ thuộc và lịch nguồn lực sau phân tích, thiết kế. | Quản lý dự án, nhóm kỹ thuật |

Khả năng tiết kiệm từ tái sử dụng mã nguồn được tính vào dự toán sau khi có
kết quả đánh giá kỹ thuật.

### Các khoản chưa bao gồm

- Marketing và thu hút người dùng.
- Thuế và phí phát sinh theo điều kiện hợp đồng.
- Vận hành, hỗ trợ và bảo trì dài hạn sau thời gian dự án.
- Tính năng và nền tảng ngoài phạm vi bản hoàn thiện v1.

### Rủi ro

| Mã | Rủi ro | Ảnh hưởng | Biện pháp xử lý | Đầu mối dự kiến |
|---|---|---|---|---|
| RSK-001 | Phòng thoại trong server và gọi riêng hai người có video/chia sẻ màn hình; yêu cầu chất lượng chưa đầy đủ. | Tăng công sức tích hợp, kiểm thử và kéo dài tiến độ. | Làm rõ ma trận chức năng, giới hạn và điều kiện nghiệm thu trước khi ước lượng chi tiết. | Tech Lead, QA |
| RSK-002 | Giới hạn phòng/media đã chốt DEC-079; lượng sử dụng, bitrate thực tế và giá hạ tầng còn thiếu. | Chi phí hạ tầng vượt dự trù. | Tính chi phí theo thời lượng, dữ liệu truyền tải và theo dõi mức sử dụng. | Phụ trách vận hành |
| RSK-003 | Chuyển source một host của MVP sang microservice ở v1 theo DEC-116 có thể vượt ước lượng. | Tăng công sức triển khai, kiểm thử tích hợp và vận hành. | Chốt ranh giới/dữ liệu và cách thay transaction/guard; dành giai đoạn V1-0 riêng, chia phần hiện thực cho cả ba và giữ hồi quy MVP. | Vg chủ trì; Sáng/Thái phối hợp |
| RSK-004 | Gói fullstack, review/hướng dẫn và công cụ/provider có công sức khác nhau; tỷ lệ kế hoạch chưa chứng minh tải thực tế. | Một người quá tải hoặc chờ việc, tiến độ bàn giao bị lệch. | Theo dõi ngày công/kết quả/chờ việc hằng tuần, tối đa một gói chính, giữ 20% dự phòng và điều chuyển đầu ra rõ; khóa cấu hình nghiệm thu/người trực trước phát hành v1. | Vg, Sáng, Thái |
| RSK-005 | Thời gian học, hướng dẫn, sửa lại và rà soát của Thái chưa được lượng hóa. | Giảm thời gian dành cho các công việc khác và tăng độ bất định của tiến độ. | Giao gói có contract/AC rõ, tính học/review trong công suất người làm/người hướng dẫn, đánh giá bằng đầu ra chạy được và giải thích kết quả. | Vg, Sáng, Thái |

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
| SCP-003 Cộng đồng | [đặc tả](features/community/README.md#requirements), [5 thành phần và UC](features/community/design/README.md#organization), [thiết kế tích hợp](features/community/design/integration.md#contracts) | Luồng DEC-072–077/087 và search/tên/phạm vi/private switch/issuer DEC-093–097 đã chốt; có OpenAPI/realtime/transaction/migration, còn review/mock/proof (OQ-003/OQ-008). | Có thiết kế chi tiết; chưa triển khai/kiểm chứng |
| SCP-004 Phân quyền | [ma trận](features/community/specs/permissions.md#permissions), [thiết kế Permissions](features/community/design/permissions.md#detailed-design) | @everyone/20 custom role/union DEC-092 và quản lý cần view DEC-098 đã chốt; có role/ACL API, epoch/accessVersion/guard/fixture; còn review/migration/đo thu hồi (OQ-004/OQ-007). | Có thiết kế chi tiết; chưa triển khai/kiểm chứng |
| SCP-005 Nhắn tin | [DM](features/direct-messaging.md#requirements), [cộng đồng](features/community/README.md#requirements), [hợp đồng DM](features/direct-messaging.md#contracts), [vòng đời](data-lifecycle.md) | DEC-068–071/081/090/091 chốt nội dung/tìm/transport/draft; DEC-103–109 chốt phạm vi account/retention/restore. Có HMAC/SQL/Hub/cursor-resume/placeholder/fixture; còn review/migration/shared guard, kho sổ/worker và proof (OQ-005/008/011). | Đã chi tiết hóa; còn rà soát và thử nghiệm |
| SCP-006 Thoại | [Đặc tả](features/voice-video.md#requirements), [thiết kế media](features/voice-video.md#detailed-design) | DEC-078–085/099–102 chốt điều kiện, 10/20/2, ring/reconnect, desktop/chất lượng/cutoff/multi-device/thiết bị. Có OpenAPI 16 công khai +3 nội bộ, realtime/fixture/coordinator/lease; còn review và proof SFU/migration (OQ-006/007/008). | Có thiết kế chi tiết; chưa triển khai/kiểm chứng |
| SCP-007 Video/chia sẻ màn hình | [Thiết kế media](features/voice-video.md#detailed-design) | DEC-079/101/102 chốt nguồn/người, 2 share/phòng, ban đầu tắt, screen chỉ hình. Có source permit/quota gate/layout/AC/TC; SDK/extension/build và capture matrix thực tế còn cần review/proof. | Có thiết kế chi tiết; chưa tích hợp/kiểm chứng |
| SCP-008 Phát hành | [nghiệm thu/gate](release-operations.md#release-gates), [runbook](operations-runbook.md), [mẫu hồ sơ](templates/release-record.md) | DEC-110 chốt lỗi tồn, DEC-112 chốt quản trị kỹ thuật/no admin UI; người duyệt DEC-111 chưa chọn. Còn người trực/topology/tool và bằng chứng RLS-GAP-01–08. | Đã soạn gate/smoke/runbook/mẫu; chưa triển khai/diễn tập |

Đầu ra chuẩn bị và điều kiện còn thiếu theo gói tài khoản/DM được theo
dõi tại [SCDC-READY-001](#readiness).
Repo có bộ test tự động cho Identity/response; lần chỉnh docs này chưa chạy hoặc nghiệm thu sản phẩm. Không đánh dấu yêu cầu đạt chỉ vì có test.

### Tiến độ hoàn thiện tài liệu ngày 2026-10-04

Bảng này đánh giá nội dung đã viết và quyết định còn cần, không đánh giá phần mềm đã hoàn thành. “Có bản thiết kế” chưa có nghĩa đã chạy thử hoặc được nghiệm thu.

| Phần | Nội dung đã hoàn thiện trong đợt này | Còn cần để khóa tài liệu |
|---|---|---|
| Mục tiêu/phạm vi | REQ/SCP/SUC, luồng ưu tiên và ranh giới v1 nhất quán | Nhóm sử dụng đầu tiên/ngôn ngữ và cách đánh giá nội bộ đang được hỏi |
| Tài khoản | Quy tắc, UX, [12 use case và đối chiếu source/test](features/accounts.md#use-cases), AC/TC; hành vi mật khẩu trùng DEC-113; thiết kế chi tiết token/consume/EmailDelivery, recovery OpenAPI 4 thao tác | Limiter bổ sung hoãn DEC-089; provider/key store và kiểm chứng ACC-GAP/API/UI thuộc triển khai |
| DM | Quy tắc/UX, OpenAPI 7 thao tác, schema SignalR, HMAC/mapping SQL/guard/cursor-resume; validation/draft và fixture; account lock/restore placeholder DEC-104/108 | Review/mock, migration/projection/sổ bảo vệ và proof concurrency/thu hồi/restore khi triển khai |
| Cộng đồng/quyền | Quy tắc DEC-092–098; role/ACL/epoch, OpenAPI 45 thao tác, realtime 9 loại, fingerprint/fixture, AC/TC; room retention/restore DEC-105/108 | Review/mock, migration/guards và proof concurrency/thu hồi/restore; metadata nghiệp vụ khác chưa đặt TTL riêng |
| Vòng đời dữ liệu | Policy/matrix DEC-103–109, schema sổ bảo vệ, placeholder/availability, 49 vector +10 schema cases +6 kịch bản, AC/TC và DATA-GAP-01–06 | Review/migration/worker, kho sổ/key/checkpoint và proof hai kho/restore; scope quyền/thẩm quyền vận hành còn mở |
| Media | Quy tắc DEC-099–102, OpenAPI 19 thao tác, realtime 8 loại, room/call/participation/capacity/source/lease, 30 vector +8 transition +4 HMAC, AC/TC và MEDIA-GAP-01–07; TTL terminal DEC-107 | Review/migration, chọn/pin SDK/server và đánh giá extension/fork, proof quota/admission/cutoff/load/cleanup; host/key store và vận hành còn mở |
| Kế hoạch/dự toán | Ngày đầu 05/10/2026, công suất 3–4 ngày/tuần; phân công/giai đoạn theo DEC-117, 80% phân bổ + 20% dự phòng; giữ ngân sách tượng trưng 250 triệu từ phương án cũ | Ước lượng lịch MVP/v1 theo gói; đo ngày công/lịch nghỉ và kết quả thực tế để cân lại tải |
| Nghiệm thu/vận hành | Quy trình/lỗi tồn DEC-110, 10 gate/12 smoke; [runbook](operations-runbook.md) 8 tình huống, lịch/bàn giao; mẫu phát hành/sự cố; quản trị kỹ thuật DEC-112 | Người duyệt chưa chọn DEC-111; người trực/thay thế/lịch, provider/domain/key store/tool và lệnh production còn mở; kết quả diễn tập thuộc triển khai |

Các đầu vào sản phẩm còn mở: lịch MVP/v1, đối tượng/ngôn ngữ, người duyệt và phân công/lịch trực vận hành. Phân công phát triển theo DEC-117; phạm vi quản trị kỹ thuật/no admin UI theo DEC-112. Các gói technical evidence/PREP/RLS-GAP vẫn cần thực thi trong kế hoạch phát triển, không phải ca đã chạy trong lần hoàn thiện tài liệu.

<a id="documentation-remaining"></a>

### Phần tài liệu còn cần hoàn thiện

Rà soát ngày 2026-10-04; cập nhật mốc kiến trúc và phân công ngày 2026-10-06 theo DEC-116/117. Các mục dưới đây tách công việc viết/rà soát thiết kế khỏi quyết định sản phẩm và bằng chứng cần tạo khi triển khai. DEC-089 hoãn lựa chọn limiter, DEC-111 giữ người duyệt chưa được chọn; quy tắc nghiệp vụ đã chốt được giữ.

| Nhóm | Nội dung tài liệu còn thiếu hoặc chưa khóa | Việc có thể tiếp tục soạn | Quyết định/đầu vào còn cần |
|---|---|---|---|
| Nhu cầu và giá trị sản phẩm | Người dùng đầu tiên, ngôn ngữ/khu vực, kết quả mong muốn và cách đánh giá nội bộ | Kịch bản đánh giá theo hành trình DM và tổ chức phòng; ghi rõ giả định chưa khảo sát | Chọn nhóm sử dụng/ngôn ngữ và cách đánh giá; OQ-001/OQ-012 |
| UX/UI | Có wireframe văn bản; chưa có hồ sơ tương tác/prototype được rà soát, tiêu chí tiếp cận và cấu hình thiết bị cụ thể | Chi tiết trạng thái màn hình, thao tác bàn phím/focus, lỗi/mất mạng/mất quyền, bố cục cuộc gọi và checklist rà soát | Nhóm dùng/ngôn ngữ; ghi browser/OS/thiết bị thực tế khi có build, giữ ma trận DEC-082 |
| Tài khoản | Có thiết kế chi tiết/request-response/consume/schema email; provider/key store và limiter còn mở | Rà soát contract recovery + schema migration/policy/delivery; ACC-GAP theo gói triển khai | Ngưỡng limiter được hoãn DEC-089; email provider/domain/key store; OQ-002/OQ-008 |
| DM | Validation/bản nháp đã chốt; HMAC/SQL/Hub/cursor-resume và fixture đã chi tiết hóa, chưa có mock/proof | Review đồng bộ OpenAPI/schema và UI, migration legacy/transaction guard; mock/proof thuộc triển khai | DEC-103/104/108 chốt no self-delete/account lock/restore placeholder; projection/guard/schema và DATA-GAP proof còn triển khai |
| Cộng đồng và quyền | Có thiết kế chi tiết role/ACL/epoch, OpenAPI/realtime/fixture và mapping migration; chưa có review/mock/proof | Rà soát contract/UX, migration Unicode và guards/outbox/key ring; bằng chứng transaction/thu hồi thuộc triển khai | Vai trò/search/tên/private switch/issuer/quản lý phòng đã chốt DEC-092–098; limiter bổ sung còn mở, không xóa server v1; retention chính DEC-105–109 đã chốt, còn proof/migration |
| Media | Có thiết kế chi tiết/OpenAPI/realtime/fixture/coordinator/lease/quota gate; chưa có implementation hoặc proof | Review schema/UX/fingerprint, migration/shared guards; thử quota gate và fail-close trên build LiveKit tự host được pin | Quy tắc bổ sung đã chốt DEC-099–102; limiter media, host/domain/key store, extension/server/SDK và kho sổ/worker còn OQ-006/007/008/010/011; retention DEC-106/107 đã chốt |
| Kế hoạch và dự toán | Có phân công/công suất theo giai đoạn tại DEC-117, 20% dự phòng; ngân sách tượng trưng 250 triệu giữ cơ sở cũ, lịch chưa được chọn | Ước lượng từng gói và lịch MVP/v1; dùng ngày công/kết quả thực tế để cân lại tải | Nhóm rà soát ngày công/lịch nghỉ, thử nghiệm service/media và cập nhật dự toán; OQ-009 |
| Nghiệm thu và vận hành | Có [quy trình/10 gate/12 smoke](release-operations.md), [runbook 8 tình huống](operations-runbook.md), [mẫu phát hành](templates/release-record.md)/[sự cố](templates/incident-record.md); DEC-110/112 chốt lỗi tồn/quản trị | Review và bổ sung manifest/lệnh thật khi chọn công cụ, khóa ma trận đo/thiết bị, diễn tập và điền hồ sơ theo RLS-GAP-01–08 | Người duyệt chưa chọn DEC-111; người trực/thay thế/lịch, provider/domain/key store/tool; OQ-007/008/010/011 |
| Vòng đời dữ liệu | Có [policy/matrix/thiết kế](data-lifecycle.md), DEC-103–109, schema sổ bảo vệ, contentState/availability, fixture và AC-DATA/TC-DATA | Review/migration/guard/worker, chọn kho sổ/key/tool và proof hai kho/restore/retention | Phạm vi/TTL/restore đã chốt; người vận hành/thẩm quyền và công cụ còn OQ-008/011 |

**Bằng chứng thuộc gói triển khai:** mock/prototype chạy được, kiểm thử ACC-GAP, lưu bền/chống trùng/đồng thời/reconnect/thu hồi chat, admission LiveKit, tải/chất lượng media và diễn tập restore/rollback. Cần kế hoạch và mẫu ghi nhận trong docs; kết quả chỉ điền sau khi thực thi trên build/môi trường cụ thể. Không dùng việc chưa chạy các ca này để kết luận mọi quy tắc nghiệp vụ đều chưa được chốt.

Tài khoản/DM, cộng đồng/quyền, media và vòng đời dữ liệu đã có bản thiết kế chi tiết; nghiệm thu/vận hành đã có checklist, runbook và mẫu hồ sơ. Các bản này còn review và đầu vào ở bảng trên. Phần có thể tiếp tục soạn là UX/UI; nhu cầu/ngôn ngữ, lịch và tên người vận hành/người duyệt được cập nhật khi người dùng chọn. Không ghi các đề xuất phân công hoặc kết quả diễn tập là đã xác nhận.

<a id="delivery"></a>

## 8. Kế hoạch bàn giao và ước lượng

Theo [DEC-116](decisions.md#dec-116), MVP dùng một host Modular Monolith để nhóm bắt đầu làm; chuyển microservice thuộc v1. Phân công theo DEC-117 và [nhân sự](#team). Mục tiêu ba tháng, mô hình 16 tuần/250 triệu là cơ sở minh họa cũ; chưa có lịch riêng MVP/v1 hoặc số liệu năng suất để cam kết. Không bắt đầu lại toàn bộ kế hoạch sau MVP.

### Đợt bàn giao và điều kiện chuyển tiếp

| Đợt | Đầu ra | Vg | Sáng | Thái | Điều kiện chuyển |
|---|---|---|---|---|---|
| MVP-0. Chốt gói đầu | Luồng Identity đủ dùng, hợp đồng module/job email, gói triển khai và môi trường thử | Hoàn thiện phần Identity trực tiếp cần; chốt quyền và hợp đồng | Chốt gói DM/nền tin và ca thử | Dựng Compose/CI/dataset hiện có; bắt đầu worker theo job contract | Phụ thuộc trực tiếp đủ rõ; không chờ toàn bộ media/kiến trúc v1 |
| MVP-1. Làm song song | Community, DM, email và bộ chạy hoạt động từ DB tới UI | Community trọn luồng | DM trọn luồng; nền tin phòng | Email Worker và bộ chạy/kiểm tra hệ thống | Ba tính năng nền tích hợp thật, quyền và dữ liệu được kiểm chứng |
| MVP-2. Bàn giao | Chạy lại trên bản checkout/dataset được ghi rõ, lỗi chính được sửa | Review quyền/Identity/Community và điều phối | Sửa/kiểm tra Messaging và luồng liên thông | Smoke/hồi quy, dữ liệu demo và hồ sơ build/kết quả | Đạt [điều kiện MVP](releases/mvp.md#acceptance); chưa yêu cầu microservice/media |
| V1-0. Chuyển microservice | Ranh giới được chốt, API/dữ liệu/triển khai độc lập, sửa các giả định transaction xuyên service | Thiết kế ranh giới/chính sách; chuyển Identity và phần Community liên quan | Chuyển Messaging, hợp đồng/dữ liệu và kiểm thử tích hợp | Compose nhiều service, cấu hình/định tuyến/CI và bộ chạy hồi quy | Đạt gói kiến trúc v1; luồng MVP tiếp tục hoạt động |
| V1-1. Hoàn thiện và media | Đủ Community/Messaging; gọi riêng/phòng thoại, camera và share theo scope | Community/Identity còn lại; quyền/admission/quota/thu hồi media | Nhắn tin còn lại; lifecycle và UI điều khiển cuộc gọi | Provider/SDK, thiết bị/nguồn media; worker và công cụ chạy | Các gói theo [v1](releases/v1.md) có bằng chứng; chức năng lớn không xếp chồng ngoài công suất |
| V1-2. Ổn định/phát hành | Kiểm thử đầy đủ, dữ liệu, rollback/restore và bàn giao | Review phần rủi ro cao, sửa gói sở hữu | Kiểm thử đồng thời/chất lượng và sửa gói sở hữu | Bộ chạy, diễn tập theo runbook, tổng hợp evidence và sửa gói sở hữu | Đạt gate v1; người duyệt/người vận hành còn phải được chọn theo DEC-111/OQ-011 |

V1-1 có nhiều gói; hoàn thiện nhắn tin/Community và làm media được xếp theo phụ thuộc và công suất, không mặc định cùng lúc. Gói nào chưa đủ kỹ thuật/quyền/provider thì người đó tiếp tục một gói độc lập trong scope; không thêm tính năng ngoài scope chỉ để lấp thời gian.

<a id="capacity"></a>

### Phân bổ công suất để cân tải

Tỷ lệ dưới đây là baseline lập kế hoạch, không phải đo năng lực hoặc ước lượng thời gian hoàn thành. Mỗi hàng là quỹ của **một người trong một giai đoạn**, tổng 100%; các giai đoạn không cộng cùng một tuần. Với 3–4 ngày/tuần, 80% công việc tương ứng 2,4–3,2 ngày và 20% dự phòng là 0,6–0,8 ngày. Baseline 3,5 ngày là 2,8 ngày công việc + 0,7 ngày dự phòng.

| Giai đoạn | Người | Công việc chính | Phần phối hợp đã tính trong quỹ | Dự phòng |
|---|---|---|---|---|
| MVP | Vg | 50% Community + 15% Identity còn lại | 15% quyết định/hợp đồng/review/hướng dẫn | 20% |
| MVP | Sáng | 55% DM + 15% nền tin phòng dùng chung | 10% kiểm tra tích hợp và review gói liên quan | 20% |
| MVP | Thái | 40% Email Worker + 25% môi trường/CI | 15% dataset/smoke, học và tổng hợp kết quả | 20% |
| V1-0 — chuyển service | Vg | 60% thiết kế/chuyển Identity/Community và chính sách xuyên service | 20% review/điều phối/hướng dẫn | 20% |
| V1-0 — chuyển service | Sáng | 60% chuyển Messaging, hợp đồng và dữ liệu | 20% hồi quy/kiểm tra tích hợp | 20% |
| V1-0 — chuyển service | Thái | 60% môi trường nhiều service/định tuyến/CI | 20% bộ chạy hồi quy, dataset và học | 20% |
| V1-1 — hoàn thiện/media | Vg | 45% Community/Identity còn lại + 15% chính sách/admission/quota/thu hồi media | 20% review/điều phối/hướng dẫn | 20% |
| V1-1 — hoàn thiện/media | Sáng | 50% lifecycle/UI điều khiển cuộc gọi + 20% nhắn tin còn lại | 10% kiểm tra liên thông/đồng thời | 20% |
| V1-1 — hoàn thiện/media | Thái | 50% provider/SDK/thiết bị/hiển thị nguồn + 25% worker/công cụ vận hành | 5% tổng hợp smoke/chẩn đoán; tự kiểm thử/học tính trong gói chính | 20% |
| V1-2 — ổn định | Mỗi người | 60% kiểm chứng/sửa lỗi phần sở hữu | 20% kiểm tra chéo, hồ sơ và diễn tập được phân công | 20% |

Mỗi người giữ tối đa một gói chính đang thực hiện và một gói hỗ trợ nhỏ. Khi Vg đang chốt chuyển service, không đồng thời cam kết phát triển toàn bộ Community/media; khi Sáng làm lifecycle cuộc gọi, phần nhắn tin còn lại được xếp theo quỹ 20%, không coi là một việc toàn thời gian thứ hai. Công cụ của Thái bắt đầu ở MVP và tiếp tục mở rộng ở v1, không chờ hai người khác làm xong mới có việc.

### Điều chỉnh theo kết quả thực tế

- Cuối mỗi tuần làm việc, ghi ngày công đã dùng, gói chạy được, lỗi/việc phải làm lại, thời gian review/hướng dẫn và phần bị chặn.
- Nếu gói được ước lượng vượt quỹ 80%, giảm phần xếp trong kỳ, kéo lịch hoặc chuyển một đầu ra triển khai đã có thiết kế và cách thử rõ cho người còn công suất. Vg giữ quyết định nghiệp vụ/kiến trúc, không phải tự code mọi phần.
- Khi Vg quá tải Community hoặc Thái hoàn tất worker/bộ chạy sớm, ưu tiên chuyển cho Thái một gói từ DB tới UI đã chốt contract/AC, chẳng hạn tạo phòng text trong [UC-COM-16](features/community/specs/channels.md#uc-com-16). Vg giữ chính sách/quyền và review; chỉ giao Sáng khi gói DM/lifecycle hiện tại đã bàn giao. Gói chuyển chủ thay thế một phần quỹ công việc hiện có, không cộng thêm nhiệm vụ hoặc tự mở rộng scope MVP.
- Nếu người hoàn thành sớm, lấy gói kế tiếp trong scope của mình; nếu chuyển gói từ người khác, ghi người sở hữu mới, phụ thuộc, người review và phần việc cũ được giảm tương ứng.
- Nếu review/hướng dẫn hoặc thử nghiệm media vượt phần đã dành, cập nhật phân bổ/lịch trước khi nhận thêm gói. Dùng dữ liệu sau 1–2 tuần để hiệu chỉnh baseline; chưa tuyên bố công việc đã cân bằng chỉ từ các tỷ lệ.

### Ước lượng và khóa lịch/chi phí

Ước lượng **ngày công còn lại** cho từng gói, gồm thiết kế, code, học/hướng dẫn, review, test, tích hợp và sửa lỗi. Identity gần xong nên chỉ tính phần còn lại. AI là công cụ của cả nhóm; không tự gán hệ số tăng năng suất hay coi viết code xong là gói đã hoàn tất.

Phương án 16 tuần cũ chỉ giữ làm cơ sở ngân sách minh họa ở [dự toán](#budget); bảng ngày công theo vai trò frontend/backend cũ không tiếp tục dùng để phân công sau DEC-117. Lịch mới cần kết quả MVP, thử nghiệm chuyển service/media, ngày nghỉ và công suất thực tế. Chi phí nhân sự/hạ tầng vẫn thay theo đơn giá và cấu hình được xác nhận; không dùng ngân sách tượng trưng làm bằng chứng đủ công sức.

Microservice là gói v1 theo DEC-116. Ranh giới cụ thể, Gateway/broker và công cụ triển khai còn cần chọn; chúng không cản trở gói MVP đang chạy trên một host.

<a id="readiness"></a>

## 9. Sẵn sàng phát triển và gói chuẩn bị

Trạng thái của bảng này là mức sẵn sàng đặc tả và bằng chứng bàn giao. Identity đã có implementation; Messaging/Community mới có nền module. Bộ ca TC trong docs chưa có kết quả chạy được ghi nhận. Các test tự động có trong repo được liệt kê ở [hướng dẫn phát triển](development.md#testing).

### Bảng điều kiện sẵn sàng

| Mã | Điều kiện/đầu ra | Tình trạng và bằng chứng | Việc còn lại/đầu mối dự kiến |
|---|---|---|---|
| READY-01 | Tài khoản tối thiểu | Đã chốt ACC-P01–05 (DEC-063–068) và mật khẩu trùng DEC-113; có thiết kế token/email/consume, OpenAPI recovery 4 thao tác, AC-ACC-01–22 và [12 use case/coverage source-test](features/accounts.md#use-case-coverage); chưa chạy kiểm thử trong bước tài liệu | Vg hoàn thiện Identity/job contract; Thái làm delivery/provider theo contract và ghi proof; Sáng kiểm tra phần DM sử dụng. Limiter bổ sung hoãn DEC-089 |
| READY-02 | DM và ngoại lệ | Đã chốt 2.000 UTF-16, tìm người, không tự hết hạn, cách gửi, validation và bản nháp (DEC-068–071/090/091); có AC-DM và ngoại lệ tại [đặc tả](features/direct-messaging.md#requirements) | Validation/bản nháp đã chốt DEC-090/091; có Unicode fixture, HMAC/mapping SQL/Hub/resume; còn review/mock, kiểm chứng đồng thời và no self-delete/account lock/restore đã chốt DEC-103/104/108, còn DATA-GAP proof; Vg/Sáng, Thái đối chiếu |
| READY-03 | Ma trận quyền và thiết kế Community | [5 thành phần và 25 UC](features/community/design/README.md#organization), [Permissions](features/community/design/permissions.md#detailed-design) và [tích hợp](features/community/design/integration.md#contracts) có role/ACL/epoch/45 REST/9 realtime/fixture, AC-COM-01–42; [gói đầu tiên](features/community/delivery/README.md#use-case-delivery) đã xác định | [Tiến độ Community](features/community/status.md) quản lý implementation/bằng chứng theo gói; thu hồi/phòng/role và các nhánh còn lại cần proof. Media deadline DEC-099 và retention chính DEC-105–109 đã chốt, còn worker/restore proof; DM vẫn độc lập role cộng đồng |
| READY-04 | UX hai hành trình | Có wireframe văn bản [tài khoản/DM](features/direct-messaging.md#ux) và [cộng đồng](features/community/specs/integration.md#ux) | Vg làm UI Identity/Community, Sáng làm UI DM theo gói; Thái cung cấp dataset/bộ chạy. Ma trận trình duyệt đã chốt DEC-082; khóa OS/thiết bị/build và trạng thái còn mở |
| READY-05 | API/dữ liệu DM | Có [hợp đồng đề xuất](features/direct-messaging.md#contracts) với schema logic, lỗi, chống trùng, lịch sử và cập nhật | REST/SignalR, ID/cursor đã chọn DEC-081; Vg/Sáng rà soát schema/lỗi cùng Thái; Identity có OpenAPI từ code; DM có [OpenAPI dự thảo](contracts/direct-messaging.openapi.json), có schema realtime/fixture/thiết kế chi tiết; chưa có mock hoặc proof chạy được |
| READY-06 | Thử nghiệm kỹ thuật | Có kịch bản cần chứng minh tại [bằng chứng thử nghiệm](#technical-evidence); chưa chạy | MVP kiểm chứng luồng trên một host; v1 chuyển microservice theo DEC-116. Vg thiết kế/Identity/Community, Sáng Messaging, Thái môi trường/bộ chạy theo gói V1-ARCH |
| READY-07 | Kiểm thử | Có [ca kiểm thử](features/direct-messaging.md#tests) và dữ liệu dự kiến; tất cả chưa chạy | Mỗi người thử phần sở hữu, người khác kiểm tra lại; Vg/Sáng giữ kỳ vọng quyền/đồng thời, Thái dataset/bộ chạy/kết quả. Ngưỡng chat DEC-083 và media DEC-085 áp dụng theo hồ sơ mốc |
| READY-08 | Công việc/nguồn lực | Có gói chuẩn bị, [phân công](#team) và [công suất](#capacity) theo DEC-117; lịch từng mốc chưa khóa | Vg/Sáng/Thái ước lượng trong quỹ thời gian thật, tính học/hướng dẫn/review/tự kiểm thử và 20% dự phòng; đo lại sau 1–2 tuần |

Không tính phần trăm từ số dòng hoàn tất. Mỗi gói chỉ sẵn sàng khi có
yêu cầu, thiết kế, kiểm chứng và người phụ trách tương ứng. Quyết định
media, chi phí toàn dự án và vận hành vẫn phải hoàn thành ở các đợt
liên quan; không tự loại chúng khỏi bản hoàn thiện v1 để đánh dấu Đợt 0 xong.

<a id="preparation"></a>

### Gói việc đủ cụ thể để ước lượng

| Gói | Đầu ra | Phụ thuộc | Thực hiện / rà soát dự kiến | Điều kiện hoàn tất |
|---|---|---|---|---|
| PREP-01 | Hoàn thiện thiết kế tài khoản/job email và fixture cho luồng đã chọn | ACC-GAP-01–07, OQ-002 | Vg giữ contract; Thái proof delivery / Sáng kiểm tra phần DM dùng | Quy tắc/token/job không còn chỗ diễn giải khác nhau; AC của luồng được cập nhật |
| PREP-02 | UI tài khoản/DM và cộng đồng theo mã màn hình | Wireframe, quyết định liên quan | Vg: Identity/Community; Sáng: DM / kiểm tra chéo; Thái dataset/bộ chạy | Có trạng thái rỗng/lỗi/mất mạng/mất quyền; ghi kết quả rà soát |
| PREP-03 | Hợp đồng API có schema máy đọc được và mock | SCDC-API-DM-001, PREP-01 | Sáng sở hữu DM; Vg review quyền / Thái dùng mock cho bộ chạy | Request/response/lỗi thống nhất; không trả dữ liệu ngoài quyền |
| PREP-04 | Thử nghiệm DM lưu bền/chống trùng/phân trang | PREP-03, lựa chọn công nghệ | Sáng / Vg review quyền/dữ liệu; Thái hỗ trợ bộ chạy | Các [kịch bản thử nghiệm](#technical-evidence) có bằng chứng theo scope; rủi ro được ghi nhận |
| PREP-05 | Thử nghiệm cập nhật, reconnect và thu hồi phiên | PREP-04, thiết kế phiên | Sáng: realtime; Vg: session/revoke / Thái bộ chạy lỗi mạng | Không trùng/sót dữ liệu; kết nối bị thu hồi đúng ngưỡng áp dụng |
| PREP-06 | Ma trận thiết bị và fixture kiểm thử | DEC-059, AC và ca kiểm thử | Thái: matrix/dataset/bộ chạy; Vg/Sáng: kỳ vọng và thực thi phần sở hữu | Danh sách trình duyệt/phiên bản, kích thước, dữ liệu và kết quả mong đợi rõ |
| PREP-07 | Ước lượng và lịch đợt tài khoản/DM | PREP-01–06 đủ rõ | Vg, Sáng, Thái | Ngày công, người làm/rà soát, thời gian hướng dẫn và phụ thuộc không trùng quỹ thời gian |

Đây là phân công kế hoạch theo DEC-117; không ghi các gói đã hoàn tất hoặc lịch đã được cam kết. PREP nằm trong quỹ của gói tính năng/công cụ tương ứng, không cộng thành việc toàn thời gian bổ sung. Chọn PREP theo luồng MVP trước; phần hoàn thiện Community, chuyển service và media tiếp tục ở v1.

<a id="technical-evidence"></a>

### Bằng chứng thử nghiệm kỹ thuật cần có

Đây là mục tiêu của bản đầy đủ v1; MVP chọn bằng chứng theo luồng tại [hồ sơ MVP](releases/mvp.md#acceptance). Thiết kế transaction/row lock xuyên Identity–Messaging trong một host phải được rà soát lại khi chuyển service ở v1 theo DEC-116; không coi việc đổi tên mốc là đã chuyển thiết kế monolith thành microservice.

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

Ngày 2026-10-04 đã bổ sung thiết kế chi tiết Accounts/DM, Community/quyền/tin phòng, Media/LiveKit tự host, [vòng đời dữ liệu](data-lifecycle.md) và [nghiệm thu/vận hành](release-operations.md) với gate/smoke/runbook/mẫu hồ sơ. Ngày 2026-10-05 bổ sung use case Identity và DEC-113 về mật khẩu trùng. Ngày 2026-10-06 tách MVP/v1, chốt MVP một host/microservice ở v1 và cập nhật phân công/công suất theo DEC-114/116/117; chưa sửa mã hoặc chạy kiểm thử sản phẩm. Limiter tài khoản hoãn DEC-089, người duyệt chưa chọn DEC-111. Các bản thiết kế còn cần rà soát; provider/key store/kho sổ, lịch và bằng chứng triển khai ở [phần còn cần hoàn thiện](#documentation-remaining). UX/UI tiếp tục theo chủ gói. Chỉ xác nhận gói phát triển khi các phụ thuộc trực tiếp được giải quyết; không cần chờ toàn bộ media để rà soát Accounts/DM/Community.

Các nội dung còn mở được tập trung tại OQ-002–OQ-011; bảng này dẫn chiếu
và theo dõi đầu ra, không tạo một nguồn quyết định sản phẩm khác.

<a id="process"></a>

## 10. Quy trình và quản lý thay đổi

Khung vòng đời và trách nhiệm dưới đây là đề xuất hợp nhất từ SCDC-PRC-001; cần xác nhận cách áp dụng theo từng gói bàn giao. Quy ước duy trì nguồn docs và lưu lịch sử được áp dụng trong lần tổ chức lại này.


### Phạm vi áp dụng

Quy trình xác định trách nhiệm, đầu ra và điều kiện bàn giao từ tiếp nhận
nhu cầu đến phát hành, vận hành và cải tiến SCDC. Phạm vi sản phẩm được quản
lý tại [Project Brief](#scope).

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
| 5. Thiết kế kỹ thuật | Vg chủ trì; Sáng/Thái theo gói, vận hành khi được chọn | Ranh giới module cho MVP một host; chuyển microservice, dữ liệu/hợp đồng mạng và phương án vận hành ở v1 theo DEC-116/117. | Giải pháp được rà soát về khả thi, tích hợp, hiệu năng và chi phí. |
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
