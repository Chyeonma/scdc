# SCDC — Thiết kế gói tìm kiếm cộng đồng

[Phạm vi](../delivery/search/plan.md) triển khai DEC-093 và search/summary của UC-COM-02. [Trạng thái](../status.md) phân biệt kế hoạch với bằng chứng đã chạy.

## Key tên và migration

`UnicodeTextPolicy.NormalizeNameKey` dùng trim theo text-policy → NFC → ToLowerInvariant. Không đổi tên hiển thị và không bỏ dấu. NFC chuẩn hóa các biểu diễn Unicode tương đương; ToLowerInvariant dùng casing invariant theo [.NET Normalize](https://learn.microsoft.com/en-us/dotnet/api/system.string.normalize?view=net-10.0), [.NET ToLowerInvariant](https://learn.microsoft.com/en-us/dotnet/api/system.string.tolowerinvariant?view=net-10.0).

Thêm `community.servers.search_name text COLLATE "C" NOT NULL`; không dùng `normalized_name` generated theo locale PostgreSQL làm key .NET. Create ghi key cùng tên trong transaction; writer sửa tên tương lai phải cập nhật cả hai. So sánh/sort key theo C để giữ dấu và thứ tự byte ổn định, theo [PostgreSQL collation](https://www.postgresql.org/docs/18/collation.html). Exact rank 0, partial rank 1, key tăng dần rồi UUID tăng dần.

Migration `002-community-search.sql` dùng temp mapping key do runner .NET tính theo batch tối đa 500 record. Cùng advisory lock ledger của 001 và table lock, kiểm tra prerequisite 001/checksum, validate tên theo policy, apply DDL/backfill trong transaction; không đổi tên/metadata version/accessVersion/epoch. Bootstrap DB trắng ghi ledger 001/002; seed có key explicit. Không đổi migration/checksum 001 đã áp dụng.

Apply migration 002 với writer cũ đã dừng/drain trước deploy writer mới: NOT NULL search_name khiến writer tạo server cũ không còn tương thích. API không tự migrate. Dữ liệu cần sửa được báo ID và rollback, không sửa âm thầm. Không chạy bootstrap có DROP SCHEMA lên DB cần giữ dữ liệu. Khi rollback app, giữ schema và dùng writer tương thích; không xóa cột/backfill.

Partial btree `(search_name,id)` chỉ public/active/nondeleted hỗ trợ filter/keyset; contains với leading wildcard vẫn có thể quét/sort nhiều kết quả. Gói chưa có workload/load proof, không hứa index này đủ cho mọi quy mô hoặc thêm pg_trgm trước khi đo.

## API và cursor

GET `/api/v1/servers/search`: q bắt buộc, trim rồi validate 2–100 UTF-16/Unicode hợp lệ/có nội dung/một dòng trước normalize; limit 1–50, mặc định 20. 200 `{items: ServerSummary[],nextCursor}`. Luôn sáu field summary, không owner/membership/quyền/phòng. Search không tạo membership/event.

LIKE dùng parameter và escape `%`, `_`, backslash, không coi user input là pattern/SQL theo [PostgreSQL LIKE](https://www.postgresql.org/docs/18/functions-matching.html). Một SQL statement lọc public/active/nondeleted và keyset `(rank,key,id)`, lấy limit+1; không OFFSET hoặc tải toàn bộ server lên RAM ứng dụng.

Cursor purpose `Community.Search.v1`, payload version/actor/queryKey/limit/lastRank/lastKey/lastId/expiry 24 giờ. Q khác cách viết nhưng cùng normalized key dùng được cursor; actor/queryKey/limit/purpose khác, tamper/expiry/position lỗi trả 400 CURSOR_INVALID. Key ring dùng chung cơ chế bền của list nhưng purpose riêng; lỗi storage trả 503 CURSOR_KEY_UNAVAILABLE.

Identity guard giữ user share lock/session lease trong shared work scope; đọc Community bằng một statement snapshot, kiểm tra lease trước hoàn tất read. Mỗi trang kiểm tra lại public/active; cursor không cấp quyền cố định. Public summary đã tải có thể cũ đến lần refresh; mở detail/join luôn kiểm tra trạng thái hiện hành, chưa có realtime xóa cache public.

Lỗi: 400 VALIDATION_FAILED theo q/limit; 401 middleware hoặc SESSION_INVALID; 403 ACCOUNT_ACCESS_DENIED; 400 CURSOR_INVALID; 503 ACCESS_CHECK_UNAVAILABLE/CURSOR_KEY_UNAVAILABLE/COMMUNITY_TEMPORARILY_UNAVAILABLE. Không chuyển dependency failure thành danh sách rỗng.

## WebClient

Nút Khám phá từ own list mở `#discover`; submit hợp lệ lưu q trong hash URL `#discover?q=...`, chỉ GET. Empty query hiển thị hướng dẫn, không gọi API search. Input validate cùng text-policy/UTF-16 trước submit, không cắt hoặc lowercase tên người dùng nhập.

Kết quả có tên/mô tả/visibility/joinMode và nút Xem. Mở detail giữ đích quay lại kết quả/cùng từ khóa; public/immediate dùng nút join của gói trước, approval chỉ giải thích trạng thái tới khi có requests. Own list được tải lại sau join.

Query/actor/route đổi abort request và bỏ response cũ; state kết quả gắn query đã submit và actor. Pagination dedup UUID. Lỗi page/cursor bỏ kết quả cũ, cho tải lại từ trang đầu; không ghép trang của hai query/cursor. Reload URL tự đọc lại GET từ trang đầu; không tự POST join. Browser không giữ kết quả vào storage.
