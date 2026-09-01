sequenceDiagram
    autonumber
    actor Alice as Sender (Alice)
    participant Gateway as WebSocket Gateway (SignalR / Netty)
    participant RateLimit as In-Memory Cache (Redis)
    participant CoreSvc as Chat Core Service
    participant EventBus as Message Broker (Kafka / Redis PubSub)
    participant Database as Primary DB (PostgreSQL / ScyllaDB)
    participant PushSvc as Push Notification Svc (FCM/APNs)
    actor Bob as Online Recipient (Bob)
    actor Charlie as Offline User (Charlie)

    Alice->>Alice: 1. Optimistic UI (Hiển thị 'Sending...', tạo UUIDv7 nonce)
    Alice->>Gateway: 2. Gửi tin nhắn qua WebSocket Frame
    Gateway->>RateLimit: 3. Kiểm tra Rate Limit (Token Bucket 30 req/10s)
    RateLimit-->>Gateway: Hợp lệ
    Gateway->>CoreSvc: 4. Chuyển tiếp Request đã xác thực

    rect rgb(240, 248, 255)
    Note over CoreSvc,Database: Giai đoạn Xử lý & Lưu trữ (Persistence)
    CoreSvc->>Database: 5. Ghi vào DB (Message Entity + Idempotency Key)
    CoreSvc->>RateLimit: 6. Cache tin nhắn mới vào Hot Cache
    CoreSvc-->>Alice: 7. Trả về ACK (Server Message ID, CreatedAt, status 'Sent')
    end

    rect rgb(255, 245, 238)
    Note over CoreSvc,Bob: Giai đoạn Phát tán tức thời (Fan-out)
    CoreSvc->>EventBus: 8. Publish sự kiện "MessageCreated"
    EventBus->>Gateway: 9. Phân phối sự kiện tới các Gateway Nodes
    Gateway->>Bob: 10. Đẩy tin nhắn qua WebSocket tới Bob
    Bob-->>Gateway: 11. Gửi ACK đã nhận (Delivery Receipt)
    end

    rect rgb(245, 255, 245)
    Note over EventBus,Charlie: Giai đoạn Xử lý người dùng Offline
    EventBus->>PushSvc: 12. Phát hiện Charlie đang Offline
    PushSvc->>Charlie: 13. Gửi APNs / FCM Push Notification
    end