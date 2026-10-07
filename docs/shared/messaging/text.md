# SCDC — Nội dung tin và kiểm thử chung

Pipeline văn bản, state/merge và fixture áp dụng cho DM và tin phòng. Ngữ cảnh conversation trong state UI minh họa DM; cache/bản nháp luôn tách theo actor và tài nguyên.

<a id="contract-6"></a>

### Hợp đồng 6 — Chuẩn hóa và kiểm tra nội dung

Theo DEC-053/068, tin tối đa 2.000 đơn vị UTF-16, nhận xuống dòng và emoji, từ chối tin chỉ có khoảng trắng. Phép đếm đã chốt; chuẩn hóa/UTF-16/vô hình đã chốt DEC-090; bảng Unicode và fixture cụ thể hóa thiết kế:

1. Chuẩn hóa CRLF/CR thành LF; không cắt khoảng trắng đầu/cuối của một
   tin có nội dung và không diễn giải HTML/Markdown.
2. Đếm đơn vị UTF-16 sau chuẩn hóa xuống dòng bằng `.Length` của .NET
   và `string.length` của JavaScript. LF tính một đơn vị; emoji ngoài BMP
   tính hai, emoji ghép/ký tự tổ hợp có thể tính nhiều đơn vị. Server
   quyết định hợp lệ; client không được cắt giữa một cặp surrogate.
3. Tin trống hoặc chỉ gồm ký tự khoảng trắng/xuống dòng bị từ chối.
   Bộ ký tự theo [text-policy.json](../../fixtures/text-policy.json); surrogate không ghép đôi bị từ chối, không tự thay bằng ký tự khác.
4. Sửa tin áp dụng cùng giới hạn. Vượt giới hạn trả `400 CONTENT_TOO_LONG`;
   trống trả `400 CONTENT_EMPTY`; không lưu tin hoặc phát sự kiện.

