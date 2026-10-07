# SCDC — Mục tiêu, yêu cầu và phạm vi

Nguồn chuẩn cho nhu cầu, thuật ngữ và mã REQ/SCP/SUC. Phạm vi bàn giao theo mốc nằm trong [MVP](../releases/mvp.md) và [v1](../releases/v1.md).

| Cần tìm | Đọc |
|---|---|
| Nhân sự, công suất, ngân sách và lịch | [Kế hoạch](planning.md) |
| Độ phủ đặc tả và đầu vào còn thiếu | [Sẵn sàng phát triển](readiness.md) |
| Trách nhiệm, đầu ra và quản lý thay đổi | [Quy trình](process.md) |

Phạm vi sản phẩm đã được xác định ở mức tổng thể. Đặc tả và bằng chứng còn thiếu được theo dõi tại [readiness](readiness.md); có code trong repo chưa đồng nghĩa đã nghiệm thu hoặc sẵn sàng phát hành.

<a id="needs"></a>

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
[SCDC-FR-DM-001](../features/direct-messaging/specs/requirements.md#requirements).

Đợt hiện tại không khảo sát người dùng bên ngoài theo
[DEC-030](../decisions.md#decisions). Đầu vào từ đại diện sản phẩm
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
[sổ vấn đề cần làm rõ](../decisions.md#decisions).


Đầu vào hiện tại là từ đại diện sản phẩm theo DEC-030. Chưa xác định nhóm người dùng đầu tiên, ngôn ngữ/khu vực và thước đo giá trị tại OQ-001/OQ-012. DM là nền chức năng ưu tiên; giá trị tách nhiều chủ đề cần tiếp tục kiểm chứng ở cộng đồng. Bộ câu hỏi phỏng vấn cũ được lưu dự phòng trong archive, không phải hoạt động khảo sát đã thực hiện.

<a id="requirements"></a>

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

## Hai mốc bàn giao, phạm vi v1 và ràng buộc

Theo [DEC-114](../decisions.md#dec-114), thêm MVP trước bản đầy đủ đã đặc tả để nhóm dễ bắt đầu triển khai. Phạm vi đầy đủ trước đây được gọi là MVP nay mang tên **v1**; yêu cầu, mã truy vết và các quy tắc nghiệp vụ được giữ. DEC-116 chốt MVP một API host và microservice ở v1; DEC-117 cập nhật phân công kế hoạch.

| Mốc | Phạm vi đã xác nhận | Hồ sơ sử dụng |
|---|---|---|
| MVP | Identity, Community và Direct Messaging; chọn luồng tối thiểu theo từng gói, chạy trong một API host | [Phạm vi, gói việc và điều kiện MVP](../releases/mvp.md); phân công theo DEC-117, chi tiết UC/AC còn cần khóa |
| v1 | Chuyển microservice và hoàn thiện toàn bộ phạm vi ở bảng SCP bên dưới, gồm gọi riêng/phòng thoại, video và chia sẻ màn hình | [Hồ sơ v1](../releases/v1.md), đặc tả tính năng và điều kiện phát hành hiện có |

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
| Kiến trúc | MVP một API host Modular Monolith; microservice thuộc v1 theo DEC-116. Phương án ranh giới tại [kiến trúc](../architecture.md#target) còn cần chốt, chưa có chuyển đổi code. |
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

### Kết quả bàn giao

- Ứng dụng web và các dịch vụ đáp ứng phạm vi chức năng được đặc tả.
- Hồ sơ yêu cầu, thiết kế trải nghiệm và thiết kế kỹ thuật.
- Kết quả kiểm thử và hồ sơ nghiệm thu.
- Hướng dẫn triển khai, cấu hình, vận hành và bàn giao hệ thống.

Nội dung chi tiết và điều kiện chấp nhận của từng kết quả bàn giao được xác
định trong đặc tả yêu cầu và kế hoạch thực hiện.

<a id="success"></a>

## Tiêu chí thành công và chất lượng

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
| Lưu giữ và khôi phục | [Chính sách dữ liệu](../data-lifecycle.md) DEC-103–109, RPO ≤15 phút/RTO ≤4 giờ và tuổi mỗi backup/WAL ≤30 ngày đã chốt; còn công cụ, cửa sổ PITR thực tế và bằng chứng restore | OQ-011 |

1.000 tài khoản và 100 người trực tuyến là giả định dự toán, không phải giới hạn đăng ký/online. Ngày 2026-10-04, 20 người gọi đồng thời trở thành giới hạn media v1 theo DEC-079; chưa có kết quả đo. Các mốc hiệu năng trong tài liệu kỹ thuật cũ chưa có bằng chứng và chưa được xác nhận làm ngưỡng nghiệm thu. Không tuyên bố truy vấn luôn dưới 10ms.
