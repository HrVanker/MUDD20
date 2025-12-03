using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Threading.Tasks;

public class WakeCommand : ICommand
{
    public string Name => "wake";

    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;
        var player = session.PlayerEntity.Value;

        // 1. If not unconscious, this command does nothing (or wakes from normal sleep later)
        if (!world.Has<UnconsciousComponent>(player))
        {
            await session.WriteLineAsync("You are already awake.");
            return;
        }

        // 2. Parse Arguments (wake pay / wake wait)
        string subCommand = args.Length > 0 ? args[0].ToLower() : "";

        if (subCommand == "pay")
        {
            await ProcessPayment(session, world, player);
        }
        else if (subCommand == "wait")
        {
            await ProcessWait(session, world, player);
        }
        else
        {
            // 3. No arguments? Show the "Menu" / Flavor Text
            await ShowOptions(session, world, player);
        }
    }

    private async Task ShowOptions(TelnetSession session, World world, Entity player)
    {
        var deity = world.Has<DeityComponent>(player) ? world.Get<DeityComponent>(player).DeityName : "None";
        string message = "";

        switch (deity)
        {
            case "Crom":
                message = "The spirit of Crom laughs at your weakness. 'Valhalla is not yet for you,' he booms. \r" +
                          "A spectral warrior extends a hand. 'Pay the toll of gold, or rot here until your strength returns.'";
                break;
            case "The Light":
                message = "A warm glow surrounds you. A soft voice whispers, 'Child, your work is not done.' \r" +
                          "You feel you could give an offering to return now, or rest in the light to recover.";
                break;
            default:
                message = "You float in a dark void. A hooded figure approaches silently. \r" +
                          "It extends a skeletal hand for a coin. It seems you can pay to leave, or wait for the darkness to pass.";
                break;
        }

        await session.WriteLineAsync("\n" + message);
        await session.WriteLineAsync("--------------------------------");
        await session.WriteLineAsync("Options:");
        await session.WriteLineAsync("  wake pay   (Cost: 10 Gold - Instant)");
        await session.WriteLineAsync("  wake wait  (Cost: Time    - 30 Seconds)");
        await session.WriteLineAsync("--------------------------------");
    }

    private async Task ProcessPayment(TelnetSession session, World world, Entity player)
    {
        int cost = 10;

        // FIX: Retrieve by value (copy), not by ref, because we are in an async method.
        if (!world.Has<MoneyComponent>(player))
        {
            await session.WriteLineAsync("You have no money to pay the toll.");
            return;
        }

        var money = world.Get<MoneyComponent>(player);

        if (money.Amount >= cost)
        {
            money.Amount -= cost;

            // FIX: Update the component in the World explicitly
            world.Set(player, money);

            await session.WriteLineAsync($"You offer {cost} gold to the void...");

            // Perform Instant Revive
            PerformRevive(world, player);
            await session.WriteLineAsync("You gasp for air, your body restored!");
        }
        else
        {
            await session.WriteLineAsync($"You search your pockets, but you only have {money.Amount} gold. You cannot pay the toll.");
        }
    }

    private async Task ProcessWait(TelnetSession session, World world, Entity player)
    {
        if (world.Has<ReviveTimerComponent>(player))
        {
            await session.WriteLineAsync("You are already resting. Patience.");
            return;
        }

        world.Add(player, new ReviveTimerComponent { TimeRemaining = 30.0f });
        await session.WriteLineAsync("You choose to rest. The world fades away as you recover your strength...");
    }

    private void PerformRevive(World world, Entity entity)
    {
        // 1. Remove Status
        world.Remove<UnconsciousComponent>(entity);
        if (world.Has<ReviveTimerComponent>(entity)) world.Remove<ReviveTimerComponent>(entity);

        // 2. Heal to Max (Benefit of paying!)
        if (world.Has<VitalsComponent>(entity))
        {
            ref var vitals = ref world.Get<VitalsComponent>(entity);
            vitals.CurrentHP = vitals.MaxHP;
        }

        // 3. Teleport to Anchor
        if (world.Has<RespawnAnchorComponent>(entity))
        {
            var anchor = world.Get<RespawnAnchorComponent>(entity);
            if (world.Has<LocationComponent>(entity))
            {
                ref var loc = ref world.Get<LocationComponent>(entity);
                loc.RoomId = anchor.RoomId;
                loc.X = anchor.X;
                loc.Y = anchor.Y;
            }
        }
    }
}