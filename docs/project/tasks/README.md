# Thẻ Trello theo bước

Mỗi file trong thư mục này chứa các thẻ Trello của một bước trong [lộ trình](../planning.md#roadmap). Khi file được push lên `main`, GitHub Actions tự tạo hoặc cập nhật thẻ trên bảng Trello của nhóm.

| File | Bước |
|---|---|
| [step-1.md](step-1.md) | STEP-1 — Phạm vi chung hoàn tất; S1-07 giữ riêng cho Media |
| [step-2.md](step-2.md) | STEP-2 — S2-01 đến S2-11 đã triển khai; còn S2-12 tổng kết |
| [step-3.md](step-3.md) | STEP-3 — Tách từng service (nháp, chưa đồng bộ) |
| [step-4.md](step-4.md) | STEP-4 — Tính năng trên kiến trúc mới (nháp, chưa đồng bộ) |
| [step-4-early.md](step-4-early.md) | Việc của STEP-4 làm được ngay, ai rảnh nhận (đồng bộ) |

## Task có thể bắt đầu

Rà ngày **2026-11-25**: S2-01 đến S2-10 đã vào `main`; S2-11 đã triển khai và có [hồ sơ kiểm chứng](../../records/verification/architecture/s2-11-2026-11-25.md). Sau khi merge S2-11 và CI đạt, có **7 task đủ phụ thuộc để bắt đầu**, gồm S2-12 và sáu việc làm sớm. Sáu việc làm sớm tiếp tục nhận được độc lập. Người nhận chuẩn bị ước lượng, dữ liệu thử và minh chứng theo [điều kiện bắt đầu](../workflow.md#readiness).

| Task | Người làm đề xuất | Phần có thể làm ngay |
|---|---|---|
| S2-12 | Vg, cả nhóm | Sau khi merge S2-11 và CI đạt: demo ba role database tách quyền, tổng kết STEP-2 và ước lượng STEP-3 |
| E4-01 | Thái | Đối chiếu, tích hợp hoặc ghi lý do bỏ nhánh worker email trước S3-04 |
| E4-02 | Người còn công suất; Sáng kiểm tra | Giao diện tin phòng với dữ liệu giả |
| E4-03 | Người còn công suất; Thái kiểm tra | Prototype gọi riêng, phòng thoại và thiết bị với dữ liệu giả |
| E4-04 | Người còn công suất; Vg kiểm tra | Giao diện lời mời và duyệt tham gia với dữ liệu giả |
| E4-05 | Người còn công suất; Thái kiểm tra | Đối chiếu hợp đồng và giao diện Media, ghi các chỗ thiếu |
| E4-06 | Vg | Rà quy tắc Community theo các quyết định đã chốt; tách phần triển khai còn thiếu |

Ưu tiên S2-12 sau khi merge S2-11 và CI đạt trước khi chuyển sang tách service. Các task STEP-3 trên `main` vẫn là nháp: tách service chờ kết thúc STEP-2; nhánh Media còn chờ S1-07 và hợp đồng phiên tương ứng. Backend STEP-4 chờ STEP-3. Phần chuẩn bị S3/S4 đang được đề xuất riêng ở PR #39, chưa coi là đã công bố khi PR chưa merge.

## Ai giữ phần nào

| Repo (file này) | Trello |
|---|---|
| Tiêu đề, mô tả: người làm đề xuất, đầu ra, tiêu chí xong, phụ thuộc; thêm nhãn | Cột trạng thái, người được gán, hạn, bình luận, checklist; gỡ nhãn |

- Đồng bộ **một chiều** từ repo lên Trello. Sửa mô tả thẻ trên Trello sẽ bị ghi đè ở lần đồng bộ sau; muốn đổi nội dung thì sửa file và push.
- Mặc định script **không** chuyển cột; không gán người, không đổi hạn, không xóa hay lưu trữ thẻ. Chỉ thao tác thủ công với danh sách `complete` mới chuyển các task đã có kết quả hoàn tất trong repo. Thẻ bị bỏ khỏi file chỉ được liệt kê trong log.
- Thẻ mới vào cột cấu hình ở `TRELLO_LIST_ID` (nên là cột "Backlog"), có nhãn theo bước (ví dụ `STEP-1`).
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
- Thẻ việc phát sinh, nhỏ (sửa lỗi, việc vặt) tạo thẳng trên Trello, không cần đưa vào đây. Thư mục này chỉ giữ các thẻ thuộc lộ trình.

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

Kiểm tra công cụ bằng `python3 -m unittest discover -s scripts/tests -p 'test_sync_trello.py'`. Cách cập nhật thẻ dựa trên [API Trello chính thức](https://developer.atlassian.com/cloud/trello/rest/api-group-cards/).

## Cài đặt một lần (người quản trị repo)

1. Lấy API key và token Trello tại trang Power-Up admin của Trello. Token chỉ cần quyền đọc, ghi trên bảng của nhóm.
2. GitHub repo → Settings → Secrets and variables → Actions:
   - **Secrets:** `TRELLO_KEY`, `TRELLO_TOKEN` (có thể đã có từ workflow cũ).
   - **Variables:** `TRELLO_BOARD_ID` (đoạn mã sau `trello.com/b/` trong link bảng) và `TRELLO_LIST_ID` (ID cột nhận thẻ mới).
3. Lấy ID cột: mở `https://trello.com/b/<board>.json` khi đã đăng nhập, tìm tên cột trong mục `lists`; hoặc gọi API `GET /1/boards/<board>/lists`.
4. Chạy lần đầu: tab Actions → "Sync tasks to Trello" → Run workflow. Xem log để biết thẻ nào được tạo.
