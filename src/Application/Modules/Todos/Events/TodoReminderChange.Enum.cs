namespace Application.Modules.Todos.Events;

/// <summary>Identifies which reminder transition produced an integration event.</summary>
public enum TodoReminderChange
{
    /// <summary>The reminder was scheduled.</summary>
    Scheduled = 1,

    /// <summary>The reminder was moved forward.</summary>
    Snoozed = 2,

    /// <summary>The reminder was dismissed.</summary>
    Dismissed = 3,
}
