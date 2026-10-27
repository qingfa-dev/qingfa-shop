namespace Core.Todos.Items;

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