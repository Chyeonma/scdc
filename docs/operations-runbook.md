# SCDC — Hướng dẫn thao tác vận hành

Cập nhật: 2026-10-04. Phạm vi SCP-008; DEC-110/111/112. Đây là runbook để chuẩn bị và diễn tập; chưa có cấu hình production, công cụ backup hoặc kết quả diễn tập được xác nhận. Quyết định nghiệm thu/phát hành ở [release-operations.md](release-operations.md#release-gates); chính sách dữ liệu ở [data-lifecycle.md](data-lifecycle.md#policy).

## Mục lục

- [Đầu vào và hiện trạng](#inputs)
- [Triển khai bản mới](#deploy)
- [Quay lại bản ứng dụng trước](#rollback)
- [Sự cố API, DB và chat](#service-incident)
- [Sự cố email](#email-incident)
- [Sự cố media hoặc thu hồi quyền](#media-incident)
- [Backup và khôi phục](#restore)
- [Dọn dữ liệu](#cleanup)
- [Hỗ trợ và khóa tài khoản](#account-support)
- [Lịch công việc và bàn giao](#handover)

<a id="inputs"></a>

## 1. Đầu vào trước khi thao tác

Mỗi lần thao tác cần có người thực hiện đã được cấp quyền, môi trường đích, bản chạy, phạm vi ảnh hưởng và hồ sơ thay đổi/sự cố. Tên người trực, người thay thế và lịch trực chưa được xác nhận. Không dùng phân công phát triển để tự cấp quyền vận hành production.

| Đầu vào | Cần ghi vào hồ sơ |
|---|---|
| Môi trường | Tên, domain/region, tài nguyên và định danh DB; staging tách khỏi production |
| Bản chạy | Commit, checksum/digest artefact Web/API, schema/migration và phiên bản thành phần media nếu có |
| Cấu hình | Phiên bản cấu hình đã bỏ secrets; tham chiếu kho secrets và key ID, không giá trị khóa |
| Truy cập | Người thực hiện, vai trò, tài khoản kỹ thuật đúng phạm vi và đầu mối thay thế |
| Khôi phục | Bản ứng dụng trước, ma trận tương thích schema, base/WAL hợp lệ và bằng chứng restore |
| Điều khiển | Cách đóng/mở ingress, ngừng ghi theo scope, tạm dừng email/outbox/admission và fencing media |
| Quan sát | Dashboard, nguồn thời gian, trace/error ID, cảnh báo và nơi lưu hồ sơ hạn chế truy cập |
| Lệnh thực tế | Manifest, đường dẫn và lệnh đã diễn tập trên cấu hình được chọn; còn thiếu thì ghi “Chưa có” |

### Đối chiếu repo hiện tại

| Hiện có | Giới hạn cần xử lý trước production |
|---|---|
| [compose.yaml](../compose.yaml) có Web/API/PostgreSQL | Development, publish cổng DB, credentials local và SQL init/seed; chưa có HTTPS, kho secrets, WAL archiving hoặc media |
| [Dockerfile API](../services/SCDC.Api/Dockerfile) và [Web](../clients/WebClient/Dockerfile) | Có bước build; chưa có manifest production và bản artefact đã khóa digest cho một đợt phát hành |
| [HealthController](../services/SCDC.Api/Controllers/HealthController.cs) trả healthy/module stage | Không truy vấn DB hoặc kiểm tra worker; HTTP 200 không chứng minh ứng dụng dùng được |
| [Nginx](../clients/WebClient/nginx.conf) có `/healthz`, proxy API/hub | `/healthz` chỉ trả chuỗi tĩnh; access log mặc định cần kiểm tra query nhạy cảm trước dùng production |
| [Program.cs](../services/SCDC.Api/Program.cs) và [IdentityModule](../services/Modules/Identity/IdentityModule.cs) | Swagger bật trong Development; cần cấu hình proxy/HTTPS/CORS và thử đường truy cập thực tế |
| [appsettings.json](../services/SCDC.Api/appsettings.json) | Connection string/signing key trống; `ExposeDevelopmentTokens` mặc định false, chưa có kiểm tra cấm bật ngoài Development |
| Identity có nghiệp vụ; Community/Messaging ở Foundation | Chưa có triển khai chat/realtime/media, worker retention hoặc sổ bảo vệ độc lập; không dùng UI mẫu để ký nghiệm thu các phần này |

Chi tiết cấu hình có trong [hướng dẫn phát triển](development.md#configuration). Production phải kiểm tra giá trị cấu hình thực sự được nạp: environment phù hợp, signing key riêng, token Development tắt, origin hợp lệ, domain liên kết email đúng và DB không mở trực tiếp cho browser. Kiểm tra từ response/log đã lọc; không dump toàn environment hoặc connection string vào hồ sơ.

Các lệnh dưới đây chỉ quan sát stack **local Development** từ root repo:

```bash
docker compose ps
curl --fail --silent --show-error http://localhost:5026/api/v1/health
curl --fail --silent --show-error http://localhost:3000/healthz
```

Kết quả hai endpoint chỉ là tín hiệu tiến trình/proxy. Kiểm tra DB và hành trình cần tài khoản thử và phép kiểm tra thực sự. Không có lệnh production có thể chạy nguyên văn trong runbook này; bổ sung sau khi chọn manifest/công cụ và diễn tập, không đổi tên service local rồi coi đã sẵn sàng.

<a id="deploy"></a>

## 2. RB-DEPLOY — Triển khai bản mới

**Đầu vào:** [hồ sơ phát hành](templates/release-record.md), bản chạy đã khóa, người duyệt, kế hoạch migration/rollback và cửa sổ thao tác. **Dừng trước thao tác:** chưa đủ điều kiện phát hành, backup/restore chưa kiểm chứng hoặc không biết schema cũ tương thích đến đâu.

1. Đối chiếu đúng môi trường và artefact đã duyệt; ghi schema/build trước thay đổi. Xác nhận backup/WAL và checkpoint sổ bảo vệ còn hợp lệ, cùng đường truy cập kho keys để khôi phục.
2. Diễn tập staging với migration thực tế; chạy [bộ smoke](release-operations.md#smoke), kiểm tra secrets không xuất hiện trong response/log, key ring còn dùng sau restart. Ghi lệnh thực tế và mã thoát, không lưu token trong lệnh/hồ sơ.
3. Xác nhận thời điểm và người thực hiện. Đóng ghi/ingress theo kế hoạch migration; media admission ngừng cấp mới nếu có restart/generation thay đổi. Không đánh dấu slot đã nhả trước khi node cũ ngừng truyền.
4. Áp migration đã duyệt một lần theo công cụ được chọn, rồi triển khai artefact/cấu hình. Thất bại phải xác định trạng thái thực tế; không chạy lại một migration có kết quả không rõ hoặc tạo lại schema từ SQL init.
5. Kiểm tra schema/build, kết nối DB, worker, proxy/API/hub, email và admission. Chạy smoke bằng tài khoản thử đúng môi trường; đối chiếu lỗi/backlog/quyền và ghi bằng chứng.
6. Mở lại các đường đã đóng khi kiểm tra đạt và người có thẩm quyền xác nhận. Đề xuất theo dõi tăng cường 30 phút đầu; thời lượng được ghi trong kế hoạch từng lần, không thay kiểm thử tải 60 phút.
7. Ghi kết quả thực tế, downtime, migration, quyết định và đầu mối tiếp nhận; nếu không đạt, giữ phần ảnh hưởng đóng và chuyển RB-ROLLBACK hoặc runbook sự cố tương ứng.

**Kết thúc:** bản đúng artefact, smoke đạt, quyền/token/epoch đúng, backlog có thể xử lý, backup tiếp tục và hồ sơ đã bàn giao. Mở đăng ký công khai là quyết định riêng sau nghiệm thu; deploy thành công chưa tự cho phép mở đăng ký.

<a id="rollback"></a>

## 3. RB-ROLLBACK — Quay lại bản ứng dụng trước

**Kích hoạt:** smoke trọng yếu thất bại, truy cập trái quyền, dữ liệu sai hoặc lỗi dịch vụ tăng sau deploy. Người có quyền xử lý sự cố được cô lập ảnh hưởng trong phạm vi đã cấp; người quyết định rollback phải có tên trong kế hoạch, chưa tự gán tại tài liệu này.

1. Đóng phần ghi/truy cập bị ảnh hưởng, ghi mốc cuối còn tốt và bản hiện tại. Giữ bằng chứng đã lọc và đánh dấu các thao tác có kết quả chưa rõ; không tự replay mutation.
2. Đối chiếu ma trận app–schema–key–generation. Nếu bản cũ còn đọc/ghi đúng schema mới và giữ đầy đủ thu hồi/floor/admission gate, dùng đường rollback ứng dụng đã diễn tập.
3. Nếu schema đã thay đổi không tương thích hoặc không thể giữ các bảo vệ hiện hành, chọn forward fix hoặc RB-RESTORE theo kế hoạch đã duyệt. Không tự đảo migration hoặc restore DB chỉ để quay lại giao diện cũ.
4. Deploy artefact/cấu hình cũ phù hợp nhưng giữ key ring, deny marker, bảo vệ restore và quyền hiện hành. Media cần fence generation cũ; không bỏ admission/quota gate để làm phiên cũ hoạt động.
5. Chạy smoke, kiểm tra dữ liệu/thao tác đã commit trong thời gian bản lỗi chạy và token/cursor cũ. Chỉ mở lại khi có kết quả và xác nhận; theo dõi như sau deploy.

**Kết thúc:** bản hoạt động, không hạ version/quyền hoặc hồi sinh nội dung đã xóa, thao tác chưa rõ đã được đối chiếu. Restore DB là thao tác riêng có thể mất dữ liệu theo recovery point, cần ghi phạm vi mất và người chấp nhận trong hồ sơ.

<a id="service-incident"></a>

## 4. RB-SERVICE — API, DB hoặc chat gặp sự cố

1. Lập [hồ sơ sự cố](templates/incident-record.md), ghi thời điểm đầu tiên có ảnh hưởng, phát hiện và xác nhận; phân loại theo [mức ảnh hưởng](release-operations.md#defects). Xác định cả Web/API/DB/worker thay vì chỉ nhìn health 200.
2. Kiểm tra bản deploy gần nhất, request/trace ID, tỷ lệ lỗi, latency, DB connection/lock/đĩa và tuổi outbox chưa xử lý. Dùng số lượng/ID, không lấy body DM vào log điều tra.
3. Với lỗi sau deploy, theo RB-ROLLBACK. Với DB không dùng được, ngừng ghi và không báo tin đã gửi nếu chưa commit; bảo vệ idempotency khi client thử lại. Không xóa WAL hoặc volume để giải phóng đĩa.
4. Chat realtime lỗi nhưng lưu trữ còn hoạt động: ghi rõ phần gửi/lịch sử và cập nhật online đang bị ảnh hưởng; theo dõi outbox, thử reconnect/bù trang bằng account thử. Không đánh dấu đạt ngưỡng online khi chỉ GET thấy tin.
5. Khôi phục dependency/worker theo cấu hình đã diễn tập. Đối chiếu operation ID/message ID/version để xử lý kết quả chưa rõ; worker phát lại sự kiện theo receipt/guard, không thực hiện lại thao tác gửi tin.
6. Kiểm tra các hành trình bị ảnh hưởng và thu hồi quyền, rồi mở lại theo xác nhận. Ghi nguyên nhân, thời gian ảnh hưởng, thiếu dữ liệu nếu có và hành động ngăn lặp.

**Kết thúc:** gửi/lưu/lịch sử/realtime theo scope hoạt động, không mất/trùng hoặc vượt quyền, backlog được đối chiếu và các ca thực tế có bằng chứng.

<a id="email-incident"></a>

## 5. RB-EMAIL — Email xác minh/khôi phục lỗi hoặc bị chậm

1. Kiểm tra trạng thái worker/delivery, provider receipt, retry/lease, độ tuổi và thời hạn token. Phân biệt API nhận yêu cầu, provider nhận thư và hộp thư thử nhận được; một response accepted không chứng minh giao thư.
2. Kiểm tra domain liên kết, cấu hình provider và quyền key mà không ghi envelope/token hoặc email người thật vào hồ sơ. Dùng tài khoản thử, ghi delivery ID và mã lỗi đã lọc.
3. Thư/token đã hết hạn hoặc bị thay thế phải kết thúc delivery và dọn envelope theo Accounts. Không gửi thư cũ trở lại sau restore hoặc tạo link mới bỏ qua cooldown.
4. Thư có kết quả giao không rõ: đối chiếu receipt và retry theo thiết kế worker; không báo chưa gửi nếu chưa biết, không làm token dùng được nhiều lần vì giao lặp.
5. Sau sửa dependency, thử register→verify và forgot→reset thật ngoài Development; thử link cũ, dùng lại và các phiên bị thu hồi. Không chuyển sang trả token Development để vượt sự cố email.

**Kết thúc:** đường email đúng môi trường/mục đích, có receipt và kết quả hộp thư thử, link đúng hiệu lực, không lộ token. Hỗ trợ mất quyền truy cập email vẫn chỉ theo DEC-067; runbook không tạo quy trình khôi phục thủ công.

<a id="media-incident"></a>

## 6. RB-MEDIA — Media, quota hoặc thu hồi quyền lỗi

1. Ghi room/call/participation ID, build node, generation, trạng thái slot/lease và mốc commit thu hồi; không ghi JWT, SDP chứa thông tin nhạy cảm hoặc nội dung thu âm/chia sẻ.
2. Nếu không xác nhận được quyền hoặc RTP tiếp tục quá hạn DEC-099, dừng admission/publish/subscription liên quan bằng đường điều khiển đã diễn tập. Nếu RoomService không thực hiện được, dùng cơ chế fencing node/network đã thiết kế; không chỉ đóng websocket rồi cho là đã ngừng media.
3. Giữ reservation/reconnect/draining trong quota đến khi có bằng chứng quiesced. Không xóa participation hoặc nhả slot để cho thêm người vào trong lúc node cũ còn truyền.
4. Kiểm tra signaling qua admission, quyền lease, quota gate, TURN/ICE, clock skew và direct route. Thay đổi firewall theo manifest/version đã chọn; [LiveKit ports/firewall](https://docs.livekit.io/transport/self-hosting/ports-firewall/) phân biệt signaling, ICE và TURN, cần kiểm tra từng đường.
5. Sau xử lý, thử token/epoch cũ và SFU refresh không vào lại; thử người còn quyền vào bằng participation mới, listen-only và bật nguồn riêng. Đo sender/receiver ngừng RTP thực tế, không suy từ trạng thái UI.

**Kết thúc:** không còn media trái quyền, không vượt 10/20/2, capacity khớp trạng thái thực tế và nguồn đúng phạm vi. Extension/fork và đường fencing chưa có build/proof; RB-MEDIA chưa phải runbook có thể thi hành production.

<a id="restore"></a>

## 7. RB-RESTORE — Backup và khôi phục dữ liệu

### Kiểm tra backup

Kiểm tra base đã hoàn tất/đã kiểm chứng, WAL liên tục, catalogue/timeline, checksum, tuổi gốc và quyền truy cập kho lưu. Đo điểm dữ liệu mới nhất thật sự có thể khôi phục, không chỉ thời điểm job trả thành công. Có bản base mới hợp lệ trước khi bản cũ hết tuổi; không reset tuổi khi copy, hoặc xóa WAL còn là dependency của bản base hợp lệ.

Theo [PostgreSQL 18 PITR](https://www.postgresql.org/docs/18/continuous-archiving.html), base và chuỗi WAL phục hồi cluster; dump logic không thay base cho WAL replay. Cần giữ riêng cấu hình/key cần phục hồi. Phương án `archive_timeout` để chuyển segment ở tải thấp phải cộng thời gian lưu WAL ra kho và backlog; riêng việc cấu hình timeout chưa chứng minh RPO theo [PostgreSQL WAL settings](https://www.postgresql.org/docs/18/runtime-config-wal.html).

### Diễn tập hoặc xử lý sự cố

1. Ghi sự cố/đợt diễn tập, môi trường, mốc ảnh hưởng/phát hiện/xác nhận, recovery point dự kiến và người quyết định. Với sự cố thật, cô lập ghi mới và giữ bằng chứng nguồn; restore vào môi trường đích cô lập.
2. Chọn base/WAL còn hợp lệ và kiểm tra cấu hình/key/checkpoint sổ bảo vệ. Ghi cửa sổ recoverable thực tế; tuổi artefact tối đa 30 ngày không hứa phục hồi mọi điểm trong tròn 30 ngày.
3. Restore theo công cụ đã chọn; giữ ingress, email/outbox và admission đóng. Áp đúng [thứ tự restore/floor/generation](data-lifecycle.md#restore), giải quyết prepared chưa rõ trước mở scope liên quan.
4. Kiểm tra xóa/thu hồi đã commit, placeholder khi mất bản sửa mới nhất, TTL gốc, chống trùng và quyền. Thu hồi auth session/link cũ; media cũ kết thúc/fenced, capacity chỉ cấp mới sau quiescence.
5. Đối soát marker/thao tác đã commit trước sự cố và phần không khôi phục được. Ghi RPO thực đo, RTO từng bước, thời gian chờ người/keys/kho sổ và các scope chưa mở.
6. Chạy smoke quyền/lịch sử/email/media phù hợp, xác nhận kết quả và thời điểm mở lại. Đạt một phần scope phải ghi rõ phần còn đóng, không ký “khôi phục toàn hệ thống” khi còn scope chưa xác nhận.

**Kết thúc:** đáp ứng DEC-086/108/109 với bằng chứng; phiên/quyền/nội dung cũ không hồi sinh. Mẫu kết quả trong [hồ sơ phát hành](templates/release-record.md#restore-record). Diễn tập dùng dữ liệu giả; không gửi email cho người dùng thật.

<a id="cleanup"></a>

## 8. RB-CLEANUP — Dọn dữ liệu đến hạn

1. Đọc [ma trận và quy tắc worker](data-lifecycle.md#cleanup), kiểm tra đúng module/payload/cutoff UTC, không áp TTL chung cho cả DB.
2. Quan sát số candidate, tuổi eligible lâu nhất, batch/lease và lỗi. Chỉ worker đã triển khai/kiểm chứng mới purge/redact; không dùng SQL xóa tay thay worker hoặc cascade account/message.
3. Giữ dữ liệu chưa ack/quiesced, refresh family active, dedup/tombstone/deny marker cần thiết. Phòng đã xóa chưa đặt hạn purge nội dung; job kỹ thuật không được xóa nội dung phòng theo tuổi.
4. Khi job trễ, kiểm tra index/lock/quyền/dung lượng rồi xử lý theo cấu hình. Đường đọc vẫn lọc trường tới hạn và auth vẫn từ chối token hết hiệu lực; không gia hạn TTL vì job lỗi.
5. Ghi số lượng đã purge/redact, cutoff, thời lượng/lỗi và lần chạy lại. Kiểm tra biên thời gian, cooldown/reuse/retry sau dọn; không sao chép dữ liệu vừa bị dọn vào log.

**Kết thúc:** payload/field đúng chính sách, không ảnh hưởng dữ liệu đang hoạt động và bằng chứng storage được phân biệt với lọc DTO. Worker hiện chưa triển khai.

<a id="account-support"></a>

## 9. RB-ACCOUNT — Hỗ trợ và khóa/mở khóa tài khoản

DEC-112 đã chốt chưa có trang quản trị riêng; khóa/mở khóa qua quy trình kỹ thuật có phân quyền/audit, không đọc DM/sửa/xóa thay tác giả. Các bước dưới đây cụ thể hóa thiết kế cần rà soát; chưa có API/CLI khóa tài khoản trong repo, chưa chọn hoặc cấp quyền cho người thực hiện. Không dùng runbook làm căn cứ tự chạy UPDATE trạng thái trong DB.

1. Nhận yêu cầu, phân loại lỗi sử dụng/khôi phục/vi phạm; tạo mã hồ sơ, user ID cần xử lý và mã lý do. Không yêu cầu gửi mật khẩu, token, nội dung DM hoặc audio/video vào hồ sơ.
2. Khôi phục mật khẩu qua email đã đăng ký theo DEC-067. Mất email không có khôi phục thủ công; không thay email, phát token hoặc tự xác minh thay người dùng.
3. Khóa tài khoản chỉ khi có người quyết định và người thực hiện đã được phân quyền. Công cụ tương lai phải kiểm tra trạng thái/version, ghi audit và receipt/sổ bảo vệ cần thiết, thu hồi phiên/stamp cùng transaction; cutoff chat/media theo quy tắc đã chốt.
4. Đối chiếu tài khoản không truy cập/gửi/gọi, peer còn quyền đọc lịch sử và sửa/xóa tin của chính mình. Không xóa nội dung, tự chuyển ownership hoặc thu hồi lời mời đang còn hạn chỉ vì người tạo bị khóa.
5. Mở khóa theo quyết định có audit, không khôi phục phiên cũ; người dùng đăng nhập lại và quyền được kiểm tra hiện hành. Không cấp quyền đọc DM/sửa/xóa thay tác giả từ quyền vận hành.

**Kết thúc:** đúng user/phạm vi, có người quyết định/thực hiện, audit đã lọc và kiểm chứng thu hồi. Phân biệt account suspend/disable với lockout đăng nhập 15 phút.

<a id="handover"></a>

## 10. Lịch công việc và bàn giao đề xuất

| Nhịp | Công việc | Bằng chứng tối thiểu |
|---|---|---|
| Tự động liên tục khi có hệ thống giám sát | Lỗi/latency/quyền, backlog, WAL/archive, dung lượng, key/certificate expiry | Tín hiệu, ngưỡng, thời điểm, cảnh báo và người nhận được cấu hình |
| Mỗi ngày trong phạm vi trực được chọn | Xem cảnh báo chưa xử lý, backup/WAL, cleanup và delivery; kiểm tra dashboard/smoke tổng hợp | Check ID, thời điểm, người thực hiện, lỗi và hồ sơ tiếp nhận |
| Mỗi tuần | Tăng trưởng DB/log/backup/băng thông, chi phí, lỗi lặp và các đầu việc sắp tới hạn | Số liệu và hành động có người/hạn xử lý |
| Trước phát hành, sau thay đổi lớn về schema/topology | Smoke, rollback/restore phù hợp và bàn giao quyền/cấu hình | Hồ sơ phát hành/diễn tập cùng build và cấu hình |
| Đề xuất mỗi tháng | Diễn tập restore đầy đủ và kiểm tra các đường nhận cảnh báo | RPO/RTO thực đo, floor/generation, quyền và danh sách phần chưa đạt |

Lịch hàng ngày/hàng tuần/hàng tháng là đề xuất để nhóm chọn công suất, không cam kết hỗ trợ 24/7. Nhóm hiện dự kiến 3–4 ngày full time/tuần/người; cần chỉ rõ người nhận cảnh báo ngoài lịch làm việc và khả năng đạt RTO 4 giờ. Không loại thời gian chờ người khỏi hồ sơ để làm số liệu trông đạt; nếu không đáp ứng thì ghi chưa đạt và đưa ra quyết định vận hành trước phát hành.

Bàn giao phải có người chính/thay thế nhận việc, giờ/phạm vi trực, kênh nhận sự cố đã thống nhất, quyền dashboard/host/backup/keys theo phạm vi, nơi tìm runbook và kết quả diễn tập. Chỉ ghi tham chiếu kho secrets, không chép secrets vào tài liệu bàn giao. Công việc không có người nhận giữ trạng thái “Chưa bàn giao”.
