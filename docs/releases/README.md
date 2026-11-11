# SCDC — Các mốc bàn giao

SCDC có hai mốc. **MVP** là mốc nội bộ, ghi lại phần nền đã làm xong. **v1** là sản phẩm hoàn thiện đầu tiên và là mục tiêu của toàn bộ tài liệu hiện hành.

| Mốc | Nội dung | Hồ sơ |
|---|---|---|
| MVP (nội bộ) | Tài khoản, Community và nhắn tin riêng chạy trong một ứng dụng API duy nhất (modular monolith). Ghi lại bằng chứng đã có và việc còn chuyển sang v1. | [Nền MVP](mvp.md) |
| v1 (sản phẩm) | Tách thành các service, hoàn thiện tài khoản, cộng đồng, nhắn tin, thêm thoại, video, chia sẻ màn hình và điều kiện phát hành công khai. | [Hồ sơ v1](v1.md) |

Việc MVP còn dở được làm tiếp trong các gói v1. Mỗi gói bắt đầu khi phần nó phụ thuộc đã sẵn sàng, không cần chờ nghiệm thu toàn bộ MVP.

Lưu ý: "v1" ở đây là phiên bản sản phẩm, khác với tiền tố API `/api/v1`.

Mỗi tài liệu có vai trò riêng:

- Hành vi, giao diện, dữ liệu và API: trong tài liệu từng tính năng.
- Công suất và lịch: [kế hoạch](../project/planning.md).
- Điều kiện nghiệm thu và phát hành: [nghiệm thu](acceptance.md).
- Thay đổi phạm vi: [hồ sơ quyết định](../records/decisions/README.md).

Bằng chứng của MVP chỉ đúng với phiên bản code và phạm vi lúc ghi nhận. Nghiệm thu v1 cần kết quả chạy trên chính bản bàn giao v1.

---

Nguồn: DEC-114, DEC-116.
