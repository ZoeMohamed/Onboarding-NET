namespace TaskManagement.API.Common.Constants;

public static class TaskStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class TaskPriorities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Low,
        Medium,
        High
    };
}

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
}

public static class ApprovalActions
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
