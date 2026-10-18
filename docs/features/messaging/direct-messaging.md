# Nhắn tin riêng

REQ-005; phần DM của SCP-005. Sáng phụ trách Messaging và UI DM; Vg rà soát quyền và hợp đồng, Thái cung cấp dataset/bộ chạy. Phạm vi phải bàn giao theo [MVP](../../releases/mvp.md) và [v1](../../releases/v1.md).

Backup/restore, sổ bảo vệ độc lập và UI placeholder sau restore thuộc [giai đoạn sau v1](../../project/overview.md#backup-scope) theo DEC-156. Retention online, sửa/xóa, chống trùng và đồng bộ sau reconnect vẫn thuộc v1.

<a id="status"></a>

## Trạng thái hiện tại

Nền MVP `900e226` đã có tìm người, mở hội thoại, inbox, gửi/lịch sử tin, retry chủ động, sửa/xóa, ChatHub/outbox và tự reconnect/bù tin trong một API host. Hồ sơ theo gói tại [hồ sơ DM](../../records/verification/messaging/README.md) ghi xác nhận người dùng cho P0–P3 và bằng chứng agent của [DM-MVP](../../records/verification/messaging/DM-MVP.md); [kiểm tra hợp nhất 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md) ghi luồng DM tích hợp Community. Kết quả giữ revision/phạm vi của từng hồ sơ; không tuyên bố toàn bộ hợp đồng/AC, toàn DM hoặc v1 đã được người dùng nghiệm thu.

v1 kế thừa nền này, tiếp tục phần còn thiếu và hồi quy sau tách service/database. Tin phòng, Media và cơ chế quyền/thu hồi xuyên service cần bằng chứng riêng. Lần cập nhật hiện trạng này chỉ đối chiếu mã/tài liệu, không chạy lại test ứng dụng.

DM chỉ dựa trên hai người tham gia, không dùng vai trò/ACL Community. Identity sở hữu danh tính và phiên; Messaging dùng hợp đồng `SCDC.Contracts`, không đọc bảng Identity trực tiếp. Cơ chế [lưu và thay đổi tin](message-lifecycle.md), [đồng bộ](synchronization.md) và [nội dung](text-policy.md) áp dụng chung với [tin phòng](channel-messaging.md).

<a id="requirements"></a>

<a id="phạm-vi-hành-trình-và-quy-tắc"></a>

## Phạm vi, hành trình và quy tắc

<a id="mục-tiêu-và-phạm-vi-đợt-đầu"></a>

### Mục tiêu và phạm vi đợt đầu

Người dùng đã đăng nhập có thể tìm một người khác, bắt đầu hội thoại
riêng, gửi và xem lại tin nhắn văn bản. Người gửi có thể sửa hoặc xóa tin
của mình. Tin nhắn và lịch sử hội thoại được lưu để xem lại theo SCP-005
và SUC-003 trong [Project Brief](../../project/overview.md#scope).

Đợt đầu ưu tiên tin nhắn văn bản. Hình ảnh, file tài liệu và cách xử lý
khi người nhận không muốn nhận tin từ một tài khoản cụ thể được xếp vào
đợt sau theo DEC-017 và DEC-018. Việc chọn đợt triển khai không thay đổi
phạm vi bản hoàn thiện v1 trong Project Brief.

<a id="hành-trình-chính"></a>

### Hành trình chính

1. Người dùng đăng nhập và tìm người nhận bằng một phần tên tài khoản
   hoặc tên hiển thị.
2. Người dùng chọn đúng tài khoản trong kết quả và mở hội thoại riêng.
   Không cần kết bạn hoặc cùng cộng đồng để bắt đầu.
3. Người dùng nhập và gửi tin nhắn văn bản. Khi hệ thống đã lưu tin, giao
   diện hiển thị “Đã gửi”; tin không lưu được có trạng thái lỗi gửi.
4. Người nhận mở hội thoại và xem tin nhắn, kể cả tin đã được lưu khi họ
   chưa mở ứng dụng. Khi mở lại hội thoại, hai bên có thể xem lịch sử còn
   hiệu lực.
5. Người gửi có thể sửa tin của mình bất cứ lúc nào. Nội dung mới và dấu
   “Đã sửa” được thể hiện cho cả hai bên.
6. Người gửi có thể xóa tin của mình cho cả hai bên. Vị trí tin được thay
   bằng dòng “Tin nhắn đã bị xóa”.
7. Nếu gửi lỗi, người gửi chủ động bấm thử lại. Ứng dụng không tự thử lại.
   Dù kết quả lần gửi đầu chưa rõ, cùng thao tác chỉ tạo một tin trong
   hội thoại.

<a id="quy-tắc-đã-xác-định"></a>

### Quy tắc đã xác định

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| DM-001 | Tìm người nhận theo một phần tên tài khoản hoặc tên hiển thị. | DEC-016, DEC-019 |
| DM-002 | Có thể bắt đầu nhắn ngay; không cần kết bạn hoặc cùng cộng đồng. | DEC-016 |
| DM-003 | Đợt đầu gửi tin nhắn văn bản. | DEC-017 |
| DM-004 | Người gửi sửa tin của mình bất cứ lúc nào; hiển thị dấu “Đã sửa”. | DEC-020 |
| DM-005 | Người gửi xóa tin của mình cho cả hai người; hiện dòng “Tin nhắn đã bị xóa”. | DEC-020 |
| DM-006 | “Đã gửi” nghĩa là hệ thống đã lưu tin; nếu không lưu được thì báo lỗi và người gửi bấm thử lại. | DEC-021 |
| DM-007 | Trường hợp người nhận không muốn nhận tin từ một tài khoản cụ thể được xử lý ở đợt sau. | DEC-018 |
| DM-008 | Người nhận thấy tin đã được hệ thống lưu khi mở lại ứng dụng sau lúc vắng mặt. | DEC-035 |
| DM-009 | Bấm thử lại cùng một thao tác gửi không tạo tin trùng, kể cả khi kết quả lần đầu không rõ. | DEC-037 |
| DM-010 | Người gửi phải xác minh email trước khi gửi tin riêng. | DEC-041, SCDC-FR-ACC-001 |
| DM-011 | Chỉ giữ nội dung mới nhất sau khi sửa, không cung cấp lịch sử phiên bản cũ; vẫn hiện dấu “Đã sửa”. | DEC-052 |
| DM-012 | Mỗi tin tối đa 2.000 đơn vị UTF-16; cho xuống dòng/emoji; từ chối tin rỗng/chỉ có khoảng trắng. | DEC-053, DEC-068 |
| DM-013 | Từ khóa tìm người 2–64 UTF-16, một phần username/displayName; không phân biệt hoa/thường, giữ dấu; ưu tiên username khớp đúng; trang mặc định 20, tối đa 50. | DEC-069 |
| DM-014 | Tin không tự hết hạn trong v1; sửa/xóa theo quy tắc đã chốt. Account chưa có self-delete, khóa không xóa lịch sử; backup/restore theo chính sách vòng đời áp dụng sau v1. | DEC-070/103/104/108/109/156 |
| DM-015 | Desktop Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng, nút Gửi gửi tin. | DEC-071 |
| DM-016 | Chuẩn hóa CRLF/CR thành LF trước đếm; từ chối UTF-16 lỗi và tin chỉ khoảng trắng/vô hình; giữ Unicode/ZWJ trong tin có nội dung. | DEC-090 |
| DM-017 | Bản nháp chưa Gửi chỉ ở bộ nhớ tab, giữ khi chuyển hội thoại; reload/đóng tab/logout mất bản nháp; không lưu nội dung xuống localStorage/IndexedDB. | DEC-091 |

Các quyết định DEC-* được ghi tại
[sổ quyết định](../../records/decisions/README.md#decisions).

<a id="exceptions"></a>

<a id="ngoại-lệ-và-cách-xử-lý-đề-xuất"></a>

### Ngoại lệ và cách xử lý đề xuất

| Tình huống | Kết quả để backend, frontend và QA rà soát |
|---|---|
| Không tìm thấy người | Danh sách rỗng và thông báo rõ; không tự tạo người nhận hoặc hội thoại |
| Trùng tên hiển thị | Hiển thị thêm tên tài khoản duy nhất; chọn bằng ID; không đưa email vào kết quả |
| Chọn chính mình | Không tạo DM một người; trả lỗi dữ liệu; tính năng ghi chú cá nhân không thuộc yêu cầu hiện tại |
| Mở DM đã có | Trả cùng hội thoại của cặp hai người, kể cả khi hai bên mở đồng thời |
| Người nhận bị khóa | Chặn gửi/gọi mới; actor active vẫn đọc lịch sử và sửa/xóa tin của mình theo quyền. Không self-delete v1; DEC-103/104 và [vòng đời dữ liệu](../../system/data-lifecycle.md#account-state) |
| Mất phản hồi gửi | Giữ tin tạm; người gửi bấm thử lại cùng mã thao tác; chỉ một tin được lưu |
| Chủ ý gửi cùng nội dung lần nữa | Là thao tác mới với mã mới; được tạo tin mới, không chống trùng bằng nội dung đơn thuần |
| Thử lại tin đã sửa/xóa sau lần gửi đầu | Trả cùng ID với trạng thái hiện hành, không tạo lại nội dung gửi ban đầu |
| Sửa tin đã xóa | Từ chối; không phục hồi nội dung; xóa lặp của chính tác giả trả trạng thái đã xóa |
| Tin bị xóa sau khi đã được đọc | Bỏ nội dung khỏi dữ liệu đang phục vụ và giao diện khi đồng bộ; không cam kết thu hồi bản người nhận tự sao chép |
| Phiên hết hạn/mất mạng | Không gửi tin tự động; xác thực lại, đồng bộ lịch sử và để người gửi chủ động thử lại tin lỗi |

Các cơ chế ID thao tác, lưu giữ khóa chống trùng, phiên bản sửa/xóa và
phân trang được mô tả tại [hợp đồng DM](#contracts).
Đây là thiết kế đề xuất để kiểm chứng hành vi đã chốt, không chọn ngầm
công nghệ triển khai. Không lưu nội dung cũ trong lịch sử sửa, sự kiện
hoặc log; backup/restore theo [vòng đời dữ liệu](../../system/data-lifecycle.md#restore), proof DATA-GAP cho phần này áp dụng sau v1.

<a id="permissions"></a>

<a id="quyền-truy-cập"></a>

## Quyền truy cập

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-02 | Tìm người và mở DM | Đã đăng nhập sau xác minh; chọn đúng định danh người nhận | Chưa đăng nhập hoặc chưa xác minh | DEC-016, DEC-019, DEC-051 |
| ACL-03 | Đọc lịch sử DM | Là một trong hai người của hội thoại và phiên còn hợp lệ | Người thứ ba, kể cả người quản lý một cộng đồng mà hai bên tham gia | REQ-005, AC-ACC-04; suy ra từ phạm vi hội thoại riêng |
| ACL-04 | Gửi tin DM | Là người tham gia hội thoại, phiên hợp lệ, đã xác minh email | Chưa xác minh hoặc không thuộc hội thoại | DM-002, DM-010 |
| ACL-05 | Sửa/xóa tin DM | Có quyền truy cập hội thoại và là tác giả tin | Người nhận sửa/xóa tin của người gửi; người thứ ba | DEC-020 |

DM không phụ thuộc vai trò hoặc tư cách thành viên cộng đồng. Không thêm chặn người gửi trong đợt này (DEC-018). Máy chủ kiểm tra quyền trước khi trả lịch sử, gửi/sửa/xóa hoặc đăng ký cập nhật, kể cả khi trả lại kết quả gửi lặp.

<a id="ux"></a>

<a id="giao-diện-và-trạng-thái"></a>

## Giao diện và trạng thái

<a id="phạm-vi-bàn-giao"></a>

### Phạm vi bàn giao

Các phác thảo dưới đây xác định vùng giao diện, thao tác và phản hồi để
thảo luận với frontend và QA. Chưa chốt màu sắc, kiểu chữ hoặc thiết kế
tương tác trực quan. Kích thước tham chiếu đề xuất: màn hình rộng 1280 ×
800 và màn hình hẹp 390 × 844; đây chưa phải ma trận thiết bị hỗ trợ.

DEC-059 xác nhận mục tiêu bố cục thích ứng cho desktop và trình duyệt
điện thoại của tài khoản/DM. Mỗi màn hình phải có nhãn trường, thứ tự đi bằng bàn phím hợp lý, focus
nhìn thấy được và thông báo lỗi bằng chữ. Trạng thái gửi không chỉ dựa
vào màu. Tiêu chí tiếp cận đầy đủ tiếp tục thuộc OQ-007.

<a id="tìm-người-và-hội-thoại-trên-màn-hình-rộng"></a>

### Tìm người và hội thoại trên màn hình rộng

```text
DM-S01 / DM-S02 · Tham chiếu 1280 × 800
┌─────────────────────────┬─────────────────────────────────────────┐
│ Hội thoại               │ Tên hiển thị · @ten_tai_khoan            │
│ [Tìm người…           ] │                                         │
│                         │ [Tải tin cũ hơn]                        │
│ Tên hiển thị            │                                         │
│ @ten_tai_khoan          │ Người kia · thời gian                   │
│                         │ Nội dung tin                            │
│ Hội thoại gần đây       │                                         │
│ • Người A               │                 Nội dung tin của tôi    │
│ • Người B               │                 Đã gửi · Đã sửa [⋯]    │
│                         │                                         │
│                         │                 Tin lỗi [Thử lại]       │
│                         ├─────────────────────────────────────────┤
│                         │ [Nhập tin nhắn…                        ]│
│                         │ [                                      ]│
│                         │                         123/giới hạn [Gửi]│
└─────────────────────────┴─────────────────────────────────────────┘
```

Kết quả tìm kiếm luôn đi kèm tên tài khoản để phân biệt tên hiển thị
trùng nhau; API trả định danh ổn định của người được chọn. Không đưa
email vào kết quả. Đây là cách thể hiện đề xuất cho AC-ACC-03/AC-DM-01.

Giới hạn 2.000 ký tự theo DEC-053; bộ đếm dùng cùng quy tắc với máy chủ. Khi chưa chọn ai, vùng chính hướng dẫn tìm người để
bắt đầu. Hội thoại mới có lời nhắc gửi tin đầu tiên và không dùng tin giả.

<a id="màn-hình-hẹp-và-thao-tác-tin"></a>

### Màn hình hẹp và thao tác tin

Màn hình hẹp chỉ hiện một vùng chính mỗi lần: danh sách/tìm người → hội
thoại. Có nút quay lại danh sách; quay lại không tự gửi hoặc tự xóa bản
nháp đang nhập. Bản nháp theo DEC-091: giữ theo tài khoản/hội thoại trong bộ nhớ tab; tải lại/đóng tab/logout mất bản nháp, không lưu nội dung vào localStorage/IndexedDB.

```text
DM-S03 · Sửa tin                     DM-S04 · Xóa tin
┌─────────────────────────────┐     ┌─────────────────────────────┐
│ Sửa tin nhắn                │     │ Xóa tin nhắn này?           │
│ [Nội dung hiện tại        ] │     │ Cả hai người sẽ thấy        │
│ [                         ] │     │ “Tin nhắn đã bị xóa”.       │
│ [Hủy]               [Lưu]  │     │ [Hủy]                 [Xóa] │
└─────────────────────────────┘     └─────────────────────────────┘
```

Menu sửa/xóa chỉ có trên tin của mình; máy chủ vẫn kiểm tra tác giả.
Hủy sửa không thay nội dung đã lưu. Xóa thành công giữ vị trí và thời
gian của tin với dòng thay thế, bỏ nội dung cũ khỏi giao diện đang xem.
Tin đã xóa không có thao tác sửa. Các quy tắc này được chi tiết hóa trong
đặc tả DM; prototype không tự thêm khôi phục tin đã xóa.

<a id="trạng-thái-giao-diện-gắn-với-tiêu-chí-chấp-nhận"></a>

### Trạng thái giao diện gắn với tiêu chí chấp nhận

| Mã | Trạng thái | Hiển thị và thao tác đề xuất | Căn cứ |
|---|---|---|---|
| UX-DM-01 | Đang tìm/không có kết quả/lỗi tìm | Hiện tiến trình; “Không tìm thấy người phù hợp”; lỗi có nút thử lại; giữ từ khóa | AC-DM-01 |
| UX-DM-02 | Đang tải/lỗi lịch sử | Không xóa tin đang thấy; báo lỗi riêng cho phần tải thêm; giữ vị trí cuộn | AC-DM-03 |
| UX-DM-03 | Đang gửi | Giữ tin tạm và hiển thị “Đang gửi”; chưa hiện “Đã gửi” | AC-DM-02 |
| UX-DM-04 | Gửi lỗi hoặc chưa rõ kết quả | Giữ tin tạm với “Chưa gửi được” và “Thử lại”; không tự gửi khi có mạng | AC-DM-06, AC-DM-08 |
| UX-DM-05 | Thử lại thành công | Thay tin tạm bằng tin đã lưu; chỉ một tin xuất hiện | AC-DM-08 |
| UX-DM-06 | Mất mạng/kết nối trở lại | Thông báo gián đoạn; đồng bộ lịch sử; tin đang lỗi vẫn chờ người gửi bấm thử lại | AC-DM-07, AC-DM-08 |
| UX-DM-07 | Phiên hết hạn | Yêu cầu đăng nhập lại; chỉ tải lại hội thoại khi đã kiểm tra quyền; không gửi lại tin tự động | AC-ACC-04 |
| UX-DM-08 | Sửa/xóa thất bại | Giữ nội dung xác nhận gần nhất; lỗi không làm tin biến mất; cho tải bản hiện tại nếu xung đột | AC-DM-04, AC-DM-05 |
| UX-DM-09 | Chưa xác minh email | Không gửi tin; có hướng dẫn xác minh theo luồng tài khoản | AC-DM-09 |
| UX-DM-10 | Peer không khả dụng do account bị khóa | Đọc lịch sử vẫn được, composer/call mới bị chặn; không hiển thị lý do khóa/email | DEC-104 |
| UX-DM-11 | Tin unavailable sau restore — sau v1 | Hiển thị “Nội dung chưa khôi phục được”, giữ vị trí/tác giả; author có quyền được viết lại body mới hoặc xóa, không tải bản cũ | DEC-108/156 |

Nút “Gửi” gửi tin trên mọi thiết bị. Desktop dùng Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng theo DEC-071. Không gửi trong khi IME đang composition; hành vi bàn phím ảo phải kiểm thử trên ma trận thiết bị. Không đưa đọc/đã nhận, file, chặn tài khoản hoặc
cuộc gọi vào wireframe của đợt DM văn bản.

<a id="bàn-giao-và-nội-dung-còn-cần-rà-soát"></a>

### Bàn giao và nội dung còn cần rà soát

Sáng sở hữu UI DM và phản hồi API theo gói fullstack; Vg rà soát bố cục, trạng thái và quyền. Thái cung cấp dataset/bộ chạy kiểm tra theo [DEC-117](../../project/planning.md#team). Hồ sơ DM đã ghi kiểm tra chức năng UI/API theo gói; các kết quả đó không thay nghiên cứu khả dụng hoặc kiểm chứng bàn phím/IME trên thiết bị vật lý.

Trước nghiệm thu v1 cần khóa trình duyệt/kích thước và thiết bị thực tế,
kiểm chứng liên kết email, UTF-16, bàn phím/IME và xử lý
xung đột trên bản chạy được chọn. Chính sách đếm/tìm kiếm và phím gửi đã chốt tại DEC-068/069/071. Wireframe cộng đồng là đầu ra riêng tại
[SCDC-UX-COM-001](../../system/community.md#ux).

<a id="contracts"></a>

<a id="hợp-đồng-api-dữ-liệu-và-đồng-bộ-đề-xuất"></a>

## Hợp đồng API, dữ liệu và đồng bộ đề xuất

Thiết kế bên dưới cụ thể hóa hành vi đã chốt; các endpoint DM văn bản đã có trong nền monolith `900e226`. Tài khoản sở hữu danh tính/phiên; Messaging sở hữu hội thoại/tin/thao tác. Giao tiếp qua `SCDC.Contracts` trong Modular Monolith; DM không đọc bảng tài khoản và không phụ thuộc Community. Hợp đồng mạng/quyền khi chuyển v1 phải thiết kế riêng, không áp nguyên shared transaction.

[OpenAPI dự thảo](../../contracts/direct-messaging.openapi.json) vẫn mang metadata `design-draft`; đây là trạng thái tài liệu hợp đồng, không có nghĩa các endpoint DM đều chưa triển khai. API runtime nằm tại [controllers Messaging](../../../services/SCDC.Api/Controllers/Messaging) và [tìm người Identity](../../../services/SCDC.Api/Controllers/Identity/UsersController.cs); kết quả theo revision tại hồ sơ DM. Phạm vi v1 và các nhánh còn thiếu vẫn cần review/kiểm chứng. Giới hạn UTF-16 được ghi bằng extension, không dùng minLength/maxLength để thay thế validation server.

DM-P0-T01 chọn [baseline contract/thiết kế](../../records/planning/messaging/dm-p0-baseline-design.md) gồm taxonomy lỗi cụ thể, reuse/replace, migration theo lát cắt và UoW/guard; OpenAPI/realtime có extension baseline tương ứng. P0 và các lát cắt P1–P3 đã có triển khai/xác nhận theo [hồ sơ bàn giao](../../records/verification/messaging/README.md); Hub/outbox/reconnect/sửa/xóa có bằng chứng agent của gói DM-MVP. Không suy metadata baseline hoặc nghiệm thu từng gói thành toàn bộ hợp đồng đã đạt.

Prefix runtime DM là `/api/v1`. API Accounts ở [đặc tả tài khoản](../accounts/README.md). Lỗi DM dùng [ProblemDetails chung](../../system/api-conventions.md#contracts) theo DEC-061, validation 400; các mã lỗi của luồng đã triển khai có trong source và hồ sơ kiểm chứng. Các nhánh mục tiêu v1/sau v1 trong hợp đồng vẫn cần đối chiếu riêng.

`sequence`/`version` truyền dưới dạng chuỗi số nguyên; thời điểm theo UTC. Actor lấy từ phiên. ID/cursor/transport đã chọn DEC-081 và được dùng trong DM hiện tại; khi chuyển v1 cần hồi quy schema, mapping SQL và quyền trên nền service/database riêng.

<a id="contract-5"></a>

<a id="hợp-đồng-5--api-dm-đề-xuất"></a>

### API DM đề xuất

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `GET /users/search?q=...&cursor=...&limit=...` | Một phần tên tài khoản/tên hiển thị | `200 {items: UserSummary[], nextCursor}`; không có kết quả trả mảng rỗng |
| `POST /direct-conversations` | `{ peerUserId }` | `200 { id, participants }`; tạo hoặc lấy hội thoại duy nhất của cặp |
| `GET /direct-conversations?cursor=...&limit=...` | Con trỏ danh sách | Các hội thoại của chính người gọi; không trả hội thoại người khác |
| `GET /direct-conversations/{id}/messages` | `before`, hoặc `after` với mốc `through`; `limit` | Trang tin; cấu trúc và thứ tự ở [hợp đồng lịch sử](synchronization.md#contract-8) |
| `POST /direct-conversations/{id}/messages` | `{ clientMessageId, content }` | `200 MessageDto` sau commit, gồm cả lần gửi đầu và lần thử lại |
| `PATCH /direct-conversations/{id}/messages/{messageId}` | `{ content, expectedVersion }` | `200 MessageDto` với nội dung mới; sai version trả `409 VERSION_CONFLICT` |
| `DELETE /direct-conversations/{id}/messages/{messageId}?expectedVersion=...` | Phiên bản đang thấy | `200 MessageDto` dạng tombstone; xóa lại tin đã xóa trả cùng tombstone |

`MessageDto` gồm `id, conversationId, author: UserSummary, clientMessageId,
sequence, version, content, createdAt, editedAt, deletedAt`.

```json
{
  "clientMessageId": "7c8e7c59-b35a-4d12-b22f-965b96ff4e44",
  "content": "Chào bạn!"
}
```

`clientMessageId` được tạo **một lần khi người dùng bấm gửi**, giữ nguyên
trong tin tạm và mọi lần “Thử lại”. Response và sự kiện có cùng ID này
để client thay tin tạm bằng đúng tin đã lưu. Ví dụ trên minh họa cấu trúc;
định dạng `clientMessageId` là UUIDv4 theo DEC-081; server tạo UUIDv7 cho conversation/message.

Quy tắc tìm người đã chốt DEC-069: chuỗi tìm từ 2 đến 64 đơn vị UTF-16,
không phân biệt hoa/thường, không tự bỏ dấu; khớp một phần, ưu tiên khớp
đúng tên tài khoản, sau đó thứ tự tên tài khoản và ID để phân trang ổn
định. Trang mặc định 20, tối đa 50. Chỉ trả người đủ điều kiện nhận DM;
không trả chính người tìm. Các giới hạn/cách khớp, thứ tự username/ID và cursor đã có trong Identity search; bằng chứng theo revision tại [P1-T01](../../records/verification/messaging/DM-P1-T01.md). Không dùng endpoint tìm kiếm để lộ email; cần hồi quy khi chuyển v1.

<a id="đối-chiếu-sql-trước-triển-khai"></a>

### Đối chiếu SQL trước triển khai

Các mã DM-SQL dưới đây giữ truy vết từ schema ngày 2026-10-04 tới nền DM `900e226`. Mã, bootstrap và migration DM đã có; khi chuyển database v1 phải bảo toàn dữ liệu/khóa và kiểm chứng lại, không coi bảng này hoặc seed là proof migration dữ liệu thật.

| Mã | Baseline trước triển khai DM | Hiện trạng nền DM và yêu cầu kế thừa |
|---|---|---|
| DM-SQL-01 | `direct_conversations` unique `(user_low_id,user_high_id)`, kiểm tra low < high | Writer đã chuẩn hóa UUID và tạo space/cặp cùng transaction; proof mở đồng thời tại [P1-T02](../../records/verification/messaging/DM-P1-T02.md) |
| DM-SQL-02 | `messages.sequence_no` là identity toàn cục, cấp trước commit và có thể có khoảng trống | Đã thêm `conversation_sequence` và counter/khóa theo space; proof commit order tại [P3-T02](../../records/verification/messaging/DM-P3-T02.md); v1 giữ cơ chế bù không sót |
| DM-SQL-03 | Unique `(space_id,author_user_id,client_message_id)` đã có | Đã có SendOperation/fingerprint giữ sau xóa; retry trả current DTO/tombstone; giữ khóa và key khi chuyển dữ liệu |
| DM-SQL-04 | Constraint văn bản dùng `char_length` tối đa 10.000; text message yêu cầu content khác null | Validator đã dùng 2.000 UTF-16, constraint đã cho tombstone content null; không dùng char_length thay phép đếm đã chốt |
| DM-SQL-05 | Có `message_edits.previous_content` trong schema/seed | Writer DM hiện tại không ghi nội dung cũ theo DEC-052; bảng legacy còn trong bootstrap, việc import/cleanup dữ liệu thật phải được rà soát |
| DM-SQL-06 | Có outbox chung nhưng chưa có dispatcher Messaging | Đã có transaction tin/khóa/outbox, lease dispatcher và current DTO; proof worker/merge/quyền tại [DM-MVP](../../records/verification/messaging/DM-MVP.md); transport liên service v1 còn phải tích hợp |

Nguồn: [schema.sql](../../../database/postgres/schema.sql), [migration DM](../../../database/postgres/migrations) và [Messaging module](../../../services/Modules/Messaging/MessagingModule.cs). Các trường/schema cho nhóm chat, file, reactions/read state trong seed không tự mở rộng scope DM văn bản.

<a id="contract-10"></a>

<a id="hợp-đồng-10--điều-kiện-chốt-hợp-đồng"></a>

### Điều kiện chốt hợp đồng

Vg/Sáng rà soát các thuật toán ID/fingerprint/cursor/transaction guard và schema chi tiết bên dưới; chính sách UTF-16/bản nháp đã chốt DEC-068/090/091. Thái đối
chiếu mỗi lỗi/trạng thái với [wireframe](#ux)
và [bộ ca kiểm thử](#tests).
Thay đổi đường dẫn/trường sau khi chốt phải cập nhật đồng thời mock,
frontend, backend và dữ liệu thử; không coi tài liệu này là bằng chứng
đã có API hoặc đã chạy thử nghiệm.

<a id="checklist-bàn-giao-thiết-kế"></a>

#### Danh sách kiểm tra bàn giao thiết kế

Review Accounts + DM + OpenAPI/schema/fixture cùng một phiên bản; đối chiếu từng errorCode với UI và TC. Prototype/mock và proof cho concurrent commit, revoke race, key rotation, partial-page reconnect, lost response và email worker thuộc gói triển khai. DEC-103/104 chốt không self-delete/giữ lịch sử khi peer bị khóa; guard read/author edit/delete không yêu cầu peer active, send/call mới vẫn kiểm tra peer. Nền DM `900e226` đã có [IHistoricalUserSummaryReader](../../../services/SCDC.Contracts/Identity/IAccountPairAccessGuard.cs) và [UI availability](../../../clients/WebClient/src/components/DmChat.jsx); tiếp tục hồi quy và tích hợp chúng khi chuyển v1, không cấp quyền đọc DM cho quản trị. Cleanup online theo [vòng đời dữ liệu](../../system/data-lifecycle.md#cleanup) vẫn thuộc v1; restore và DATA-GAP proof riêng cho backup áp dụng sau v1.

Các mã lỗi dưới đây đã có trong backend DM nền `900e226`, gồm [validation nội dung](../../../services/Modules/Messaging/Application/TextContent.cs), [gửi/retry](../../../services/Modules/Messaging/Infrastructure/Services/TextMessageSender.cs), [sửa/xóa](../../../services/Modules/Messaging/Infrastructure/Services/MessageMutator.cs) và [cursor lịch sử](../../../services/Modules/Messaging/Infrastructure/Services/MessageHistory.cs). Bảng giữ cách xử lý giao diện cần đối chiếu theo từng build; có mã lỗi không đồng nghĩa mọi nhánh UI hoặc toàn bộ hợp đồng đã được kiểm chứng:

| HTTP / errorCode | Cách xử lý giao diện |
|---|---|
| 400 `CONTENT_INVALID` / `CONTENT_EMPTY` / `CONTENT_TOO_LONG` | Giữ nội dung để sửa; không tự gửi lại hoặc cắt tin |
| 400 `CURSOR_INVALID` | Bỏ cursor, tải lại lịch sử hoặc tìm kiếm đúng scope; không gửi lại mutation |
| 409 `OPERATION_CONFLICT` | Báo thao tác gửi có nội dung không khớp; không tự đổi khóa rồi gửi |
| 409 `OPERATION_UNVERIFIABLE` | Báo không thể xác nhận lần gửi cũ; đối chiếu lịch sử, không tự tạo tin mới |
| 409 `VERSION_CONFLICT` / `MESSAGE_DELETED` | Tải lại bản hiện hành của trang đang xem; không ghi đè hoặc khôi phục tin đã xóa |
| 503 `FINGERPRINT_KEY_UNAVAILABLE` | Giữ tin lỗi, báo dịch vụ tạm không xử lý được; chỉ retry khi người dùng chọn |
| 401 / 403 | Xử lý phiên hoặc quyền hiện hành; ngừng subscription trái quyền, không tự replay thao tác |

<a id="search-and-conversation-pagination"></a>

### Phân trang tìm người và danh sách hội thoại

Danh sách hội thoại mặc định 20/tối đa 50; sort last_activity_at DESC NULLS LAST rồi ID ASC, có cursor theo actor/position. Đây là danh sách đang thay đổi: khi có tin mới làm hội thoại đổi vị trí, client dedup ID và tải lại trang đầu, không hứa snapshot cố định qua các trang. Nguồn lịch sử tin vẫn là query theo conversation_sequence, không dùng lastActivity để bù tin.

Tìm người do Identity query theo DEC-069: key NFC/ToLowerInvariant, username exact rank trước rồi username ASCII/ID; DB collation cố định và escaped LIKE `%`, `_`, `\` để query là substring literal. Cursor gắn query đã chuẩn hóa; đổi q/limit mà giữ cursor trả validation. Profile đổi giữa các trang có thể đổi membership kết quả, nên search refresh từ đầu; không trả email/status security. Không coi collation bỏ dấu là phù hợp DEC-069.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

Các tiêu chí dưới đây mô tả hành vi quan sát được, cần rà soát cùng các
[ngoại lệ](#exceptions) và [nội dung còn mở](#gaps) trước khi xác nhận đặc tả.

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-DM-01 | Người dùng đã đăng nhập tìm bằng một phần tên tài khoản hoặc tên hiển thị, rồi chọn kết quả. | Mở được hội thoại riêng với đúng tài khoản mà không cần quan hệ kết bạn hoặc cùng cộng đồng. |
| AC-DM-02 | Một người gửi tin văn bản và hệ thống lưu thành công. | Người gửi thấy trạng thái “Đã gửi”; người nhận thấy nội dung khi mở hội thoại. |
| AC-DM-03 | Hai người đóng rồi mở lại hội thoại sau khi đã trao đổi. | Lịch sử tin nhắn còn hiệu lực được hiển thị. |
| AC-DM-04 | Người gửi sửa một tin cũ của mình. | Nội dung mới và dấu “Đã sửa” hiển thị cho cả hai người, kể cả khi tin được gửi từ trước. |
| AC-DM-05 | Người gửi xóa một tin của mình. | Cả hai người thấy dòng “Tin nhắn đã bị xóa” tại vị trí tin đó. |
| AC-DM-06 | Lần gửi tin thất bại. | Tin được báo lỗi; người gửi có thể bấm thử lại. |
| AC-DM-07 | A gửi tin khi B chưa mở ứng dụng; hệ thống đã lưu tin. Sau đó B mở ứng dụng và hội thoại với A. | B thấy tin đã lưu trong lịch sử hội thoại. |
| AC-DM-08 | Kết quả gửi lần đầu không rõ; người gửi bấm “Thử lại” cho chính tin đó. | Hội thoại chỉ có một tin tương ứng với thao tác gửi. |
| AC-DM-09 | Tài khoản chưa xác minh email thử gửi tin riêng. | Hệ thống từ chối gửi và chỉ dẫn bước xác minh email. |
| AC-DM-10 | Sửa một tin nhiều lần rồi mở lại hội thoại trên hai thiết bị. | Chỉ nội dung mới nhất và dấu “Đã sửa” được cung cấp; không có API/giao diện đọc phiên bản cũ. |
| AC-DM-11 | Gửi/sửa tin gồm 2.000/2.001 đơn vị UTF-16, nhiều dòng, emoji, tin chỉ khoảng trắng. | Nhận đến 2.000; từ chối vượt/trống; client/server đếm đúng DEC-068. |
| AC-DM-12 | Người thứ ba biết ID hội thoại/tin và gọi API đọc/gửi/sửa/xóa hoặc đăng ký nhận cập nhật. | Không được truy cập; phản hồi không tiết lộ nội dung hay người tham gia. |
| AC-DM-13 | Hai phía đồng thời mở hội thoại với nhau. | Cùng một hội thoại hai người; không tạo bản trùng hoặc hội thoại mồ côi. |
| AC-DM-14 | Hai phiên cùng sửa hoặc sửa/xóa một tin từ cùng phiên bản cũ. | Không ghi đè âm thầm; yêu cầu thua tranh chấp nhận xung đột và có thể tải trạng thái hiện hành. |
| AC-DM-15 | B mất mạng; A sửa/xóa tin cũ; B kết nối lại và xem tin đó. | B thấy nội dung hiện hành hoặc dòng thay thế, không tiếp tục dùng bản cache cũ làm kết quả chính xác. |
| AC-DM-16 | Tìm với từ khóa ở biên 1/2/64/65, khác hoa/thường/dấu và nhiều trang kết quả. | Giới hạn theo DEC-069; giữ phân biệt dấu; username khớp đúng được ưu tiên; không lộ email hay trả chính người tìm. |
| AC-DM-17 | Gửi bằng Enter/Shift+Enter trên desktop và bàn phím điện thoại; nhập tiếng Việt bằng IME. | Desktop Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng; nút Gửi luôn gửi; composition không vô tình gửi tin. |
| AC-DM-18 | Mở lại lịch sử tin cũ chưa bị người gửi xóa. | Tin không bị mất chỉ vì hết một thời hạn tự động; backup/restore kiểm chứng sau v1, chưa có self-delete tài khoản. |
| AC-DM-19 | Gửi chỉ zero-width/variation selector, surrogate lỗi, CRLF và emoji có ZWJ | Quy tắc DEC-090/text-policy thống nhất; giữ emoji/chữ và xuống dòng hợp lệ; không thay/cắt nội dung lỗi |
| AC-DM-20 | Gõ chưa Gửi rồi đổi hội thoại, reload/đóng tab/logout | Giữ bản nháp trong cùng tab/hội thoại; reload/đóng/logout mất; không lưu nội dung DM trên storage trình duyệt |
| AC-DM-21 | Refresh khi POST đã thất bại, response/event đảo thứ tự; reconnect bù nhiều trang | Không tự replay mutation; một dòng/tin; resumeCursor chỉ tiến sau merge đủ trang |

AC-DM-12 đến AC-DM-15 cụ thể hóa bảo vệ hội thoại và hành vi đồng thời
để rà soát; chưa phải kết quả kiểm thử đã đạt. AC-DM-16–18 cụ thể hóa DEC-069–071.

<a id="tests"></a>

<a id="dữ-liệu-và-ca-kiểm-thử"></a>

## Dữ liệu và ca kiểm thử

<a id="dữ-liệu-và-môi-trường-cần-chuẩn-bị"></a>

### Dữ liệu và môi trường cần chuẩn bị

| Mã dữ liệu | Mục đích |
|---|---|
| A, B | Hai tài khoản đã xác minh, không cùng cộng đồng, có tên hiển thị trùng nhưng tên tài khoản khác |
| C | Tài khoản thứ ba, không tham gia DM của A/B |
| U | Tài khoản chưa xác minh; kiểm tra cả xác minh và khôi phục mật khẩu |
| A1, A2 | Hai phiên của A để thử sửa/xóa đồng thời và thu hồi phiên |

Tạo dữ liệu thử riêng theo từng ca; không dùng thông tin người thật.
Môi trường cần email thử để nhận/xác minh liên kết, hai phiên trình duyệt
độc lập, khả năng ngắt kết nối hoặc làm mất phản hồi sau commit, và quan
sát dữ liệu/sự kiện bằng quyền kiểm thử. Không đưa API tạo lỗi vào sản
phẩm công khai. Thái chuẩn bị dữ liệu; Vg/Sáng rà soát cơ chế tạo lỗi và
bằng chứng kiểm tra giao dịch.

<a id="dm-hành-vi-giao-dịch-và-khôi-phục"></a>

### DM: hành vi, giao dịch và khôi phục

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-DM-01 | A tìm một phần tên của B; chọn giữa hai người trùng tên hiển thị | Phân biệt bằng tên tài khoản, mở đúng người; không cần kết bạn/cùng cộng đồng | AC-DM-01, AC-ACC-03 |
| TC-DM-02 | A và B đồng thời mở DM với nhau | Cùng một ID hội thoại, đúng hai người, không có dữ liệu mồ côi | AC-DM-13 |
| TC-DM-03 | A gửi; B đang mở; đóng rồi mở lại cả hai phiên | “Đã gửi” sau lưu; cùng nội dung/ID; lịch sử còn sau mở lại | AC-DM-02, AC-DM-03 |
| TC-DM-04 | B không mở ứng dụng khi A gửi; B mở lại | Thấy tin đã lưu; không phụ thuộc sự kiện realtime lúc B vắng mặt | AC-DM-07 |
| TC-DM-05 | Làm lỗi trước commit; mạng trở lại | Không có tin trong DB hoặc sự kiện; UI giữ tin lỗi, không tự gửi lại | AC-DM-06 |
| TC-DM-06 | Commit thành công nhưng mất response; bấm thử lại nhiều lần, gồm hai request đồng thời | Chỉ một tin/ID và một thao tác tạo tin; UI chỉ một dòng sau merge | AC-DM-08 |
| TC-DM-07 | Cùng khóa gửi nhưng payload khác; sau đó gửi cùng nội dung với khóa mới | Trường hợp đầu xung đột; trường hợp sau là tin mới hợp lệ | [hợp đồng gửi/đồng thời](message-lifecycle.md#contract-7), AC-DM-08 |
| TC-DM-08 | Tin gửi đã sửa/xóa; thử lại thao tác gửi ban đầu | Cùng ID, trả nội dung hiện hành/tombstone; không phục hồi bản cũ | DM-009, DM-011; [hợp đồng gửi/đồng thời](message-lifecycle.md#contract-7) |
| TC-DM-09 | A sửa tin nhiều lần trên A1; B và A2 mở lại | Chỉ nội dung mới nhất, dấu “Đã sửa”; không có endpoint hoặc dữ liệu lịch sử nội dung sửa | AC-DM-04, AC-DM-10 |
| TC-DM-10 | A xóa tin; thử xóa lại và thử sửa tin đã xóa | Hai bên thấy dòng thay thế; xóa lặp an toàn; sửa bị từ chối | AC-DM-05; [hợp đồng gửi/đồng thời](message-lifecycle.md#contract-7) |
| TC-DM-11 | A1/A2 sửa cùng version; lặp với một sửa và một xóa | Chỉ thao tác hợp lệ theo version được áp dụng; bên còn lại nhận xung đột, không ghi đè âm thầm | AC-DM-14 |
| TC-DM-12 | C gọi trực tiếp các API và đăng ký nhận cập nhật với ID DM/tin của A/B | Không đọc/gửi/sửa/xóa/nhận tin; không lộ người tham gia | AC-DM-12 |
| TC-DM-13 | B sửa/xóa tin của A qua API, không chỉ qua UI | Máy chủ từ chối; tin không đổi | DM-004, DM-005, ACL-05 |
| TC-DM-14 | U gửi trực tiếp qua API | Từ chối dù client giả quyền hoặc trạng thái xác minh | AC-DM-09 |
| TC-DM-15 | Hai request gửi tin khác nhau; giữ transaction thứ nhất chưa commit trong khi request thứ hai chạy | Phân trang/bù tin không bỏ sót bản commit trễ; sequence trong hội thoại tuân thứ tự commit | [gửi/đồng thời](message-lifecycle.md#contract-7) và [lịch sử](synchronization.md#contract-8), AC-DM-03 |
| TC-DM-16 | Dừng worker sau commit rồi bật lại; phát sự kiện trùng/đảo thứ tự | Lịch sử vẫn có tin; cập nhật phục hồi; client merge theo ID/version, không nhân đôi hoặc quay lại bản cũ | AC-DM-02, AC-DM-03; [hợp đồng lịch sử/thu hồi](synchronization.md#contract-8) |
| TC-DM-17 | B offline; A gửi hơn một trang tin mới và sửa/xóa tin cũ đang nằm trong cửa sổ B từng xem | B bù hết trang mới, tải lại tin cũ và bỏ cache hết hiệu lực; không sót tin hoặc giữ nội dung đã xóa | AC-DM-15 |

Ca kiểm tra nội dung cũ chỉ xét dữ liệu nghiệp vụ đang phục vụ, sự kiện
và log trong thiết kế; vòng đời bản sao lưu kiểm chứng sau v1 theo
[DEC-103–109 và TC-DATA](../../system/data-lifecycle.md#acceptance). Không coi thu hồi bản người nhận tự sao chép là điều
kiện đạt của xóa/sửa tin.

Các ca thiết kế bổ sung, trạng thái Chưa chạy:

| Mã ca | Tình huống | Kết quả | Dẫn chiếu |
|---|---|---|---|
| TC-DM-18 | Search biên/case/dấu/NFC, q chứa %/_/backslash, đổi q/limit với cursor cũ | Substring literal; đúng DEC-069, không lộ email; cursor gắn query | AC-DM-16 |
| TC-DM-19 | Enter/Shift+Enter, mobile và IME | Theo thiết bị; composition không gửi sớm | AC-DM-17 |
| TC-DM-20 | Lịch sử tin cũ sau thời gian dài | Không tự hết hạn tin theo DEC-070 | AC-DM-18 |
| TC-DM-21 | Fixture invisible/invalid surrogate/NUL/CRLF khi send/edit và tin phòng | Đúng text-policy; không ghi/phát tin không hợp lệ | AC-DM-19 |
| TC-DM-22 | Đổi hội thoại, reload, logout, đổi account và hai tab | Bản nháp/cache chỉ trong tab, đúng account; sent lưu ở DB | AC-DM-20 |
| TC-DM-23 | Send 401/timeout; event trước response; refresh concurrent/lost response | Không tự gửi lại; UUIDv4/nội dung giữ đúng khi bấm retry; một dòng | AC-DM-08/21 |
| TC-DM-24 | HMAC key rotation/thiếu key; UUID byte-order đối chiếu DB | Retry dùng key cũ; thiếu key không tạo tin; cặp UUID nhất quán | AC-DM-13, hợp đồng fingerprint |
| TC-DM-25 | 101 tin mới với limit 50, mất kết nối sau từng trang, cursor hết hạn/bị sửa/dùng chéo user | Merge 3 trang trước tiến resume; không bỏ tin; cursor trái scope bị từ chối | AC-DM-15/21 |
| TC-DM-26 | Session revoke commit tranh send/subscribe/dispatch | Thứ tự guard/commit xác định; không có thao tác mới sau revoke, chat ngừng dữ liệu ≤5 giây | DEC-083, AC-DM-12/21 |

<a id="ma-trận-giao-diện-và-cách-ghi-kết-quả"></a>

### Ma trận giao diện và cách ghi kết quả

Theo DEC-059, chạy hành trình tài khoản/DM trên desktop và trình duyệt
điện thoại; có kiểm tra đổi chiều màn hình, bàn phím ảo, cuộn lịch sử,
focus khi lỗi và menu sửa/xóa. Hai kích thước wireframe là dữ liệu thiết
kế, chưa thay thế danh sách OS/thiết bị và phiên bản cụ thể của ma trận DEC-082 tại nghiệm thu.

Mỗi lần chạy ghi: mã ca, build, cấu hình, dữ liệu, trình duyệt/thiết bị,
bước tái hiện, kỳ vọng, thực tế, bằng chứng, người thực hiện và lỗi liên
quan. Mỗi lần chạy mới bắt đầu ở trạng thái **Chưa chạy**; kết quả nền đã có
được dẫn từ hồ sơ DM cùng revision/phạm vi, không tự chuyển thành Đạt cho build v1.
Ca phụ thuộc đề xuất chưa xác nhận thêm ghi chú “Chờ chốt quy tắc”, không tính là đạt.

<a id="gaps"></a>

<a id="vấn-đề-còn-mở"></a>

## Vấn đề còn mở

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Kết quả tìm kiếm | Identity search/cursor và frontend API thật đã có bằng chứng [P1-T01](../../records/verification/messaging/DM-P1-T01.md); còn hồi quy và tích hợp hợp đồng mạng sau tách v1. | OQ-005 |
| Lưu giữ | DEC-103–109 chốt account lock/no self-delete và retention online; còn review/migration/worker theo [chính sách chung](../../system/data-lifecycle.md). Sổ độc lập, restore placeholder và proof backup/restore áp dụng sau v1 theo DEC-156. | OQ-005/011, DATA-GAP |
| Thử lại/đồng thời | Nền DM có proof retry/HMAC/commit order tại [P3-T02](../../records/verification/messaging/DM-P3-T02.md) và mutation/reconnect tại [DM-MVP](../../records/verification/messaging/DM-MVP.md); còn hồi quy xuyên service và kiểm chứng dọn dữ liệu theo scope v1. | OQ-005, OQ-008 |
| Giới hạn nội dung | Validator client/server và corpus đã có bằng chứng [P2-T01](../../records/verification/messaging/DM-P2-T01.md); sửa tin có bằng chứng trong gói DM-MVP. Tin phòng và các build v1 phải kiểm chứng riêng cùng [text-policy](text-policy.md). | OQ-005 |
| Chất lượng | Ngưỡng và ma trận đã chốt DEC-082/083; còn cấu hình/build/thiết bị và kết quả đo. | OQ-007 |

[OpenAPI dự thảo](../../contracts/direct-messaging.openapi.json) được bổ sung ngày 2026-10-04 và vẫn mang metadata thiết kế; API/Hub DM đã có trong nền `900e226`, không đồng nghĩa mọi nhánh schema đã thực thi hoặc nghiệm thu. Schema dùng `x-scdc-utf16-length` vì minLength/maxLength của JSON Schema không tự biểu diễn phép đếm UTF-16. `clientMessageId` UUIDv4, ID server UUIDv7 theo DEC-081; ví dụ là dữ liệu minh họa. SQL dùng chat space và `conversation_sequence` cho wire sequence; `sequence_no` legacy vẫn tồn tại. Kết quả mapping/unique key/commit order phải dẫn từ hồ sơ build đã chạy, không suy từ seed/schema và không tự áp cho database v1.

Các quyết định chưa chốt được giữ ở OQ-005/OQ-007/OQ-008/OQ-011. Chọn framework hoặc mô hình lưu không thay thế việc kiểm chứng lost response, concurrent commit, worker dừng và reconnect.

## Bàn giao

Gói MVP-DM tại [hồ sơ MVP](../../releases/mvp.md#packages); phần hoàn thiện tại [v1](../../releases/v1.md). Chọn AC/TC tại [đặc tả](direct-messaging.md), rà soát API DM cùng [Messaging dùng chung](README.md) và quyền tin phòng.

Hồ sơ triển khai và nghiệm thu theo gói tại [hồ sơ DM](../../records/verification/messaging/README.md); kế hoạch lịch sử tại [kế hoạch DM](../../records/planning/messaging/direct-messaging.md).
