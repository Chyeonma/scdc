# Tài liệu SCDC

Bộ tài liệu hiện hành được tổ chức theo dự án và tính năng. Mỗi nội dung có một nguồn chuẩn; các bản đã thay thế nằm trong archive. Cập nhật hợp nhất: 2026-10-03.

## Bắt đầu từ đâu

| Bạn cần làm gì | Đọc |
|---|---|
| Hiểu mục tiêu, requirement, scope và kế hoạch | [Dự án](project.md) |
| Tra cứu quyết định, căn cứ hoặc vấn đề chưa chốt | [Quyết định](decisions.md) |
| Thiết kế/tích hợp xuyên module | [Kiến trúc](architecture.md) |
| Chạy repo, chuẩn bị dữ liệu hoặc kiểm thử kỹ thuật | [Phát triển](development.md) |
| Triển khai một tính năng | Mở đặc tả tương ứng bên dưới; trong cùng file có nghiệp vụ, UX, hợp đồng, AC và test |
| Nghiệm thu, phát hành, vận hành | [Phát hành và vận hành](release-operations.md) |

## Đặc tả tính năng

| Tài liệu | Scope | Nội dung |
|---|---|---|
| [Tài khoản](features/accounts.md) | SCP-002 | Đăng ký/xác minh, đăng nhập, hồ sơ, mật khẩu, phiên và API hiện tại |
| [Nhắn tin riêng](features/direct-messaging.md) | Phần DM của SCP-005 | Tìm người, hội thoại hai người, tin văn bản, thử lại, đồng thời và reconnect |
| [Cộng đồng](features/community.md) | SCP-003/004 và phần tin phòng của SCP-005 | Tham gia, phòng, lời mời, vai trò, quyền và thu hồi |
| [Thoại/video](features/voice-video.md) | SCP-006/007 | Gọi riêng, phòng thoại, video/chia sẻ màn hình và đầu vào còn thiếu |

Tình trạng sẵn sàng theo scope được quản lý tại [project.md](project.md#coverage); mã quyết định ở [decisions.md](decisions.md#decisions). Không duy trì một bảng tiến độ độc lập tại mục lục.

## Cách quản lý thông tin

- Scope xác định phạm vi bàn giao; requirement xác định hành vi/điều kiện cần đáp ứng. REQ/SCP/SUC ở project.md; quy tắc và AC/TC chi tiết ở từng tính năng.
- DEC/OQ quản lý kết luận và nội dung cần làm rõ; đặc tả dẫn chiếu các mã này. Giữ nguyên mã khi chuyển file, không cấp lại mã đã loại.
- Mỗi đặc tả ghi riêng mức xác nhận nghiệp vụ, trạng thái thiết kế, implementation và bằng chứng kiểm thử. “Có code” hoặc “có ca test” không đồng nghĩa “đã nghiệm thu”.
- API đã triển khai được đối chiếu với source/OpenAPI; phương án tương lai có nhãn đề xuất. Kiến trúc hiện tại và định hướng đợt sau được ghi riêng.
- Nội dung thay đổi được cập nhật ở nguồn chuẩn và các phần phụ thuộc trong cùng thay đổi. Trang tổng quan dẫn link, không chép một phiên bản quy tắc khác.

## Cấu trúc

```text
docs/
├── README.md
├── project.md
├── decisions.md
├── architecture.md
├── development.md
├── release-operations.md
├── features/
│   ├── accounts.md
│   ├── direct-messaging.md
│   ├── community.md
│   └── voice-video.md
└── archive/
```

[Bản đồ chuyển đổi và lịch sử](archive/README.md) cho biết nội dung file cũ đã chuyển về đâu. Các bản lưu dùng để tra cứu lịch sử, không dùng làm nguồn yêu cầu hiện hành.
