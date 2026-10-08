# SCDC — Trạng thái DM

Tổ chức lại: 2026-10-07. Đây là nguồn trạng thái và khoảng trống triển khai DM; cơ chế dùng chung không tự chứng minh một tính năng đã hoàn tất.

Cập nhật: 2026-10-06. Phạm vi: REQ-005, SCP-005. Quy tắc DM, AC-DM, ACL-02–05, màn hình DM-S, UX-DM và TC-DM/TEXT.

Đặc tả đầy đủ cho [v1](../../releases/v1.md); [MVP](../../releases/mvp.md) chọn luồng nhắn tin nền để bắt đầu trong một API host. Các quy tắc áp dụng của luồng được chọn vẫn giữ; transaction/guard xuyên Identity–Messaging được rà soát khi chuyển sang [microservice ở v1](../../architecture.md#target) theo DEC-116.

Quy tắc cốt lõi đã xác nhận. UX và hợp đồng được dẫn từ [tổng quan](README.md) còn đề xuất; Messaging mới ở nền module, chưa có DM API/Hub. Các ca TC chưa có kết quả chạy được ghi nhận.

Module thực hiện DM là **Messaging**. [Cơ chế Messaging](../../shared/messaging/README.md), [thiết kế chi tiết](../../shared/messaging/README.md#detailed-design) và [ca TC-TEXT](../../shared/messaging/text.md#tests) đồng thời là nguồn chuẩn cho cơ chế xử lý tin dùng chung với tin phòng. Phần áp dụng vào cộng đồng, quyền phòng và phối hợp realtime được mô tả tại [tích hợp Community](../community/specs/integration.md#responsibilities); DM có điều kiện truy cập riêng, không dùng role/ACL cộng đồng.

<a id="gaps"></a>

## Vấn đề còn mở

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Kết quả tìm kiếm | Đã chốt độ dài, phân trang và khớp tại DEC-069; schema/cursor đã chi tiết hóa; còn kiểm chứng truy vấn và frontend/mock. | OQ-005 |
| Lưu giữ | DEC-103–109 chốt account lock/no self-delete, TTL và restore placeholder; có [chính sách chung](../../data-lifecycle.md), còn review/migration/sổ độc lập/worker/restore proof. | OQ-005/011, DATA-GAP |
| Thử lại/đồng thời | Rà soát và thử nghiệm hợp đồng chống trùng, khóa theo hội thoại, xung đột sửa/xóa và dọn dữ liệu. | OQ-005, OQ-008 |
| Giới hạn nội dung | Đã chốt 2.000 UTF-16, xuống dòng/emoji và từ chối trống; đã có bảng text-policy và fixture theo DEC-090; còn kiểm chứng client/server. | OQ-005 |
| Chất lượng | Ngưỡng và ma trận đã chốt DEC-082/083; còn cấu hình/build/thiết bị và kết quả đo. | OQ-007 |

Đã bổ sung [OpenAPI dự thảo](../../contracts/direct-messaging.openapi.json) ngày 2026-10-04; chưa xác nhận thiết kế hoặc có mock/API/Hub chạy được. Schema dùng `x-scdc-utf16-length` vì minLength/maxLength của JSON Schema không tự biểu diễn phép đếm UTF-16. `clientMessageId` UUIDv4, ID server UUIDv7 theo DEC-081; ví dụ là dữ liệu minh họa, không phải ID của dữ liệu thật. Cách biểu diễn SQL hiện tại dùng chat space/`sequence_no`; thiết kế logic dùng conversation/sequence. Cần rà soát ánh xạ, unique key theo tác giả và commit order trước triển khai, không coi seed/schema hiện tại là đã chứng minh hợp đồng đề xuất.

Các quyết định chưa chốt được giữ ở OQ-005/OQ-007/OQ-008/OQ-011. Chọn framework hoặc mô hình lưu không thay thế việc kiểm chứng lost response, concurrent commit, worker dừng và reconnect.


## Gói tìm người DM-P1-T01 — 07/10/2026

P0 đã được người dùng PASS FE/BE và đồng ý baseline; tích hợp vào `message` tại `170d959`. P1-T01 bổ sung GET `/api/v1/users/search` do Identity thực hiện và UI tìm/chọn người bằng API thật. Bằng chứng, dataset và các bước người dùng test ở [biên bản P1](../../acceptance/direct-messaging/DM-P1-T01.md); [kế hoạch](../../plans/direct-messaging.md) và [20 prompt](../../plans/direct-messaging-prompts.md) giữ quy trình dừng từng task. Search đang chờ người dùng nghiệm thu. Điều chỉnh 08/10/2026: UI cho chọn nhiều người, giữ lựa chọn khi đổi q/tải thêm/lỗi và bỏ chọn riêng hoặc tất cả. Người dùng chốt mục gần đây là người vừa nhắn tin; UI/dữ liệu thuộc P1-T03, proof thứ tự bằng tin thật thuộc P2-T01, hiện chưa triển khai. AC-DM-01 chỉ đạt lát cắt tìm/chọn; mở hội thoại, gửi/lịch sử và Hub còn thuộc task sau. Các khoảng trống phía trên tiếp tục áp dụng cho phần chưa triển khai.
