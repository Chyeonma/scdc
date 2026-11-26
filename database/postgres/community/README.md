# Baseline database Community

Baseline này tạo database Community sạch cho preview và kiểm thử bước 1 của S3-10. DDL lấy từ schema bootstrap `main@7a2a88b` sau STEP-2 trên PostgreSQL 18; không chứa dữ liệu người dùng.

| File | Vai trò |
|---|---|
| [schema.sql](schema.sql) | Extension `pg_trgm`, schema Community, 21 bảng, hàm, trigger, index và ràng buộc cục bộ |
| [baseline.sql](baseline.sql) | Sáu permission và tám dòng ledger checksum của schema đã được gộp; thời điểm baseline ghi lúc khởi tạo |
| [runtime-grants.sql](runtime-grants.sql) | Quyền role runtime, giữ hạn chế ledger, fence, receipt và tiến trình phối hợp của S2-11 |
| [init.sh](init.sh) | Khởi tạo role bằng secret file trên volume preview mới |

Thứ tự khởi tạo là schema → baseline → role/grants, chạy bởi administrator. API không tự chạy migration. `schema.sql` dùng CREATE và dừng nếu đối tượng đã có; không phải công cụ nâng cấp hoặc chuyển database dev.

Chạy toàn bộ bằng [profile preview](../../../services/Community/README.md#chạy-preview-riêng). Instance thử không có schema Identity hoặc Messaging; runtime không sở hữu schema và không có quyền DDL. Host chỉ nhận connection Community, giữ readiness chưa sẵn sàng tới khi hoàn thiện các tích hợp service.

Bộ chuyển dữ liệu Community, manifest catalog nguồn/đích, bảo toàn trạng thái dở dang và diễn tập phục hồi thuộc bước chuyển tiếp theo. Khi đó cần kiểm tra catalog của dữ liệu thật, bao gồm constraint/helper legacy; baseline sạch không thay cho đối chiếu nguồn.

Nguồn: [bố trí database](../../../docs/system/database.md#v1-database-layout), [checklist tách service](../../../docs/guides/operations.md#service-extraction), [test baseline](../../../tests/SCDC.Api.Tests/Infrastructure/CommunityHostTests.cs).
