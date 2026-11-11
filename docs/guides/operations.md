<a id="scdc--hướng-dẫn-thao-tác-vận-hành"></a>

# SCDC — Hướng dẫn vận hành (runbook)

Trang này gồm các quy trình vận hành: triển khai, rollback, xử lý sự cố (API, email, media), backup và khôi phục, dọn dữ liệu, khóa tài khoản, lịch công việc và bàn giao.

**Trạng thái (2026-10-13, nền `900e226`):** đây là runbook để chuẩn bị và diễn tập. Chưa có cấu hình production, công cụ backup hay kết quả diễn tập. Chưa có lệnh production nào chạy được nguyên văn. Backup và khôi phục để [sau v1](../project/overview.md#backup-scope).

Điều kiện phát hành xem [nghiệm thu](../releases/acceptance.md#release-gates); chính sách dữ liệu xem [vòng đời dữ liệu](../system/data-lifecycle.md#policy).

<a id="mục-lục"></a>

<a id="inputs"></a>

<a id="1-đầu-vào-trước-khi-thao-tác"></a>

## 1. Trước khi thao tác

Mỗi lần thao tác cần: người thực hiện đã được cấp quyền, môi trường đích, bản chạy, phạm vi ảnh hưởng và hồ sơ thay đổi hoặc sự cố. Chưa chọn người trực, người thay thế, lịch trực. Được phân công phát triển không có nghĩa được quyền vận hành production.

| Cần có | Ghi vào hồ sơ |
|---|---|
| Môi trường | Tên, domain hoặc region, tài nguyên, định danh DB; staging tách khỏi production |
| Bản chạy | Commit, checksum hoặc digest của Web và API, schema và migration, phiên bản media nếu có |
| Cấu hình | Phiên bản cấu hình (không chứa bí mật); tham chiếu kho bí mật và ID khóa, không có giá trị |
| Truy cập | Người thực hiện, vai trò, tài khoản kỹ thuật đúng phạm vi, người thay thế |
| Quay lại | v1: bản trước, bảng tương thích schema, khóa còn dùng được. Sau v1: thêm backup và WAL hợp lệ, bằng chứng khôi phục |
| Điều khiển | Cách đóng và mở cổng vào, ngừng ghi theo phạm vi, tạm dừng email, outbox, cho vào media, chặn node media |
| Quan sát | Dashboard, nguồn thời gian, trace hoặc error ID, cảnh báo, nơi lưu hồ sơ có giới hạn truy cập |
| Lệnh thật | Manifest, đường dẫn, lệnh đã diễn tập; chưa có thì ghi "Chưa có" |

<a id="đối-chiếu-repo-hiện-tại"></a>

### Repo hiện có gì

| Hiện có | Giới hạn |
|---|---|
| [compose.yaml](../../compose.yaml): Web, API, PostgreSQL | Chỉ cho phát triển: mở cổng DB, mật khẩu trên máy, SQL init và seed; chưa có HTTPS, kho bí mật, media |
| [Dockerfile API](../../services/SCDC.Api/Dockerfile), [Dockerfile Web](../../clients/WebClient/Dockerfile) | Có build; chưa có manifest production hay bản build khóa digest |
| [HealthController](../../services/SCDC.Api/Controllers/HealthController.cs) | Không kiểm tra DB hay worker; HTTP 200 không chứng minh ứng dụng dùng được |
| [Nginx](../../clients/WebClient/nginx.conf): `/healthz`, proxy API và hub; tắt access log cho `/hubs/` | `/healthz` chỉ trả chuỗi tĩnh; cần kiểm tra log trên production không lộ query hay token |
| [Program.cs](../../services/SCDC.Api/Program.cs), [IdentityModule](../../services/Modules/Identity/IdentityModule.cs) | Swagger bật khi Development; cần cấu hình proxy, HTTPS, CORS |
| [appsettings.json](../../services/SCDC.Api/appsettings.json) | Connection string và signing key trống; `ExposeDevelopmentTokens` mặc định false nhưng chưa có kiểm tra cấm bật ngoài Development |
| Nền có Identity, Community, DM (ghi tin, lịch sử, sửa, xóa, Hub, outbox, kết nối lại) | Chưa có tin phòng, realtime Community, Media, worker dọn dữ liệu; worker email ở nhánh riêng. Bằng chứng: [hồ sơ 2026-10-13](../records/verification/messaging/merge-2026-10-13.md) |

Trên production phải kiểm tra cấu hình thật sự được nạp: đúng môi trường, signing key riêng, token Development tắt, origin hợp lệ, domain link email đúng, DB không mở cho trình duyệt. Kiểm tra qua response và log đã lọc; không dump toàn bộ biến môi trường hay connection string. Cấu hình chi tiết: [hướng dẫn phát triển](development.md#configuration).

Lệnh chỉ để quan sát **stack phát triển trên máy** (từ thư mục gốc repo):

```bash
docker compose ps
curl --fail --silent --show-error http://localhost:5026/api/v1/health
curl --fail --silent --show-error http://localhost:3000/healthz
```

Hai endpoint chỉ cho biết tiến trình và proxy còn sống. Muốn kiểm tra DB và hành trình thì dùng tài khoản thử.

<a id="deploy"></a>

<a id="2-rb-deploy--triển-khai-bản-mới"></a>

## 2. RB-DEPLOY — Triển khai bản mới

Làm offline theo [chính sách cập nhật](../system/environments.md#v1-offline-updates): ngừng phục vụ trong lúc thay đổi và kiểm tra, mở lại khi đạt.

**Cần có:** [hồ sơ phát hành](../records/templates/release-record.md), bản build đã khóa, người duyệt, kế hoạch migration và rollback, thời gian thao tác.

**Dừng lại nếu:** chưa đủ điều kiện phát hành, hoặc không biết schema cũ tương thích tới đâu.

1. **Đối chiếu:** đúng môi trường và bản build đã duyệt; ghi schema và build trước khi đổi; kiểm tra dữ liệu và key ring còn sau khởi động lại. (Sau v1: kiểm tra thêm backup, WAL, checkpoint sổ bảo vệ, quyền truy cập khóa.)
2. **Diễn tập trên staging** với migration thật; chạy [kiểm tra nhanh](../releases/acceptance.md#smoke); kiểm tra bí mật không lộ trong response hay log; key ring còn dùng được sau khởi động lại. Ghi lệnh và mã thoát, không ghi token.
3. **Đóng:** xác nhận thời điểm và người thực hiện. Đóng cổng vào, ngừng phục vụ; dừng an toàn API, worker, job đang ghi; chờ transaction đang chạy xong. Giữ DB và thành phần cần cho migration; giữ volume và khóa. Media ngừng cấp mới; kết thúc hay chặn phiên bị ảnh hưởng; chưa trả chỗ khi node cũ còn truyền.
4. **Migration và triển khai:** chạy migration đã duyệt một lần; rồi triển khai bản build và cấu hình. Lỗi thì xác định trạng thái thật; không chạy lại migration chưa rõ kết quả, không tạo lại schema từ SQL init.
5. **Kiểm tra:** schema, build, kết nối DB, worker, proxy, API, hub, email, cho vào media. Chạy kiểm tra nhanh bằng tài khoản thử; xem lỗi, hàng đợi tồn, quyền; ghi bằng chứng.
6. **Mở lại** khi kiểm tra đạt và người có thẩm quyền xác nhận. Đề xuất theo dõi kỹ 30 phút đầu (không thay kiểm thử tải 60 phút).
7. **Ghi kết quả:** thời gian ngừng, migration, quyết định, người tiếp nhận. Không đạt thì giữ phần ảnh hưởng đóng và chuyển sang RB-ROLLBACK hoặc runbook sự cố.

**Xong khi:** đúng bản build, kiểm tra nhanh đạt, quyền, token, epoch đúng, hàng đợi xử lý được, hồ sơ đã bàn giao. Triển khai thành công không có nghĩa được mở đăng ký công khai; đó là quyết định riêng.

<a id="rollback"></a>

<a id="3-rb-rollback--quay-lại-bản-ứng-dụng-trước"></a>

## 3. RB-ROLLBACK — Quay lại bản trước

Làm offline. Nếu đã chuyển dữ liệu MVP sang v1 và v1 đã có ghi mới, phải xử lý phần chênh lệch trước khi nghĩ tới quay về nguồn cũ ([D52](../system/database.md#v1-mvp-data-cutover)).

**Khi nào:** kiểm tra nhanh quan trọng thất bại, truy cập trái quyền, dữ liệu sai, lỗi tăng sau triển khai. Người quyết định rollback phải có tên trong kế hoạch.

1. Đóng phần ghi và truy cập bị ảnh hưởng; ghi mốc tốt cuối cùng và bản hiện tại. Giữ bằng chứng đã lọc; đánh dấu thao tác chưa rõ kết quả; không tự chạy lại.
2. Đối chiếu bảng tương thích ứng dụng, schema, khóa, thế hệ. Nếu bản cũ đọc ghi đúng schema mới và vẫn giữ được thu hồi, mốc, kiểm soát vào: dùng đường rollback đã diễn tập.
3. Nếu schema đã đổi không tương thích, hoặc bản cũ không giữ được bảo vệ hiện tại: giữ đóng và sửa tiến (forward fix) theo phương án đã duyệt. Không tự đảo migration hay khôi phục DB chỉ để quay về giao diện cũ.
4. Triển khai bản cũ nhưng giữ key ring, dấu chặn và quyền hiện tại. Media: chặn thế hệ cũ; không bỏ kiểm soát vào và hạn mức để phiên cũ chạy.
5. Chạy kiểm tra nhanh; kiểm tra dữ liệu đã commit trong lúc bản lỗi chạy; thử token và cursor cũ. Mở lại khi có kết quả và xác nhận.

**Xong khi:** hệ thống chạy, không hạ version hay quyền, không hồi sinh nội dung đã xóa, các thao tác chưa rõ đã được đối chiếu. Khôi phục DB (sau v1) là thao tác riêng, có thể mất dữ liệu; phải ghi phạm vi mất và người chấp nhận.

<a id="service-incident"></a>

<a id="4-rb-service--api-db-hoặc-chat-gặp-sự-cố"></a>

## 4. RB-SERVICE — Sự cố API, DB hoặc chat

1. Lập [hồ sơ sự cố](../records/templates/incident-record.md): thời điểm bắt đầu ảnh hưởng, phát hiện, xác nhận; phân [mức](../releases/acceptance.md#defects). Xem cả Web, API, DB, worker; không chỉ nhìn health 200.
2. Kiểm tra lần triển khai gần nhất, request và trace ID, tỷ lệ lỗi, độ trễ, kết nối, khóa và đĩa của DB, tuổi outbox chưa xử lý. Dùng số lượng và ID; không lấy nội dung DM vào log điều tra.
3. Lỗi sau triển khai: theo RB-ROLLBACK. DB không dùng được: ngừng ghi; không báo tin đã gửi khi chưa commit; giữ chống trùng khi client gửi lại. Không xóa WAL hay volume để lấy chỗ trống.
4. Realtime lỗi nhưng lưu trữ vẫn chạy: ghi rõ phần nào bị ảnh hưởng; theo dõi outbox; thử kết nối lại và bù trang bằng tài khoản thử. GET thấy tin không có nghĩa realtime đạt.
5. Khôi phục phụ thuộc và worker theo cấu hình đã diễn tập. Đối chiếu ID thao tác, ID tin, version để xử lý kết quả chưa rõ; worker phát lại sự kiện, không gửi lại tin.
6. Kiểm tra các hành trình bị ảnh hưởng và thu hồi quyền; mở lại khi xác nhận. Ghi nguyên nhân, thời gian ảnh hưởng, dữ liệu thiếu (nếu có), hành động ngăn lặp.

**Xong khi:** gửi, lưu, lịch sử, realtime chạy; không mất, không trùng, không vượt quyền; hàng đợi đã đối chiếu; có bằng chứng.

<a id="email-incident"></a>

<a id="5-rb-email--email-xác-minhkhôi-phục-lỗi-hoặc-bị-chậm"></a>

## 5. RB-EMAIL — Email xác minh hay khôi phục lỗi hoặc chậm

1. Kiểm tra worker, delivery, biên nhận của nhà cung cấp, thử lại, lease, tuổi và hạn token. Phân biệt ba mức: API đã nhận yêu cầu, nhà cung cấp đã nhận thư, hộp thư thử đã nhận.
2. Kiểm tra domain link, cấu hình nhà cung cấp, quyền khóa; không ghi envelope, token hay email người thật vào hồ sơ. Dùng tài khoản thử, ghi ID delivery và mã lỗi đã lọc.
3. Thư hay token đã hết hạn hoặc bị thay: kết thúc delivery, dọn envelope. Không gửi lại thư cũ, không tạo link mới bỏ qua thời gian chờ.
4. Không rõ đã giao chưa: đối chiếu biên nhận và thử lại theo thiết kế worker. Thư lặp không làm token dùng được nhiều lần.
5. Sau khi sửa: thử đăng ký → xác minh và quên mật khẩu → đặt lại ngoài Development; thử link cũ, dùng lại, phiên đã thu hồi. Không chuyển sang trả token Development để né sự cố.

**Xong khi:** email đúng môi trường và mục đích, có biên nhận và hộp thư thử đã nhận, link đúng hiệu lực, không lộ token. Mất quyền truy cập email vẫn không có khôi phục thủ công (DEC-067).

<a id="media-incident"></a>

<a id="6-rb-media--media-quota-hoặc-thu-hồi-quyền-lỗi"></a>

## 6. RB-MEDIA — Sự cố media, hạn mức hoặc thu hồi

Runbook này chưa thi hành được trên production: extension hay fork LiveKit và cơ chế chặn node chưa có bản build và bằng chứng.

1. Ghi ID phòng, cuộc gọi, lượt tham gia; build node; thế hệ; trạng thái chỗ và lease; mốc commit thu hồi. Không ghi JWT, SDP nhạy cảm hay nội dung âm thanh, màn hình.
2. Không xác nhận được quyền, hoặc media vẫn chạy quá hạn 5 giây: dừng cho vào, phát, nhận của phần liên quan qua đường điều khiển đã diễn tập. RoomService không làm được thì chặn node hay mạng theo thiết kế; đóng websocket không đủ.
3. Giữ chỗ (giữ chỗ, kết nối lại, đang thoát) cho tới khi có bằng chứng đã dừng hẳn. Không xóa lượt tham gia hay trả chỗ khi node cũ còn truyền.
4. Kiểm tra signaling qua admission, lease, kiểm soát hạn mức, TURN và ICE, lệch đồng hồ, đường truy cập trực tiếp. Đổi firewall theo manifest đã chọn ([cổng LiveKit](https://docs.livekit.io/transport/self-hosting/ports-firewall/): kiểm tra riêng signaling, ICE, TURN).
5. Sau khi xử lý: thử token và epoch cũ, token đã làm mới đều không vào được; người còn quyền vào bằng lượt mới, nghe được, bật từng nguồn. Đo thật lúc bên gửi và nhận ngừng; không suy từ giao diện.

**Xong khi:** không còn media trái quyền, không vượt 10, 20, 2; hạn mức khớp thực tế.

<a id="restore"></a>

<a id="7-rb-restore--backup-và-khôi-phục-dữ-liệu"></a>

## 7. RB-RESTORE — Backup và khôi phục (sau v1)

Toàn bộ mục này để [sau v1](../project/overview.md#backup-scope) (DEC-156). Chưa có công cụ hay diễn tập; không phải điều kiện của v1.

<a id="kiểm-tra-backup"></a>

### Kiểm tra backup

- Base backup đã xong và đã kiểm tra; WAL liên tục; catalogue và timeline; checksum; tuổi gốc; quyền truy cập kho.
- Đo điểm mới nhất thật sự khôi phục được, không chỉ lúc job báo thành công.
- Có base mới hợp lệ trước khi base cũ hết tuổi. Không đặt lại tuổi khi sao chép; không xóa WAL mà base hợp lệ còn cần.
- Base và WAL khôi phục cả cluster; dump logic không thay được ([PITR](https://www.postgresql.org/docs/18/continuous-archiving.html)). Giữ riêng cấu hình và khóa cần để khôi phục. Đặt `archive_timeout` chưa chứng minh RPO; phải cộng thời gian chép WAL và hàng đợi ([WAL](https://www.postgresql.org/docs/18/runtime-config-wal.html)).

<a id="diễn-tập-hoặc-xử-lý-sự-cố"></a>

### Diễn tập hoặc xử lý sự cố

1. Lập hồ sơ: môi trường, mốc ảnh hưởng, phát hiện, xác nhận; điểm khôi phục dự kiến; người quyết định. Sự cố thật: chặn ghi mới, giữ bằng chứng; khôi phục vào môi trường riêng.
2. Chọn base và WAL hợp lệ; kiểm tra cấu hình, khóa, checkpoint sổ bảo vệ. Ghi khoảng thời gian thật sự khôi phục được (backup giữ 30 ngày không có nghĩa khôi phục được mọi điểm trong 30 ngày).
3. Khôi phục; giữ đóng cổng vào, email, outbox, cho vào media. Áp đúng [thứ tự khôi phục](../system/data-lifecycle.md#restore); giải quyết các prepared chưa rõ trước khi mở phạm vi liên quan.
4. Kiểm tra: xóa và thu hồi đã commit, placeholder khi mất bản sửa, thời hạn gốc, chống trùng, quyền. Thu hồi phiên và link cũ; media cũ kết thúc hay bị chặn; chỉ cấp chỗ mới khi đã dừng hẳn.
5. Đối chiếu dấu và thao tác đã commit trước sự cố, và phần không khôi phục được. Ghi RPO thực đo, RTO từng bước, thời gian chờ người, khóa, sổ, và các phạm vi chưa mở.
6. Chạy kiểm tra nhanh về quyền, lịch sử, email, media; xác nhận và ghi thời điểm mở lại. Chỉ đạt một phần thì ghi rõ phần còn đóng.

**Xong khi:** đạt DEC-086, 108, 109 có bằng chứng; phiên, quyền, nội dung cũ không sống lại. Mẫu: [hồ sơ phát hành](../records/templates/release-record.md#restore-record). Diễn tập dùng dữ liệu giả, không gửi email cho người thật.

<a id="cleanup"></a>

<a id="8-rb-cleanup--dọn-dữ-liệu-đến-hạn"></a>

## 8. RB-CLEANUP — Dọn dữ liệu tới hạn

Worker dọn dữ liệu chưa được làm.

1. Đọc [bảng dữ liệu và quy tắc worker](../system/data-lifecycle.md#cleanup); kiểm tra đúng module, nội dung, mốc UTC. Không dùng một TTL cho cả DB.
2. Theo dõi số ứng viên, tuổi lâu nhất, lô và lease, lỗi. Chỉ worker đã làm và kiểm thử mới được xóa hay ẩn; không dùng SQL xóa tay hay cascade tài khoản, tin.
3. Giữ dữ liệu chưa xác nhận, chưa dừng hẳn; chuỗi refresh đang dùng; dấu chống trùng, tombstone, dấu chặn. Không xóa nội dung phòng đã xóa theo tuổi.
4. Job trễ: kiểm tra index, khóa, quyền, dung lượng. Đường đọc vẫn ẩn trường tới hạn; xác thực vẫn từ chối token hết hạn. Job lỗi không làm kéo dài TTL.
5. Ghi số lượng đã xóa hay ẩn, mốc, thời lượng, lỗi, lần chạy lại. Kiểm tra biên thời gian, thời gian chờ, phát hiện dùng lại, gửi lại sau khi dọn. Không chép dữ liệu vừa dọn vào log.

**Xong khi:** đúng chính sách; không ảnh hưởng dữ liệu đang dùng; phân biệt rõ đã xóa khỏi lưu trữ với chỉ ẩn khi trả về.

<a id="account-support"></a>

<a id="9-rb-account--hỗ-trợ-và-khóamở-khóa-tài-khoản"></a>

## 9. RB-ACCOUNT — Hỗ trợ, khóa và mở khóa tài khoản

v1 không có trang quản trị; khóa và mở khóa qua quy trình kỹ thuật có phân quyền và audit; không được đọc DM hay sửa, xóa tin thay tác giả (DEC-112). Chưa có API hay CLI khóa tài khoản; chưa chọn người thực hiện. **Không dùng runbook này làm căn cứ để tự UPDATE trạng thái trong DB.**

1. Nhận yêu cầu; phân loại (lỗi sử dụng, khôi phục, vi phạm); tạo mã hồ sơ, user ID, mã lý do. Không yêu cầu gửi mật khẩu, token, nội dung DM, âm thanh hay hình.
2. Khôi phục mật khẩu chỉ qua email đã đăng ký (DEC-067). Mất email thì không có khôi phục thủ công; không thay email, không phát token, không tự xác minh thay người dùng.
3. Chỉ khóa khi có người quyết định và người thực hiện đã được phân quyền. Công cụ (tương lai) phải kiểm tra trạng thái và version, ghi audit, thu hồi phiên và đổi stamp cùng transaction; ngắt chat và media đúng hạn.
4. Kiểm tra: tài khoản bị khóa không vào, không gửi, không gọi được; người khác vẫn đọc lịch sử và sửa, xóa tin của mình. Không xóa nội dung, không tự chuyển chủ server, không thu hồi lời mời chỉ vì người tạo bị khóa.
5. Mở khóa theo quyết định, có audit; không khôi phục phiên cũ; người dùng đăng nhập lại và được kiểm tra quyền hiện tại.

**Xong khi:** đúng người, đúng phạm vi, có người quyết định và người thực hiện, audit đã lọc, thu hồi đã kiểm tra. Khóa tài khoản khác với khóa đăng nhập tạm 15 phút.

<a id="handover"></a>

<a id="10-lịch-công-việc-và-bàn-giao-đề-xuất"></a>

## 10. Lịch công việc và bàn giao (đề xuất)

| Nhịp | Việc | Bằng chứng |
|---|---|---|
| Liên tục (khi có giám sát) | Lỗi, độ trễ, quyền, hàng đợi, dung lượng, hạn khóa và chứng chỉ. Sau v1: WAL | Tín hiệu, ngưỡng, thời điểm, cảnh báo, người nhận |
| Mỗi ngày trong lịch trực | Cảnh báo chưa xử lý, dọn dữ liệu, delivery, dashboard, kiểm tra nhanh. Sau v1: backup | Mã kiểm tra, thời điểm, người làm, lỗi |
| Mỗi tuần | Tăng trưởng DB, log, băng thông, chi phí, lỗi lặp, việc sắp tới hạn | Số liệu, hành động có người và hạn |
| Trước phát hành, sau thay đổi lớn | Kiểm tra nhanh, rollback, bàn giao quyền và cấu hình | Hồ sơ cùng build và cấu hình |
| Mỗi tháng (sau v1) | Diễn tập khôi phục đầy đủ, kiểm tra đường nhận cảnh báo | RPO, RTO thực đo, mốc, thế hệ, quyền, phần chưa đạt |

- Đây là đề xuất, không cam kết hỗ trợ 24/7. Nhóm làm 3–4 ngày/tuần/người; phải chỉ rõ ai nhận cảnh báo ngoài giờ.
- Khi có backup (sau v1), đo cả thời gian chờ người để đánh giá RTO 4 giờ.
- **Bàn giao** phải có: người chính và thay thế, giờ và phạm vi trực, kênh nhận sự cố, quyền dashboard, máy, backup, khóa theo phạm vi, nơi để runbook, kết quả diễn tập. Chỉ ghi tham chiếu kho bí mật. Việc chưa có người nhận ghi "Chưa bàn giao".

<a id="quan-sát-và-xử-lý-sự-cố-đề-xuất"></a>

### Cảnh báo đề xuất

Ngưỡng dưới đây để thử, không thay mục tiêu nghiệm thu.

| Tín hiệu | Cảnh báo | Làm gì trước |
|---|---|---|
| API, chat | p95 > 500 ms, hoặc lỗi bất thường ≥ 1% trong 5 phút | Kiểm tra phụ thuộc, trace, khóa DB, hàng đợi; tách lỗi validation khỏi 5xx |
| Thu hồi | Kết nối còn nhận nội dung quá 5 giây sau commit | Ngừng gửi cho phần ảnh hưởng, tìm nguyên nhân, ghi sự cố truy cập |
| Outbox chat, email | Sự kiện cũ nhất chưa xử lý > 60 giây; delivery sắp hết hạn | Kiểm tra lease, worker, nhà cung cấp; không gửi lại tin |
| WAL, backup (sau v1) | Dữ liệu lưu trễ > 10 phút; backup ngày thất bại | Kiểm tra kho, mạng, dung lượng |
| Dung lượng | Đĩa DB > 80%; > 90% là khẩn | Dự báo tăng trưởng, mở rộng hoặc dọn theo chính sách; không xóa WAL đang cần |
| Media | Kết nối hợp lệ thất bại > 1%, hoặc chỗ không được trả | Kiểm tra LiveKit, TURN, admission, bộ điều phối; dừng cấp mới nếu không xác nhận được quyền |

**Quy trình chung:** xác nhận và cô lập ảnh hưởng → kiểm tra trace và phụ thuộc → giảm ảnh hưởng hoặc rollback theo runbook → kiểm tra hành trình và dữ liệu → ghi nguyên nhân và hành động ngăn lặp. Dùng [mẫu hồ sơ sự cố](../records/templates/incident-record.md) và [phân loại M1, M2, M3](../releases/acceptance.md#defects). Không thu thập nội dung DM, âm thanh hay hình làm log điều tra.

<a id="vòng-đời-và-cảnh-báo-retention"></a>

### Cảnh báo về thời hạn lưu

Theo [bảng dữ liệu](../system/data-lifecycle.md#inventory): log 14 ngày, audit 90 ngày, IP và user-agent trong audit 7 ngày; chi tiết kỹ thuật đã kết thúc 7 ngày; giữ dấu chống trùng, dấu chặn, chuỗi refresh đang dùng. Chưa có worker hay số liệu dung lượng.

Cảnh báo đề xuất: nội dung đủ điều kiện dọn mà quá hạn > 24 giờ; email envelope đã vô hiệu còn > 60 giây. Sau v1 thêm: prepared hay biên nhận của sổ bảo vệ chưa kết luận; bộ backup hết hạn mà chưa có base mới. Job trễ không kéo dài quyền hay token.

<a id="phạm-vi-quản-trị-đã-chốt-phân-công-còn-mở"></a>

### Phân công vận hành

Theo [phân công](../project/planning.md#team), Vg chủ trì triển khai và môi trường chung; mỗi người làm và kiểm tra công cụ, cấu hình, migration, hướng dẫn chạy và hồ sơ cho phần của mình, có kiểm tra chéo. Người trực, người ký nghiệm thu và người có quyền mở công khai chưa chọn (DEC-111, OQ-011).

<a id="implementation-gaps"></a>

<a id="gói-việc-còn-thiếu-để-chạy-nghiệm-thuvận-hành"></a>

## 11. Việc còn thiếu để nghiệm thu và vận hành

| Gói | Cần tạo | Xong khi |
|---|---|---|
| RLS-GAP-01 | Người duyệt, người thực hiện, người trực và thay thế; lịch, kênh trực | Người được chọn nhận việc và quyền (DEC-111 chưa chọn người duyệt) |
| RLS-GAP-02 | Nhà cung cấp, domain, topology, tài nguyên; manifest production; bản build; kho bí mật | Staging và production tách biệt; cấu hình nạp đúng; runbook có lệnh đã diễn tập |
| RLS-GAP-03 | Danh sách trình duyệt, thiết bị, dữ liệu thử; công cụ chạy và đo | Chạy các ca trong phạm vi, trace và đồng hồ tin cậy; có bằng chứng, không chỉ fixture |
| RLS-GAP-04 | Kiểm tra phụ thuộc thật; metric, log đã lọc; cảnh báo và đường nhận | Phát hiện được lỗi DB, worker, media; cảnh báo thử tới người nhận; không dùng health tĩnh thay |
| RLS-GAP-05 | v1: migration, tương thích, rollback, dọn dữ liệu. Sau v1: backup, WAL, sổ bảo vệ | Ca lỗi đúng phạm vi; giữ quyền, dấu, dữ liệu; khôi phục không hồi sinh dữ liệu |
| RLS-GAP-06 | Admission, hạn mức, chặn node; phiên bản media đã ghim | MEDIA-GAP có bằng chứng ngắt, hạn mức, vào lại; rollback vẫn giữ kiểm soát |
| RLS-GAP-07 | Công cụ khóa và mở khóa có guard, audit, biên nhận | Đúng DEC-104, 112; không đọc DM hay sửa, xóa thay tác giả |
| RLS-GAP-08 | Hồ sơ lần chạy, lỗi còn tồn, triển khai, khôi phục, bàn giao | Mỗi điều kiện có bằng chứng, build, người; lỗi theo DEC-110 |

<a id="gaps"></a>

<a id="4-thông-tin-cần-chốt"></a>

## Còn mở

- **OQ-007:** cách kết luận chất lượng hình ([chuẩn bị đo](../system/quality.md#measurement-readiness)); hệ điều hành, thiết bị, build; tải; bằng chứng ngắt và tải.
- **OQ-010:** có [mô hình chi phí minh họa](../project/planning.md#budget); còn nơi triển khai, tài nguyên, mức dùng thật, báo giá.
- **OQ-011:** kho sổ, khóa, công cụ, topology, bằng chứng worker; người trực, lịch trực; người duyệt (DEC-111).
- **OQ-008:** review, migration, worker email; chọn extension, SDK, phiên bản LiveKit; kho khóa; bằng chứng guard, realtime, LiveKit.
- **Backup (sau v1):** đề xuất base backup hằng ngày, WAL liên tục tới kho riêng ([môi trường](../system/environments.md#v1-postgres-backup-storage)); chưa chọn công cụ.

---

Nguồn: SCP-008, DEC-067, DEC-086, DEC-104, DEC-108 đến 112, DEC-156, DEC-170, [DEC-171](../records/decisions/README.md#dec-171).
