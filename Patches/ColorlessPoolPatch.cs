using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using HarmonyLib;

namespace ClassicModeMod;

[HarmonyPatch(typeof(ColorlessCardPool), "GenerateAllCards")]
internal static class ColorlessPoolPatch
{
    static void Postfix(ref CardModel[] __result)
    {
        var classicOnly = ClassicConfig.ClassicColorless;
        var hybrid = ClassicConfig.ClassicColorlessHybrid;

        if (!classicOnly && !hybrid)
            return;

        var classicCards = GetClassicColorlessCards().ToList();

        // Mutually-exclusive by design; if both are ever true, mixed wins as safety net.
        if (classicOnly && !hybrid)
        {
            // Keep vanilla Ultimate cards registered for event compatibility.
            __result = classicCards
                .Concat(GetVanillaEventCompatibilityCards())
                .ToArray();
            return;
        }

        var baseCards = (__result ?? []).ToList();
        var merged = new List<CardModel>(baseCards);

        var seenIds = new HashSet<ModelId>(baseCards.Select(c => c.Id));
        var seenNames = new HashSet<string>(baseCards.Select(c => c.Title), StringComparer.OrdinalIgnoreCase);
        var dedupe = ClassicConfig.ClassicColorlessDedupe;

        foreach (var card in classicCards)
        {
            if (!seenIds.Add(card.Id))
                continue;

            if (dedupe && !seenNames.Add(card.Title))
                continue;

            if (!dedupe)
                seenNames.Add(card.Title);

            merged.Add(card);
        }

        __result = merged.ToArray();
    }

    private static IEnumerable<CardModel> GetClassicColorlessCards()
    {
        // STS1 colorless set (desktop-1) implemented as _C copies.
        yield return ModelDb.Card<Panacea_C>();
        yield return ModelDb.Card<SwiftStrike_C>();
        yield return ModelDb.Card<GoodInstincts_C>();
        yield return ModelDb.Card<Purity_C>();
        yield return ModelDb.Card<BandageUp_C>();
        yield return ModelDb.Card<Discovery_C>();
        yield return ModelDb.Card<Finesse_C>();
        yield return ModelDb.Card<PanicButton_C>();
        yield return ModelDb.Card<Enlightenment_C>();
        yield return ModelDb.Card<MindBlast_C>();
        yield return ModelDb.Card<Impatience_C>();
        yield return ModelDb.Card<DeepBreath_C>();
        yield return ModelDb.Card<Madness_C>();
        yield return ModelDb.Card<Trip_C>();
        yield return ModelDb.Card<Blind_C>();
        yield return ModelDb.Card<JackOfAllTrades_C>();
        yield return ModelDb.Card<FlashOfSteel_C>();
        yield return ModelDb.Card<DramaticEntrance_C>();
        yield return ModelDb.Card<Forethought_C>();
        yield return ModelDb.Card<DarkShackles_C>();
        yield return ModelDb.Card<Mayhem_C>();
        yield return ModelDb.Card<MasterOfStrategy_C>();
        yield return ModelDb.Card<Violence_C>();
        yield return ModelDb.Card<SadisticNature_C>();
        yield return ModelDb.Card<ThinkingAhead_C>();
        yield return ModelDb.Card<TheBomb_C>();
        yield return ModelDb.Card<Magnetism_C>();
        yield return ModelDb.Card<Apotheosis_C>();
        yield return ModelDb.Card<Panache_C>();
        yield return ModelDb.Card<SecretTechnique_C>();
        yield return ModelDb.Card<SecretWeapon_C>();
        yield return ModelDb.Card<Chrysalis_C>();
        yield return ModelDb.Card<Metamorphosis_C>();
        yield return ModelDb.Card<HandOfGreed_C>();
        yield return ModelDb.Card<Transmutation_C>();
    }

    private static IEnumerable<CardModel> GetVanillaEventCompatibilityCards()
    {
        yield return ModelDb.Card<UltimateStrike>();
        yield return ModelDb.Card<UltimateDefend>();
    }
}
