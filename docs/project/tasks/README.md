# Thẻ Trello theo bước

Mỗi file trong thư mục này chứa các thẻ Trello của một bước trong [lộ trình](../planning.md#roadmap). Khi file được push lên `main`, GitHub Actions tự tạo hoặc cập nhật thẻ trên bảng Trello của nhóm.

| File | Bước |
|---|---|
| [step-1.md](step-1.md) | STEP-1 — Khung chạy |
| [step-2.md](step-2.md) | STEP-2 — Tách ranh giới trong monolith (nháp, chưa đồng bộ) |
| [step-3.md](step-3.md) | STEP-3 — Tách từng service (nháp, chưa đồng bộ) |
| [step-4.md](step-4.md) | STEP-4 — Tính năng trên kiến trúc mới (nháp, chưa đồng bộ) |
| [step-4-early.md](step-4-early.md) | Việc của STEP-4 làm được ngay, ai rảnh nhận (đồng bộ) |

## Ai giữ phần nào

| Repo (file này) | Trello |
|---|---|
| Tiêu đề, mô tả: người làm đề xuất, đầu ra, tiêu chí xong, phụ thuộc; thêm nhãn | Cột trạng thái, người được gán, hạn, bình luận, checklist; gỡ nhãn |

- Đồng bộ **một chiều** từ repo lên Trello. Sửa mô tả thẻ trên Trello sẽ bị ghi đè ở lần đồng bộ sau; muốn đổi nội dung thì sửa file và push.
- Script **không** chuyển cột, không gán người, không đổi hạn, không xóa hay lưu trữ thẻ. Thẻ bị bỏ khỏi file chỉ được liệt kê trong log để người dùng tự xử lý.
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

## Cài đặt một lần (người quản trị repo)

1. Lấy API key và token Trello tại trang Power-Up admin của Trello. Token chỉ cần quyền đọc, ghi trên bảng của nhóm.
2. GitHub repo → Settings → Secrets and variables → Actions:
   - **Secrets:** `TRELLO_KEY`, `TRELLO_TOKEN` (có thể đã có từ workflow cũ).
   - **Variables:** `TRELLO_BOARD_ID` (đoạn mã sau `trello.com/b/` trong link bảng) và `TRELLO_LIST_ID` (ID cột nhận thẻ mới).
3. Lấy ID cột: mở `https://trello.com/b/<board>.json` khi đã đăng nhập, tìm tên cột trong mục `lists`; hoặc gọi API `GET /1/boards/<board>/lists`.
4. Chạy lần đầu: tab Actions → "Sync tasks to Trello" → Run workflow. Xem log để biết thẻ nào được tạo.
