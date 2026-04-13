using Discord.WebSocket;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using RaidNotificator.DTOs;

namespace RaidNotificator;

public class DifferenceGenerator
{
    public static async Task<RegistrationDiff?> GetRegistrationDiffAsync(RaidEvent before, RaidEvent after)
    {
        if (before.Equals(after))
            return null;

        var idsBefore = before.SignUps.Select(x => x.UserId).ToList(); 
        var idsAfter =  after.SignUps.Select(x => x.UserId).ToList();
        
        // new sign up
        if (idsBefore.Count != idsAfter.Count)
        {
            var diffId = idsAfter.FirstOrDefault(x => !idsBefore.Contains(x));
            
            if (string.IsNullOrEmpty(diffId))
                return null;
            
            var signUp = after.SignUps.First(s => idsAfter.Contains(diffId));
            return new RegistrationDiff
            {
                Type = DiffType.SignedIn,
                Username = signUp.Name,
                OldClass = string.Empty,
                Class = signUp.ClassName,
                Role = signUp.RoleName,
                Number = signUp.Position
            };
        }
        
        // role change
        foreach (var aSignUp in after.SignUps)
        {
            foreach (var bSignUp in before.SignUps)
            {
                if (aSignUp.UserId.Equals(bSignUp.UserId) && !aSignUp.ClassName.Equals(bSignUp.ClassName))
                {
                    if (aSignUp.ClassName.Contains("Bench") || aSignUp.ClassName.Contains("Tentative") ||
                        aSignUp.ClassName.Contains("Absence"))
                    {
                        return new RegistrationDiff
                        {
                            Type = DiffType.SignedOut,
                            Username = aSignUp.Name,
                            OldClass = bSignUp.ClassName,
                            Class = aSignUp.ClassName,
                            Role = aSignUp.RoleName,
                            Number = aSignUp.Position
                        };
                    }

                    if (aSignUp.ClassName.Contains("Late"))
                    {
                        return new RegistrationDiff
                        {
                            Type = DiffType.Late,
                            Username = aSignUp.Name,
                            OldClass = bSignUp.ClassName,
                            Class = aSignUp.ClassName,
                            Role = aSignUp.RoleName,
                            Number = aSignUp.Position
                        };
                    }

                    return new RegistrationDiff
                    {
                        Type = DiffType.SignedIn,
                        Username = aSignUp.Name,
                        OldClass = bSignUp.ClassName,
                        Class = aSignUp.ClassName,
                        Role = aSignUp.RoleName,
                        Number = aSignUp.Position
                    };
                }
            }
        }

        return null;
    }
}