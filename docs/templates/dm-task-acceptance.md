# Biên bản kiểm tra task nhắn tin riêng

Mẫu theo [kế hoạch DM](../plans/direct-messaging.md). Sao chép vào hồ sơ task khi thực thi; mẫu này chưa ghi kết quả đã chạy. Không điền Đạt từ việc có source, build hoặc dữ liệu mock.

Từng prompt có bốn ví dụ C01–C04 trong [prompt DM](../plans/direct-messaging-prompts.md), đối chiếu được với [catalogue ca thử](../fixtures/dm-acceptance-cases.json). Ghi riêng từng case và biến thể; bổ sung AC/TC bắt buộc chưa có trong ví dụ. Không dùng kết quả agent để điền phần người dùng xác nhận.

## Task và bản chạy

| Trường | Giá trị cần điền |
|---|---|
| Task / phase / scope | ... |
| Branch / base / PR | ... |
| Commit/build đang chạy | ... |
| Schema/migration/config version | ... |
| Web/API/Swagger URL thực tế | ... |
| DB thử / Compose project | ... |
| Dataset run / manifest ID không nhạy cảm | ... |
| Browser/profile/OS/device/network | ... |
| Trạng thái task | Chưa làm / Đang làm / Chờ người dùng test / Người dùng FAIL / Người dùng PASS / Bị chặn |

## Chuẩn bị và chạy thử

- Lệnh dựng/chạy/dừng thực tế đã được agent kiểm tra: ...
- Tài khoản A/B/C/U/K và mật khẩu local: ... Không đưa access/refresh/verify token hoặc key vào hồ sơ.
- Conversation/message/clientMessageId cần dùng, lấy từ response: ...
- Fault recipe và cách tắt fault/reset trạng thái chỉ trong DB thử: ...
- Tập request Swagger/REST/PowerShell và query DB chỉ đọc: ...
- Phần chưa có runtime hoặc chưa thuộc task: ...

## Hồ sơ chi tiết từng test case

Lặp mục dưới đây cho từng C01–C04 và các ca bổ sung. Agent điền lệnh/request/SQL chạy được đúng bản bàn giao; ID, version, cursor lấy từ response/manifest. Token/secret giữ ngoài hồ sơ. Những lệnh/helper chưa triển khai không được ghi là đã chạy.

- Case ID / AC / TC: ...
- Build / run / lane / actor / session ID không nhạy cảm: ...
- Điều kiện trước và lệnh setup đã kiểm tra: ...
- Dữ liệu nhập đầy đủ, file payload biên, conversation/message/clientMessageId: ...
- Baseline DB: số message/operation/outbox, counter, ID/version/editedAt/deletedAt: ...
- FE và BE dùng dữ liệu riêng hay BE replay thao tác FE: ... Nếu replay, giữ đúng UUID/body/actor; không tạo thêm send mới rồi kỳ vọng một bản ghi.
- Fault recipe: lệnh bật/tắt, scope, barrier, trước/sau commit, cách biết transaction kết thúc: ...

| Bước | Actor / lane | Thao tác FE hoặc nguyên request BE | Mong đợi ở bước này | Thực tế / bằng chứng | Kết quả agent | Người dùng |
|---|---|---|---|---|---|---|
| FE 1 | ... | ... | ... | ... | Chưa chạy | Chưa xác nhận |
| FE 2 | ... | ... | ... | ... | Chưa chạy | Chưa xác nhận |
| FE 3 | ... | ... | ... | ... | Chưa chạy | Chưa xác nhận |
| BE 1 | ... | URL / method / body / auth actor | HTTP status, errorCode, DTO fields | ... | Chưa chạy | Chưa xác nhận |
| BE 2 | ... | Request đọc lại hoặc replay đúng UUID/body | ID/version/content/counts | ... | Chưa chạy | Chưa xác nhận |
| DB | ... | SQL chỉ đọc thực tế đúng schema/run | Baseline, delta và invariants | ... | Chưa chạy | Chưa xác nhận |

- HTTP/errorCode chưa chốt: agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể; không dùng "403 hoặc 404 đều được" làm PASS.
- Đối soát cuối: tập ID/sequence/version, delta records/counter, content hiện hành/tombstone, outbox metadata, guard/session: ...
- Bằng chứng đã lọc token/key/body riêng: ... Với revoke ghi DB commit t0, payload receipt cuối, sai số đồng hồ và deadline; mobile ghi thiết bị/bàn phím thật.
- Cleanup đã chạy: tắt fault/stream/clock injection, giữ manifest/bằng chứng, không drop DB/xóa volume: ...
- Kết luận case/biến thể: Chưa chạy / Bị chặn / Đạt / Chưa đạt. Nếu lỗi, ghi bước, expected/actual và build cần test lại.
- Xác nhận người dùng FE và BE trên build này: **Chưa xác nhận**.

## Kết quả agent trước bàn giao

| Ca / AC / TC | Dữ liệu và thao tác | Mong đợi | Thực tế | Loại bằng chứng / build | Kết quả |
|---|---|---|---|---|---|
| FE thành công | ... | ... | ... | Browser thật / ... | Chưa chạy |
| FE lỗi/ngoại lệ | ... | ... | ... | Browser thật / ... | Chưa chạy |
| BE thành công | ... | ... | ... | API + PostgreSQL / ... | Chưa chạy |
| BE quyền/lỗi/đồng thời | ... | ... | ... | API/runner/DB / ... | Chưa chạy |
| DB invariants | ... | ... | ... | Query chỉ đọc / ... | Chưa chạy |
| Test/build liên quan | ... | ... | ... | Unit/integration/build / ... | Chưa chạy |

Kết quả từng ca: Chưa chạy / Bị chặn / Đạt / Chưa đạt / Không áp dụng có căn cứ. Mô phỏng fetch, fault fixture hoặc mobile viewport phải ghi đúng loại; không thay bằng chứng API/DB/thiết bị thật.

## Người dùng tự kiểm tra

Chỉ điền sau khi người dùng phản hồi rõ, gắn build đã test.

| Ca | Bước cụ thể để người dùng làm | Request hoặc thao tác UI | Kỳ vọng | Kết quả người dùng / lỗi |
|---|---|---|---|---|
| FE 01 | ... | ... | ... | Chưa xác nhận |
| FE 02 | ... | ... | ... | Chưa xác nhận |
| BE 01 | ... | ... | ... | Chưa xác nhận |
| BE 02 | ... | ... | ... | Chưa xác nhận |

## Lỗi và lần kiểm tra lại

| Lỗi | Ảnh hưởng / ca thất bại | Commit sửa | Regression đã chạy | Build cần người dùng test lại | Kết quả |
|---|---|---|---|---|---|
| ... | ... | ... | ... | ... | Chưa xác nhận |

## Xác nhận và quyền bước tiếp

- Người dùng PASS/FAIL, nội dung phản hồi và thời điểm: Chưa có.
- Build và phạm vi FE/BE người dùng đã kiểm tra: Chưa có.
- Quyền merge đã cấp trong phiên hay phản hồi này: Chưa ghi nhận.
- Quyền task kế / ID task kế: Chưa ghi nhận.
- Kết luận: **Chờ người dùng test** sau khi agent bàn giao bản chạy đạt; không tự đi bước kế.
- Gate phát hành/v1 còn mở và lỗi tồn: ...

Xác nhận task chỉ có hiệu lực trên build/phạm vi ghi ở trên. Nếu thay đổi hành vi/config/schema ảnh hưởng ca đã test, đánh giá và kiểm tra lại phần đó trước dùng kết quả cũ.
