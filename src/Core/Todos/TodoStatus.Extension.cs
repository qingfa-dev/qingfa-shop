namespace Core.Todos;

/// <summary>Transition rules for <see cref="TodoStatus"/>.</summary>
public static class TodoStatusExtensions
{
    /// <summary>Determines whether a todo may move from one status to another.</summary>
    /// <param name="current">The status the todo is in now.</param>
    /// <param name="next">The status the todo would move to.</param>
    /// <returns><c>true</c> when the move is legal; otherwise <c>false</c>.</returns>
    public static bool CanTransitionTo(this TodoStatus current, TodoStatus next) =>
        current switch
        {
            TodoStatus.Pending => next is TodoStatus.InProgress or TodoStatus.Cancelled,
            TodoStatus.InProgress => next is TodoStatus.Completed or TodoStatus.Cancelled,
            TodoStatus.Completed => next is TodoStatus.Pending,
            TodoStatus.Cancelled => next is TodoStatus.Pending,
            _ => false
        };

    /// <summary>Determines whether a status is an end state that must be reopened.</summary>
    /// <param name="status">The status to inspect.</param>
    /// <returns><c>true</c> when the status is terminal; otherwise <c>false</c>.</returns>
    public static bool IsTerminal(this TodoStatus status) =>
        status is TodoStatus.Completed or TodoStatus.Cancelled;
}