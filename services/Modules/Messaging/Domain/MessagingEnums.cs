namespace SCDC.Modules.Messaging.Domain;

internal enum SpaceType : short
{
    Direct = 1,
    Group = 2,
    Channel = 3
}

internal enum SpaceStatus : short
{
    Active = 1,
    Archived = 2,
    Deleted = 3
}

internal enum SpaceMemberRole : short
{
    Member = 1,
    Admin = 2,
    Owner = 3
}

internal enum SpaceMembershipStatus : short
{
    Active = 1,
    Left = 2,
    Removed = 3
}

internal enum NotificationLevel : short
{
    None = 0,
    MentionsOnly = 1,
    AllMessages = 2
}
