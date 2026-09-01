// Seed & Mock Data aligned with database/postgres/seed.sql and schema.sql
export const INITIAL_SERVERS = [
  {
    id: '01990000-0000-7400-8000-000000000001',
    name: 'SCDC Community',
    slug: 'scdc-community',
    description: 'Server mẫu mô tả luồng dữ liệu thực tế của SCDC.',
    avatar: null,
    banner: null,
    ownerUserId: '01990000-0000-7000-8000-000000000001',
    role: 'owner',
    unreadCount: 0,
    channels: [
      {
        spaceId: '01990000-0000-7300-8000-000000000003',
        name: 'general',
        topic: 'Trao đổi chung của dự án',
        visibility: 1, // 1=public, 2=private, 3=read-only
        position: 0,
        unread: false,
      },
      {
        spaceId: '01990000-0000-7300-8000-000000000004',
        name: 'backend',
        topic: 'Thảo luận API, modular monolith và PostgreSQL database',
        visibility: 1,
        position: 1,
        unread: true,
      },
      {
        spaceId: '01990000-0000-7300-8000-000000000005',
        name: 'thong-bao',
        topic: 'Kênh chỉ đọc dành cho thông báo chính thức',
        visibility: 3, // read-only
        position: 2,
        unread: false,
      },
      {
        spaceId: '01990000-0000-7300-8000-000000000006',
        name: 'core-team',
        topic: 'Kênh riêng tư dành cho ban quản trị',
        visibility: 2, // private
        position: 3,
        unread: false,
      }
    ]
  },
  {
    id: '01990000-0000-7400-8000-000000000002',
    name: 'Frontend Developers',
    slug: 'frontend-devs',
    description: 'Cộng đồng lập trình viên React, Vite, CSS và UI/UX.',
    avatar: null,
    banner: null,
    ownerUserId: '01990000-0000-7000-8000-000000000003',
    role: 'member',
    unreadCount: 2,
    channels: [
      {
        spaceId: '01990000-0000-7300-8000-000000000007',
        name: 'welcome',
        topic: 'Chào mừng các thành viên mới gia nhập',
        visibility: 1,
        position: 0,
        unread: false,
      },
      {
        spaceId: '01990000-0000-7300-8000-000000000008',
        name: 'react-vite',
        topic: 'Chia sẻ kinh nghiệm thiết kế UI/UX hiện đại',
        visibility: 1,
        position: 1,
        unread: true,
      }
    ]
  }
];

export const INITIAL_DMS = [
  {
    spaceId: '01990000-0000-7300-8000-000000000001',
    spaceType: 1, // Direct Message
    user: {
      id: '01990000-0000-7000-8000-000000000002',
      username: 'bob',
      displayName: 'Bob Trần',
      status: 'online', // online, idle, dnd, offline
      bio: 'Backend developer tại SCDC.',
      customStatus: 'Đang coding .NET 10 🚀',
    },
    lastMessage: 'Chào Alice, mình đã kiểm tra và database đã sẵn sàng.',
    lastActivityAt: new Date(Date.now() - 15 * 60000).toISOString(),
    unreadCount: 1,
  },
  {
    spaceId: '01990000-0000-7300-8000-000000000002',
    spaceType: 2, // Group Chat
    name: 'Nhóm triển khai SCDC',
    membersCount: 3,
    user: {
      id: 'group-1',
      username: 'group_scdc',
      displayName: 'Nhóm triển khai SCDC',
      status: 'online',
      bio: 'Nhóm thảo luận chung giữa Frontend và Backend.',
    },
    lastMessage: 'Tài liệu mô hình database đính kèm.',
    lastActivityAt: new Date(Date.now() - 20 * 60000).toISOString(),
    unreadCount: 0,
  },
  {
    spaceId: '01990000-0000-7300-8000-000000000009',
    spaceType: 1,
    user: {
      id: '01990000-0000-7000-8000-000000000003',
      username: 'charlie',
      displayName: 'Charlie Lê',
      status: 'idle',
      bio: 'Frontend developer.',
      customStatus: 'Đang xem lại thiết kế web',
    },
    lastMessage: 'Mình bắt đầu làm giao diện danh sách cuộc trò chuyện nhé.',
    lastActivityAt: new Date(Date.now() - 90 * 60000).toISOString(),
    unreadCount: 0,
  }
];

