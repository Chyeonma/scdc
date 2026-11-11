# Thẻ Trello theo bước

Mỗi file trong thư mục này chứa các thẻ Trello của một bước trong [lộ trình](../planning.md#roadmap). Khi file được push lên `main`, GitHub Actions tự tạo hoặc cập nhật thẻ trên bảng Trello của nhóm.

| File | Bước |
|---|---|
| [step-1.md](step-1.md) | STEP-1 — Khung chạy |

## Ai giữ phần nào

| Repo (file này) | Trello |
|---|---|
| Tiêu đề, mô tả: người làm đề xuất, đầu ra, tiêu chí xong, phụ thuộc | Cột trạng thái, người được gán, hạn, bình luận, checklist |

- Đồng bộ **một chiều** từ repo lên Trello. Sửa mô tả thẻ trên Trello sẽ bị ghi đè ở lần đồng bộ sau; muốn đổi nội dung thì sửa file và push.
- Script **không** chuyển cột, không gán người, không đổi hạn, không xóa hay lưu trữ thẻ. Thẻ bị bỏ khỏi file chỉ được liệt kê trong log để người dùng tự xử lý.
- Thẻ mới vào cột cấu hình ở `TRELLO_LIST_ID` (nên là cột "Backlog"), có nhãn theo bước (ví dụ `STEP-1`).
- Thẻ được nhận ra bằng tiền tố `[S1-01]` trong tên. **Không xóa tiền tố này trên Trello**, nếu không lần sau sẽ tạo thẻ mới.

## Định dạng

```markdown
# STEP-2 — Tên bước

Mô tả chung (không đồng bộ).

## S2-01 · Tiêu đề thẻ
- Người làm: ... · Review: ...
- Đầu ra: ...
- Xong khi: ...
- Phụ thuộc: ...
```

- Mã đầu tiên của dòng `#` thành nhãn Trello.
- Mỗi dòng `## <mã> · <tiêu đề>` là một thẻ; mọi dòng tới thẻ tiếp theo thành mô tả. Mã không được trùng.
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
