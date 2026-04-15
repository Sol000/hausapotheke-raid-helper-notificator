namespace RaidNotificator.DTOs;

public enum DiffType
{
    SignedOut,
    SignedIn,
    Abcence,
    Late
}

public class RegistrationDiff
{
    public DiffType Type { get; set; }
    public required string Username { get; set; }
    public required string OldClass { get; set; }
    public required string Class { get; set; }
    public required string Role { get; set; }
    public required string Spec { get; set; }
    public int Number { get; set; }
}