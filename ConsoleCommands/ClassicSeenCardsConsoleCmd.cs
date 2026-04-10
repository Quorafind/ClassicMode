using System;
using System.Linq;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace ClassicModeMod;

/// <summary>
/// Marks all card-library entries as seen without affecting unlock state.
/// Unlocks are still governed by epoch/character progression.
/// </summary>
public sealed class ClassicSeenCardsConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "classic_seen_cards";

    public override string Args => "[all]";

    public override string Description => "Marks all card encyclopedia entries as seen (does not unlock cards).";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length > 1 || (args.Length == 1 && !args[0].Equals("all", StringComparison.OrdinalIgnoreCase)))
        {
            return new CmdResult(success: false, "Usage: classic_seen_cards [all]");
        }

        // Keep card-db snapshots in sync with current Classic/Hybrid toggles.
        HybridPoolCache.InvalidateAll();

        int added = 0;
        int totalCandidates = 0;
        foreach (CardModel card in ModelDb.AllCards)
        {
            if (!card.ShouldShowInCardLibrary)
                continue;

            totalCandidates++;
            if (SaveManager.Instance.Progress.MarkCardAsSeen(card.Id))
                added++;
        }

        SaveManager.Instance.SaveProgressFile();
        return new CmdResult(success: true, $"Marked card encyclopedia seen: +{added} (pool total {totalCandidates}). Unlock progression unchanged.");
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            return CompleteArgument(["all"], Array.Empty<string>(), args.FirstOrDefault() ?? string.Empty);
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}
