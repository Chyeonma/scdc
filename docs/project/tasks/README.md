# Thẻ Trello theo bước

Các file trong thư mục này chứa task theo [lộ trình](../planning.md#roadmap) và vấn đề phát sinh. Khi file được push lên `main`, GitHub Actions tự tạo hoặc cập nhật nội dung thẻ trên bảng Trello của nhóm.

| File | Bước |
|---|---|
| [step-1.md](step-1.md) | STEP-1 — Phạm vi chung hoàn tất; S1-07 giữ riêng cho Media |
| [step-2.md](step-2.md) | STEP-2 — Hoàn tất theo xác nhận của người dùng ngày 2026-11-25 |
| [step-3.md](step-3.md) | STEP-3 — Tách từng service (12 thẻ đã công bố) |
| [step-4.md](step-4.md) | STEP-4 — Tính năng trên kiến trúc mới (15 thẻ đã công bố) |
| [step-4-early.md](step-4-early.md) | Việc của STEP-4 làm được ngay, ai rảnh nhận (đồng bộ) |
| [issues.md](issues.md) | Vấn đề phát sinh; mã `ISSUE-…`, thẻ mới vào cột Issue |

## Task có thể bắt đầu

Rà ngày **2026-11-26**: [STEP-2 đã hoàn tất](../../records/verification/environment/s2-12-2026-11-25.md); [S3-01/S3-02/S3-03 đã có kết quả, Identity và PostgreSQL riêng đang chạy dev](../../records/verification/architecture/s3-03-2026-11-25.md). Phần S3-03 được đưa vào main bằng nhánh đồng bộ sau khi PR #46 merge vào nhánh S3-02. E4-01 đã tích hợp worker email vào Identity. Có **7 việc đủ phụ thuộc để bắt đầu**: S3-07, S3-04 và năm việc làm sớm. Ước lượng các task tiếp theo chưa chốt; người nhận rà đầu ra, công sức và minh chứng theo [điều kiện bắt đầu](../workflow.md#readiness).

| Task | Người làm đề xuất | Phần có thể làm ngay |
|---|---|---|
| S3-07 | Sáng | Tách host Messaging dựa trên Identity đã chạy qua mạng và [checklist đã áp dụng](../../records/verification/architecture/s3-01-2026-11-25.md) |
| S3-04 | Thái | Cấu hình Gmail và kiểm chứng thư thật trên dev từ worker đã tích hợp ở E4-01 |
| E4-02 | Người còn công suất; Sáng kiểm tra | Giao diện tin phòng với dữ liệu giả |
| E4-03 | Người còn công suất; Thái kiểm tra | Prototype gọi riêng, phòng thoại và thiết bị với dữ liệu giả |
| E4-04 | Người còn công suất; Vg kiểm tra | Giao diện lời mời và duyệt tham gia với dữ liệu giả |
| E4-05 | Người còn công suất; Thái kiểm tra | Đối chiếu hợp đồng và giao diện Media, ghi các chỗ thiếu |
| E4-06 | Vg | Rà quy tắc Community theo các quyết định đã chốt; tách phần triển khai còn thiếu |

E4-01 đã tích hợp và kiểm thử; S3-04 tiếp tục hoàn thiện email thật trên dev. S3-07 đã đủ phụ thuộc để bắt đầu. Theo yêu cầu ngày 2026-11-25, cả 12 thẻ STEP-3 và 15 thẻ STEP-4 được công bố để đồng bộ lên Trello. S3-01/S3-02/S3-03/S3-09/S3-10 có kết quả hoàn tất trong nguồn; các task còn lại giữ nguyên phụ thuộc và chưa tự chuyển sang đang làm. Backend STEP-4 vẫn chờ các phần STEP-3 liên quan. [PR #39](https://github.com/Chyeonma/scdc/pull/39) đã đóng; lần công bố này dùng nguyên hai file hiện tại, không áp dụng cách tách file trong PR cũ.

## Nhánh tính năng chưa tích hợp

S3-09/S3-10 đã hoàn tất và vào main qua [PR #50](https://github.com/Chyeonma/scdc/pull/50) và [PR #52](https://github.com/Chyeonma/scdc/pull/52). [Community](../../../services/Community/README.md) đang chạy host/database riêng trên dev, nối Identity/Messaging qua gRPC/mTLS; [hồ sơ bước 3](../../records/verification/architecture/s3-10-phase-3-2026-11-26.md) ghi dữ liệu, gateway, browser và rollback. Messaging tiếp tục ở API cũ tới S3-07/S3-08.

| Nhánh | Bằng chứng đang có | Phần còn lại |
|---|---|---|
| `feat/media-direct-calls` (`adf6a61`, 2026-11-23) | [Hồ sơ trên nhánh Media](https://github.com/Chyeonma/scdc/blob/feat/media-direct-calls/docs/verification/media-direct-calls.md) ghi LiveKit thật, 16 unit test và 5 ca E2E; [CI đạt](https://github.com/Chyeonma/scdc/actions/runs/45049551943) | Chưa có PR vào main. Cần đối chiếu nghiệm thu S1-07, hợp đồng phiên/thu hồi hiện tại và S3-05/S3-06; chưa kết luận Media hoàn tất trên main |


E4-01 đã chọn lọc phần email từ `60f4db3` vào Identity hiện tại; PR #28 được thay bằng bản tích hợp này. [Hồ sơ E4-01](../../records/verification/authentication/e4-01-2026-11-26.md) ghi phần lấy/bỏ và giới hạn S3-04.

## Ai giữ phần nào

| Repo (file này) | Trello |
|---|---|
| Tiêu đề, mô tả: người làm đề xuất, đầu ra, tiêu chí xong, phụ thuộc; thêm nhãn | Cột trạng thái, người được gán, hạn, bình luận, checklist; gỡ nhãn |

- Đồng bộ **một chiều** từ repo lên Trello. Sửa mô tả thẻ trên Trello sẽ bị ghi đè ở lần đồng bộ sau; muốn đổi nội dung thì sửa file và push.
- Mặc định script **không** chuyển cột; không gán người, không đổi hạn, không xóa hay lưu trữ thẻ. Chỉ thao tác thủ công với danh sách `complete` mới chuyển các task đã có kết quả hoàn tất trong repo. Thẻ bị bỏ khỏi file chỉ được liệt kê trong log.
- Thẻ mới vào cột cấu hình ở `TRELLO_LIST_ID` (nên là cột "Backlog"), có nhãn theo bước (ví dụ `STEP-1`).
- Riêng thẻ `ISSUE-…` mới vào `TRELLO_ISSUE_LIST_ID`, hiện là cột **Issue (Vấn đề phát sinh)**, ID `6ac4d5f0da5bb6adcf1a78f2`. Script kiểm cột còn mở và thuộc đúng bảng trước khi ghi. Thẻ issue đã có giữ cột do người dùng chọn; không tự kéo về Issue khi đồng bộ lại.
- Thẻ được nhận ra bằng tiền tố `[S1-01]` trong tên. **Không xóa tiền tố này trên Trello**, nếu không lần sau sẽ tạo thẻ mới.

## Định dạng

```markdown
# STEP-2 — Tên bước

Mô tả chung (không đồng bộ).

- Nhãn: Backend

## S2-01 · Tiêu đề thẻ
- Nhãn: Identity, Ưu tiên cao
- Người làm: ... · Review: ...
- Đầu ra: ...
- Xong khi: ...
- Phụ thuộc: ...
```

- Mã đầu tiên của dòng `#` thành nhãn Trello (ví dụ `STEP-2`), tự tạo nếu bảng chưa có.
- `- Nhãn: A, B` trong một thẻ gắn nhãn cho thẻ đó; đặt trước thẻ đầu tiên thì gắn cho mọi thẻ trong file. Dòng này không đưa vào mô tả.
- Nhãn phải **có sẵn trên bảng**; so tên không phân biệt hoa thường. Tên sai hoặc chưa có thì chỉ cảnh báo trong lần chạy, không tự tạo (tránh tạo nhãn rác vì gõ nhầm). Nhãn chỉ có màu, không có tên, thì không dùng được.
- Script chỉ **thêm** nhãn, không gỡ. Bỏ một nhãn khỏi file thì phải tự gỡ trên Trello. Nhãn gắn tay trên Trello (ví dụ "Bị chặn") được giữ nguyên.
- Mỗi dòng `## <mã> · <tiêu đề>` là một thẻ; mọi dòng tới thẻ tiếp theo thành mô tả. Mã không được trùng, kể cả giữa các file.
- File có dòng `Trạng thái: nháp` trước thẻ đầu tiên thì **không đồng bộ**. Dùng để viết trước thẻ của bước sau; xóa dòng đó và push để đưa thẻ lên Trello.
- Một dòng `## ` không theo dạng `<mã> · <tiêu đề>` (ví dụ `## Hiện trạng`) không phải thẻ; nội dung dưới nó không đồng bộ.
- Vấn đề cần đồng bộ từ repo được ghi ở `issues.md`, dùng mã `ISSUE-001`, `ISSUE-002`… và không có H1 dạng STEP để tránh tạo nhãn bước. Việc vặt vẫn có thể tạo thủ công trên Trello; chỉ thẻ có nguồn trong repo mới được công cụ đồng bộ.

Chạy thử trên máy, không gọi Trello:

```bash
python3 scripts/sync_trello.py --dry-run
```

## Đồng bộ trạng thái khi được yêu cầu

Workflow `Sync tasks to Trello` giữ nguyên toàn bộ S1-07 theo chỉ đạo ngày 2026-11-22, kể cả mô tả, nhãn và cột; việc Media này không chặn S1-09. Khi push task lên `main`, workflow tự đồng bộ nội dung các thẻ khác. Nó không tự suy trạng thái từ mô tả để di chuyển thẻ.

Để chuyển các task đã hoàn tất sang đúng cột, chạy workflow thủ công. `complete` là danh sách mã cách nhau bằng dấu phẩy, `done_list` là tên cột hiện có trên bảng. Nên đọc kế hoạch trước:

```bash
gh workflow run sync-trello.yml --ref main \
  -f complete='S1-01,S1-02,S1-03,S1-04,S1-05,S1-06,S1-08,S1-09,S2-01,S2-02,S2-03' \
  -f done_list='Hoàn tất' -f preview=true
```

Đối chiếu log để biết tên cột thực tế; dùng cùng lệnh với `preview=false` để thực hiện. Nếu chưa biết tên cột, để `complete` trống và `preview=true`; workflow sẽ liệt kê các cột đang mở mà không ghi dữ liệu.

Script từ chối task chưa hoàn tất trong nguồn, task bị loại trừ, mã thẻ trùng trên Trello hoặc tên cột không duy nhất. Sau khi ghi, script đọc lại để xác nhận các mã yêu cầu đã ở cột hoàn tất và S1-07 giữ nguyên. Chạy lại không tạo thẻ trùng hoặc di chuyển lại thẻ đã đúng cột. Không thay người được gán, hạn, bình luận hoặc trạng thái lưu trữ.

Chỉ đồng bộ một issue bằng cùng workflow:

```bash
gh workflow run sync-trello.yml --ref main -f only=ISSUE-001 -f preview=true
```

Đối chiếu kế hoạch rồi dùng `preview=false` để ghi. Không truyền `complete` thì giữ nguyên cột của thẻ đã có. Khi chạy thủ công từ nhánh khác, footer nguồn trỏ đúng nhánh đó; lần sync main sau merge cập nhật lại link main. `--only` cũng dùng được khi chạy script local; `--dry-run --only ISSUE-001` không gọi Trello.

Kiểm tra công cụ bằng `python3 -m unittest discover -s scripts/tests -p 'test_sync_trello.py'`. Cách cập nhật thẻ dựa trên [API Trello chính thức](https://developer.atlassian.com/cloud/trello/rest/api-group-cards/).

## Cài đặt một lần (người quản trị repo)

1. Lấy API key và token Trello tại trang Power-Up admin của Trello. Token chỉ cần quyền đọc, ghi trên bảng của nhóm.
2. GitHub repo → Settings → Secrets and variables → Actions:
   - **Secrets:** `TRELLO_KEY`, `TRELLO_TOKEN` (có thể đã có từ workflow cũ).
   - **Variables:** `TRELLO_BOARD_ID` (đoạn mã sau `trello.com/b/` trong link bảng), `TRELLO_LIST_ID` (cột nhận task mới) và `TRELLO_ISSUE_LIST_ID` (cột nhận issue mới).
3. Lấy ID cột: mở `https://trello.com/b/<board>.json` khi đã đăng nhập, tìm tên cột trong mục `lists`; hoặc gọi API `GET /1/boards/<board>/lists`.
4. Chạy lần đầu: tab Actions → "Sync tasks to Trello" → Run workflow. Xem log để biết thẻ nào được tạo.
