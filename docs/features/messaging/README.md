# Messaging

Messaging phụ trách DM và tin trong phòng Community. Identity cung cấp danh tính/phiên; Community cung cấp channel, membership và quyền phòng. Một nền lưu/đồng bộ tin phục vụ cả hai ngữ cảnh; DM không dùng role/ACL cộng đồng.

| Chủ đề | Nội dung |
|---|---|
| [Nhắn tin riêng](direct-messaging.md) | Tìm người, hội thoại hai người, quyền DM, UI, API và AC/TC |
| [Tin phòng](channel-messaging.md) | UC-COM-23/24/25, quyền phòng, API/Hub và kiểm chứng tích hợp |
| [Lưu và thay đổi tin](message-lifecycle.md) | Model, transaction, sequence, chống trùng, sửa/xóa và state client |
| [Lịch sử và đồng bộ](synchronization.md) | Cursor/history, outbox, Hub, reconnect và thu hồi |
| [Nội dung văn bản](text-policy.md) | Unicode, UTF-16, pipeline validation và TC-TEXT |

## Trạng thái và cách dùng

Các chủ đề giữ quy tắc, UI, kỹ thuật và cách kiểm chứng liên quan cùng chỗ. Phạm vi bàn giao nằm tại [MVP](../../releases/mvp.md) và [v1](../../releases/v1.md); trạng thái hiện tại và phần còn lại ghi trong từng chủ đề. Messaging đã có nền module/lifecycle space; DM API, writer tin phòng và Hub còn là thiết kế mục tiêu, chưa có hồ sơ nghiệm thu riêng được ghi nhận.

Sáng phụ trách nền Messaging và UI DM; Vg giữ quyền/Community và review tích hợp; Thái cung cấp bộ chạy/dataset theo [phân công](../../project/planning.md#team). Chuẩn dữ liệu/API, trạng thái dự thảo và phụ thuộc trực tiếp được ghi ở từng chủ đề. Giữ mã DM/ACL/UC/AC/TC/DEC; schema hoặc fixture không thay bằng chứng chạy trên build cụ thể.

<a id="detailed-design"></a>

## Hợp đồng chung

REST xử lý mutation, SignalR `/hubs/chat` nhận cập nhật theo DEC-081; lỗi theo [quy ước API](../../system/api-conventions.md#contracts). Tài liệu [vòng đời tin](message-lifecycle.md), [đồng bộ](synchronization.md) và [nội dung](text-policy.md) là nguồn cơ chế chung; [Community](../../system/community.md) giữ điều kiện nền và ownership dữ liệu tích hợp. Transaction/guard của một host MVP phải được rà soát lại khi chuyển microservice ở v1.