export const INITIAL_MEMBERS = [
  {
    userId: '01990000-0000-7000-8000-000000000001',
    username: 'alice',
    displayName: 'Alice Nguyễn',
    nickname: 'Alice (Owner)',
    status: 'online',
    roleId: 'role-owner',
    roleName: 'Owner',
    roleColor: '#E53935',
    bio: 'Product Owner của dự án SCDC.',
    joinedAt: '2026-08-01T00:00:00Z',
  },
  {
    userId: '01990000-0000-7000-8000-000000000002',
    username: 'bob',
    displayName: 'Bob Trần',
    nickname: 'Bob BE',
    status: 'online',
    roleId: 'role-mod',
    roleName: 'Moderator',
    roleColor: '#1E88E5',
    bio: 'Backend developer .NET & PostgreSQL.',
    joinedAt: '2026-08-02T00:00:00Z',
  },
  {
    userId: '01990000-0000-7000-8000-000000000003',
    username: 'charlie',
    displayName: 'Charlie Lê',
    nickname: 'Charlie FE',
    status: 'idle',
    roleId: 'role-member',
    roleName: 'Member',
    roleColor: '#9E9E9E',
    bio: 'Frontend developer React & UI/UX.',
    joinedAt: '2026-08-03T00:00:00Z',
  },
  {
    userId: '01990000-0000-7000-8000-000000000004',
    username: 'linh',
    displayName: 'Linh Phạm',
    nickname: 'Linh',
    status: 'offline',
    roleId: 'role-member',
    roleName: 'Member',
    roleColor: '#9E9E9E',
    bio: 'QA Engineer.',
    joinedAt: '2026-09-01T00:00:00Z',
  }
];

export const INITIAL_ROLES = [
  {
    id: 'role-owner',
    name: 'Owner',
    color: '#E53935',
    position: 100,
    isDefault: false,
    isSystem: true,
    permissions: [
      'manage_server', 'manage_channels', 'manage_roles', 'invite_members',
      'kick_members', 'ban_members', 'read_messages', 'send_messages',
      'edit_own_messages', 'delete_messages', 'attach_files', 'add_reactions',
      'mention_everyone'
    ]
  },
  {
    id: 'role-mod',
    name: 'Moderator',
    color: '#1E88E5',
    position: 50,
    isDefault: false,
    isSystem: false,
    permissions: [
      'invite_members', 'kick_members', 'read_messages', 'send_messages',
      'edit_own_messages', 'delete_messages', 'attach_files', 'add_reactions'
    ]
  },
  {
    id: 'role-member',
    name: 'Member',
    color: '#9E9E9E',
    position: 0,
    isDefault: true,
    isSystem: true,
    permissions: [
      'read_messages', 'send_messages', 'edit_own_messages', 'attach_files', 'add_reactions'
    ]
  }
];

export const PERMISSION_DEFINITIONS = [
  { code: 'manage_server', name: 'Quản lý Server', description: 'Cập nhật thông tin và cấu hình server' },
  { code: 'manage_channels', name: 'Quản lý Kênh', description: 'Tạo, sửa và xóa channel' },
  { code: 'manage_roles', name: 'Quản lý Vai trò', description: 'Tạo role và gán quyền' },
  { code: 'invite_members', name: 'Tạo lời mời', description: 'Tạo link mời thành viên mới vào server' },
  { code: 'kick_members', name: 'Trục xuất thành viên', description: 'Kick thành viên khỏi server' },
  { code: 'ban_members', name: 'Cấm thành viên', description: 'Cấm vĩnh viễn thành viên vào server' },
  { code: 'read_messages', name: 'Xem tin nhắn', description: 'Xem lịch sử và đọc tin nhắn trong kênh' },
  { code: 'send_messages', name: 'Gửi tin nhắn', description: 'Gửi tin nhắn trong các kênh văn bản' },
  { code: 'edit_own_messages', name: 'Sửa tin nhắn của mình', description: 'Chỉnh sửa nội dung tin nhắn đã gửi' },
  { code: 'delete_messages', name: 'Xóa tin nhắn', description: 'Xóa tin nhắn của bất kỳ thành viên nào' },
  { code: 'attach_files', name: 'Đính kèm tệp', description: 'Tải lên hình ảnh, video và tài liệu' },
  { code: 'add_reactions', name: 'Thả biểu cảm', description: 'Thêm emoji reaction vào tin nhắn' },
  { code: 'mention_everyone', name: 'Mention @everyone', description: 'Thông báo tới toàn bộ thành viên server' },
];

