using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System.Threading.Tasks;

public class LevelUpCommand : ICommand
{
    public string Name => "levelup";

    // We need access to the Factory. 
    // Since ICommand doesn't inject it, we will fetch it from the Ruleset/World or 
    // pass it via a Service/Singleton. 
    // FOR NOW: We will rely on the fact that D20Ruleset initialized it.
    // Ideally, we'd inject this properly, but for this architecture:
    private readonly EntityFactory _factory;

    public LevelUpCommand(EntityFactory factory)
    {
        _factory = factory;
    }

    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;
        var player = session.PlayerEntity.Value;

        if (args.Length == 0)
        {
            await session.WriteLineAsync("Usage: levelup <class_name> (e.g., 'levelup fighter')");
            return;
        }

        string className = args[0].ToLower();

        if (!world.Has<ExperienceComponent>(player))
        {
            await session.WriteLineAsync("You cannot level up.");
            return;
        }

        var xp = world.Get<ExperienceComponent>(player);

        if (xp.CurrentXP < xp.NextLevelXP)
        {
            await session.WriteLineAsync($"Not enough XP! Need {xp.NextLevelXP}, have {xp.CurrentXP}.");
            return;
        }

        // Apply the Template
        // Note: We assume the template file is named exactly like the class (e.g., "fighter")
        // and registered in the factory.

        await session.WriteLineAsync($"You channel your power to become a stronger {className}...");

        // Use the Factory to apply the stats/hp/bab
        _factory.ApplyTemplate(player, className);

        // Update Level
        xp.Level++;
        world.Set(player, xp);

        // Heal on Level Up? (Optional D20 rule)
        if (world.Has<VitalsComponent>(player))
        {
            var vitals = world.Get<VitalsComponent>(player);
            vitals.CurrentHP = vitals.MaxHP; // Free heal!
            world.Set(player, vitals);
        }

        await session.WriteLineAsync($"*** LEVEL UP! You are now level {xp.Level}! ***");
    }
}