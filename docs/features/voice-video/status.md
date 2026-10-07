# SCDC — Trạng thái media

Tổ chức lại: 2026-10-07. Nguồn trạng thái implementation, đầu vào còn thiếu và liên kết bằng chứng media.

Cập nhật: 2026-10-06. Phạm vi: REQ-007/008/009, SCP-006/007, DEC-046–050/078–085/099–102 và AC-MEDIA.

Media thuộc [v1](../../releases/v1.md) theo DEC-114; không phải điều kiện hoàn thành mốc MVP mới. Điều kiện gọi, giới hạn 10/20/2 và reconnect 30 giây đã chốt DEC-078–080; thu hồi 5 giây, nhận đầu tiên thắng, thiết bị mặc định tắt và không truyền âm thanh màn hình đã chốt DEC-099–102. Có thiết kế chi tiết, OpenAPI, catalogue realtime và fixture; LiveKit tự host/media desktop/chất lượng theo DEC-082/084/085. Provider/backend/API chưa triển khai; bộ kiểm soát tại SFU và các mục tiêu cần thử nghiệm.

<a id="gaps"></a>

## Quyết định còn thiếu

| Nhóm | Cần quyết định | Liên quan |
|---|---|---|
| Bắt đầu cuộc gọi | DEC-078/100/101 chốt điều kiện/30 giây/multi-device/thiết bị; có OpenAPI/coordinator/UX. Còn review/limiter và proof race. | OQ-006, MEDIA-GAP-01/02/05 |
| Phòng thoại | View cho vào; giới hạn 10/20 theo DEC-079 và cutoff/fail-close DEC-099. Có room lifecycle/reservation/draining; còn migration/guard và bằng chứng SFU. | OQ-004/006, MEDIA-GAP-02/03/04 |
| Video/chia sẻ | Một camera/share/người, 2 share/room; chỉ hình screen DEC-102. Có nguồn/permit/gate/layout; còn SDK/quota gate/capture matrix thực tế. | OQ-006, MEDIA-GAP-03/05 |
| Mất kết nối | Tự reconnect/giữ chỗ 30 giây DEC-080; đã có retry/heartbeat/epoch/deadline design. Còn proof resume, drain và stale callback. | OQ-006, MEDIA-GAP-02/03/04 |
| Chất lượng | Đã chốt DEC-085; còn môi trường, thiết bị/version, hiệu chỉnh đồng hồ và kết quả đo. | OQ-007 |
| Kỹ thuật/chi phí | LiveKit tự host DEC-084; có admission/lease/nguồn design, chưa chọn extension/fork hoặc pin server/SDK. Host/domain/TURN, key store, mức dùng, chi phí và công sức duy trì cần review. | OQ-008/010, MEDIA-GAP-03/06/07 |

Theo [phân công DEC-117](../../project/planning.md#team), Vg giữ chính sách quyền/admission/quota/thu hồi; Sáng sở hữu backend và UI điều khiển lifecycle cuộc gọi; Thái sở hữu adapter provider/SDK, thiết bị/hiển thị nguồn và bộ chạy. Mỗi người chứng minh phần sở hữu theo kịch bản đã chốt trước khi xác nhận lịch, chi phí và nghiệm thu; [gói v1](../../releases/v1.md) ghi phụ thuộc và công suất theo giai đoạn.
