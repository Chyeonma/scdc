# Hồ sơ Community — direct-join

Hồ sơ giữ phạm vi được chọn và kết quả kiểm chứng tại revision ghi trong từng phần. Những mô tả nhánh, thiết kế chưa triển khai hoặc chưa hợp nhất phản ánh thời điểm bàn giao. Quy tắc hiện hành và trạng thái theo khả năng ở [Community](../../../features/community/README.md); không suy bằng chứng cũ thành kết quả của code mới.

## Phạm vi tại thời điểm triển khai

Bước 6 được người dùng cho phép tiếp tục ngày 2026-10-07. Gói đầu chọn đường public/immediate của [UC-COM-06](../../../features/community/memberships.md#uc-com-06), kế thừa `feat/community-create-view` trên nhánh `feat/community-join`.

<a id="scope-phạm-vi"></a>

### Phạm vi

- Mở URL `/#community/{serverId}`, xem public summary và chủ động bấm Tham gia.
- Kiểm tra Identity và server trong transaction; tạo membership active hoặc trả active membership hiện hành khi gọi lặp.
- Left → active dùng membershipId mới, tăng version, dọn role/ngoại lệ cá nhân cũ; chỉ có quyền mặc định.
- Membership, server accessVersion và outbox commit cùng nhau; UI đọc lại detail/own list sau thành công.
- Kết quả không rõ được đối soát bằng GET trước lần thử tiếp; không tự POST sau 401, timeout, reload hoặc đổi actor.

Search/discovery, requests chờ duyệt, leave/transfer, invitations, quản lý role/phòng và realtime thuộc gói sau. Chưa có bảng/writer pending request nên gói này không có pending để kết thúc. Khi bổ sung requests, mọi đường join phải đóng pending trong transaction theo đặc tả Memberships.

Contract mục tiêu có 200/202; runtime chỉ triển khai public/immediate. Người chưa active gặp approval nhận 409 `JOIN_APPROVAL_REQUIRED`, không tạo membership/request; UI đọc lại mode và giải thích cần được duyệt.

<a id="scope-tiêu-chí-kiểm-chứng"></a>

### Tiêu chí kiểm chứng

| Nội dung | Bằng chứng cần có |
|---|---|
| Join mới/lặp | 200 Membership của actor; một epoch active/event/accessVersion; list/detail đúng sau commit |
| Đồng thời | Barrier và khóa server: hai transaction trả cùng membership, một lần ghi |
| Trạng thái hiện hành | Private/inactive/deleted/unknown 404; approval không ghi; mode/visibility đổi trong lúc đợi khóa được đọc lại |
| Identity | Anonymous/unverified/inactive/revoked bị chặn; expiry trước commit rollback |
| Nguyên tử | Fault outbox rollback membership/cleanup/server/event; timeout không tự replay |
| Rejoin | Fixture left có grant cũ: ID mới, version tăng, grant cũ bị dọn; không suy ra leave API đã có |
| WebClient | API thật: preview → join → detail/list/reload; mất response đối soát GET; double click, lỗi, đổi actor/response muộn |
| Hồi quy | Backend Release, Node, Chromium, build WebClient; docs-check và diff sạch |

[TC-COM-03](../../../features/community/memberships.md#tc-com-03) kiểm chứng phần join/lặp/đồng thời. [AC-COM-02](../../../features/community/memberships.md#ac-com-02) còn đường search; [TC-COM-16/22](../../../features/community/memberships.md#tests) còn leave, mode khác, writer role/ACL và lịch sử phòng; không đóng toàn bộ các tiêu chí này.

[Thiết kế](../../../features/community/memberships.md) · [Trạng thái](../../../features/community/README.md).

## Kết quả theo revision

Ngày kiểm chứng: 2026-10-07. Phạm vi [public/immediate](direct-join.md), [thiết kế](../../../features/community/memberships.md). Hồ sơ gắn revision đã thử; [các chủ đề Community](../../../features/community/README.md) ghi tiến độ hiện tại.

| Phần | Commit trên feat/community-join | Bằng chứng |
|---|---|---|
| Backend | `e5b5750`, kế thừa gói tạo/xem | 81 ca Release đạt, gồm 18 ca join/rejoin mới |
| WebClient | `e0cf116`, cùng backend trên | 16 ca Node, 24 ca Chromium (10 ca join mới), build production đạt |

<a id="results-hành-vi-bàn-giao"></a>

### Hành vi bàn giao

- POST `/api/v1/servers/{id}/join` không body, actor từ JWT. Public/immediate tạo membership active/default; active retry trả cùng membership, không tăng version hoặc ghi event thêm.
- Identity guard và server FOR UPDATE giữ tới commit. Membership, server accessVersion và một Community.MembershipJoined.v1 commit nguyên tử; metadata version do trigger tăng. Event lưu bền với published_at NULL, chưa có dispatcher/Hub.
- Fixture left rejoin có membershipId mới/version tăng; member_roles, channel_user_overrides và nickname/timeout/inviter cũ được dọn cùng transaction. Không thêm migration ngoài baseline 001.
- Private/inactive/deleted/unknown nhận 404; nonmember trên approval nhận 409 JOIN_APPROVAL_REQUIRED, không tạo membership hoặc request. Active public retry vẫn nhận tư cách hiện hành khi mode đã đổi sang approval.
- URL `/#community/{id}` mở preview public; người dùng chủ động bấm Tham gia. Sau commit UI đọc lại detail/own list, hiển thị tư cách thành viên và quyền mặc định; reload vẫn đọc được kết quả.
- POST không tự replay. Double click gửi một request. Mất response/401/503/timeout 15 giây yêu cầu Kiểm tra kết quả bằng GET trước lần thử tiếp. Đổi actor/route bỏ response cũ; lỗi đọc detail sau commit được phục hồi bằng GET.

<a id="results-kiểm-chứng-backend"></a>

### Kiểm chứng backend

PostgreSQL 18 trong container riêng `scdc-community-step6-db`, DB `scdc_community_step6_test`, cổng 15432. Chỉ DB thử nghiệm được bootstrap; migration tests tạo/drop DB fixture riêng kết thúc `_test`. Khóa và tài khoản tổng hợp, không dùng DB ứng dụng.

Lệnh cuối:

```bash
# Đặt ConnectionStrings__Database tới DB thử nghiệm và cấu hình Identity thử nghiệm.
dotnet test SCDC.slnx --configuration Release --no-restore -m:1 \
  --logger 'trx;LogFileName=community-step6.trx' \
  --results-directory artifacts/community-step6/backend
```

**81 passed, 0 failed, 0 skipped**; build Release 0 warning/error. 63 ca hồi quy gói trước và 18 ca mới:

- Join/default permissions, list/detail, event payload và active retry qua factory restart.
- Private/inactive/deleted/unknown, approval và body giả actor bị chặn.
- Rejoin fixture với grant/override/field legacy cũ; membershipId mới và không có quyền quản lý cũ.
- Account unverified/inactive/revoked bị chặn (middleware có thể trả 401 cho inactive; guard trả 403 khi account không đủ điều kiện).
- Hai transaction có barrier ở outbox: join thứ hai đợi server lock, sau đó trả epoch đã commit; chỉ một event/accessVersion tăng một lần.
- Đổi visibility/joinMode trong transaction giữ server lock: join đợi rồi đọc lại trạng thái đã commit, không dùng snapshot trước khóa.
- Fault ở outbox rollback cả cleanup/epoch/server versions; expiry trước commit rollback; lock timeout trả 503 và không tự replay.
- Overflow accessVersion/membership version dừng trước ghi; logout đợi Identity guard rồi token cũ bị chặn.

<a id="results-kiểm-chứng-webclient"></a>

### Kiểm chứng WebClient

API Release tại `127.0.0.1:15026`, Vite tại `127.0.0.1:15300` proxy về API trên, cùng DB thử nghiệm. Dùng Node 24 và image Playwright `mcr.microsoft.com/playwright:v1.63.0-noble`.

| Kiểm tra | Kết quả |
|---|---|
| Node `npm test` | 16/16 ca hồi quy đạt |
| Chromium `npm run test:e2e` | 24/24 đạt; 14 create/view và 10 join mới |
| Vite `npm run build` | Build production thành công |
| Đối chiếu DB sau các suite | 0 event join trùng, 0 event sai epoch active, 0 event join bị đánh dấu published |

10 ca mới: preview → join/detail/list/reload trên mobile; mất response sau commit và đối soát GET; hai click; 401; 503; read lỗi sau commit; response muộn khi đổi actor; private không lộ tên/nút join; mode approval thay đổi; timeout 15 giây. Ca join thành công, retry, lost response, actor switch và read-after-commit dùng API thật. 401/503, approval đổi mode và lỗi read được tiêm response để kiểm chứng UI; mode/visibility race thực tế được chứng minh riêng ở backend.

Ảnh `artifacts/community-step6-joined.png`, TRX và browser output ở `artifacts/community-step6/` được Git ignore. Không commit token, DB hoặc key ring. Sau kiểm chứng, dừng API/WebClient và xóa container/thư mục khóa thử nghiệm.

<a id="results-chạy-lại-và-giới-hạn"></a>

### Chạy lại và giới hạn

Trên `feat/community-join`, chuẩn bị PostgreSQL thử nghiệm đã bootstrap/migrate 001. Theo [README module](../../../../services/Modules/Community/README.md) trên nhánh code để cấu hình HMAC, key ring và Identity. API Development cần ExposeDevelopmentTokens để tạo tài khoản tổng hợp.

```bash
cd clients/WebClient
npm test
npm run build
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 npm run dev -- --port 15300 --strictPort
# Terminal khác, cùng thư mục:
SCDC_E2E_URL=http://127.0.0.1:15300 SCDC_E2E_DATABASE=scdc_community_step6_test \
  npm run test:e2e -- --output ../../artifacts/community-step6/browser
```

Phải kiểm tra API thực sự trỏ vào DB thử nghiệm; biến E2E_DATABASE chỉ là xác nhận của người chạy. Các tài khoản/server thử có ID riêng; không dọn DB ứng dụng để chạy test.

Đạt phần public/immediate của UC-COM-06 và join/lặp/đồng thời TC-COM-03. AC-COM-02 còn đường tìm kiếm; UC-COM-06 còn phối hợp pending request và mở phòng; AC-COM-35/TC-COM-16/22 còn leave, mode khác, writer role/ACL theo epoch và lịch sử phòng. Rejoin fixture không chứng minh endpoint leave hay toàn bộ chống ABA. Chưa đo tải/contended server capacity, chưa chạy join trên image production hoặc nghiệm thu realtime. Code chưa merge main; người dùng tự push các nhánh.
