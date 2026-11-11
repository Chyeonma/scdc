# SCDC — Nội dung tin văn bản

Quy tắc kiểm tra nội dung tin nhắn: độ dài tối đa, cách đếm ký tự, tin trống, ký tự đặc biệt. Áp dụng cho DM và tin phòng, cả khi gửi mới lẫn khi sửa tin.

**Trạng thái:** quy tắc đã chốt. DM trong nền `900e226` đã kiểm tra nội dung ở cả [backend](../../../services/SCDC.BuildingBlocks/Application/Text/UnicodeTextPolicy.cs) và [client](../../../clients/WebClient/src/dmTextPolicy.js); bằng chứng xem [P2-T01](../../records/verification/messaging/DM-P2-T01.md) và [DM-MVP](../../records/verification/messaging/DM-MVP.md). Tin phòng chưa có, khi làm phải dùng cùng quy tắc.

Trạng thái gửi, gửi lại và bộ nhớ đệm phía client xem [vòng đời tin](message-lifecycle.md#client-state). Quyền gửi xem [DM](direct-messaging.md#permissions) và [tin phòng](channel-messaging.md).

<a id="contract-6"></a>

<a id="hợp-đồng-6--chuẩn-hóa-và-kiểm-tra-nội-dung"></a>

## Quy tắc

- Tin dài tối đa **2.000 đơn vị UTF-16**. Cho phép xuống dòng và emoji.
- Tin trống hoặc chỉ có khoảng trắng, xuống dòng hay ký tự vô hình bị từ chối.
- Sửa tin dùng cùng quy tắc.
- Server quyết định hợp lệ hay không; client kiểm tra trước để báo lỗi sớm.
- Tin không hợp lệ thì không lưu và không phát sự kiện.

### Các bước kiểm tra

1. **Kiểm tra chuỗi Unicode hợp lệ.** Surrogate không ghép đôi bị từ chối (`400 CONTENT_INVALID`), không tự thay bằng U+FFFD. Ký tự `U+0000` cũng bị từ chối vì [PostgreSQL không lưu được NUL](https://www.postgresql.org/docs/18/datatype-character.html).
2. **Chuẩn hóa xuống dòng:** đổi CRLF và CR thành LF. Không cắt khoảng trắng đầu và cuối của tin có nội dung, không chuẩn hóa NFC, không diễn giải HTML hay Markdown.
3. **Đếm độ dài** bằng `.Length` của .NET và `string.length` của JavaScript (cùng đếm UTF-16). Phải từ 1 đến 2.000; vượt thì trả `400 CONTENT_TOO_LONG`. Client không được cắt giữa một cặp surrogate.
4. **Kiểm tra không trống:** tin chỉ gồm khoảng trắng hoặc ký tự vô hình trả `400 CONTENT_EMPTY`.
5. **Lưu** nội dung đã chuẩn hóa ở bước 2.

Nếu bộ phân tích JSON từ chối request trước bước 1, trả lỗi validation chung (ProblemDetails). Không ghi nội dung request vào log.

<a id="nội-dung-và-state-ui"></a>

### Thế nào là "trống"

Danh sách ký tự lấy từ Unicode 17.0.0 và được khóa trong [text-policy.json](../../fixtures/text-policy.json):

- Khoảng trắng: thuộc tính `White_Space` trong [PropList](https://www.unicode.org/Public/17.0.0/ucd/PropList.txt).
- Ký tự vô hình: `Default_Ignorable_Code_Point` trong [DerivedCoreProperties](https://www.unicode.org/Public/17.0.0/ucd/DerivedCoreProperties.txt).
- Ký tự điều khiển `0000–001F` và `007F–009F`.

Một ký tự mà font không vẽ được không làm tin bị coi là trống. ZWJ và variation selector đi kèm nội dung khác vẫn được giữ và tính độ dài, ví dụ `👩‍💻` hợp lệ.

### Chống gửi trùng

Trường `requestFingerprint` là dấu vân tay có khóa (HMAC-SHA256), tính từ nội dung đã chuẩn hóa và ngữ cảnh thao tác. Bảng chống trùng chỉ lưu dấu vân tay, không lưu văn bản. Khóa phải được giữ đủ lâu để các lần gửi lại vẫn đối chiếu được. Định dạng chi tiết xem [thiết kế fingerprint](message-lifecycle.md#uuid-fingerprint-và-khóa-giao-dịch).

<a id="tests"></a>

<a id="dữ-liệu-biên-nội-dung"></a>

## Ca kiểm thử

| Mã ca | Dữ liệu | Kết quả mong đợi |
|---|---|---|
| TC-TEXT-01 | Chuỗi rỗng, dấu cách, tab hoặc chỉ xuống dòng | Bị từ chối; không lưu, không phát sự kiện |
| TC-TEXT-02 | 1; 1.999; 2.000 và 2.001 đơn vị UTF-16 | Ba giá trị đầu được nhận; 2.001 bị từ chối |
| TC-TEXT-03 | Tiếng Việt có dấu, ký tự tổ hợp, emoji đơn và emoji ghép | Hiển thị đúng; client và server đếm giống nhau |
| TC-TEXT-04 | Cùng nội dung với CRLF và LF; khoảng trắng đầu và cuối | Chuẩn hóa xuống dòng; giữ khoảng trắng; hai bản không bị coi là khác nhau khi chống trùng |
| TC-TEXT-05 | Nội dung trông như HTML, script, ký tự đặc biệt | Hiển thị như văn bản, không chạy mã |
| TC-TEXT-06 | Lặp các ca trên khi sửa tin và khi gửi tin phòng | Cùng quy tắc, không có đường nào bỏ qua giới hạn |

Các ca này áp dụng cho AC-DM-11 và AC-COM-24.

### Độ dài mẫu (UTF-16)

| Chuỗi | Độ dài |
|---|---|
| `a` | 1 |
| `ế` (dựng sẵn) | 1 |
| `e` + dấu sắc tổ hợp | 2 |
| `😀` | 2 |
| `👩‍💻` | 5 |
| LF | 1 |
| 1.000 × `😀` | 2.000 (hợp lệ) |
| 1.001 × `😀` | 2.002 (quá dài) |

Fixture máy đọc: [text-validation.json](../../fixtures/text-validation.json) và [text-policy.json](../../fixtures/text-policy.json), gồm dữ liệu gốc, độ dài sau chuẩn hóa và các ca lỗi. [dm-fingerprint.json](../../fixtures/dm-fingerprint.json) chứa khóa giả và mã băm mong đợi, không chứa bí mật thật.

Fixture đúng chưa chứng minh mọi đường gửi và sửa đã đạt. Người làm fixture và người đối chiếu code phải là hai người khác nhau. Kết quả chạy xem trạng thái [DM](direct-messaging.md) và [Community](../community/README.md).

---

Nguồn: DEC-053 (giới hạn), DEC-068 (đếm UTF-16), DEC-090 (chuẩn hóa và ký tự vô hình).
