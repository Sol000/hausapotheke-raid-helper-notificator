using Discord;
using Discord.WebSocket;
using RaidNotificator.DTOs;

namespace RaidNotificator.Components;

public class InfoMessage
{
    public static async Task<ComponentBuilderV2> UpdateRegistrationInfoAsync(RegistrationDiff diff, RaidEvent e)
    {
        var builder = new ComponentBuilderV2();
        
        var container = new ContainerBuilder();

        switch (diff.Type)
        {
            case DiffType.SignedIn:
                container.AccentColor = Color.Green;
                if (string.IsNullOrEmpty(diff.OldClass))
                {
                    container.WithTextDisplay("### Anmeldung")
                        .WithTextDisplay($"**{diff.Username}** hat sich als `{diff.Role} {diff.Class}` angemeldet.");
                }
                else
                {
                    container.WithTextDisplay("### Ummeldung")
                        .WithTextDisplay($"**{diff.Username}** hat sich von `{(IsMimimiClass(diff.OldClass) ? string.Empty : $"{diff.Role} ")}{diff.OldClass}` zu `{diff.Role} {diff.Class}` umgemeldet.");                    
                }
                break;
            case DiffType.SignedOut:
                container.AccentColor = Color.Red;
                container.WithTextDisplay("### Abmeldung")
                    .WithTextDisplay($"**{diff.Username}** hat sich von `{diff.Role} {diff.OldClass}` zu `{diff.Class}` umgemeldet.");     
                break;
            case DiffType.Late:
                container.AccentColor = Color.Orange;
                container.WithTextDisplay("### Kommt später")
                    .WithTextDisplay($"**{diff.Username}** hat sich von `{(IsMimimiClass(diff.OldClass) ? string.Empty : $"{diff.Role} ")}{diff.OldClass}` zu `{diff.Class}` umgemeldet.");
                break;
        }
        container.WithTextDisplay($"Es sind **{e.SignUps.Count(s => !s.ClassName.Equals("Bench") && !s.ClassName.Equals("Absence"))}** Personen zu `{e.Title}` angemmeldet");
        
        return builder.WithContainer(container);
    }

    public static async Task<ComponentBuilderV2> NewEventMessageAsync(SocketGuildEvent e)
    {
        var builder = new ComponentBuilderV2();

        var container = new ContainerBuilder();
        container.WithAccentColor(Color.Gold)
            .WithTextDisplay("### Neues Event erstellt!!!")
            .WithTextDisplay($"## `{e.Name}`")
            .WithTextDisplay($"Datum: {e.StartTime.Date.ToShortDateString()}");
        
        return builder.WithContainer(container);
    }
    
    private static readonly List<string> mimimiList = new List<string> { "Bench", "Late", "Absence", "Tentative" };
    private static bool IsMimimiClass(string c){
        return mimimiList.Contains(c);
        
    }
}