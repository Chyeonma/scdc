# SCDC — Độ phủ đặc tả và sẵn sàng phát triển

Nguồn chuẩn cho độ phủ tài liệu, READY/PREP và bằng chứng cần chuẩn bị. Trạng thái triển khai hiện tại nằm tại `features/<tính-năng>/status.md`; bảng dưới đây không thay hồ sơ nghiệm thu từng gói.

<a id="coverage"></a>

## Độ phủ đặc tả

| Phạm vi | Đầu ra hiện có | Khoảng trống chính | Trạng thái |
|---|---|---|---|
| SCP-001 Web | DEC-059; [wireframe tài khoản/DM](../features/direct-messaging/specs/ux.md#ux) | Đã chốt ma trận trình duyệt cho Accounts/DM/Community và media desktop (DEC-082), ngưỡng chat/thu hồi (DEC-083); còn OS/thiết bị/build, tiếp cận và kết quả đo (OQ-007). | Phạm vi/ngưỡng đã chốt; cần kiểm chứng |
| SCP-002 Tài khoản | [SCDC-FR-ACC-001](../features/accounts/specs/requirements.md#requirements) | Chốt định danh, mật khẩu/lockout, phiên/thời hạn, gửi lại, hồ sơ và kênh khôi phục tại DEC-063–068; có trạng thái/dữ liệu và ACC-GAP-01–07. Email/resend/limiter đã có phương án; còn khóa schema/ngưỡng/provider/key store và kiểm chứng các chênh lệch source (OQ-002/OQ-008). | [Trạng thái theo gói](../features/accounts/status.md) |
| SCP-003 Cộng đồng | [đặc tả](../features/community/README.md#requirements), [5 thành phần và UC](../features/community/design/README.md#organization), [thiết kế tích hợp](../features/community/design/integration.md#contracts) | Luồng DEC-072–077/087 và search/tên/phạm vi/private switch/issuer DEC-093–097 đã chốt; có OpenAPI/realtime/transaction/migration, còn review/mock/proof (OQ-003/OQ-008). | [Trạng thái theo gói](../features/community/status.md) |
| SCP-004 Phân quyền | [ma trận](../features/community/specs/permissions.md#permissions), [thiết kế Permissions](../features/community/design/permissions.md#detailed-design) | @everyone/20 custom role/union DEC-092 và quản lý cần view DEC-098 đã chốt; có role/ACL API, epoch/accessVersion/guard/fixture; còn review/migration/đo thu hồi (OQ-004/OQ-007). | [Trạng thái theo gói](../features/community/status.md) |
| SCP-005 Nhắn tin | [DM](../features/direct-messaging/specs/requirements.md#requirements), [cộng đồng](../features/community/README.md#requirements), [hợp đồng DM](../features/direct-messaging/design/README.md#contracts), [vòng đời](../data-lifecycle.md) | DEC-068–071/081/090/091 chốt nội dung/tìm/transport/draft; DEC-103–109 chốt phạm vi account/retention/restore. Có HMAC/SQL/Hub/cursor-resume/placeholder/fixture; còn review/migration/shared guard, kho sổ/worker và proof (OQ-005/008/011). | [Trạng thái theo gói](../features/direct-messaging/status.md) |
| SCP-006 Thoại | [Đặc tả](../features/voice-video/specs/requirements.md#requirements), [thiết kế media](../features/voice-video/design/README.md#detailed-design) | DEC-078–085/099–102 chốt điều kiện, 10/20/2, ring/reconnect, desktop/chất lượng/cutoff/multi-device/thiết bị. Có OpenAPI 16 công khai +3 nội bộ, realtime/fixture/coordinator/lease; còn review và proof SFU/migration (OQ-006/007/008). | [Trạng thái theo gói](../features/voice-video/status.md) |
| SCP-007 Video/chia sẻ màn hình | [Thiết kế media](../features/voice-video/design/README.md#detailed-design) | DEC-079/101/102 chốt nguồn/người, 2 share/phòng, ban đầu tắt, screen chỉ hình. Có source permit/quota gate/layout/AC/TC; SDK/extension/build và capture matrix thực tế còn cần review/proof. | [Trạng thái theo gói](../features/voice-video/status.md) |
| SCP-008 Phát hành | [nghiệm thu/gate](../release-operations.md#release-gates), [runbook](../operations-runbook.md), [mẫu hồ sơ](../templates/release-record.md) | DEC-110 chốt lỗi tồn, DEC-112 chốt quản trị kỹ thuật/no admin UI; người duyệt DEC-111 chưa chọn. Còn người trực/topology/tool và bằng chứng RLS-GAP-01–08. | Đã soạn gate/smoke/runbook/mẫu; chưa triển khai/diễn tập |

Đầu ra chuẩn bị và điều kiện còn thiếu theo gói tài khoản/DM được theo
dõi tại [SCDC-READY-001](#readiness).
Bảng độ phủ ghi nội dung đặc tả và đầu vào còn thiếu; trạng thái theo gói ở các liên kết trên. Không đánh dấu toàn bộ scope đạt từ việc có test hoặc một gói đã được kiểm chứng.

### Tiến độ hoàn thiện tài liệu ngày 2026-10-04

Bảng này đánh giá nội dung đã viết và quyết định còn cần, không đánh giá phần mềm đã hoàn thành. “Có bản thiết kế” chưa có nghĩa đã chạy thử hoặc được nghiệm thu.

| Phần | Nội dung đã hoàn thiện trong đợt này | Còn cần để khóa tài liệu |
|---|---|---|
| Mục tiêu/phạm vi | REQ/SCP/SUC, luồng ưu tiên và ranh giới v1 nhất quán | Nhóm sử dụng đầu tiên/ngôn ngữ và cách đánh giá nội bộ đang được hỏi |
| Tài khoản | Quy tắc, UX, [12 use case và đối chiếu source/test](../features/accounts/specs/use-cases.md#use-cases), AC/TC; hành vi mật khẩu trùng DEC-113; thiết kế chi tiết token/consume/EmailDelivery, recovery OpenAPI 4 thao tác | Limiter bổ sung hoãn DEC-089; provider/key store và kiểm chứng ACC-GAP/API/UI thuộc triển khai |
| DM | Quy tắc/UX, OpenAPI 7 thao tác, schema SignalR, HMAC/mapping SQL/guard/cursor-resume; validation/draft và fixture; account lock/restore placeholder DEC-104/108 | Review/mock, migration/projection/sổ bảo vệ và proof concurrency/thu hồi/restore khi triển khai |
| Cộng đồng/quyền | Quy tắc DEC-092–098; role/ACL/epoch, OpenAPI 45 thao tác, realtime 9 loại, fingerprint/fixture, AC/TC; room retention/restore DEC-105/108 | Review/mock, migration/guards và proof concurrency/thu hồi/restore; metadata nghiệp vụ khác chưa đặt TTL riêng |
| Vòng đời dữ liệu | Policy/matrix DEC-103–109, schema sổ bảo vệ, placeholder/availability, 49 vector +10 schema cases +6 kịch bản, AC/TC và DATA-GAP-01–06 | Review/migration/worker, kho sổ/key/checkpoint và proof hai kho/restore; scope quyền/thẩm quyền vận hành còn mở |
| Media | Quy tắc DEC-099–102, OpenAPI 19 thao tác, realtime 8 loại, room/call/participation/capacity/source/lease, 30 vector +8 transition +4 HMAC, AC/TC và MEDIA-GAP-01–07; TTL terminal DEC-107 | Review/migration, chọn/pin SDK/server và đánh giá extension/fork, proof quota/admission/cutoff/load/cleanup; host/key store và vận hành còn mở |
| Kế hoạch/dự toán | Ngày đầu 05/10/2026, công suất 3–4 ngày/tuần; phân công/giai đoạn theo DEC-117, 80% phân bổ + 20% dự phòng; giữ ngân sách tượng trưng 250 triệu từ phương án cũ | Ước lượng lịch MVP/v1 theo gói; đo ngày công/lịch nghỉ và kết quả thực tế để cân lại tải |
| Nghiệm thu/vận hành | Quy trình/lỗi tồn DEC-110, 10 gate/12 smoke; [runbook](../operations-runbook.md) 8 tình huống, lịch/bàn giao; mẫu phát hành/sự cố; quản trị kỹ thuật DEC-112 | Người duyệt chưa chọn DEC-111; người trực/thay thế/lịch, provider/domain/key store/tool và lệnh production còn mở; kết quả diễn tập thuộc triển khai |

Các đầu vào sản phẩm còn mở: lịch MVP/v1, đối tượng/ngôn ngữ, người duyệt và phân công/lịch trực vận hành. Phân công phát triển theo DEC-117; phạm vi quản trị kỹ thuật/no admin UI theo DEC-112. Các gói technical evidence/PREP/RLS-GAP vẫn cần thực thi trong kế hoạch phát triển, không phải ca đã chạy trong lần hoàn thiện tài liệu.

<a id="documentation-remaining"></a>

### Phần tài liệu còn cần hoàn thiện

Rà soát ngày 2026-10-04; cập nhật mốc kiến trúc và phân công ngày 2026-10-06 theo DEC-116/117. Các mục dưới đây tách công việc viết/rà soát thiết kế khỏi quyết định sản phẩm và bằng chứng cần tạo khi triển khai. DEC-089 hoãn lựa chọn limiter, DEC-111 giữ người duyệt chưa được chọn; quy tắc nghiệp vụ đã chốt được giữ.

| Nhóm | Nội dung tài liệu còn thiếu hoặc chưa khóa | Việc có thể tiếp tục soạn | Quyết định/đầu vào còn cần |
|---|---|---|---|
| Nhu cầu và giá trị sản phẩm | Người dùng đầu tiên, ngôn ngữ/khu vực, kết quả mong muốn và cách đánh giá nội bộ | Kịch bản đánh giá theo hành trình DM và tổ chức phòng; ghi rõ giả định chưa khảo sát | Chọn nhóm sử dụng/ngôn ngữ và cách đánh giá; OQ-001/OQ-012 |
| UX/UI | Có wireframe văn bản; chưa có hồ sơ tương tác/prototype được rà soát, tiêu chí tiếp cận và cấu hình thiết bị cụ thể | Chi tiết trạng thái màn hình, thao tác bàn phím/focus, lỗi/mất mạng/mất quyền, bố cục cuộc gọi và checklist rà soát | Nhóm dùng/ngôn ngữ; ghi browser/OS/thiết bị thực tế khi có build, giữ ma trận DEC-082 |
| Tài khoản | Có thiết kế chi tiết/request-response/consume/schema email; provider/key store và limiter còn mở | Rà soát contract recovery + schema migration/policy/delivery; ACC-GAP theo gói triển khai | Ngưỡng limiter được hoãn DEC-089; email provider/domain/key store; OQ-002/OQ-008 |
| DM | Validation/bản nháp đã chốt; HMAC/SQL/Hub/cursor-resume và fixture đã chi tiết hóa, chưa có mock/proof | Review đồng bộ OpenAPI/schema và UI, migration legacy/transaction guard; mock/proof thuộc triển khai | DEC-103/104/108 chốt no self-delete/account lock/restore placeholder; projection/guard/schema và DATA-GAP proof còn triển khai |
| Cộng đồng và quyền | Có thiết kế chi tiết role/ACL/epoch, OpenAPI/realtime/fixture và mapping migration; chưa có review/mock/proof | Rà soát contract/UX, migration Unicode và guards/outbox/key ring; bằng chứng transaction/thu hồi thuộc triển khai | Vai trò/search/tên/private switch/issuer/quản lý phòng đã chốt DEC-092–098; limiter bổ sung còn mở, không xóa server v1; retention chính DEC-105–109 đã chốt, còn proof/migration |
| Media | Có thiết kế chi tiết/OpenAPI/realtime/fixture/coordinator/lease/quota gate; chưa có implementation hoặc proof | Review schema/UX/fingerprint, migration/shared guards; thử quota gate và fail-close trên build LiveKit tự host được pin | Quy tắc bổ sung đã chốt DEC-099–102; limiter media, host/domain/key store, extension/server/SDK và kho sổ/worker còn OQ-006/007/008/010/011; retention DEC-106/107 đã chốt |
| Kế hoạch và dự toán | Có phân công/công suất theo giai đoạn tại DEC-117, 20% dự phòng; ngân sách tượng trưng 250 triệu giữ cơ sở cũ, lịch chưa được chọn | Ước lượng từng gói và lịch MVP/v1; dùng ngày công/kết quả thực tế để cân lại tải | Nhóm rà soát ngày công/lịch nghỉ, thử nghiệm service/media và cập nhật dự toán; OQ-009 |
| Nghiệm thu và vận hành | Có [quy trình/10 gate/12 smoke](../release-operations.md), [runbook 8 tình huống](../operations-runbook.md), [mẫu phát hành](../templates/release-record.md)/[sự cố](../templates/incident-record.md); DEC-110/112 chốt lỗi tồn/quản trị | Review và bổ sung manifest/lệnh thật khi chọn công cụ, khóa ma trận đo/thiết bị, diễn tập và điền hồ sơ theo RLS-GAP-01–08 | Người duyệt chưa chọn DEC-111; người trực/thay thế/lịch, provider/domain/key store/tool; OQ-007/008/010/011 |
| Vòng đời dữ liệu | Có [policy/matrix/thiết kế](../data-lifecycle.md), DEC-103–109, schema sổ bảo vệ, contentState/availability, fixture và AC-DATA/TC-DATA | Review/migration/guard/worker, chọn kho sổ/key/tool và proof hai kho/restore/retention | Phạm vi/TTL/restore đã chốt; người vận hành/thẩm quyền và công cụ còn OQ-008/011 |

**Bằng chứng thuộc gói triển khai:** mock/prototype chạy được, kiểm thử ACC-GAP, lưu bền/chống trùng/đồng thời/reconnect/thu hồi chat, admission LiveKit, tải/chất lượng media và diễn tập restore/rollback. Cần kế hoạch và mẫu ghi nhận trong docs; kết quả chỉ điền sau khi thực thi trên build/môi trường cụ thể. Không dùng việc chưa chạy các ca này để kết luận mọi quy tắc nghiệp vụ đều chưa được chốt.

Tài khoản/DM, cộng đồng/quyền, media và vòng đời dữ liệu đã có bản thiết kế chi tiết; nghiệm thu/vận hành đã có checklist, runbook và mẫu hồ sơ. Các bản này còn review và đầu vào ở bảng trên. Phần có thể tiếp tục soạn là UX/UI; nhu cầu/ngôn ngữ, lịch và tên người vận hành/người duyệt được cập nhật khi người dùng chọn. Không ghi các đề xuất phân công hoặc kết quả diễn tập là đã xác nhận.

<a id="readiness"></a>

## Sẵn sàng phát triển và gói chuẩn bị

Bảng này theo dõi đầu vào và điều kiện sẵn sàng của gói. Trạng thái implementation/bằng chứng nằm tại [Accounts](../features/accounts/status.md), [Community](../features/community/status.md), [DM](../features/direct-messaging/status.md) và [Media](../features/voice-video/status.md). Community phân biệt nền trên `main` với các gói đã kiểm chứng trên nhánh feature; kết quả lịch sử chỉ áp dụng commit và phạm vi trong hồ sơ gói. Các test tự động trong repo được liệt kê ở [hướng dẫn phát triển](../development.md#testing).

### Bảng điều kiện sẵn sàng

| Mã | Điều kiện/đầu ra | Tình trạng và bằng chứng | Việc còn lại/đầu mối dự kiến |
|---|---|---|---|
| READY-01 | Tài khoản tối thiểu | Đã chốt ACC-P01–05 (DEC-063–068) và mật khẩu trùng DEC-113; có thiết kế token/email/consume, OpenAPI recovery 4 thao tác, AC-ACC-01–22 và [12 use case/coverage source-test](../features/accounts/status.md#use-case-coverage); chưa chạy kiểm thử trong bước tài liệu | Vg hoàn thiện Identity/job contract; Thái làm delivery/provider theo contract và ghi proof; Sáng kiểm tra phần DM sử dụng. Limiter bổ sung hoãn DEC-089 |
| READY-02 | DM và ngoại lệ | Đã chốt 2.000 UTF-16, tìm người, không tự hết hạn, cách gửi, validation và bản nháp (DEC-068–071/090/091); có AC-DM và ngoại lệ tại [đặc tả](../features/direct-messaging/specs/requirements.md#requirements) | Validation/bản nháp đã chốt DEC-090/091; có Unicode fixture, HMAC/mapping SQL/Hub/resume; còn review/mock, kiểm chứng đồng thời và no self-delete/account lock/restore đã chốt DEC-103/104/108, còn DATA-GAP proof; Vg/Sáng, Thái đối chiếu |
| READY-03 | Ma trận quyền và thiết kế Community | [5 thành phần và 25 UC](../features/community/design/README.md#organization), [Permissions](../features/community/design/permissions.md#detailed-design) và [tích hợp](../features/community/design/integration.md#contracts) có role/ACL/epoch/45 REST/9 realtime/fixture, AC-COM-01–42; [gói đầu tiên](../features/community/delivery/README.md#use-case-delivery) đã xác định | [Tiến độ Community](../features/community/status.md) quản lý implementation/bằng chứng theo gói; thu hồi/phòng/role và các nhánh còn lại cần proof. Media deadline DEC-099 và retention chính DEC-105–109 đã chốt, còn worker/restore proof; DM vẫn độc lập role cộng đồng |
| READY-04 | UX hai hành trình | Có wireframe văn bản [tài khoản/DM](../features/direct-messaging/specs/ux.md#ux) và [cộng đồng](../features/community/specs/integration.md#ux) | Vg làm UI Identity/Community, Sáng làm UI DM theo gói; Thái cung cấp dataset/bộ chạy. Ma trận trình duyệt đã chốt DEC-082; khóa OS/thiết bị/build và trạng thái còn mở |
| READY-05 | API/dữ liệu DM | Có [hợp đồng đề xuất](../features/direct-messaging/design/README.md#contracts) với schema logic, lỗi, chống trùng, lịch sử và cập nhật | REST/SignalR, ID/cursor đã chọn DEC-081; Vg/Sáng rà soát schema/lỗi cùng Thái; Identity có OpenAPI từ code; DM có [OpenAPI dự thảo](../contracts/direct-messaging.openapi.json), có schema realtime/fixture/thiết kế chi tiết; chưa có mock hoặc proof chạy được |
| READY-06 | Thử nghiệm kỹ thuật | Có kịch bản cần chứng minh tại [bằng chứng thử nghiệm](#technical-evidence); chưa chạy | MVP kiểm chứng luồng trên một host; v1 chuyển microservice theo DEC-116. Vg thiết kế/Identity/Community, Sáng Messaging, Thái môi trường/bộ chạy theo gói V1-ARCH |
| READY-07 | Kiểm thử | Có [ca kiểm thử](../features/direct-messaging/specs/acceptance.md#tests) và dữ liệu dự kiến; kết quả quản lý theo gói | Mỗi người thử phần sở hữu, người khác kiểm tra lại; Vg/Sáng giữ kỳ vọng quyền/đồng thời, Thái dataset/bộ chạy/kết quả. Ngưỡng chat DEC-083 và media DEC-085 áp dụng theo hồ sơ mốc |
| READY-08 | Công việc/nguồn lực | Có gói chuẩn bị, [phân công](planning.md#team) và [công suất](planning.md#capacity) theo DEC-117; lịch từng mốc chưa khóa | Vg/Sáng/Thái ước lượng trong quỹ thời gian thật, tính học/hướng dẫn/review/tự kiểm thử và 20% dự phòng; đo lại sau 1–2 tuần |

Không tính phần trăm từ số dòng hoàn tất. Mỗi gói chỉ sẵn sàng khi có
yêu cầu, thiết kế, kiểm chứng và người phụ trách tương ứng. Quyết định
media, chi phí toàn dự án và vận hành vẫn phải hoàn thành ở các đợt
liên quan; không tự loại chúng khỏi bản hoàn thiện v1 để đánh dấu Đợt 0 xong.

<a id="preparation"></a>

### Gói việc đủ cụ thể để ước lượng

| Gói | Đầu ra | Phụ thuộc | Thực hiện / rà soát dự kiến | Điều kiện hoàn tất |
|---|---|---|---|---|
| PREP-01 | Hoàn thiện thiết kế tài khoản/job email và fixture cho luồng đã chọn | ACC-GAP-01–07, OQ-002 | Vg giữ contract; Thái proof delivery / Sáng kiểm tra phần DM dùng | Quy tắc/token/job không còn chỗ diễn giải khác nhau; AC của luồng được cập nhật |
| PREP-02 | UI tài khoản/DM và cộng đồng theo mã màn hình | Wireframe, quyết định liên quan | Vg: Identity/Community; Sáng: DM / kiểm tra chéo; Thái dataset/bộ chạy | Có trạng thái rỗng/lỗi/mất mạng/mất quyền; ghi kết quả rà soát |
| PREP-03 | Hợp đồng API có schema máy đọc được và mock | SCDC-API-DM-001, PREP-01 | Sáng sở hữu DM; Vg review quyền / Thái dùng mock cho bộ chạy | Request/response/lỗi thống nhất; không trả dữ liệu ngoài quyền |
| PREP-04 | Thử nghiệm DM lưu bền/chống trùng/phân trang | PREP-03, lựa chọn công nghệ | Sáng / Vg review quyền/dữ liệu; Thái hỗ trợ bộ chạy | Các [kịch bản thử nghiệm](#technical-evidence) có bằng chứng theo scope; rủi ro được ghi nhận |
| PREP-05 | Thử nghiệm cập nhật, reconnect và thu hồi phiên | PREP-04, thiết kế phiên | Sáng: realtime; Vg: session/revoke / Thái bộ chạy lỗi mạng | Không trùng/sót dữ liệu; kết nối bị thu hồi đúng ngưỡng áp dụng |
| PREP-06 | Ma trận thiết bị và fixture kiểm thử | DEC-059, AC và ca kiểm thử | Thái: matrix/dataset/bộ chạy; Vg/Sáng: kỳ vọng và thực thi phần sở hữu | Danh sách trình duyệt/phiên bản, kích thước, dữ liệu và kết quả mong đợi rõ |
| PREP-07 | Ước lượng và lịch đợt tài khoản/DM | PREP-01–06 đủ rõ | Vg, Sáng, Thái | Ngày công, người làm/rà soát, thời gian hướng dẫn và phụ thuộc không trùng quỹ thời gian |

Đây là phân công kế hoạch theo DEC-117; không ghi các gói đã hoàn tất hoặc lịch đã được cam kết. PREP nằm trong quỹ của gói tính năng/công cụ tương ứng, không cộng thành việc toàn thời gian bổ sung. Chọn PREP theo luồng MVP trước; phần hoàn thiện Community, chuyển service và media tiếp tục ở v1.

<a id="technical-evidence"></a>

### Bằng chứng thử nghiệm kỹ thuật cần có

Đây là mục tiêu của bản đầy đủ v1; MVP chọn bằng chứng theo luồng tại [hồ sơ MVP](../releases/mvp.md#acceptance). Thiết kế transaction/row lock xuyên Identity–Messaging trong một host phải được rà soát lại khi chuyển service ở v1 theo DEC-116; không coi việc đổi tên mốc là đã chuyển thiết kế monolith thành microservice.

1. Hai người xác thực được, mở cùng một DM khi tạo đồng thời; người thứ
   ba không truy cập được API hoặc kênh cập nhật.
2. Gửi tin lưu bền; rollback không phát sự kiện; mất response sau commit
   và gửi lại đồng thời chỉ tạo một tin.
3. Phân trang không bỏ sót tin khi có giao dịch commit trễ; reconnect
   bù nhiều trang và cập nhật sửa/xóa tin cũ, không dùng cache hết hiệu lực.
4. Worker dừng rồi hoạt động lại; sự kiện lặp/đảo thứ tự không nhân đôi
   hoặc đưa giao diện về phiên bản tin cũ.
5. Thu hồi phiên chặn HTTP và kết nối chat đang mở trong ≤5 giây theo DEC-083; ghi cách đo, không tuyên bố thu hồi tức thời khi chưa chứng minh.

Mỗi bằng chứng ghi môi trường, bản thử, dữ liệu, thao tác, kết quả, hạn
chế và người rà soát. Viết kế hoạch hoặc sơ đồ không thay thế chạy thử.

### Thứ tự xử lý tiếp theo

Ngày 2026-10-04 đã bổ sung thiết kế chi tiết Accounts/DM, Community/quyền/tin phòng, Media/LiveKit tự host, [vòng đời dữ liệu](../data-lifecycle.md) và [nghiệm thu/vận hành](../release-operations.md) với gate/smoke/runbook/mẫu hồ sơ. Ngày 2026-10-05 bổ sung use case Identity và DEC-113 về mật khẩu trùng. Ngày 2026-10-06 tách MVP/v1, chốt MVP một host/microservice ở v1 và cập nhật phân công/công suất theo DEC-114/116/117; chưa sửa mã hoặc chạy kiểm thử sản phẩm. Limiter tài khoản hoãn DEC-089, người duyệt chưa chọn DEC-111. Các bản thiết kế còn cần rà soát; provider/key store/kho sổ, lịch và bằng chứng triển khai ở [phần còn cần hoàn thiện](#documentation-remaining). UX/UI tiếp tục theo chủ gói. Chỉ xác nhận gói phát triển khi các phụ thuộc trực tiếp được giải quyết; không cần chờ toàn bộ media để rà soát Accounts/DM/Community.

Các nội dung còn mở được tập trung tại OQ-002–OQ-011; bảng này dẫn chiếu
và theo dõi đầu ra, không tạo một nguồn quyết định sản phẩm khác.