export const INITIAL_MESSAGES = {
  // Messages for #general (spaceId 0199...0003)
  '01990000-0000-7300-8000-000000000003': [
    {
      id: '01990000-0000-7700-8000-000000000004',
      sequenceNo: 1004,
      spaceId: '01990000-0000-7300-8000-000000000003',
      author: {
        id: '01990000-0000-7000-8000-000000000001',
        username: 'alice',
        displayName: 'Alice Nguyễn',
        roleColor: '#E53935',
        roleName: 'Owner',
      },
      messageType: 1,
      content: 'Chào cả team! Chúng ta đang thiết kế lại toàn bộ giao diện WebClient của **SCDC** theo phong cách Discord/Slack chuyên nghiệp.',
      createdAt: new Date(Date.now() - 70 * 60000).toISOString(),
      editedAt: null,
      reactions: [
        { emoji: '🔥', count: 3, userReacted: true },
        { emoji: '🚀', count: 2, userReacted: false },
        { emoji: '❤️', count: 1, userReacted: false }
      ],
      replyTo: null,
      isPinned: true,
      threadCount: 2,
    },
    {
      id: '01990000-0000-7700-8000-000000000006',
      sequenceNo: 1006,
      spaceId: '01990000-0000-7300-8000-000000000003',
      author: null,
      messageType: 2, // System message
      content: 'Charlie Lê đã tham gia server.',
      createdAt: new Date(Date.now() - 40 * 60000).toISOString(),
      editedAt: null,
      reactions: [],
      replyTo: null,
      isPinned: false,
      threadCount: 0,
    },
    {
      id: '01990000-0000-7700-8000-000000000008',
      sequenceNo: 1008,
      spaceId: '01990000-0000-7300-8000-000000000003',
      author: {
        id: '01990000-0000-7000-8000-000000000003',
        username: 'charlie',
        displayName: 'Charlie Lê',
        roleColor: '#9E9E9E',
        roleName: 'Member',
      },
      messageType: 1,
      content: 'Giao diện mới có đầy đủ 4 cột: **Server Rail**, **Sub-Sidebar**, **Main Chat** và **Collapsible Panel** (Member List, Thread, Pinned Messages) rất mượt!',
      createdAt: new Date(Date.now() - 30 * 60000).toISOString(),
      editedAt: null,
      reactions: [
        { emoji: '👍', count: 2, userReacted: true },
        { emoji: '🎉', count: 2, userReacted: false }
      ],
      replyTo: null,
      isPinned: false,
      threadCount: 0,
    },
    {
      id: '01990000-0000-7700-8000-000000000009',
      sequenceNo: 1009,
      spaceId: '01990000-0000-7300-8000-000000000003',
      author: {
        id: '01990000-0000-7000-8000-000000000002',
        username: 'bob',
        displayName: 'Bob Trần',
        roleColor: '#1E88E5',
        roleName: 'Moderator',
      },
      messageType: 1,
      content: 'Database `scdc_chat` đã có sẵn schema `identity`, `community`, `messaging`, `moderation` và `audit` cực kỳ chuẩn chỉnh.',
      createdAt: new Date(Date.now() - 10 * 60000).toISOString(),
      editedAt: new Date(Date.now() - 8 * 60000).toISOString(),
      reactions: [
        { emoji: '💯', count: 3, userReacted: true }
      ],
      replyTo: {
        id: '01990000-0000-7700-8000-000000000008',
        authorName: 'Charlie Lê',
        content: 'Giao diện mới có đầy đủ 4 cột...'
      },
      isPinned: false,
      threadCount: 1,
    }
  ],

  // Messages for #backend (spaceId 0199...0004)
  '01990000-0000-7300-8000-000000000004': [
    {
      id: '01990000-0000-7700-8000-000000000005',
      sequenceNo: 1005,
      spaceId: '01990000-0000-7300-8000-000000000004',
      author: {
        id: '01990000-0000-7000-8000-000000000002',
        username: 'bob',
        displayName: 'Bob Trần',
        roleColor: '#1E88E5',
        roleName: 'Moderator',
      },
      messageType: 1,
      content: 'API đăng nhập JWT, refresh token rotation và session management đã xong trên .NET 10.',
      createdAt: new Date(Date.now() - 50 * 60000).toISOString(),
      editedAt: null,
      reactions: [
        { emoji: '🚀', count: 2, userReacted: true }
      ],
      replyTo: null,
      isPinned: true,
      threadCount: 0,
    }
  ],

  // Messages for DM with Bob (spaceId 0199...0001)
  '01990000-0000-7300-8000-000000000001': [
    {
      id: '01990000-0000-7700-8000-000000000001',
      sequenceNo: 1001,
      spaceId: '01990000-0000-7300-8000-000000000001',
      author: {
        id: '01990000-0000-7000-8000-000000000001',
        username: 'alice',
        displayName: 'Alice Nguyễn',
        roleColor: '#E53935',
        roleName: 'Owner',
      },
      messageType: 1,
      content: 'Chào Bob, phần database đã sẵn sàng chưa?',
      createdAt: new Date(Date.now() - 120 * 60000).toISOString(),
      editedAt: null,
      reactions: [
        { emoji: '👋', count: 1, userReacted: true }
      ],
      replyTo: null,
      isPinned: false,
      threadCount: 0,
    },
    {
      id: '01990000-0000-7700-8000-000000000002',
      sequenceNo: 1002,
      spaceId: '01990000-0000-7300-8000-000000000001',
      author: {
        id: '01990000-0000-7000-8000-000000000002',
        username: 'bob',
        displayName: 'Bob Trần',
        roleColor: '#1E88E5',
        roleName: 'Moderator',
      },
      messageType: 1,
      content: 'Chào Alice, mình đã kiểm tra và database đã sẵn sàng. Toàn bộ schema, trigger và constraint hoạt động trơn tru.',
      createdAt: new Date(Date.now() - 115 * 60000).toISOString(),
      editedAt: new Date(Date.now() - 110 * 60000).toISOString(),
      reactions: [
        { emoji: '👍', count: 1, userReacted: true },
        { emoji: '✅', count: 1, userReacted: false }
      ],
      replyTo: {
        id: '01990000-0000-7700-8000-000000000001',
        authorName: 'Alice Nguyễn',
        content: 'Chào Bob, phần database đã sẵn sàng chưa?'
      },
      isPinned: false,
      threadCount: 0,
    }
  ],

  // Messages for Group DM (spaceId 0199...0002)
  '01990000-0000-7300-8000-000000000002': [
    {
      id: '01990000-0000-7700-8000-000000000003',
      sequenceNo: 1003,
      spaceId: '01990000-0000-7300-8000-000000000002',
      author: {
        id: '01990000-0000-7000-8000-000000000003',
        username: 'charlie',
        displayName: 'Charlie Lê',
        roleColor: '#9E9E9E',
        roleName: 'Member',
      },
      messageType: 1,
      content: 'Mình bắt đầu làm giao diện danh sách cuộc trò chuyện nhé.',
      createdAt: new Date(Date.now() - 90 * 60000).toISOString(),
      editedAt: null,
      reactions: [
        { emoji: '🚀', count: 2, userReacted: true }
      ],
      replyTo: null,
      isPinned: false,
      threadCount: 0,
    },
    {
      id: '01990000-0000-7700-8000-000000000007',
      sequenceNo: 1007,
      spaceId: '01990000-0000-7300-8000-000000000002',
      author: {
        id: '01990000-0000-7000-8000-000000000001',
        username: 'alice',
        displayName: 'Alice Nguyễn',
        roleColor: '#E53935',
        roleName: 'Owner',
      },
      messageType: 3, // Attachment
      content: 'Tài liệu mô hình database đính kèm cho cả nhóm cùng tra cứu.',
      createdAt: new Date(Date.now() - 20 * 60000).toISOString(),
      editedAt: null,
      reactions: [
        { emoji: '✅', count: 2, userReacted: true }
      ],
      attachments: [
        {
          id: '01990000-0000-7720-8000-000000000001',
          name: 'database-design-v1.pdf',
          sizeBytes: 245760,
          mimeType: 'application/pdf',
        }
      ],
      replyTo: null,
      isPinned: true,
      threadCount: 0,
    }
  ]
};

export const INITIAL_THREADS = {
  '01990000-0000-7700-8000-000000000004': [
    {
      id: 'th-1',
      sequenceNo: 2001,
      author: {
        id: '01990000-0000-7000-8000-000000000002',
        username: 'bob',
        displayName: 'Bob Trần',
        roleColor: '#1E88E5',
      },
      content: 'Đồng ý với kế hoạch! Phần UI Dark theme nhìn rất bắt mắt và tiện thao tác.',
      createdAt: new Date(Date.now() - 65 * 60000).toISOString(),
      reactions: [{ emoji: '👍', count: 2, userReacted: true }]
    },
    {
      id: 'th-2',
      sequenceNo: 2002,
      author: {
        id: '01990000-0000-7000-8000-000000000003',
        username: 'charlie',
        displayName: 'Charlie Lê',
        roleColor: '#9E9E9E',
      },
      content: 'Mình vừa bổ sung phím tắt Shift+Enter xuống dòng và Enter gửi tin nhắn.',
      createdAt: new Date(Date.now() - 55 * 60000).toISOString(),
      reactions: [{ emoji: '⚡', count: 1, userReacted: false }]
    }
  ]
};
