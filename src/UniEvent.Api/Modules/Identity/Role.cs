namespace UniEvent.Api.Modules.Identity;

/// <summary>
/// The three roles supported by the system. Each user has exactly one role
/// (see phase-01 plan: single-role field chosen over a many-to-many Roles
/// table since multi-role-per-user is not a requirement here).
/// </summary>
public enum Role
{
    Organizer,
    Staff,
    Participant
}
