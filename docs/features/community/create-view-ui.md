# SCDC — Nghiệm thu giao diện tạo và xem cộng đồng

Cập nhật: 2026-10-07. Người dùng đã duyệt bước 5. Giao diện và kiểm chứng nằm trên `feat/community-create-view`; tài liệu được commit riêng trên `main`. Code chưa merge vào `main`.

## Hành vi bàn giao

WebClient dùng bốn API của [backend bước 4](create-view-backend.md). Sau đăng nhập, người dùng thấy danh sách cộng đồng có membership active của mình. Form tạo nhận tên, mô tả và public/private; tạo thành công mở chi tiết, tải lại danh sách từ API và giữ đường dẫn `#community/{id}` qua reload. Không tạo phòng mặc định. Chi tiết hiển thị metadata, tư cách của chính người dùng và quyền quản lý hiệu lực do API trả về; quyền hiện tại chỉ được hiển thị, chưa có giao diện quản lý role/ACL.

Có trạng thái đang tải, rỗng, lỗi và nút tải lại. Danh sách dùng cursor của API với limit 20; lỗi cursor yêu cầu người dùng tải lại từ đầu. Server ID, owner, membership và permissions không được suy ra từ dữ liệu mẫu. Người ngoài public chỉ thấy summary; private không đủ quyền không hiển thị metadata. Refresh detail xóa dữ liệu cũ trước khi kiểm tra lại quyền. Chuyển tài khoản hoặc đăng xuất unmount state theo actor, hủy request và bỏ response đến muộn.

Tên và mô tả dùng cùng quy tắc với [thiết kế](create-view-design.md): White_Space cố định Unicode 17, giới hạn UTF-16, từ chối Unicode lỗi/NUL, tên một dòng, CRLF/CR → LF cho mô tả. Không trim mô tả hoặc đổi NFC. Kiểm thử frontend đối chiếu fixture `docs/fixtures/community-operations.json` và `docs/fixtures/text-policy.json`; backend vẫn là nơi quyết định validation và quyền.

Yêu cầu tạo gồm UUIDv4 và payload chuẩn hóa được ghi vào `sessionStorage` theo actor **trước POST**. Sau mất mạng, timeout 15 giây, 401, 409 hoặc lỗi máy chủ, khóa và nội dung giữ nguyên, form khóa sửa. Reload hiển thị “Tiếp tục yêu cầu”; chỉ nút “Kiểm tra và thử lại” gửi lại cùng operation. Không tự POST lại sau refresh token, reload hoặc kết nối lại. Submit đang chạy chặn nhấn hai lần. Đóng form hủy request phía trình duyệt và giữ draft; không coi thao tác đóng là hủy giao dịch máy chủ. Storage bị chặn không gửi POST; bản lưu hỏng không bị âm thầm thay thế. Lỗi validation 400 giải phóng operation và giữ nội dung form cho người dùng sửa. Chỉ xóa bản lưu khớp operation sau khi xác nhận thành công.

SessionStorage giữ được khi reload trong tab hiện tại; đóng tab hoặc xóa dữ liệu trình duyệt có thể mất yêu cầu chưa xác nhận. Khi đó cần kiểm tra danh sách trước khi tạo mới. Draft của tài khoản A không được tái sử dụng cho B. Khi không có Web Locks, cơ chế Identity hiện có giữ session trong bộ nhớ từng tab; reload yêu cầu đăng nhập lại, rồi cùng actor có thể tiếp tục draft của tab.

Tin nhắn trực tiếp hiện có được giữ ở đích riêng với nhãn giao diện mẫu. Luồng Community không dùng server/phòng mẫu, không thử kết nối Hub chưa triển khai, không hiện trạng thái realtime thành công giả. Trang đăng nhập giới thiệu các chức năng đang có.

## Bằng chứng

API .NET 10 tại cổng 15026, PostgreSQL 18 trong container `scdc-community-step5-db`, database mới `scdc_community_step5_test` ở cổng 15432. Chỉ DB thử nghiệm được bootstrap; khóa và tài khoản tổng hợp. Vite tại cổng 15300 proxy sang API này. Backend không đổi ở bước 5; 63 ca Release của bước 4 được dẫn chiếu, không cộng vào số ca chạy frontend.

| Kiểm tra | Kết quả |
|---|---|
| Node/WebClient | **16 passed**, gồm 6 ca Identity session cũ, 4 ca actor-bound request/không tự replay POST, 6 ca canonical text/fixture/draft/UUID |
| Chromium với API và DB thật | **14 passed, 0 failed, 0 skipped** |
| Build/proxy production | Vite build và Dockerfile WebClient thành công với build context `clients/WebClient`; 4 ca trọng yếu qua nginx đạt, ca đóng form đang gửi trên image cuối cũng đạt |
| Dependency | Lockfile thêm Playwright 1.63.0; cập nhật riêng source-map-js 1.2.1 → 1.2.2; npm báo 0 vulnerability sau cập nhật |
| Đối chiếu PostgreSQL | Mỗi server trong ca mất response có đúng một operation và một `Community.ServerCreated.v1`; không có server thiếu owner active hoặc thiếu/thừa create event |

