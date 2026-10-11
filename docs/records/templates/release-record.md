<a id="mẫu-hồ-sơ-nghiệm-thu-và-phát-hành-scdc"></a>

# Mẫu hồ sơ nghiệm thu và phát hành SCDC

Mẫu trống, không phải kết quả nghiệm thu. Copy cho từng đợt vào nơi nhóm chọn; không điền Đạt từ việc tài liệu/test đã tồn tại. Ghi rõ mốc [MVP](../../releases/mvp.md#acceptance) hoặc [v1](../../releases/v1.md#acceptance), UC/AC và các điều kiện áp dụng; không bắt MVP điền Đạt cho gate media ngoài scope. Quy trình ở [release-operations.md](../../releases/acceptance.md#acceptance-process); runbook ở [operations-runbook.md](../../guides/operations.md). Không ghi token, secrets hoặc nội dung người dùng thật.

<a id="1-định-danh-đợt"></a>

## 1. Định danh đợt

| Trường | Giá trị |
|---|---|
| Mã hồ sơ / loại | Chưa điền — bàn giao nội bộ / bàn giao MVP / nghiệm thu v1 / diễn tập / phát hành |
| Phạm vi SCP và phần loại trừ có căn cứ | Chưa điền |
| Commit / artefact digest Web/API / cấu hình | Chưa điền |
| Schema/migration / media server-extension-SDK | Chưa điền |
| Môi trường / domain / region / tài nguyên | Chưa điền |
| Dữ liệu thử / cách tạo / thời gian nguồn UTC | Chưa điền |
| OS/browser/thiết bị/viewport và ngày khóa ma trận | Chưa điền |
| Người kiểm thử / rà soát kỹ thuật | Chưa điền |
| Người xác nhận nghiệm thu / người duyệt phát hành | Chưa xác nhận |
| Người thực hiện deploy / người nhận vận hành, thay thế | Chưa xác nhận |
| Cửa sổ thao tác / phạm vi trực / nơi nhận cảnh báo | Chưa điền |

<a id="test-results"></a>

<a id="2-kết-quả-kiểm-thử-và-bằng-chứng"></a>

## 2. Kết quả kiểm thử và bằng chứng

Mỗi lần chạy là một dòng riêng. Trạng thái: Chưa chạy / Bị chặn / Đạt / Chưa đạt / Không áp dụng. Không áp dụng cần căn cứ phạm vi đã chốt; ca bắt buộc chưa có implementation vẫn là Bị chặn/Chưa chạy.

| Lần chạy / ngày giờ | AC / TC / scope | Build/môi trường/browser/dataset | Kỳ vọng | Thực tế | Trạng thái | Bằng chứng đã lọc | Lỗi / người chạy |
|---|---|---|---|---|---|---|---|
| Chưa điền | Chưa điền | Chưa điền | Chưa điền | Chưa thực thi | Chưa chạy | Chưa có | Chưa điền |

| Nhóm bắt buộc | Tổng ca / Đạt / Chưa đạt / Bị chặn / Chưa chạy / Không áp dụng | Phần chưa bao phủ và lý do |
|---|---|---|
| Accounts | Chưa tổng hợp | Chưa đánh giá |
| DM | Chưa tổng hợp | Chưa đánh giá |
| Community/quyền/tin phòng | Chưa tổng hợp | Chưa đánh giá |
| Media desktop | Chưa tổng hợp | Chưa đánh giá |
| Vòng đời dữ liệu/restore | Chưa tổng hợp | Chưa đánh giá |
| Vận hành/triển khai/rollback | Chưa tổng hợp | Chưa đánh giá |

<a id="đo-chất-lượng"></a>

### Đo chất lượng

Dẫn [mục tiêu/phương pháp đo](../../system/quality.md#quality-targets); đính kèm mẫu đo/raw aggregate đã lọc. Ghi mẫu số, khoảng thời gian, clock skew và phần bị loại có lý do; không chỉ ghi percentile đẹp nhất.

| Chỉ số | Build/cấu hình/điều kiện/tổng mẫu | Giá trị thực tế p50/p95/p99 hoặc tỷ lệ | Mục tiêu DEC | Kết quả và bằng chứng |
|---|---|---|---|---|
| API, gửi→commit, commit→render, lỗi dịch vụ | Chưa điền | Chưa đo | DEC-083 | Chưa đánh giá |
| Thu hồi chat/media, deny rejoin | Chưa điền | Chưa đo | DEC-083/099 | Chưa đánh giá |
| Join, latency audio/video/share, chất lượng hình ảnh | Chưa điền | Chưa đo | DEC-085 | Chưa đánh giá |
| Quota 10/20/2, reconnect và race | Chưa điền | Chưa thực thi | DEC-079/080 | Chưa đánh giá |
| RPO/RTO/tuổi backup và cửa sổ recoverable | Chưa điền | Chưa đo | DEC-086/109 | Chưa đánh giá |

<a id="3-lỗi-tồn-và-kết-quả-kiểm-tra-lại"></a>

## 3. Lỗi tồn và kết quả kiểm tra lại

Mức ảnh hưởng theo [phân loại lỗi](../../releases/acceptance.md#defects) và DEC-110. Lỗi đã sửa cần kết quả kiểm tra lại trên build mới, không chỉ commit đóng issue. Chỉ lỗi giao diện nhỏ có thể để lại với ảnh hưởng/người/hạn sửa và xác nhận; người duyệt vẫn chưa được chọn DEC-111.

| Mã lỗi / AC-TC | Mức / phạm vi và tái hiện | Trạng thái | Người / hạn sửa | Cách tiếp tục nếu có | Build/lần kiểm tra lại | Người chấp nhận lỗi tồn và thời điểm |
|---|---|---|---|---|---|---|
| Chưa điền | Chưa đánh giá | Chưa đánh giá | Chưa điền | Chưa điền | Chưa có | Chưa xác nhận |

<a id="4-checklist-điều-kiện-phát-hành"></a>

## 4. Checklist điều kiện phát hành

Ghi Đạt/Chưa đạt/Chưa đánh giá/Không áp dụng, cùng link bằng chứng cho từng gate. Checkbox chỉ tick khi đã kiểm chứng; thiếu hồ sơ hoặc người duyệt thì chưa đạt điều kiện mở công khai.

- [ ] RLS-GATE-01: Phạm vi, build, cấu hình, browser/dataset đã khóa và các thay đổi đã rà soát.
- [ ] RLS-GATE-02: Hành trình chức năng trong phạm vi có kết quả đạt.
- [ ] RLS-GATE-03: Quyền, phiên, chống trùng và thu hồi có bằng chứng thực tế.
- [ ] RLS-GATE-04: Tải/chất lượng đạt các ngưỡng đã chốt.
- [ ] RLS-GATE-05: Migration, restore, retention và bảo vệ dữ liệu đã kiểm chứng.
- [ ] RLS-GATE-06: Cấu hình production, secrets, email/media và đường truy cập đã kiểm tra.
- [ ] RLS-GATE-07: Staging deploy/rollback/smoke đã diễn tập với cấu hình tương thích.
- [ ] RLS-GATE-08: Giám sát, cảnh báo, hỗ trợ và người nhận vận hành/thay thế có xác nhận.
- [ ] RLS-GATE-09: Lỗi tồn được xử lý theo chính sách đã chốt; các ca bắt buộc không bị bỏ qua.
- [ ] RLS-GATE-10: Người có thẩm quyền đã xác nhận nghiệm thu và quyết định phát hành.

| Gate | Trạng thái | Bằng chứng / phần còn thiếu | Người rà soát |
|---|---|---|---|
| Chưa điền | Chưa đánh giá | Chưa có | Chưa điền |

<a id="restore-record"></a>

<a id="5-kết-quả-diễn-tập-restore"></a>

## 5. Kết quả diễn tập restore

| Trường | Giá trị thực tế |
|---|---|
| Loại diễn tập/sự cố / build-schema nguồn và đích | Chưa điền |
| Mốc đầu tiên ảnh hưởng / phát hiện / xác nhận / bắt đầu restore | Chưa điền |
| Base ID/timeline/start-complete time/checksum/tuổi gốc | Chưa điền |
| Chuỗi WAL/segment thiếu/cửa sổ recoverable sớm nhất-mới nhất | Chưa điền |
| Recovery point dự kiến và thực tế / marker cuối khôi phục được | Chưa đo |
| Sổ bảo vệ/checkpoint/watermark/key ID, record chưa rõ | Chưa điền |
| Xóa/thu hồi/ACL/ownership floor, placeholder edit mất bản mới | Chưa thực thi |
| Session/link/email/media/cache/generation trước mở lại | Chưa thực thi |
| Retention theo timestamp gốc và chống trùng sau restore | Chưa thực thi |
| Thao tác đã commit không phục hồi được / dữ liệu thiếu | Chưa đối soát |
| RPO thực đo và phương pháp / RTO từng bước và tổng | Chưa đo |
| Thời gian chờ người/keys/tool / scope còn đóng | Chưa điền |
| Smoke sau restore / bằng chứng | Chưa thực thi |
| Người kiểm tra / người cho phép mở lại / thời điểm | Chưa xác nhận |
| Kết luận đạt RPO/RTO và DEC-108/109 / hạn chế | Chưa đánh giá |

<a id="6-kế-hoạch-và-nhật-ký-triển-khai"></a>

## 6. Kế hoạch và nhật ký triển khai

| Nội dung | Kế hoạch / kết quả |
|---|---|
| Migration, compatibility app-schema-key-generation | Chưa có |
| Backup/checkpoint trước thay đổi, đường rollback/forward fix/restore | Chưa có |
| Cách đóng/mở ingress/ghi/worker/admission và fencing | Chưa có |
| Manifest/lệnh đã diễn tập, tiêu chí dừng và theo dõi | Chưa có |

| Ngày giờ / người thực hiện | Bước và lệnh thực tế đã bỏ secrets | Build/schema/trạng thái trước-sau | Kết quả / mã thoát / bằng chứng | Quyết định và người xác nhận |
|---|---|---|---|---|
| Chưa điền | Chưa thực thi | Chưa điền | Chưa có | Chưa xác nhận |

<a id="7-bàn-giao-và-quyết-định"></a>

## 7. Bàn giao và quyết định

- [ ] Người chính/thay thế đã nhận quyền đúng phạm vi và lịch trực/kênh nhận sự cố.
- [ ] Runbook, dashboard, backup/keys và nơi lưu hồ sơ truy cập được bằng tài khoản được cấp.
- [ ] Cảnh báo thử tới đúng người nhận; công việc/hạn xử lý lỗi tồn đã được nhận.
- [ ] Không có secrets hoặc dữ liệu người dùng thật trong hồ sơ bàn giao.

| Quyết định | Kết quả / người / thời điểm / căn cứ |
|---|---|
| Nghiệm thu phạm vi bàn giao | Chưa đánh giá / chưa xác nhận |
| Cho phép triển khai production | Chưa quyết định |
| Cho phép mở đăng ký công khai | Chưa quyết định |
| Rollback/restore nếu đã kích hoạt | Chưa áp dụng |
| Hạn chế và đầu việc tiếp theo | Chưa tổng hợp |

Kết luận Đạt phải nối tới lần chạy cụ thể và người xác nhận. Thay artefact/migration/cấu hình liên quan sau duyệt cần đánh giá ảnh hưởng và chạy lại các nhóm bị tác động; không mang chữ ký của build trước sang build mới.
