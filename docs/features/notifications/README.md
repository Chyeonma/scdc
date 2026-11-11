# SCDC — Thông báo

Người dùng nhận thông báo khi có sự kiện liên quan tới mình, như tin nhắn mới, lời mời vào server hoặc cuộc gọi đến, trên web và mobile.

**Trạng thái:** đang thiết kế, chưa có code. Được đưa vào kế hoạch ngày 2026-11-01. Realtime chat hiện có không phải là thông báo đẩy (push).

**Người phụ trách:** Sáng (xem [phân công](../../project/planning.md#team)). Mỗi service tạo ra sự kiện vẫn chịu trách nhiệm về nghiệp vụ của sự kiện đó.

## Phối hợp với các phần khác

| Phần | Cần thống nhất |
|---|---|
| Messaging | Sự kiện tin nhắn và thông tin người nhận cần biết |
| Community | Sự kiện thành viên, lời mời và ai được phép biết |
| Media | Trạng thái cuộc gọi do Media giữ; Thái và Sáng thống nhất cách gửi thông báo cuộc gọi |
| Identity | Gắn thiết bị với tài khoản; xử lý khi đăng xuất hoặc thu hồi phiên |
| Mobile | Quyền thông báo, app chạy nền hay chạy chính, mở đúng màn hình khi bấm thông báo |

Chưa quyết định thông báo chạy chung với Messaging hay là service riêng.

## Cần thiết kế

1. **Sự kiện:** loại sự kiện nào gửi thông báo, gửi cho ai, khi nào không gửi.
2. **Kênh:** thông báo trong app hay push; trên web, mobile và nền tảng nào.
3. **Nội dung:** hiển thị gì, quyền riêng tư, mở màn hình nào, có trạng thái đã đọc không.
4. **Thiết bị:** đăng ký, cập nhật, hủy; nhiều thiết bị; đổi tài khoản và đăng xuất.
5. **Độ tin cậy:** nguồn sự kiện, schema, chống gửi trùng, gửi lại, hết hạn và phục hồi khi lỗi. Thông báo cũ không được làm sai trạng thái nghiệp vụ.
6. **Nhà cung cấp:** dịch vụ dùng, giới hạn, chi phí, cấu hình và cách theo dõi kết quả gửi. Chưa chọn FCM hay APNs.
7. **Thuộc service nào:** v1 chỉ có bốn service. Cần chốt thông báo chạy trong Messaging, chia theo service phát sinh sự kiện, hay tách riêng ([kiến trúc](../../system/architecture.md#ownership)).

## Kiểm thử dự kiến

Khi chốt phạm vi, cần có ca kiểm thử cho: gửi đúng người, người dùng từ chối quyền thông báo, nhiều thiết bị, đổi tài khoản hoặc bị thu hồi phiên, sự kiện lặp hoặc hết hạn, mất mạng, và mở đúng màn hình.

Cần phân biệt ba mức: nhà cung cấp đã nhận, thiết bị đã nhận, người dùng đã xem.

Câu hỏi mở: [OQ-015](../../project/open-questions.md#oq-015). Email tài khoản là phần riêng, xem [email](../accounts/email-links.md).

---

Nguồn: REQ-015, SCP-010, [DEC-171](../../records/decisions/README.md#dec-171), DEC-173. Nhiệm vụ Trello: EXT-NOTIFY-01.
