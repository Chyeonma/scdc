# Tách service — Mẫu hồ sơ theo S3-01

Sao chép mẫu này vào `docs/records/verification/` cho từng lần tách service. Điền kế hoạch trước, rồi ghi lệnh và kết quả thực tế theo [checklist EX-00 đến EX-08](../../guides/operations.md#service-extraction); để mở phần chưa kiểm chứng.

**Trạng thái:** chuẩn bị / đã diễn tập / đã chuyển / đã phục hồi. Chỉ chọn trạng thái có bằng chứng.

## Định danh và phạm vi

| Trường | Giá trị cần điền |
|---|---|
| Service, task và nhánh riêng | … |
| Người thực hiện, người kiểm tra, thời điểm | … |
| Commit, phiên bản CLI, image trước/đích | … |
| Engine, Compose project và cổng gateway | … |
| Nguồn/đích | Tên DB, instance, schema, role; tham chiếu cấu hình, không có mật khẩu |
| API, Hub, worker và caller/callee | … |
| Khóa và trạng thái cần giữ | Key ID, key ring/application name, Redis prefix/generation, ledger và các tiến trình đang xử lý; không có giá trị secret |
| Dữ liệu và thời gian thử | Bản sao nào, kích thước, mốc đóng writer, thời gian từng bước; không kèm nội dung riêng tư |

## Ma trận thay lời gọi

| Interface/phương thức hiện tại | Caller → callee và RPC đích | Danh tính/quyền RPC | Deadline, retry, ID chống trùng và lỗi | Phần còn thiếu |
|---|---|---|---|---|
| … | … | … | … | … |

Ghi cả chiều cấp permit/lookup lẫn chiều fence/ACK và công việc tiếp tục sau restart. Nếu hai owner còn cùng một host, ghi danh tính chuyển tiếp cùng tập RPC được phép; không suy peer từ payload.

## Kế hoạch và bằng chứng theo checklist

| Mục | Kế hoạch/lệnh thật | Kết quả, artifact và exit code | Trạng thái |
|---|---|---|---|
| EX-00 — Phạm vi/baseline | … | … | Chưa kiểm chứng |
| EX-01 — Host/phụ thuộc | … | … | Chưa kiểm chứng |
| EX-02 — RPC/mTLS | … | … | Chưa kiểm chứng |
| EX-03 — Dữ liệu/quyền | … | … | Chưa kiểm chứng |
| EX-04 — Cấu hình/khóa/Redis/quan sát | … | … | Chưa kiểm chứng |
| EX-05 — Diễn tập riêng | … | … | Chưa kiểm chứng |
| EX-06 — Đóng ghi/chuyển gateway | … | … | Chưa kiểm chứng |
| EX-07 — Test/thu hồi/độc lập | … | … | Chưa kiểm chứng |
| EX-08 — Phục hồi/tổng kết | … | … | Chưa kiểm chứng |

## Manifest đối chiếu và route

- Manifest nguồn/đích: từng bảng và cột, số dòng, tập ID, hash/giá trị, sequence, version/epoch, hạn gốc và trạng thái công việc. Ghi ngoại lệ đã giải thích; không chỉ đối chiếu tổng số dòng.
- Ledger: nguồn nào đã áp, checksum, cách tạo baseline đích và phiên bản công cụ; kiểm tra đúng database và chống chạy đồng thời.
- Mỗi route: method/path/query/header → upstream trước/đích; cả route chính xác và route con, negotiate/WebSocket, CORS và lỗi. Ghi endpoint RPC nội bộ riêng.
- Secret/khóa/Redis: mount, quyền reader/writer, key ID/application name và generation trước/đích; bằng chứng token, refresh, cursor, dấu thu hồi tiếp tục đúng kế hoạch.

## Phục hồi đã diễn tập

| Mốc | Điều kiện xác nhận | Lệnh đã thử và kết quả |
|---|---|---|
| Đích còn đúng baseline, chưa có ghi hoặc đổi generation | … | … |
| Đích đã ghi/công bố generation mới hoặc chưa rõ | … | … |

Ghi riêng thời điểm mở writer đích, tác động của worker và Redis publisher, cách giữ nguồn cũ không ghi, phần chênh lệch phải xử lý và phạm vi vẫn đóng. Đổi route không tự đồng bộ dữ liệu ngược.

## Kết quả và phần còn lại

Ghi bộ test, môi trường và commit thực tế, số kết nối, mốc bắt đầu đo/commit thu hồi, thời gian ngắt, payload sau ngắt và kết nối đối chứng. Liên kết kết quả cũ chỉ làm baseline, không ghi thành kết quả trên host mới.

Ghi kết luận cho lần tách, lỗi/giới hạn, mục còn mở, task tiếp nhận và thay đổi cần đưa lại vào checklist S3-01 sau trải nghiệm thực tế. Không đánh dấu toàn bộ gói đã xong từ một lượt rà tài liệu.

Nguồn: S3-01, [runbook tách service](../../guides/operations.md#service-extraction), [chuyển dữ liệu MVP](../../system/database.md#v1-mvp-data-cutover).
