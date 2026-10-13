# Messaging

Messaging phụ trách DM và tin trong phòng Community. Identity cung cấp danh tính/phiên; Community cung cấp channel, membership và quyền phòng. Một nền lưu/đồng bộ tin phục vụ cả hai ngữ cảnh; DM không dùng role/ACL cộng đồng.

| Chủ đề | Nội dung |
|---|---|
| [Nhắn tin riêng](direct-messaging.md) | Tìm người, hội thoại hai người, quyền DM, UI, API và AC/TC |
| [Tin phòng](channel-messaging.md) | UC-COM-23/24/25, quyền phòng, API/Hub và kiểm chứng tích hợp |
| [Lưu và thay đổi tin](message-lifecycle.md) | Tiếp nhận tin, chuyển sang hàng đợi, transaction, chống trùng, sửa/xóa và state client |
| [Lịch sử và đồng bộ](synchronization.md) | Cursor/history, outbox, Hub, reconnect và thu hồi |
| [Nội dung văn bản](text-policy.md) | Unicode, UTF-16, pipeline validation và TC-TEXT |

## Trạng thái và cách dùng

Các chủ đề giữ quy tắc, UI, kỹ thuật và cách kiểm chứng liên quan cùng chỗ. Phạm vi sản phẩm tại [tổng quan v1](../../project/overview.md#scope); [MVP](../../releases/mvp.md) ghi nền nội bộ được kế thừa. Đối chiếu mã nền `900e226`: đã có tìm người, mở DM, inbox, gửi/lịch sử, retry chủ động, sửa/xóa, ChatHub/outbox và reconnect; nền vẫn là một API host Modular Monolith. Lifecycle space đã có, writer/realtime tin phòng và cơ chế xuyên service v1 còn là phần việc tiếp theo.

Kết quả được giữ theo revision và phạm vi tại [hồ sơ DM](../../records/verification/messaging/README.md), [gói DM-MVP](../../records/verification/messaging/DM-MVP.md) và [kiểm tra hợp nhất ngày 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md). Các hồ sơ có bằng chứng agent và xác nhận người dùng của từng gói; không suy toàn bộ hợp đồng/AC hoặc v1 đã nghiệm thu. Lần cập nhật hiện trạng này không chạy lại test ứng dụng.

Sáng phụ trách nền Messaging và UI DM; Vg giữ quyền/Community và review tích hợp; Thái cung cấp bộ chạy/dataset theo [phân công](../../project/planning.md#team). Chuẩn dữ liệu/API, trạng thái dự thảo và phụ thuộc trực tiếp được ghi ở từng chủ đề. Giữ mã DM/ACL/UC/AC/TC/DEC; schema hoặc fixture không thay bằng chứng chạy trên build cụ thể.

<a id="detailed-design"></a>

## Hợp đồng chung

REST xử lý mutation, SignalR `/hubs/chat` nhận cập nhật theo DEC-081; lỗi theo [quy ước API](../../system/api-conventions.md#contracts). Tài liệu [vòng đời tin](message-lifecycle.md), [đồng bộ](synchronization.md) và [nội dung](text-policy.md) là nguồn cơ chế chung; [Community](../../system/community.md) giữ điều kiện nền và ownership dữ liệu tích hợp. Transaction/guard của một host MVP phải được rà soát lại khi chuyển microservice ở v1.
