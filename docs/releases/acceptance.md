# SCDC — Nghiệm thu và điều kiện phát hành

Nguồn chung cho quy trình nghiệm thu, phân loại lỗi và gate phát hành. [MVP](mvp.md) và [v1](v1.md) chọn phạm vi áp dụng. Chưa có xác nhận phát hành công khai.

<a id="testing"></a>

<a id="1-kiểm-thử-và-nghiệm-thu"></a>

## 1. Kiểm thử và nghiệm thu

<a id="phạm-vi-kiểm-chứng"></a>

### Phạm vi kiểm chứng

Theo [DEC-117](../records/decisions/README.md#dec-117), mỗi người tự kiểm thử phần sở hữu và có người khác kiểm tra lại; Vg/Sáng giữ kỳ vọng nghiệp vụ/quyền/đồng thời, Thái chuẩn bị dataset/bộ chạy và tổng hợp kết quả. Kết quả kiểm thử phải nối tới mã tiêu
chí chấp nhận, môi trường, dữ liệu thử, phiên bản sản phẩm và lỗi phát hiện.
Không coi tiêu chí dự thảo là đã đạt khi chưa có kết quả thực thi.
Bộ ca và dữ liệu chuẩn bị tại [Accounts](../features/accounts/README.md), [DM](../features/messaging/direct-messaging.md#tests), [Community](../features/community/traceability.md#tests) và [TC-TEXT dùng chung](../features/messaging/text-policy.md#tests). Kết quả từng gói được dẫn từ status của tính năng và chỉ áp dụng build/phạm vi trong hồ sơ; repo có bộ test tự động được mô tả trong hướng dẫn phát triển.

| Nhóm | Tình huống cần kiểm chứng | Căn cứ |
|---|---|---|
| DM chức năng | Tìm theo hai trường, chọn đúng người, gửi/lưu/xem lại, sửa/xóa, lỗi gửi và chủ động thử lại; validation Unicode, bản nháp trong tab và bù tin nhiều trang; từ chối gửi khi email chưa xác minh. | AC-DM-01 đến AC-DM-21 |
| DM kết nối | A gửi khi B chưa mở ứng dụng; B thấy tin đã lưu khi mở lại. Kết quả lần đầu không rõ, thử lại cùng thao tác chỉ một tin. | AC-DM-07, AC-DM-08 |
| Cộng đồng | Tìm công khai, join/request/invite, chuyển owner/rời/rejoin, private switch; tên Unicode, @everyone/giới hạn role, quyền quản lý cộng dồn và issuer lifetime. | AC-COM-01 đến AC-COM-42; TC-COM/TC-ACL ở đặc tả Community |
| Phòng/tin | Quyền xem mặc định, lịch sử, gửi/sửa/xóa/thử lại, ACL snapshot và epoch; manager cần view, guard tranh revoke/send; xóa phòng chặn HTTP/realtime/media theo phạm vi. | AC-COM-06/07, AC-COM-11 đến AC-COM-18, AC-COM-22 đến AC-COM-28, AC-COM-34/42 |
| Tài khoản | Đăng ký, xác minh, đăng nhập, hồ sơ, phiên và đặt lại mật khẩu qua email; consume/resend đồng thời, trạng thái liên kết và email worker. Chính sách DEC-063–068 đã chốt; limiter bổ sung hoãn DEC-089; thiết kế recovery còn cần triển khai và kiểm chứng ACC-GAP. | AC-ACC-01 đến AC-ACC-21, OQ-002 |
| Media | Gọi riêng/phòng thoại, 10/20/2, ring/reconnect 30 giây; DEC-099–102 chốt cutoff/fail-close, nhiều thiết bị, ban đầu tắt và không audio screen. Có OpenAPI/realtime/fixture/coordinator/admission/quota gate design; chưa có kết quả provider. | AC-MEDIA-01 đến AC-MEDIA-24; MEDIA-GAP-01–07 |
| Dữ liệu | Account lock/no self-delete, room deleted giữ nội dung, retention, dedup marker, restore xóa/thu hồi và placeholder; sổ bảo vệ có hai kho cần proof. | AC-DATA-01–16; TC-DATA-01–12; DATA-GAP-01–06 |
| Vận hành | Cài đặt/triển khai, giám sát, sao lưu/khôi phục, rollback, hỗ trợ và mở đăng ký công khai. | SCP-008, RLS-GATE-01–10; [runbook](../guides/operations.md) |

<a id="acceptance-process"></a>

<a id="quy-trình-nghiệm-thu-và-trạng-thái-kết-quả"></a>

### Quy trình nghiệm thu và trạng thái kết quả

Phân biệt **bàn giao nội bộ theo đợt** với **nghiệm thu toàn v1 để phát hành**. Một đợt Accounts/DM có thể được rà soát riêng khi đủ phụ thuộc; không suy đã nghiệm thu media hoặc đã cho phép mở công khai. Mọi hồ sơ ghi phạm vi SCP, tiêu chí AC, build và phần chưa có implementation.

| Bước | Công việc | Đầu ra / đầu mối |
|---|---|---|
| 1. Khóa đợt | Phạm vi, build/artefact, schema/config, browser/thiết bị, dataset và tiêu chí đã chốt | Định danh trong [mẫu hồ sơ](../records/templates/release-record.md); từng chủ gói chuẩn bị ca/kỳ vọng, Thái chuẩn bị dataset/bộ chạy |
| 2. Chuẩn bị | Tài khoản giả, email thử, quyền/thiết bị, quan sát trace/clock, khả năng fault injection và rollback | Ca chưa đủ đầu vào ghi Bị chặn; không dùng user/DM thật |
| 3. Thực thi | Chạy AC/TC chức năng, quyền, đồng thời, tải/media và dữ liệu/vận hành theo phạm vi | Chủ gói thực thi phần sở hữu, có kiểm tra chéo; mỗi lần chạy có thực tế, evidence, người và thời điểm; Thái tổng hợp |
| 4. Xử lý lỗi | Ghi ảnh hưởng/tái hiện, người/hạn sửa; sửa rồi kiểm tra lại trên build xác định | Lỗi đóng khi có bằng chứng kiểm tra lại; chạy hồi quy các phần chịu tác động |
| 5. Tổng hợp | Đối chiếu tất cả gate, lỗi tồn, coverage và hạn chế | Kết luận kỹ thuật và hồ sơ cho người có thẩm quyền, chưa tự ký thay |
| 6. Quyết định | Người được chọn xác nhận nghiệm thu; cho phép deploy/mở đăng ký là các quyết định có phạm vi riêng | Người duyệt còn mở DEC-111; thiếu người xác nhận thì chưa được ghi đã nghiệm thu/phát hành |

| Trạng thái ca | Cách sử dụng |
|---|---|
| Chưa chạy | Có ca nhưng chưa thực thi trên build/môi trường xác định |
| Bị chặn | Không thể chạy vì thiếu implementation, môi trường, quyết định hoặc công cụ; ghi phụ thuộc/người xử lý |
| Đạt | Kết quả thực tế đúng toàn bộ kỳ vọng, có bằng chứng và build/lần chạy |
| Chưa đạt | Có kết quả không đáp ứng kỳ vọng; nối tới lỗi và lần kiểm tra lại |
| Không áp dụng | Ngoài phạm vi đã chốt, có căn cứ DEC/SCP; ví dụ media điện thoại DEC-082. Không dùng cho ca v1 chưa có code hoặc chưa đo |

Kết luận hồ sơ là Chưa đánh giá / Chưa đạt / Đạt trong phạm vi ghi nhận, kèm người xác nhận. Bằng chứng từ mock, fixture, kiểm tra schema hoặc test local ghi đúng loại, không thay API/UI/DB/SFU thật. Thay build/schema/config ảnh hưởng kết quả phải đánh giá và chạy lại phần liên quan; không tự dùng kết quả build cũ cho bản mới.

<a id="defects"></a>

<a id="phân-loại-lỗi-và-chính-sách-lỗi-tồn"></a>

### Phân loại lỗi và chính sách lỗi tồn

DEC-110 đã chốt: không phát hành khi còn lỗi bảo mật, sai quyền, mất/trùng dữ liệu, lỗi hành trình chính hoặc chỉ tiêu bắt buộc chưa đạt/chưa kiểm chứng. Chỉ lỗi giao diện nhỏ có thể để lại khi ghi ảnh hưởng, người phụ trách và hạn sửa. Người chấp nhận lỗi tồn vẫn cần được chọn theo DEC-111.

| Mức | Ví dụ / ảnh hưởng | Xử lý trước phát hành |
|---|---|---|
| M1 — Nghiêm trọng | Truy cập trái quyền, token/secrets lộ, mất/trùng/hồi sinh dữ liệu, dịch vụ chính ngừng | Chặn phát hành; cô lập phần ảnh hưởng khi là sự cố, sửa và kiểm chứng lại |
| M2 — Hành trình/chất lượng | Không đăng ký/khôi phục/gửi/đọc/quản lý quyền/gọi theo scope; thu hồi, tải hoặc restore không đạt ngưỡng | Chặn phát hành; cách tiếp tục tạm thời không thay ca bắt buộc phải đạt |
| M3 — Giao diện nhỏ | Lệch khoảng cách/icon hoặc lỗi trình bày nhẹ, vẫn hiểu/dùng đầy đủ hành trình, không ảnh hưởng quyền/dữ liệu/chỉ tiêu | Có thể để lại với mã lỗi, ảnh hưởng, người/hạn sửa và xác nhận trong hồ sơ |

Phân loại theo tác động, không theo công sửa hoặc tên component. Lỗi giao diện làm người dùng không thao tác được hành trình bắt buộc thuộc M2. Lỗi chưa phân loại phải được đánh giá; không tự xếp M3 để mở phát hành. Không có giới hạn số M3 đã chốt; mỗi lỗi vẫn cần hồ sơ chấp nhận và xem xét tác động cộng dồn.

<a id="release-gates"></a>

<a id="điều-kiện-phát-hành-và-bằng-chứng-bắt-buộc"></a>

### Điều kiện phát hành và bằng chứng bắt buộc

Các gate cụ thể hóa phạm vi/ngưỡng đã chọn và DEC-110; chưa gate nào có kết quả nghiệm thu trong đợt chỉnh tài liệu này. Gate thiếu bằng chứng giữ Chưa đánh giá/Bị chặn, không được coi là đạt.

| Gate | Điều kiện | Bằng chứng cần có |
|---|---|---|
| RLS-GATE-01 | Phạm vi/build/config/schema/browser đã khóa, thay đổi được rà soát | Commit/digest, version cấu hình không secrets, migration và ma trận thiết bị |
| RLS-GATE-02 | AC/TC chức năng trong phạm vi đạt | Accounts/DM/Community/Media/data theo build và browser được hỗ trợ |
| RLS-GATE-03 | Quyền/phiên, chống trùng, đồng thời, thu hồi và deny rejoin đúng | Ca âm tính/race/lost response/reconnect, commit→cutoff; media có bằng chứng RTP thật |
| RLS-GATE-04 | Tải và chất lượng đạt DEC-083/085 | Dataset/workload/network, tổng mẫu/percentile/tỷ lệ lỗi, tài nguyên và raw aggregate đã lọc |
| RLS-GATE-05 | Migration/retention/restore giữ toàn vẹn và DEC-086/108/109 | Diễn tập RPO/RTO, chuỗi base/WAL, floor/placeholder, auth/media cũ bị chặn, cleanup đúng |
| RLS-GATE-06 | Cấu hình production và các dependency hoạt động đúng | HTTPS/CORS/domain, token Development tắt, email thật đến account thử, key ring/proxy/admission đúng |
| RLS-GATE-07 | Deploy/rollback và smoke đã diễn tập | Artefact/schema tương thích, lệnh đã chạy, mốc thời gian, không hạ quyền/marker/generation |
| RLS-GATE-08 | Có giám sát/cảnh báo, hỗ trợ và người nhận vận hành | Cảnh báo thử tới người nhận, người chính/thay thế, lịch/phạm vi trực, quyền và bàn giao runbook |
| RLS-GATE-09 | Lỗi tồn theo DEC-110, các ca bắt buộc không bị bỏ qua | Không M1/M2 chưa giải quyết; M3 có người/hạn sửa và chấp nhận; không tiêu chí bắt buộc chưa kiểm chứng |
| RLS-GATE-10 | Có người có thẩm quyền xác nhận và quyết định | Xác nhận nghiệm thu, deploy/mở công khai, phạm vi/build/thời điểm; DEC-111 còn mở |

Limiting bổ sung DEC-089 vẫn là đề xuất hoãn chọn, không tự biến ngưỡng chưa chọn thành tiêu chí nghiệm thu. Thay hoặc bỏ một gate bắt buộc cần quyết định phạm vi/ngưỡng mới được ghi nhận, không chỉ đánh dấu Không áp dụng. Bản v1 chưa có một tính năng trong scope vẫn chưa đủ nghiệm thu toàn v1.

Phương pháp và ngưỡng đo: [chất lượng](../system/quality.md).

<a id="các-cấp-kiểm-thử"></a>

### Các cấp kiểm thử

| Cấp | Mục đích | Điều kiện ghi kết quả |
|---|---|---|
| Thành phần | Quy tắc quyền theo vai trò/ngoại lệ cá nhân, nội dung 2.000 ký tự, chuyển trạng thái tin, xử lý mời/yêu cầu và ngoại lệ dữ liệu. | Có trường hợp đúng/sai, dữ liệu biên và kết quả rõ. |
| Tích hợp | Web–API–dịch vụ–dữ liệu; cập nhật thời gian thực và tải lại lịch sử; quyền đổi trong phiên. | Có môi trường tích hợp và định danh bản chạy. |
| Hệ thống | Hành trình người dùng đầu đến cuối trên trình duyệt được hỗ trợ. | Tài khoản/DM cần desktop và trình duyệt điện thoại theo DEC-059; ma trận đã chốt DEC-082; ghi phiên bản/thiết bị/build thực tế của lần chạy. |
| Chất lượng/vận hành | Tải, lỗi mạng, sao lưu/khôi phục, giám sát và chất lượng cuộc gọi. | Có ngưỡng đo và mức tải được xác nhận, không dùng giả định quy mô làm tiêu chí đạt. |
| Nghiệm thu | Đại diện khách hàng thực hiện hoặc xác nhận kịch bản trọng yếu. | Ghi rõ đạt/chưa đạt, lỗi tồn, người và ngày xác nhận. |

<a id="bộ-tình-huống-ưu-tiên-để-chuẩn-bị-dữ-liệu"></a>

### Bộ tình huống ưu tiên để chuẩn bị dữ liệu

1. Hai tài khoản không cùng cộng đồng: tìm người, mở DM, gửi tin, đăng
   xuất người nhận, gửi tiếp, đăng nhập lại và đọc lịch sử.
2. Gửi thất bại, người gửi bấm thử lại; xác nhận không có hai bản tin khi
   kết quả lần gửi đầu không rõ, cả trong DM và phòng.
3. Sửa/xóa tin của mình trong DM và trong phòng; tài khoản khác thử cùng
   thao tác bằng API. Kiểm tra nội dung hiển thị trên hai phiên.
4. Hai cộng đồng có chế độ tham gia khác nhau; liên kết mời hợp lệ, hết
   hạn; chủ sở hữu, người có quyền và thành viên thường thử tạo mời/duyệt.
5. Phòng mới hiện cho mọi thành viên; thành viên mới đọc được lịch sử cũ;
   người có quyền xem gửi được tin. Sau khi giới hạn quyền, thành viên
   mất quyền không đọc được lịch sử hay nhận cập nhật qua kết nối mở.
6. Khi có bản media để kiểm thử, thử quyền micro/camera, cuộc gọi riêng, phòng
   nhiều người, chia sẻ màn hình, ngắt mạng và khôi phục.

<a id="điều-kiện-nghiệm-thu-dự-kiến"></a>

### Điều kiện nghiệm thu dự kiến

Trước mỗi đợt bàn giao cần có: tiêu chí chấp nhận đã được xác nhận, môi
trường và dữ liệu thử, kết quả chạy, danh sách lỗi với mức độ và quyết
định xử lý. Trước phát hành công khai cần thêm kết quả phân quyền, kiểm
thử tải/media theo ngưỡng đã chốt, khôi phục dữ liệu, tài liệu vận hành
và người nhận hỗ trợ. Chất lượng và trình duyệt đã chốt DEC-082/083/085; mức lỗi tồn theo DEC-110 đã chốt, người ký nghiệm thu chưa được chọn DEC-111; chưa thể tuyên bố
đạt điều kiện phát hành.

Đợt hiện tại không khảo sát người dùng bên ngoài theo DEC-030. Việc đó
không thay thế kiểm thử khả dụng nội bộ, kiểm thử hệ thống hoặc nghiệm
thu của đại diện khách hàng.

<a id="mẫu-ghi-nhận-kết-quả"></a>

### Mẫu ghi nhận kết quả

| Trường | Nội dung phải ghi |
|---|---|
| Mã tình huống và yêu cầu | AC-*, SCP-* hoặc OQ-* đã được giải quyết. |
| Bản chạy/môi trường | Phiên bản triển khai, trình duyệt, thiết bị và dữ liệu thử. |
| Kết quả | Đạt/chưa đạt/chưa chạy; bằng chứng và thời điểm. |
| Lỗi và xử lý | Mã lỗi, mức ảnh hưởng, người phụ trách và kết quả kiểm tra lại. |
| Xác nhận | Người thực hiện và người xác nhận theo cấp kiểm thử. |


Ca chi tiết được quản lý tại [Accounts](../features/accounts/README.md), [DM](../features/messaging/direct-messaging.md#tests), [Community](../features/community/traceability.md#tests) và [Media](../features/media/README.md). Một ca phụ thuộc quy tắc chưa chốt phải ghi “Chờ chốt quy tắc”. Các trạng thái Đạt/Chưa đạt/Chưa chạy gắn với build và lần thực thi, không chỉ với sự tồn tại của file test.

[Vòng đời dữ liệu](../system/data-lifecycle.md#acceptance) bổ sung AC-DATA/TC-DATA. [Mẫu hồ sơ đầy đủ](../records/templates/release-record.md) có lần chạy/coverage/đo chất lượng/lỗi tồn/gate/restore/deploy/bàn giao và quyết định; [mẫu sự cố](../records/templates/incident-record.md) dùng sau phát hành hoặc trong diễn tập. Nơi lưu hồ sơ thực tế sẽ được chọn khi triển khai, không đưa secrets/dữ liệu người dùng thật vào Git.

Đầu vào thiết kế Accounts/DM gồm [OpenAPI recovery](../contracts/account-recovery.openapi.json), [OpenAPI DM](../contracts/direct-messaging.openapi.json), [schema thông điệp Hub](../contracts/chat-realtime.schema.json), [fixture nội dung](../fixtures/text-validation.json), [bảng Unicode](../fixtures/text-policy.json) và [fixture HMAC/UUID](../fixtures/dm-fingerprint.json). Kiểm tra JSON, tham chiếu và giá trị fixture chỉ xác nhận tính nhất quán tài liệu; chưa là kết quả kiểm thử API/UI/SignalR.

Community bổ sung [OpenAPI 45 thao tác](../contracts/community.openapi.json), [schema realtime](../contracts/community-realtime.schema.json), [fixture quyền/transition](../fixtures/community-permissions.json) và [fixture canonical HMAC](../fixtures/community-operations.json). Dùng cho TC-ACL-08–12/TC-COM-18–27; proof transaction/epoch/Unicode migration/thu hồi cần build và dữ liệu thử thực tế, không lấy fixture transition làm kết quả giao dịch đã chạy.

Media có [OpenAPI](../contracts/media.openapi.json), [schema realtime](../contracts/media-realtime.schema.json) và [fixture](../fixtures/media-lifecycle.json); dữ liệu có [schema sổ bảo vệ](../contracts/data-protection.schema.json) và [fixture vòng đời](../fixtures/data-lifecycle.json). Các hash/signature giả trong record mẫu chỉ minh họa, không bằng chứng kho sổ được xác thực.

<a id="smoke"></a>

<a id="bộ-kiểm-tra-nhanh-sau-triển-khai"></a>

### Bộ kiểm tra nhanh sau triển khai

Smoke nhằm phát hiện hỏng cấu hình/tích hợp sau deploy, rollback hoặc restore; không thay toàn bộ regression, tải, race hoặc media cutoff. Các ca dưới đây **chưa chạy**; ca chưa có backend phải ghi Bị chặn, không dùng dữ liệu UI mẫu thay. Dùng account giả đủ điều kiện, email thử và trình duyệt desktop; các hành trình Accounts/DM/Community có lượt kiểm tra trên mobile theo ma trận đã khóa.

| Ca | Thao tác | Kết quả và căn cứ |
|---|---|---|
| RLS-SM-01 | Mở Web/API qua domain đích, kiểm tra dependency và build/config | Đúng artefact; DB có phép kiểm tra thật, token Development tắt; health tĩnh không đủ — GATE-01/06 |
| RLS-SM-02 | Register→nhận email→verify→login bằng email/username | Cùng account, trước verify bị chặn app; email không lộ token response — AC-ACC-01/02/07/19 |
| RLS-SM-03 | Forgot→email→reset, dùng lại link/phiên cũ rồi login mới | Link một lần, mật khẩu/thu hồi đúng — AC-ACC-06/16/19/20 |
| RLS-SM-04 | Hai user tìm/mở DM, gửi, tải lại; gửi khi peer offline rồi peer vào lại | Đúng người/ID/nội dung/lịch sử — AC-DM-01–03/07 |
| RLS-SM-05 | Làm mất response gửi, manual retry cùng key; sửa/xóa rồi retry send | Một tin, trạng thái mới nhất/tombstone; không hồi sinh body — AC-DM-08/10/14 |
| RLS-SM-06 | Tạo cộng đồng/phòng; join/invite/request theo mode, transfer/rời/rejoin | Tư cách/quyền hiện hành, không phục hồi role cũ — AC-COM theo đặc tả |
| RLS-SM-07 | Member/outside user thử đọc/gửi/sửa tin, đổi ACL và rời trong khi kết nối mở | Chặn sai scope/tác giả; cutoff ≤5 giây — AC-COM-11/18/21/42, DEC-083 |
| RLS-SM-08 | Gọi DM/vào voice, nghe trước rồi bật mic/camera/share và kết thúc | Ban đầu tắt, nguồn đúng, screen không audio, slot nhả đúng — AC-MEDIA/DEC-101/102 |
| RLS-SM-09 | Revoke session/view hoặc delete channel khi chat/media đang chạy | Không nhận chat/RTP sau cutoff, token/epoch cũ không vào lại — DEC-083/099, AC-DATA-04 |
| RLS-SM-10 | Khóa peer thử lịch sử/gửi/gọi; mở khóa và thử phiên cũ qua công cụ được phép | Lịch sử giữ, tương tác mới bị chặn, phiên cũ không sống lại — AC-DATA-02/03 |
| RLS-SM-11 | Sau restore, thử tombstone/placeholder/deny marker, auth/link/media cũ và retry key | Đúng DEC-108, không email/ringing cũ, không mất khóa chống trùng — AC-DATA-05/11/13/15 |
| RLS-SM-12 | Kích hoạt cảnh báo thử, kiểm tra backup/WAL/cleanup và bàn giao | Đúng người nhận, có bằng chứng dependency/cutoff/retention — GATE-05/08 |

SM-11 chạy trong diễn tập/restore với dữ liệu tương ứng; deploy bình thường kiểm tra trạng thái floor/tombstone hiện hành, không tự tạo một restore production để chạy smoke. SM-10 cần công cụ quản trị theo DEC-112 chưa có trong repo; không dùng SQL tay bỏ qua audit/guard để làm ca đạt.