Trường `requestFingerprint` tính từ nội dung đã chuẩn hóa và ngữ cảnh
thao tác, dùng dấu vân tay có khóa để đối chiếu, không giữ bản văn bản
cũ trong bảng chống trùng. Phải quản lý phiên bản/khóa đủ lâu để các
yêu cầu thử lại còn đối chiếu được; HMAC-SHA256 và định dạng byte ở [thiết kế fingerprint](persistence.md#uuid-fingerprint-và-khóa-giao-dịch).

#### Nội dung và state UI

Pipeline: kiểm tra chuỗi Unicode hợp lệ → CRLF/CR thành LF → đếm 1–2.000 UTF-16 → kiểm tra không chỉ trắng/vô hình → lưu nguyên nội dung đã chuẩn hóa. [text-policy.json](../../fixtures/text-policy.json) khóa bảng Unicode 17.0.0: White_Space từ [PropList](https://www.unicode.org/Public/17.0.0/ucd/PropList.txt), Default_Ignorable_Code_Point từ [DerivedCoreProperties](https://www.unicode.org/Public/17.0.0/ucd/DerivedCoreProperties.txt), cộng control `0000–001F/007F–009F` khi xét toàn chuỗi rỗng hiển thị. Không suy font không vẽ được một chữ là tin rỗng. ZWJ/variation selector vẫn giữ và tính độ dài khi cùng nội dung khác, ví dụ `👩‍💻` được nhận.

Từ chối unpaired surrogate với 400 `CONTENT_INVALID`, không sửa thành U+FFFD; `U+0000` cũng bị từ chối vì [PostgreSQL text không lưu NUL](https://www.postgresql.org/docs/18/datatype-character.html). Chỉ trắng/vô hình trả `CONTENT_EMPTY`, vượt độ dài trả `CONTENT_TOO_LONG`. Trình phân tích JSON từ chối trước validation thì trả ProblemDetails validation chung; không dùng lỗi parser chứa request body làm log. Fixture có raw input và giá trị sau chuẩn hóa, gồm trường hợp surrogate lỗi.

State UI: `draft → sending → sent` hoặc `sendFailed`. Bấm Gửi tạo UUIDv4, copy nội dung vào tin tạm; từ thời điểm đó không đổi nội dung gắn với khóa gửi. Bấm Thử lại dùng đúng khóa/nội dung; muốn gửi bản đã đổi là thao tác mới với khóa mới. Tin tạm/sự kiện/response merge theo actor + clientMessageId và message ID; giữ version lớn nhất. Event đến trước HTTP response vẫn chỉ một dòng.

Bản nháp Map `(userId,conversationId)` chỉ ở bộ nhớ tab theo DEC-091; khi logout/đổi account/hết phiên không khôi phục được thì xóa Map và cache tin. Reload bỏ cả tin tạm/lỗi phía tab; tin đã commit được tìm lại trong lịch sử, tin chưa commit không được coi là đã lưu. Không coi hai tab cùng tài khoản là tự đồng bộ bản nháp.

Wrapper [api.js](../../../clients/WebClient/src/api.js) hiện mặc định retry request sau 401. Tích hợp gửi/sửa/xóa DM phải dùng `retry:false`; không để refresh/reconnect/service worker/HTTP library tự replay mutation. Client có thể refresh trước lần gửi đầu bằng `getAccessToken`; khi request gửi đã xảy ra mà thất bại, giữ trạng thái lỗi để người dùng chủ động xử lý. GET lịch sử có thể retry sau refresh; không retry token consume hoặc refresh đã có kết quả không rõ.

<a id="tests"></a>

### Dữ liệu biên nội dung

| Mã ca | Dữ liệu | Kết quả mong đợi theo DEC-053 |
|---|---|---|
| TC-TEXT-01 | Chuỗi rỗng, dấu cách, tab hoặc chỉ xuống dòng | Bị từ chối; không lưu và không phát sự kiện |
| TC-TEXT-02 | 1, 1.999, 2.000 và 2.001 đơn vị UTF-16 | Ba giá trị đầu được nhận, 2.001 bị từ chối |
| TC-TEXT-03 | Tiếng Việt có dấu, ký tự tổ hợp, emoji đơn và emoji ghép | Hiển thị đúng; đếm nhất quán client/server theo UTF-16 đã chốt tại DEC-068 |
| TC-TEXT-04 | CRLF và LF cùng nội dung; khoảng trắng đầu/cuối của tin có chữ | Chuẩn hóa xuống dòng; giữ khoảng trắng có chủ ý; không sinh xung đột chống trùng do chuẩn hóa khác nhau |
| TC-TEXT-05 | Nội dung trông như HTML/script và ký tự đặc biệt | Hiển thị như văn bản, không chạy mã hoặc diễn giải thành giao diện |
| TC-TEXT-06 | Lặp các dữ liệu trên khi sửa tin và gửi tin phòng | Cùng quy tắc nội dung, không có đường bỏ qua giới hạn |

Dữ liệu UTF-16 cố định: `a` = 1; `ế` dựng sẵn = 1; `e` + dấu sắc tổ hợp = 2; `😀` = 2; `👩‍💻` = 5; LF = 1. Chuỗi 1.000 `😀` có độ dài 2.000; 1.001 có độ dài 2.002. Chuẩn hóa CRLF thành LF trước khi kiểm tra theo DEC-090; không tự chuẩn hóa NFC hoặc cắt khoảng trắng của tin có nội dung. Fixture này dùng cho client/backend/SQL, không yêu cầu hiển thị mỗi emoji là một đơn vị.

Fixture máy đọc được: [text-validation.json](../../fixtures/text-validation.json) và [text-policy.json](../../fixtures/text-policy.json); raw/normalized length và Unicode/error case để đối chiếu client/backend. Kết quả chạy trên từng API/client được dẫn từ [status DM](../../features/direct-messaging/status.md) và [status Community](../../features/community/status.md), theo commit và phạm vi đã thử. [dm-fingerprint.json](../../fixtures/dm-fingerprint.json) chứa key giả và hash kỳ vọng cho CRLF/UUID/HMAC, không chứa secret thật.

Các ca TEXT áp dụng AC-DM-11 và AC-COM-24. Thái chuẩn bị fixture cố định
với số đơn vị UTF-16 kỳ vọng; Vg/Sáng đối chiếu implementation với text-policy trước
khi dùng fixture để kết luận đạt.
