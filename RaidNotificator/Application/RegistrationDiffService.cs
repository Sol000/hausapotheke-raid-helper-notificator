using RaidNotificator.Contracts;
using RaidNotificator.DTOs;

namespace RaidNotificator.Application;

public sealed class RegistrationDiffService : IRegistrationDiffService
{
    public RegistrationDiff? GetDiff(RaidEvent before, RaidEvent after)
    {
        var idsBefore = before.SignUps.Select(x => x.UserId).ToHashSet();
        var idsAfter = after.SignUps.Select(x => x.UserId).ToHashSet();

        if (idsAfter.Count != idsBefore.Count)
        {
            
            var newId = idsAfter.FirstOrDefault(id => !idsBefore.Contains(id));
            if (!string.IsNullOrWhiteSpace(newId))
            {
                var signUp = after.SignUps.First(s => s.UserId == newId);
                if (signUp.ClassName.Contains("Bench") ||
                    signUp.ClassName.Contains("Tentative") ||
                    signUp.ClassName.Contains("Absence"))
                {
                    return new RegistrationDiff
                    {
                        Type = DiffType.Abcence,
                        Username = signUp.Name,
                        OldClass = string.Empty,
                        Class = signUp.ClassName,
                        Spec = signUp.SpecName ?? string.Empty,
                        Role = signUp.RoleName,
                        Number = signUp.Position,
                    };
                }
                return new RegistrationDiff
                {
                    Type = DiffType.SignedIn,
                    Username = signUp.Name,
                    OldClass = string.Empty,
                    Class = signUp.ClassName,
                    Spec = signUp.SpecName ?? string.Empty,
                    Role = signUp.RoleName,
                    Number = signUp.Position,
                };
            }
        }

        foreach (var afterSignUp in after.SignUps)
        {
            var beforeSignUp = before.SignUps.FirstOrDefault(x => x.UserId == afterSignUp.UserId);
            if (beforeSignUp == null || afterSignUp.ClassName == beforeSignUp.ClassName)
            {
                continue;
            }

            if (afterSignUp.ClassName.Contains("Bench") ||
                afterSignUp.ClassName.Contains("Tentative") ||
                afterSignUp.ClassName.Contains("Absence"))
            {
                return BuildDiff(DiffType.SignedOut, beforeSignUp, afterSignUp);
            }

            if (afterSignUp.ClassName.Contains("Late"))
            {
                return BuildDiff(DiffType.Late, beforeSignUp, afterSignUp);
            }

            return BuildDiff(DiffType.SignedIn, beforeSignUp, afterSignUp);
        }

        return null;
    }

    private static RegistrationDiff BuildDiff(DiffType type, SignUp before, SignUp after)
    {
        return new RegistrationDiff
        {
            Type = type,
            Username = after.Name,
            OldClass = before.ClassName,
            Class = after.ClassName,
            Role = after.RoleName,
            Spec = after.SpecName ?? string.Empty,
            Number = after.Position
        };
    }
}