# STEP-4 — Tính năng trên kiến trúc mới

Trạng thái: nháp

Các thẻ dưới đây còn chờ phụ thuộc, chưa đưa lên Trello. Các phần có thể bắt đầu ngay đã chuyển nguyên mã thẻ sang [step-4-ready.md](step-4-ready.md); không tạo mã trùng. Rà lại khi bước trước đạt tiêu chí trước khi công bố phần còn lại.

## COM-W02 · Tin phòng: gửi và gửi lại
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: ghi tin có hàng rào quyền tới lúc commit; chống trùng; tin, sequence, outbox ghi cùng lúc (UC-COM-23).
- Xong khi: thử gửi lại, gửi đồng thời, khởi động lại, đổi quyền đúng lúc gửi.
- Phụ thuộc: COM-W01.
- Khả năng bắt đầu: **Chờ STEP-3 và COM-W01**. Backend phải ghi bằng transaction Messaging riêng và permit/fence hiện tại; không dùng transaction Community để giữ quyền.

## COM-W03 · Tin phòng: giao diện với API thật
- Nhãn: feat, UI/UX
- Người làm: Sáng · Review: Vg
- Đầu ra: nối giao diện ở E4-02 với API thật: mở phòng → lịch sử → gửi → tải lại.
- Xong khi: trạng thái đã lưu, lỗi rõ ràng; gửi lại do người dùng bấm.
- Phụ thuộc: COM-W01, COM-W02, E4-02.
- Khả năng bắt đầu: **Chờ COM-W01, COM-W02 và E4-02**. Phần giao diện với dữ liệu giả làm ngay ở E4-02; thẻ này chỉ nối và kiểm thử API thật.

## COM-W04 · Tin phòng: nhận realtime
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: outbox worker, Chat Hub, đăng ký phòng có kiểm soát (UC-COM-25).
- Xong khi: hai tài khoản nhận tin; loại trùng; đối chiếu sau kết nối lại.
- Phụ thuộc: COM-W02.
- Khả năng bắt đầu: **Chờ COM-W02**. Dùng sự kiện và quyền từ backend tin phòng thật; không tính ChatHub DM hiện tại là hoàn tất realtime phòng.

## COM-W05 · Tin phòng: mất quyền
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: HTTP, gửi, đăng ký, bù tin đều theo quyền hiện tại (UC-COM-20 đến 22, 25).
- Xong khi: đo được kết nối ngừng trong 5 giây từ lúc commit, cho cả thu hồi phiên và đổi vai trò, ACL (DEC-083).
- Phụ thuộc: COM-W04.
- Khả năng bắt đầu: **Chờ COM-W04**. Đo ngắt tin phòng cần Hub, quyền Community và các service thật; số đo thu hồi DM không thay được kết quả của thẻ này.

## COM-W06 · Toàn luồng Community và tin phòng
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: tạo → tìm → tham gia → phòng chữ → lịch sử, gửi, nhận → mất quyền, chạy trên bốn service thật.
- Xong khi: hồi quy các chức năng cũ qua; ghi commit, môi trường, kết quả.
- Phụ thuộc: COM-W03, COM-W04, COM-W05.
- Khả năng bắt đầu: **Chờ COM-W03, COM-W04 và COM-W05**. Nghiệm thu trên bốn service, dữ liệu và kết nối thật.

## COM-W07 · Tích hợp và hồ sơ kiểm thử Community
- Nhãn: docs
- Người làm: Vg · Review: Sáng
- Đầu ra: chạy lại theo [hướng dẫn](../../guides/development.md#community); hồ sơ đối chiếu [tiêu chí chuyển tiếp](../../releases/mvp.md#community-acceptance).
- Xong khi: có hồ sơ trong `records/verification/community`.
- Phụ thuộc: COM-W06.
- Khả năng bắt đầu: **Chờ COM-W06**. Hồ sơ kết quả phải lấy từ bản đã chạy kiểm thử; dùng lại tiêu chí hiện có, không ghi kết quả dự kiến thành đã đạt.

## COM-W11 · Chuyển chủ sở hữu và rời server
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: chuyển chủ, rời, dọn vai trò và ngoại lệ theo epoch (UC-COM-14, 15).
- Xong khi: chủ không tự rời được; epoch cũ bị từ chối; thu hồi HTTP và realtime.
- Phụ thuộc: COM-W05.
- Khả năng bắt đầu: **Chờ COM-W05**. Cần kiểm chứng mất quyền của tin phòng trước khi nghiệm thu chuyển chủ/rời server và ngắt realtime tương ứng.

## COM-W12 · Sửa và xóa tin phòng
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: sửa, xóa tin theo quyền tác giả, placeholder, version, outbox (UC-COM-24).
- Xong khi: chứng minh tranh chấp sửa và xóa, dùng quyền hiện tại.
- Phụ thuộc: COM-W04.
- Khả năng bắt đầu: **Chờ COM-W04**. Dùng quyền, version và sự kiện của tin phòng thật; phần sửa/xóa DM đã có không thay cho kết quả này.

## V1-MEDIA-03 · Phòng thoại (gồm COM-W13)
- Nhãn: feat
- Người làm: Thái · Review: Vg
- Đầu ra: phòng thoại từ backend tới giao diện theo [phòng thoại](../../features/media/voice-rooms.md); dùng quyền và vòng đời phòng của Community; nối giao diện E4-03.
- Xong khi: thử vào, rời, hạn mức, mất quyền, xóa phòng.
- Phụ thuộc: STEP-3, E4-03.
- Khả năng bắt đầu: **Chờ STEP-3 và E4-03**. Prototype làm ngay ở E4-03; thẻ này cần Media/Community thật và giao thức quyền, tạo/xóa phòng tương ứng.

## V1-MEDIA-04 · Micro, camera, chia sẻ màn hình
- Nhãn: feat
- Người làm: Thái · Review: Sáng
- Đầu ra: thiết bị, nguồn phát, hiển thị nguồn theo [camera và chia sẻ](../../features/media/video-screen-sharing.md).
- Xong khi: thử từ chối quyền thiết bị, dừng nguồn từ trình duyệt, kết nối lại.
- Phụ thuộc: V1-MEDIA-03.
- Khả năng bắt đầu: **Chờ V1-MEDIA-03**. Phần thử giao diện/thiết bị trước backend nằm ở E4-03; thẻ này kiểm chứng nguồn phát và kết nối thật.

## V1-MEDIA-05 · Kiểm soát vào phòng, hạn mức, thu hồi
- Nhãn: feat, tech
- Người làm: Thái · Review: Vg
- Đầu ra: kiểm soát vào, hạn mức, thu hồi, dừng khi lỗi (MEDIA-GAP-02, 04).
- Xong khi: chứng minh ngắt cả phát và nhận trong 5 giây, kể cả khi worker hay mạng lỗi; thử tải theo MEDIA-GAP-06.
- Phụ thuộc: V1-MEDIA-03.
- Khả năng bắt đầu: **Chờ V1-MEDIA-03**. Nghiệm thu giới hạn và thời hạn ngắt cần LiveKit, Media control và quyền Community thật; review hợp đồng trước đó làm ở E4-05.
