# SCDC — Nhắn tin

Người dùng có thể nhắn tin riêng với một người (DM) hoặc nhắn trong phòng của server. Cả hai dùng chung một cơ chế lưu và đồng bộ tin. DM không dùng vai trò hay quyền của cộng đồng.

**Trạng thái:** nhắn tin riêng đã có trong nền MVP (commit `900e226`). Tin phòng mới có phần nền, chưa có ghi tin và realtime.

**Người phụ trách:** Sáng (xem [phân công](../../project/planning.md#team)).

## Các trang

| Trang | Nội dung |
|---|---|
| [Nhắn tin riêng](direct-messaging.md) | Tìm người, mở cuộc trò chuyện, quyền, giao diện, API và ca kiểm thử |
| [Tin phòng](channel-messaging.md) | Nhắn trong phòng của server, quyền phòng, API và Hub realtime |
| [Lưu và thay đổi tin](message-lifecycle.md) | Nhận tin, chống gửi trùng, sửa và xóa tin, trạng thái tin phía client |
| [Lịch sử và đồng bộ](synchronization.md) | Tải lịch sử, outbox, Hub realtime, kết nối lại và thu hồi quyền |
| [Nội dung văn bản](text-policy.md) | Kiểm tra Unicode, cách đếm độ dài và ca kiểm thử TC-TEXT |

## Đã có trong nền MVP

Tìm người, mở DM, hộp thư, gửi tin, xem lịch sử, gửi lại khi lỗi, sửa và xóa tin, Hub realtime (`ChatHub`), outbox và kết nối lại. Tất cả chạy trong một ứng dụng API duy nhất.

Chưa có: ghi tin và realtime cho tin phòng, cơ chế liên service của v1.

Kết quả kiểm thử nằm ở [hồ sơ DM](../../records/verification/messaging/README.md), [gói DM-MVP](../../records/verification/messaging/DM-MVP.md) và [kiểm tra hợp nhất 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md). Mỗi hồ sơ chỉ chứng minh phạm vi của gói đó, không phải toàn bộ v1.

## Phối hợp

- Identity cung cấp danh tính và phiên đăng nhập.
- Community cung cấp phòng, thành viên và quyền phòng.
- [Thông báo](../notifications/README.md) và [gửi file](../files/README.md) là phần bổ sung, chưa chốt đợt bàn giao.

<a id="detailed-design"></a>

## Hợp đồng chung

- Thao tác thay đổi dữ liệu đi qua REST. Cập nhật realtime đi qua SignalR tại `/hubs/chat` (DEC-081).
- Mã lỗi theo [quy ước API](../../system/api-conventions.md#contracts).
- Cơ chế chung nằm ở [vòng đời tin](message-lifecycle.md), [đồng bộ](synchronization.md) và [nội dung văn bản](text-policy.md).
- Điều kiện liên quan tới Community nằm ở [tích hợp Community](../community/mechanics.md).

MVP dùng transaction trong cùng một ứng dụng. Khi tách service ở v1, phần này phải thiết kế lại.

---

Nguồn: [DEC-081](../../records/decisions/README.md#dec-081), [DEC-171](../../records/decisions/README.md#dec-171), DEC-173.
