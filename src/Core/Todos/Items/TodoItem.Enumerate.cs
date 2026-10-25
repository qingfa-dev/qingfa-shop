namespace Core.Todos.Items;

/// <summary>The urgency of a todo.</summary>
public enum TodoPriority
{
    /// <summary>No urgency.</summary>
    Low = 0,
    /// <summary>The default urgency.</summary>
    Medium = 1,
    /// <summary>Urgent work.</summary>
    High = 2,
    /// <summary>Drop everything else.</summary>
    Urgent = 3
}

/// <summary>The lifecycle state of a todo.</summary>
public enum TodoStatus
{
    /// <summary>Created but not yet started.</summary>
    Pending = 0,
    /// <summary>Work has begun.</summary>
    InProgress = 1,
    /// <summary>Finished successfully.</summary>
    Completed = 2,
    /// <summary>Abandoned before completion.</summary>
    Cancelled = 3
}