# SCDC — Thuật ngữ

Các từ kỹ thuật dùng trong tài liệu, giải thích theo nghĩa trong dự án SCDC. Sắp theo bảng chữ cái.

| Thuật ngữ | Nghĩa trong SCDC |
|---|---|
| **ACL** (access control list) | Danh sách cho phép hoặc chặn một vai trò xem hay thao tác trên một phòng cụ thể. Xem [quyền truy cập](features/community/access-control.md). |
| **accessVersion** | Số phiên bản quyền của một server. Tăng mỗi khi vai trò, ACL hay chủ sở hữu thay đổi, để các bên biết cần kiểm tra lại quyền. |
| **CAS** (compare-and-set) | Cập nhật kèm số phiên bản mong đợi (`expectedVersion`). Nếu dữ liệu đã bị người khác sửa, cập nhật bị từ chối thay vì ghi đè. |
| **Channel** | Phòng trong một server: phòng chữ hoặc phòng thoại. |
| **clientOperationId** | Mã do client tạo cho một thao tác. Gửi lại cùng mã thì server trả kết quả cũ, không tạo bản ghi trùng. |
| **Cursor** | Mốc vị trí dùng để tải trang tiếp theo của một danh sách (ví dụ lịch sử tin) mà không bỏ sót hay lặp. |
| **DEC, D-số** | Mã quyết định. DEC-xxx là mã trong [hồ sơ quyết định](records/decisions/README.md); Dxx là mã câu hỏi thiết kế v1 dẫn tới quyết định đó. |
| **DM** (direct message) | Tin nhắn riêng giữa hai người. |
| **Epoch** | Số thế hệ của một trạng thái, ví dụ tư cách thành viên. Khi bị thu hồi rồi cấp lại, epoch tăng, nên thao tác mang epoch cũ bị từ chối. |
| **Fence, floor** | Mốc tối thiểu được ghi bền (ví dụ phiên bản quyền). Sau khi khôi phục dữ liệu, mọi thứ thấp hơn mốc này bị coi là cũ và không được dùng. |
| **Fingerprint** | Mã băm của nội dung một thao tác, dùng để phát hiện cùng mã thao tác nhưng nội dung khác. |
| **Fixture** | File dữ liệu đầu vào và kết quả mong đợi dùng để kiểm thử. Xem [fixtures](fixtures/README.md). |
| **gRPC, Protobuf** | Cách các service v1 gọi nhau trực tiếp. Protobuf là định dạng mô tả hợp đồng. |
| **Guard** | Thành phần kiểm tra quyền (tài khoản còn hoạt động, còn là thành viên…) và giữ khóa dữ liệu tới khi thao tác ghi xong, để quyền không bị đổi giữa chừng. |
| **Hub** | Điểm kết nối SignalR để server đẩy cập nhật realtime xuống trình duyệt. Có Hub chat (Messaging) và Hub media (Media control). |
| **Idempotent** | Gọi lại nhiều lần cho cùng một kết quả như gọi một lần. |
| **Lease** | Quyền giữ một việc trong thời gian giới hạn. Ví dụ worker giữ một sự kiện outbox; hết hạn thì worker khác có thể lấy. |
| **LiveKit, SFU** | Máy chủ truyền âm thanh và video. SFU nhận luồng từ mỗi người rồi chuyển cho những người khác trong phòng. SCDC tự host LiveKit. |
| **Modular monolith** | Một ứng dụng duy nhất nhưng chia thành các module có ranh giới rõ. Đây là kiến trúc của MVP. |
| **mTLS** | TLS hai chiều: cả hai service đều xuất trình chứng chỉ để xác thực lẫn nhau. |
| **Outbox** | Bảng lưu sự kiện cần gửi đi, ghi cùng transaction với dữ liệu. Worker đọc bảng này và gửi sau, nên sự kiện không bị mất khi lỗi. |
| **Owner** | Người sở hữu. Với server là chủ server; với dữ liệu là service duy nhất được ghi dữ liệu đó. |
| **Proof** | Bằng chứng đã kiểm thử: kết quả chạy thật, gắn với commit và môi trường cụ thể. |
| **Server** | Một cộng đồng trong SCDC, có thành viên, vai trò và các phòng. |
| **Snapshot** | Bản chụp trạng thái tại một thời điểm, ví dụ code ở commit `900e226` cho MVP. |
| **Thu hồi (revocation)** | Cắt quyền đang có: đăng xuất phiên, rời server, mất quyền xem phòng. Kết nối chat và media phải dừng trong 5 giây. Xem [thu hồi truy cập](system/access-revocation.md). |
| **UC, AC, TC** | Mã ca sử dụng (use case), tiêu chí chấp nhận (acceptance criteria) và ca kiểm thử (test case). |
| **Worker** | Tiến trình chạy nền, ví dụ gửi email hay chuyển sự kiện outbox. Trong v1 chạy chung tiến trình với API của service. |
