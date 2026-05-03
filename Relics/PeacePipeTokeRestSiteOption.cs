using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ClassicModeMod;

public sealed class PeacePipeTokeRestSiteOption : RestSiteOption
{
    public override string OptionId => "TOKE";

    public override LocString Description =>
        base.IsEnabled
            ? new LocString("rest_site_ui", "OPTION_TOKE.description")
            : new LocString("rest_site_ui", "OPTION_TOKE.descriptionDisabled");

    public PeacePipeTokeRestSiteOption(Player owner) : base(owner)
    {
        base.IsEnabled = GetRemovableCardCount(owner) >= 1;
    }

    public override async Task<bool> OnSelect()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        CardModel? selected = (await CardSelectCmd.FromDeckForRemoval(base.Owner, prefs)).FirstOrDefault();
        if (selected == null)
            return false;

        await CardPileCmd.RemoveFromDeck(selected);
        return true;
    }

    public override Task DoLocalPostSelectVfx(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public override Task DoRemotePostSelectVfx()
    {
        var character = NRestSiteRoom.Instance?.Characters.FirstOrDefault(c => c.Player == base.Owner);
        character?.Shake();

        var flash = NRelicFlashVfx.Create(ModelDb.Relic<PeacePipeRelic>());
        if (flash == null)
            return Task.CompletedTask;

        character?.AddChildSafely(flash);
        flash.Position = Vector2.Zero;
        return Task.CompletedTask;
    }

    public static Texture2D GetCookOptionIcon()
    {
        return PreloadManager.Cache.GetTexture2D(ImageHelper.GetImagePath("ui/rest_site/option_cook.png"));
    }

    private static int GetRemovableCardCount(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Count(c => c.IsRemovable);
    }
}

