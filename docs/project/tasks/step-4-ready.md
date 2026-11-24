# STEP-4 — Phần có thể bắt đầu ngay

Công bố Trello ngày 2026-11-24 theo yêu cầu kiểm tra và đưa việc làm được ngay lên bảng. Mỗi thẻ ghi riêng **Khả năng bắt đầu** và **Phần chuẩn bị**; chỉ phần này có thể nhận ngay. Triển khai và nghiệm thu toàn bộ task vẫn theo phụ thuộc ghi trên thẻ.

Các task còn chờ nằm trong [step-4.md](step-4.md) và giữ nháp. Phần giao diện giả/rà tài liệu E4 tiếp tục ở [việc làm sớm](step-4-early.md).

## COM-W01 · Tin phòng: nền dữ liệu và đọc lịch sử
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: model tin phòng, thứ tự theo không gian, cursor, đọc lịch sử có kiểm tra quyền (UC-COM-17).
- Xong khi: thử phân trang, phòng ẩn, khác server, phiên sai, epoch cũ trên database thật.
- Phụ thuộc: STEP-3.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Đối chiếu nền DM, thiết kế hợp đồng lịch sử và dữ liệu thử; triển khai và nghiệm thu backend chờ STEP-3.
- Phần chuẩn bị: xác định phần model/sequence/cursor dùng lại từ DM, trường hợp quyền theo membership/ACL/lifecycle và response cho phòng ẩn, khác server, phiên sai, epoch cũ. Bổ sung fixture còn thiếu theo hợp đồng hiện hành để E4-02 dùng; schema hợp lệ chưa chứng minh đọc tin phòng thật.

## COM-W08 · Xóa phòng
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: thiết kế và triển khai giao thức xóa phòng giữa Community, Messaging và Media theo UC-COM-19: quyết định/trạng thái bền theo từng owner, lifecycle epoch/tombstone, thu hồi truy cập, xác nhận, thử lại và phục hồi khi một bên lỗi. Không dùng transaction chung để xóa đồng thời các database.
- Xong khi: chứng minh tranh chấp gửi tin hoặc vào cuộc gọi đúng lúc xóa; HTTP, Hub và Media ngừng truy cập theo chính sách; lệnh lặp, đến muộn hoặc mất phản hồi không hồi sinh phòng, không xóa lịch sử đã commit.
- Phụ thuộc: COM-W06.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Thiết kế giao thức và ca lỗi dựa trên STEP-2; triển khai và nghiệm thu chờ COM-W06 cùng phần Media cần kiểm chứng.
- Phần chuẩn bị: xác định owner quyết định xóa, cách chặn truy cập trước/sau từng commit, ACK thu hồi, cleanup bền và tác động tới phòng thoại; mô tả mất phản hồi, service dừng, lệnh muộn và xóa tranh gửi/vào phòng. Cập nhật đặc tả xóa v1 trước khi viết backend; không suy rằng giao thức tạo phòng S2-09 đã giải quyết xóa phòng.

## COM-W09 · Thông tin server, cách tham gia, duyệt
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: sửa thông tin server, đổi riêng tư, yêu cầu tham gia và duyệt (UC-COM-04, 05, 07, 08); nối giao diện E4-04.
- Xong khi: chứng minh duyệt, hủy, tham gia đồng thời; đổi sang riêng tư kết thúc yêu cầu đang chờ.
- Phụ thuộc: STEP-3, E4-04.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Chuẩn bị ma trận trạng thái, request/response và fixture theo chính sách hiện hành; triển khai backend/nối API chờ STEP-3 và E4-04.
- Phần chuẩn bị: đối chiếu dữ liệu cho duyệt/hủy/tham gia đồng thời, CAS, đổi sang riêng tư và kết thúc yêu cầu đang chờ; ghi kết quả mong đợi và câu hỏi thật sự chặn triển khai. Dùng đầu vào rà quy tắc của E4-06 và cấp fixture cho E4-04; giữ phần giao diện trong E4-04.

## COM-W10 · Lời mời
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: link mời, lượt dùng, hạn, mời đích danh (UC-COM-09 đến 13); nối giao diện E4-04.
- Xong khi: chứng minh lượt cuối, thu hồi và nhận đồng thời.
- Phụ thuộc: STEP-3, E4-04.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Chuẩn bị hợp đồng và fixture cho lời mời theo chính sách đã chốt; triển khai backend/nối API chờ STEP-3 và E4-04.
- Phần chuẩn bị: đối chiếu token không lộ trên URL/log, hạn/lượt dùng, người tạo mất quyền, mời đích danh, epoch và tranh lượt cuối/thu hồi/nhận đồng thời; bổ sung fixture còn thiếu dùng chung với E4-04. Câu hỏi chính sách theo E4-06; giới hạn tần suất bổ sung vẫn là đề xuất.
