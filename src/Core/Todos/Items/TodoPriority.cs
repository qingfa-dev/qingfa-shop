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