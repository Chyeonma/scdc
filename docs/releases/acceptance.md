# SCDC — Nghiệm thu và điều kiện phát hành

Trang này quy định khi nào v1 được coi là hoàn tất, cách kiểm thử và nghiệm thu, cách phân loại lỗi, các điều kiện phải đạt trước khi phát hành, và bộ kiểm tra nhanh sau khi triển khai.

**Trạng thái:** chưa có kết quả nghiệm thu nào. v1 chưa hoàn tất, chưa phát hành công khai. Phần đã có của MVP xem [MVP](mvp.md).

Backup, khôi phục và đo RPO, RTO để [sau v1](../project/overview.md#backup-scope). v1 vẫn phải kiểm thử: lưu bền, migration, thời hạn lưu, quyền và thu hồi, chống trùng, rollback ứng dụng.

<a id="v1-completion"></a>

## Khi nào v1 hoàn tất

1. Làm xong [phạm vi sản phẩm](../project/overview.md#scope) và mọi ca sử dụng, tiêu chí đã chốt của từng tính năng, kể cả Media.
2. Bốn service build, chạy, khởi động lại và triển khai độc lập; mỗi service chỉ dùng database của mình. Các luồng cũ chạy đúng quyền và dữ liệu; lỗi và timeout giữa service xử lý đúng chính sách. Một ứng dụng MVP đổi tên container là chưa đủ ([gói V1-ARCH](../project/planning.md#v1-architecture-packages)).
3. Có kết quả chạy thật cho các tiêu chí chức năng, quyền, đồng thời, trình duyệt và [chất lượng](../system/quality.md#quality-targets). Fixture hay chỉ nhìn giao diện là chưa đủ.
4. Đạt các [điều kiện phát hành](#release-gates), [kiểm tra sau triển khai](#smoke) và các buổi diễn tập dữ liệu, vận hành thuộc phạm vi.
5. Có người nhận vận hành, và người có quyền xác nhận nghiệm thu và mở công khai ([DEC-111](../records/decisions/README.md#dec-111)). Hiện chưa chọn.
6. Có hồ sơ: commit, build, hợp đồng, kết quả kiểm thử, lỗi còn tồn đã xử lý, hướng dẫn bàn giao.

### Mobile, thông báo, gửi file

Ba phần này ([SCP-009 đến 011](../project/overview.md#extensions)) chưa chốt đợt bàn giao. Trước khi đưa vào một mốc, cần chốt: phạm vi, UC, AC, TC, thiết bị, giới hạn, hợp đồng, cách đo, người kiểm tra; rồi cập nhật điều kiện phát hành và bộ kiểm tra nhanh.

<a id="testing"></a>

<a id="1-kiểm-thử-và-nghiệm-thu"></a>

## Kiểm thử

<a id="phạm-vi-kiểm-chứng"></a>

### Kiểm thử những gì

Mỗi người tự chuẩn bị kết quả mong đợi, dữ liệu thử, công cụ chạy, tự kiểm thử và lập hồ sơ cho phần của mình; một người khác kiểm tra lại. Vg điều phối phạm vi và tổng hợp.

Mỗi kết quả phải ghi: mã tiêu chí chấp nhận, môi trường, dữ liệu thử, phiên bản sản phẩm và lỗi phát hiện. Tiêu chí chưa có kết quả chạy thì chưa đạt.

Ca kiểm thử chi tiết: [Tài khoản](../features/accounts/README.md), [DM](../features/messaging/direct-messaging.md#tests), [Community](../features/community/traceability.md#tests), [TC-TEXT](../features/messaging/text-policy.md#tests).

| Nhóm | Cần kiểm thử | Mã |
|---|---|---|
| DM | Tìm theo hai trường, chọn đúng người, gửi, lưu, xem lại, sửa, xóa, gửi lỗi và gửi lại; kiểm tra Unicode; bản nháp trong tab; bù tin nhiều trang; chặn gửi khi chưa xác minh | AC-DM-01 đến 21 |
| DM khi offline | A gửi khi B chưa mở ứng dụng, B mở lại thấy tin. Gửi không rõ kết quả, gửi lại chỉ ra một tin | AC-DM-07, 08 |
| Cộng đồng | Tìm server công khai; tham gia, xin duyệt, lời mời; chuyển chủ, rời, tham gia lại; đổi sang riêng tư; tên Unicode; @everyone và giới hạn vai trò; quyền quản lý cộng dồn | AC-COM-01 đến 42; TC-COM, TC-ACL |
| Phòng và tin | Quyền xem mặc định; lịch sử; gửi, sửa, xóa, gửi lại; ACL và epoch; người quản lý cần quyền xem; thu hồi tranh với gửi; xóa phòng chặn HTTP, realtime, media | AC-COM-06, 07, 11 đến 18, 22 đến 28, 34, 42 |
| Tài khoản | Đăng ký, xác minh, đăng nhập, hồ sơ, phiên, đổi và đặt lại mật khẩu; dùng token và gửi lại đồng thời; worker email; mật khẩu mới trùng mật khẩu cũ ([AC-ACC-22, TC-ACC-19](../features/accounts/password-recovery.md#acceptance)) | AC-ACC-01 đến 22, OQ-002 |
| Media | Gọi riêng, phòng thoại; giới hạn 10, 20, 2; đổ chuông và kết nối lại 30 giây; ngắt khi mất quyền; nhiều thiết bị; thiết bị ban đầu tắt; màn hình không có âm thanh. Đã có thiết kế, chưa có kết quả với LiveKit | AC-MEDIA-01 đến 24; MEDIA-GAP-01 đến 07 |
| Dữ liệu | Khóa tài khoản, không tự xóa; phòng đã xóa giữ nội dung; thời hạn lưu; dấu chống trùng và chặn. Phần khôi phục để sau v1 | AC-DATA-01 đến 16; TC-DATA-01 đến 12; DATA-GAP-01 đến 06 |
| Vận hành | Cài đặt, triển khai, giám sát, rollback ứng dụng, hỗ trợ, mở đăng ký công khai. Backup để sau v1 | SCP-008, RLS-GATE-01 đến 10; [runbook](../guides/operations.md) |

<a id="acceptance-process"></a>

<a id="quy-trình-nghiệm-thu-và-trạng-thái-kết-quả"></a>

### Quy trình nghiệm thu

Phân biệt **bàn giao nội bộ từng đợt** với **nghiệm thu toàn bộ v1 để phát hành**. Một đợt Tài khoản và DM có thể review riêng; điều đó không có nghĩa Media đã nghiệm thu hay được mở công khai.

| Bước | Việc | Đầu ra |
|---|---|---|
| 1. Chốt đợt | Phạm vi, build, schema, cấu hình, trình duyệt, thiết bị, dữ liệu thử, tiêu chí | Ghi trong [mẫu hồ sơ](../records/templates/release-record.md). Mỗi người chuẩn bị ca của mình; Vg điều phối |
| 2. Chuẩn bị | Tài khoản giả, email thử, quyền, thiết bị, công cụ trace và đồng hồ, khả năng tạo lỗi và rollback | Ca thiếu đầu vào ghi "Bị chặn". Không dùng người dùng hay DM thật |
| 3. Chạy | Chạy các ca chức năng, quyền, đồng thời, tải, media, dữ liệu, vận hành | Mỗi lần chạy ghi kết quả thực tế, bằng chứng, người chạy, thời điểm. Có kiểm tra chéo |
| 4. Xử lý lỗi | Ghi ảnh hưởng, cách tái hiện, người và hạn sửa; sửa rồi kiểm tra lại trên build xác định | Lỗi chỉ đóng khi có bằng chứng kiểm tra lại; chạy lại các phần bị ảnh hưởng |
| 5. Tổng hợp | Đối chiếu điều kiện phát hành, lỗi còn tồn, độ phủ, giới hạn | Kết luận kỹ thuật và hồ sơ cho người có quyền; không tự ký thay |
| 6. Quyết định | Người có quyền xác nhận nghiệm thu; triển khai và mở đăng ký là các quyết định riêng | Chưa có người (DEC-111); chưa có người thì chưa ghi là đã nghiệm thu |

**Trạng thái một ca:**

| Trạng thái | Nghĩa |
|---|---|
| Chưa chạy | Có ca nhưng chưa chạy trên build và môi trường xác định |
| Bị chặn | Không chạy được vì thiếu code, môi trường, quyết định hay công cụ; ghi phụ thuộc và người xử lý |
| Đạt | Kết quả đúng toàn bộ kỳ vọng, có bằng chứng, build và lần chạy |
| Chưa đạt | Có kết quả sai; link tới lỗi và lần kiểm tra lại |
| Không áp dụng | Ngoài phạm vi đã chốt, có căn cứ (ví dụ media trên điện thoại, DEC-082). Không dùng cho ca chưa có code hay chưa đo |

**Kết luận hồ sơ:** Chưa đánh giá, Chưa đạt, hoặc Đạt trong phạm vi ghi nhận, kèm người xác nhận.

- Bằng chứng từ mock, fixture, kiểm tra schema hay test trên máy phải ghi đúng loại; không thay cho API, giao diện, DB, LiveKit thật.
- Đổi build, schema hay cấu hình thì phải chạy lại phần liên quan; không dùng kết quả của bản cũ.

<a id="defects"></a>

<a id="phân-loại-lỗi-và-chính-sách-lỗi-tồn"></a>

### Phân loại lỗi

**Không phát hành** khi còn lỗi bảo mật, sai quyền, mất hoặc trùng dữ liệu, lỗi ở hành trình chính, hoặc chỉ tiêu bắt buộc chưa đạt hay chưa kiểm thử. Chỉ lỗi giao diện nhỏ được để lại, nếu ghi rõ ảnh hưởng, người và hạn sửa (DEC-110). Người chấp nhận lỗi còn tồn chưa chọn (DEC-111).

| Mức | Ví dụ | Xử lý trước phát hành |
|---|---|---|
| M1 — Nghiêm trọng | Truy cập trái quyền; lộ token hay bí mật; mất, trùng, hồi sinh dữ liệu; dịch vụ chính ngừng | Chặn phát hành; sửa và kiểm tra lại |
| M2 — Hành trình, chất lượng | Không đăng ký, khôi phục, gửi, đọc, quản lý quyền hay gọi được; thu hồi hay tải không đạt ngưỡng | Chặn phát hành; cách làm tạm không thay được ca bắt buộc |
| M3 — Giao diện nhỏ | Lệch khoảng cách, biểu tượng, lỗi trình bày nhẹ; vẫn dùng được đầy đủ, không ảnh hưởng quyền, dữ liệu, chỉ tiêu | Có thể để lại, kèm mã lỗi, ảnh hưởng, người và hạn sửa, xác nhận trong hồ sơ |

- Phân loại theo tác động, không theo công sửa.
- Lỗi giao diện làm người dùng không đi hết được hành trình bắt buộc là M2.
- Lỗi chưa phân loại phải được đánh giá; không tự xếp M3 để phát hành.
- Không giới hạn số lỗi M3, nhưng phải xem tác động cộng dồn.

<a id="release-gates"></a>

<a id="điều-kiện-phát-hành-và-bằng-chứng-bắt-buộc"></a>

### Điều kiện phát hành

Chưa điều kiện nào có kết quả. Thiếu bằng chứng thì giữ "Chưa đánh giá" hoặc "Bị chặn".

| Mã | Điều kiện | Bằng chứng |
|---|---|---|
| RLS-GATE-01 | Phạm vi, build, cấu hình, schema, trình duyệt đã chốt; thay đổi đã review | Commit hoặc digest, phiên bản cấu hình (không chứa bí mật), migration, danh sách thiết bị |
| RLS-GATE-02 | Các ca chức năng trong phạm vi đạt | Tài khoản, DM, Community, Media, dữ liệu theo build và trình duyệt hỗ trợ |
| RLS-GATE-03 | Quyền, phiên, chống trùng, đồng thời, thu hồi, chặn tham gia lại đúng | Ca từ chối, tranh chấp, mất phản hồi, kết nối lại; đo từ commit tới lúc ngắt; media có bằng chứng luồng thật |
| RLS-GATE-04 | Tải và chất lượng đạt DEC-083, DEC-085 | Dữ liệu thử, tải, mạng; số mẫu, percentile, tỷ lệ lỗi, tài nguyên, dữ liệu tổng hợp |
| RLS-GATE-05 | v1: migration và thời hạn lưu giữ đúng dữ liệu. Sau v1: backup và khôi phục (DEC-086, 108, 109) | v1: dữ liệu đã commit vẫn đúng, dọn dữ liệu không mất dấu chống trùng và chặn. Sau v1: chuỗi backup, mốc bảo vệ, RPO và RTO đo thật |
| RLS-GATE-06 | Cấu hình production và các phụ thuộc chạy đúng | HTTPS, CORS, domain; token Development tắt; email thật tới tài khoản thử; key ring, proxy, kiểm soát media đúng |
| RLS-GATE-07 | Đã diễn tập triển khai, rollback và kiểm tra nhanh | Bản build và schema tương thích, lệnh đã chạy, mốc thời gian; không hạ quyền hay mất dấu |
| RLS-GATE-08 | Có giám sát, cảnh báo, hỗ trợ và người nhận vận hành | Cảnh báo thử tới đúng người; người chính và thay thế, lịch trực, quyền; bàn giao runbook |
| RLS-GATE-09 | Lỗi còn tồn theo DEC-110; không bỏ ca bắt buộc | Không còn M1, M2; M3 có người, hạn và xác nhận; không có tiêu chí bắt buộc chưa kiểm thử |
| RLS-GATE-10 | Có người có quyền xác nhận và quyết định | Xác nhận nghiệm thu, triển khai, mở công khai; ghi phạm vi, build, thời điểm. DEC-111 còn mở |

- Giới hạn tần suất bổ sung (DEC-089) chưa chọn, nên không thành tiêu chí.
- Phần backup của GATE-05 chưa áp dụng cho v1; không ghi "Đạt" khi chưa làm.
- Muốn bỏ hay đổi một điều kiện bắt buộc phải có quyết định mới, không chỉ đánh "Không áp dụng".
- v1 thiếu một tính năng trong phạm vi thì chưa đủ nghiệm thu.

Phương pháp và ngưỡng đo: [chất lượng](../system/quality.md).

<a id="các-cấp-kiểm-thử"></a>

### Các cấp kiểm thử

| Cấp | Mục đích | Điều kiện ghi kết quả |
|---|---|---|
| Thành phần | Quy tắc quyền, giới hạn 2.000 ký tự, trạng thái tin, xử lý lời mời, yêu cầu, dữ liệu biên | Có ca đúng, sai, biên và kết quả rõ |
| Tích hợp | Web, API, service, dữ liệu; realtime và tải lại lịch sử; đổi quyền giữa phiên | Có môi trường tích hợp và mã bản chạy |
| Hệ thống | Hành trình đầu cuối trên trình duyệt được hỗ trợ | Tài khoản và DM cần máy tính và điện thoại (DEC-059, DEC-082); ghi phiên bản, thiết bị, build |
| Chất lượng, vận hành | Tải, lỗi mạng, giám sát, chất lượng cuộc gọi; backup sau v1 | Có ngưỡng và mức tải đã xác nhận |
| Nghiệm thu | Đại diện khách hàng chạy hoặc xác nhận các kịch bản chính | Ghi đạt hay chưa, lỗi còn tồn, người và ngày xác nhận |

<a id="bộ-tình-huống-ưu-tiên-để-chuẩn-bị-dữ-liệu"></a>

### Kịch bản ưu tiên

1. Hai tài khoản không chung cộng đồng: tìm người, mở DM, gửi tin; người nhận đăng xuất, gửi tiếp; người nhận đăng nhập lại và đọc lịch sử.
2. Gửi thất bại, bấm Thử lại; không có hai bản khi lần đầu không rõ kết quả. Làm cả cho DM và phòng.
3. Sửa và xóa tin của mình trong DM và phòng; tài khoản khác thử cùng thao tác qua API. Kiểm tra hiển thị trên hai phiên.
4. Hai cộng đồng có cách tham gia khác nhau; link mời còn hạn và hết hạn; chủ server, người có quyền và thành viên thường thử tạo lời mời và duyệt.
5. Phòng mới hiện cho mọi thành viên; thành viên mới đọc được lịch sử cũ; người có quyền xem gửi được tin. Sau khi giới hạn quyền, người mất quyền không đọc được lịch sử và không nhận cập nhật qua kết nối đang mở.
6. Khi có Media: thử quyền micro và camera, gọi riêng, phòng nhiều người, chia sẻ màn hình, ngắt mạng và khôi phục.

<a id="điều-kiện-nghiệm-thu-dự-kiến"></a>

### Trước mỗi đợt bàn giao và trước phát hành

- **Mỗi đợt bàn giao:** tiêu chí đã xác nhận, môi trường và dữ liệu thử, kết quả chạy, danh sách lỗi với mức độ và quyết định xử lý.
- **Trước phát hành công khai:** thêm kết quả về quyền, tải và media theo ngưỡng, dữ liệu và vận hành trong phạm vi, người nhận hỗ trợ.
- Chưa có người ký nghiệm thu (DEC-111), nên chưa thể tuyên bố đạt điều kiện phát hành.
- Đợt này không khảo sát người dùng bên ngoài (DEC-030); điều đó không thay cho kiểm thử khả dụng nội bộ, kiểm thử hệ thống hay nghiệm thu của đại diện khách hàng.

<a id="mẫu-ghi-nhận-kết-quả"></a>

### Ghi kết quả

| Trường | Ghi gì |
|---|---|
| Mã | AC, SCP hoặc OQ liên quan |
| Bản chạy | Phiên bản triển khai, trình duyệt, thiết bị, dữ liệu thử |
| Kết quả | Đạt, chưa đạt hay chưa chạy; bằng chứng và thời điểm |
| Lỗi | Mã lỗi, mức, người phụ trách, kết quả kiểm tra lại |
| Xác nhận | Người chạy và người xác nhận |

- Ca phụ thuộc quy tắc chưa chốt ghi "Chờ chốt quy tắc".
- Trạng thái gắn với build và lần chạy, không gắn với việc có file test.
- Ca dữ liệu: [vòng đời dữ liệu](../system/data-lifecycle.md#acceptance). Mẫu đầy đủ: [hồ sơ phát hành](../records/templates/release-record.md), [hồ sơ sự cố](../records/templates/incident-record.md). Nơi lưu hồ sơ chọn khi triển khai; không đưa bí mật hay dữ liệu thật vào Git.

**Hợp đồng và fixture dùng để kiểm thử** (kiểm tra JSON và fixture chỉ xác nhận tài liệu nhất quán, không phải kết quả chạy):

- Tài khoản và DM: [account-recovery](../contracts/account-recovery.openapi.json), [direct-messaging](../contracts/direct-messaging.openapi.json), [chat-realtime](../contracts/chat-realtime.schema.json), [text-validation](../fixtures/text-validation.json), [text-policy](../fixtures/text-policy.json), [dm-fingerprint](../fixtures/dm-fingerprint.json).
- Community: [OpenAPI 45 thao tác](../contracts/community.openapi.json), [community-realtime](../contracts/community-realtime.schema.json), [community-permissions](../fixtures/community-permissions.json), [community-operations](../fixtures/community-operations.json). Dùng cho TC-ACL-08 đến 12, TC-COM-18 đến 27.
- Media: [media OpenAPI](../contracts/media.openapi.json), [media-realtime](../contracts/media-realtime.schema.json), [media-lifecycle](../fixtures/media-lifecycle.json).
- Dữ liệu: [data-protection](../contracts/data-protection.schema.json), [data-lifecycle](../fixtures/data-lifecycle.json). Hash và chữ ký trong bản ghi mẫu chỉ là minh họa.

<a id="smoke"></a>

<a id="bộ-kiểm-tra-nhanh-sau-triển-khai"></a>

## Kiểm tra nhanh sau triển khai

Bộ này phát hiện lỗi cấu hình hay tích hợp sau khi triển khai hoặc rollback. Nó không thay kiểm thử hồi quy, tải, tranh chấp hay ngắt media.

**Chưa ca nào được chạy.** Ca chưa có backend ghi "Bị chặn", không dùng dữ liệu mẫu trên giao diện thay. Dùng tài khoản giả, email thử, trình duyệt máy tính; các hành trình Tài khoản, DM, Community chạy thêm trên điện thoại.

| Ca | Thao tác | Kết quả mong đợi |
|---|---|---|
| RLS-SM-01 | Mở web và API qua domain thật; kiểm tra phụ thuộc, build, cấu hình | Đúng bản build; có kiểm tra DB thật; token Development tắt. Health tĩnh không đủ (GATE-01, 06) |
| RLS-SM-02 | Đăng ký → nhận email → xác minh → đăng nhập bằng email và tên tài khoản | Cùng tài khoản; trước xác minh bị chặn; response không lộ token (AC-ACC-01, 02, 07, 19) |
| RLS-SM-03 | Quên mật khẩu → email → đặt lại; dùng lại link và phiên cũ; đăng nhập mới | Link dùng một lần; mật khẩu đổi và phiên cũ bị thu hồi (AC-ACC-06, 16, 19, 20) |
| RLS-SM-04 | Hai người tìm nhau, mở DM, gửi, tải lại; gửi khi người kia offline rồi họ vào lại | Đúng người, ID, nội dung, lịch sử (AC-DM-01 đến 03, 07) |
| RLS-SM-05 | Làm mất phản hồi khi gửi, gửi lại cùng khóa; sửa, xóa rồi gửi lại | Một tin; trạng thái mới nhất hoặc tombstone; không hồi sinh nội dung (AC-DM-08, 10, 14) |
| RLS-SM-06 | Tạo cộng đồng và phòng; tham gia, mời, xin duyệt theo từng cách; chuyển chủ, rời, tham gia lại | Tư cách và quyền đúng hiện tại; không khôi phục vai trò cũ (AC-COM) |
| RLS-SM-07 | Thành viên và người ngoài thử đọc, gửi, sửa tin; đổi ACL và rời server khi kết nối đang mở | Chặn sai phạm vi và sai tác giả; ngắt trong 5 giây (AC-COM-11, 18, 21, 42; DEC-083) |
| RLS-SM-08 | Gọi DM hoặc vào phòng thoại; nghe trước rồi bật micro, camera, chia sẻ; kết thúc | Ban đầu tắt; nguồn đúng; màn hình không có âm thanh; trả chỗ đúng (AC-MEDIA, DEC-101, 102) |
| RLS-SM-09 | Thu hồi phiên, quyền xem, hoặc xóa phòng khi chat và media đang chạy | Không nhận chat hay luồng media sau khi ngắt; token và epoch cũ không vào lại được (DEC-083, 099; AC-DATA-04) |
| RLS-SM-10 | Khóa người kia; thử xem lịch sử, gửi, gọi; mở khóa rồi thử phiên cũ (bằng công cụ được phép) | Lịch sử còn; tương tác mới bị chặn; phiên cũ không sống lại (AC-DATA-02, 03) |
| RLS-SM-11 | (Sau v1, khi có khôi phục) thử tombstone, placeholder, dấu chặn; xác thực, link, media cũ; khóa gửi lại | Đúng DEC-108; không email hay đổ chuông cũ; không mất khóa chống trùng (AC-DATA-05, 11, 13, 15). Không phải điều kiện v1 |
| RLS-SM-12 | v1: kích hoạt cảnh báo thử, kiểm tra dọn dữ liệu và bàn giao. Sau v1 thêm backup | Cảnh báo đúng người; có bằng chứng phụ thuộc, ngắt, thời hạn lưu (GATE-05, 08) |

- SM-11 chỉ chạy khi phạm vi có khôi phục; không tự tạo một lần khôi phục production để chạy.
- SM-10 cần công cụ quản trị (DEC-112), chưa có trong repo. Không dùng SQL tay để bỏ qua audit và guard rồi ghi là đạt.

---

Nguồn: DEC-030, DEC-059, DEC-082, DEC-083, DEC-085, DEC-089, DEC-099, DEC-101, DEC-102, DEC-108, DEC-110, DEC-111, DEC-112, DEC-113, DEC-156, [DEC-171](../records/decisions/README.md#dec-171).
