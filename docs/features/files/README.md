# SCDC — Gửi file

Người dùng có thể gửi file trong tin nhắn và tải file về khi có quyền xem cuộc trò chuyện đó.

**Trạng thái:** đang thiết kế, chưa có code. Tính năng được đưa vào kế hoạch ngày 2026-11-01.

**Người phụ trách:** Thái (xem [phân công](../../project/planning.md#team)).

## Phạm vi dự kiến

- Chọn file, tải lên và gửi kèm tin nhắn.
- Xem tiến độ tải lên, hủy hoặc thử lại khi lỗi.
- Tải file về từ tin nhắn.
- Chặn tải về khi người dùng không còn quyền xem cuộc trò chuyện hoặc phòng.

Chưa quyết định: dùng trong tin nhắn riêng, tin phòng hay cả hai; có hỗ trợ trên mobile ngay đợt đầu hay không.

## Phối hợp giữa các phần

| Phần việc | Ai làm |
|---|---|
| Tải lên, lưu trữ, tải về và giao diện cho file | Thái |
| Gắn file vào tin nhắn và hiển thị trong cuộc trò chuyện | Sáng, phối hợp với Thái |
| Kiểm tra phiên đăng nhập và quyền xem cuộc trò chuyện hoặc phòng | Vg và Sáng cung cấp; Thái áp dụng khi tải file |
| Cấu hình lưu trữ và triển khai | Thái chuẩn bị; Vg thống nhất với cấu hình chung |

Tin nhắn hiện chỉ có văn bản. Phần đính kèm sẽ được thêm vào [tin nhắn riêng](../messaging/direct-messaging.md) và [tin phòng](../messaging/channel-messaging.md) sau khi thiết kế xong.

## Cần thiết kế trước khi làm

1. **Giới hạn:** loại file được phép, kích thước tối đa, số file mỗi tin, và thông báo khi vượt giới hạn.
2. **Luồng gửi:** điều gì xảy ra với file đã tải lên nhưng gửi tin thất bại hoặc người dùng bỏ dở.
3. **Quyền:** ai được tải về, ai được xóa; file xử lý thế nào khi tin nhắn hoặc phòng bị xóa.
4. **Lưu trữ:** lưu ở đâu, giữ bao lâu, chi phí. Chưa chọn công nghệ (ví dụ MinIO).
5. **Hợp đồng API:** các endpoint tải lên/tải về, cách tin nhắn tham chiếu tới file, và cách tránh tạo file trùng khi người dùng bấm thử lại.
6. **Thuộc service nào:** v1 chỉ có bốn service (Identity, Community, Messaging, Media control). Cần chốt file do Messaging giữ hay tách thành phần riêng; phân công cho Thái không tự tạo service mới ([kiến trúc](../../system/architecture.md#ownership)).

Câu hỏi mở được theo dõi tại [OQ-016](../../project/open-questions.md#oq-016).

## Kiểm thử dự kiến

Sau khi thiết kế xong, cần có ca kiểm thử cho:

- Gửi và tải file khi có quyền; bị từ chối khi không có quyền.
- File sai loại hoặc quá lớn.
- Mất mạng giữa chừng và thử lại (không tạo đính kèm trùng).
- Người dùng mất quyền sau khi file đã được gửi.

Kiểm thử phải chạy với tin nhắn, giao diện và nơi lưu trữ thật; chỉ thử tải lên thành công là chưa đủ.

---

Nguồn: REQ-016, SCP-011, [DEC-171](../../records/decisions/README.md#dec-171) (phân công), [DEC-173](../../records/decisions/README.md#dec-173) (đưa vào kế hoạch, thay DEC-017/023). Nhiệm vụ Trello: EXT-FILE-01.