14 ca trình duyệt: đăng nhập → empty → private create → detail/list/reload; mất response sau commit và retry 200; local validation/server field errors; public outsider/private 404; 21 server phân trang và cursor lỗi; danh sách 503/recovery; đổi actor và draft isolation; POST 401 không replay; keyboard/focus/mobile; double submit/503; storage bị chặn/hỏng; response detail muộn và refresh mất quyền; đăng ký/xác minh development/login/public create bằng UI; đóng form đang gửi giữ được draft và focus.

Các ca thành công, mất response sau commit, quyền public/private, pagination, đổi actor và đăng ký đều gọi API thật. Các lỗi 400/401/503, cursor hết hạn, storage fault và snapshot mất quyền được tiêm chủ động vào trình duyệt để kiểm chứng cách xử lý UI. Riêng snapshot own-left/private là response fixture; chưa có leave API trong gói này. Quyền own-left thực tế, điều kiện tài khoản/phiên, race, transaction và các invariant đã được kiểm chứng độc lập ở backend bước 4. Không suy ra leave/rejoin/realtime đã hoạt động từ các ca UI.

Ảnh giao diện được tạo tại `artifacts/community-step5-detail.png`, output/trace nằm trong `artifacts/community-step5/` và được Git ignore. Không đưa session/token, dữ liệu DB hoặc key ring vào Git.

## Chạy lại

Chuyển sang `feat/community-create-view`. Chuẩn bị PostgreSQL thử nghiệm riêng, schema/migration đúng bước 4 và API trỏ vào DB đó; không dùng DB ứng dụng. API cần bật `Modules__Identity__ExposeDevelopmentTokens=true` trong môi trường Development để tạo tài khoản tổng hợp.

Với Node 24 và Chromium của [Playwright](https://playwright.dev/docs/test-cli):

```bash
cd clients/WebClient
npm ci
npm test
npm run build
npx playwright install chromium
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 npm run dev -- --port 15300 --strictPort
# Terminal khác, cùng clients/WebClient:
SCDC_E2E_URL=http://127.0.0.1:15300 \
SCDC_E2E_DATABASE=scdc_community_step5_test npm run test:e2e
```

`playwright.config.js` yêu cầu URL localhost và tên DB kết thúc `_test`. Tên DB này xác nhận cấu hình người chạy cung cấp; test không tự truy vấn connection string của API. Phải kiểm tra API đã trỏ vào DB thử nghiệm trước khi chạy. Test tạo tài khoản/server mới với tên riêng, không xóa dữ liệu để dọn DB ứng dụng.

Trong môi trường không có Node/Chromium trên host, dùng [image Playwright cùng phiên bản](https://playwright.dev/docs/docker) `mcr.microsoft.com/playwright:v1.63.0-noble`, mount repo, working directory `/repo/clients/WebClient`, network host cho localhost API. Trên Fedora có thể dùng `--security-opt label=disable` cho container kiểm thử để tránh thay nhãn SELinux của repo. Đây là cách đã chạy kiểm chứng ở bước 5.

## Git

- `2b9d51e` trên `feat/community-create-view`: actor-bound API, canonical text và draft idempotency cùng kiểm thử.
- `4203e05` trên `feat/community-create-view`: tích hợp create/list/detail, trạng thái UI, Playwright và kiểm chứng trình duyệt.
- Tài liệu này cùng cập nhật trạng thái được commit riêng trên `main`, rồi đồng bộ `docs/` sang feature; không merge code.

Push trong môi trường agent lỗi vì thiếu xác thực GitHub HTTPS. Người dùng đã chọn tự push hai nhánh. Sau khi nhận bàn giao, chạy từ repo:

```bash
git push origin feat/community-create-view
git push origin main
```

## Phạm vi đạt và bước tiếp

Gói đầu có bằng chứng backend và UI cho UC-COM-01, phần danh sách/detail/tư cách đã chọn của UC-COM-03, cùng projection summary public của route dùng chung. Chưa đánh dấu toàn bộ UC-COM-02/03 đạt: search/discovery, join/leave/rejoin, phòng/lịch sử, role/ACL, lời mời và realtime có gói kiểm chứng riêng.

Bước 6 chưa bắt đầu: đề xuất duyệt gói UC-COM-06 tham gia trực tiếp cộng đồng công khai trước, rồi mở rộng theo phụ thuộc trong [lộ trình](../community.md#use-case-delivery). Không bắt đầu gói mới khi chưa có ý kiến người dùng.
