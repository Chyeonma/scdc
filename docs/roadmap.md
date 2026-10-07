# SCDC — Lộ trình MVP và v1

Cập nhật bố cục: 2026-10-07. Phân biệt mốc bàn giao nhỏ đầu tiên với bản hoàn thiện đã được đặc tả từ trước.

## Hai mốc sản phẩm

| Mốc | Mục đích | Phạm vi đã xác nhận | Hồ sơ bàn giao |
|---|---|---|---|
| MVP | Có bản chạy được để nhóm bắt đầu triển khai và tích hợp mà không phải xử lý toàn bộ sản phẩm cùng lúc | Identity, Community và Direct Messaging trong một API host Modular Monolith | [MVP](releases/mvp.md) |
| v1 | Bản hoàn thiện đầu tiên, kế thừa phạm vi đầy đủ trước đây được gọi là MVP | Chuyển microservice; hoàn thiện ba tính năng nền; bổ sung gọi riêng, phòng thoại, camera/video và chia sẻ màn hình | [v1](releases/v1.md) |

Vg xác nhận ngày 2026-10-06: ban đầu dự kiến một bản; nay thêm MVP trước bản đầy đủ để nhóm dễ bắt đầu. Media thuộc v1 theo DEC-114; microservice được chuyển sang v1 theo [DEC-116](decisions.md#dec-116), thay thời điểm trong DEC-115. Phân công/cân tải cập nhật theo [DEC-117](decisions.md#dec-117) tại [nhân sự](project/planning.md#team) và [công suất](project/planning.md#capacity).

MVP là nền để phát triển tiếp thành v1. Hoàn thành MVP không đồng nghĩa hoàn thành mọi use case trong một tính năng, đạt toàn bộ chỉ tiêu v1 hoặc được phép mở đăng ký công khai. `v1` ở đây là mốc sản phẩm; prefix API `/api/v1`, tên test hay phiên bản tài liệu có cách quản lý riêng.

## Cách tổ chức tài liệu

[Mục lục và cây thư mục](README.md#cấu-trúc) quản lý bố cục hiện hành. Các tính năng dùng `README/status/specs/design/delivery`; hồ sơ dự án tách trong `project/`, cơ chế Messaging dùng chung trong `shared/messaging/`.

| Nội dung | Nguồn chuẩn | Cách dùng ở bản còn lại |
|---|---|---|
| Hành vi, quyền, dữ liệu/API, UX, AC/TC của tính năng | File trong `features/` và hợp đồng liên quan | Hồ sơ MVP/v1 dẫn tới UC/quy tắc/AC được chọn, không chép lại đặc tả |
| Phần phải bàn giao ở một mốc | File tương ứng trong `releases/` | Roadmap chỉ dẫn đường và tóm tắt các nhóm chức năng |
| Gói việc, phụ thuộc và cách chứng minh hoàn tất mốc | File tương ứng trong `releases/` | Gói Community cụ thể nằm trong `features/community/delivery/`; task dẫn tới scope và đặc tả nguồn |
| Trạng thái hiện tại và bằng chứng của từng tính năng | `features/<tính-năng>/status.md` và hồ sơ trong `delivery/` | Tổng quan và các hồ sơ mốc dẫn liên kết, không lặp bảng tiến độ |
| Mục tiêu toàn sản phẩm, nguồn lực, dự toán và khoảng trống | [Dự án](project/README.md), [kế hoạch](project/planning.md), [readiness](project/readiness.md) | Hồ sơ mốc dẫn chiếu; không tạo thêm dự toán hoặc bảng tiến độ tài liệu độc lập |
| Cơ chế xử lý tin dùng chung | [Messaging](shared/messaging/README.md) | DM và tin phòng dẫn chiếu; quyền/hành trình riêng nằm ở đặc tả tính năng |
| Quyết định và thay đổi phạm vi | `decisions.md` | Các file bị ảnh hưởng cập nhật cùng thay đổi |

Giữ nguyên mã REQ/SCP/UC/AC/TC/DEC đã có. Các bản đặc tả đầy đủ tiếp tục dùng cho v1; MVP chỉ chọn phần cần làm trước. Nếu một hành vi MVP khác bản đầy đủ, ghi riêng hành vi và quyết định áp dụng trong đặc tả nguồn, rồi dẫn chiếu từ hồ sơ mốc. Không dùng nhãn MVP để tự sửa các quy tắc quyền hoặc dữ liệu đã chốt.

Khi bàn giao một mốc, ghi commit, phạm vi UC/AC, hợp đồng, build và kết quả kiểm thử trong [mẫu hồ sơ](templates/release-record.md). Commit cố định phiên bản đặc tả/hợp đồng đã dùng; không cần sao chép toàn bộ `features/` sang hai cây tài liệu.

## Cách đọc để bắt đầu làm

1. Đọc [MVP](releases/mvp.md) để biết gói hiện tại và phần chưa cần triển khai.
2. Mở đúng mục UC/quy tắc/API/AC được gói dẫn chiếu trong đặc tả tính năng.
3. Rà soát phụ thuộc trực tiếp và chốt gói đầu tiên; làm, kiểm thử và tích hợp gói đó.
4. Ghi phần đã chứng minh trên build cụ thể; phần chưa làm vẫn thuộc kế hoạch v1.

Không cần đọc toàn bộ media, restore hay vận hành công khai để bắt đầu một gói tạo server hoặc gửi DM. Những quy tắc trực tiếp chi phối gói, như xác thực, quyền đọc/gửi và ý nghĩa tin đã lưu, vẫn phải được hiểu và kiểm chứng.

## Những nhóm vấn đề cần quản lý tiếp

| Nhóm | Nơi theo dõi | Trạng thái phạm vi |
|---|---|---|
| Gọi riêng, phòng thoại, video, chia sẻ màn hình | [Media](features/voice-video/README.md), [v1](releases/v1.md) | Đã nằm trong phạm vi bản đầy đủ; cần triển khai và kiểm chứng |
| Email, worker và môi trường chạy | [Accounts](features/accounts/design/README.md#detailed-design), [MVP](releases/mvp.md#packages) | Thái phụ trách theo phân công kế hoạch DEC-117; Vg cung cấp job/policy, mỗi chủ gói nghiệp vụ tự kiểm thử |
| Phân quyền, lỗi/mất kết nối, trình duyệt, tải và bảo vệ dữ liệu | Đặc tả tính năng, [vòng đời dữ liệu](data-lifecycle.md), [nghiệm thu](release-operations.md) | Có yêu cầu hiện hành; lựa chọn tiêu chí cho MVP và hoàn thiện v1 được ghi ở hồ sơ mốc |
| Gửi file, xử lý DM không mong muốn, thông báo sản phẩm hay công cụ quản trị bổ sung | [Sổ quyết định](decisions.md#open-questions) | Chưa tự đưa vào v1; cần chọn tính năng và mức hỗ trợ trước khi đặc tả thêm |

Mô hình 16 tuần/250 triệu ở [dự toán](project/planning.md#budget) là cơ sở minh họa cũ, chưa phải hai lịch phát hành mới. [Kế hoạch hiện hành](project/planning.md#delivery) chia giai đoạn và quỹ công suất từng người, gồm 20% dự phòng; thời gian hoàn thành vẫn cần ước lượng theo kết quả thật.
