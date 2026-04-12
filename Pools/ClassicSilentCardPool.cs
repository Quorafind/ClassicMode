using Godot;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ClassicModeMod;

public sealed class ClassicSilentCardPool : CardPoolModel
{
    public override string Title => "silent";

    public override string EnergyColorName => "silent";

    public override string CardFrameMaterialPath => "card_frame_green";

    public override Color DeckEntryCardColor => new("70CC83");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        var classicCards = new CardModel[]
        {
            // ── Basic ──
            ModelDb.Card<StrikeSilent_C>(),
            ModelDb.Card<DefendSilent_C>(),
            ModelDb.Card<Neutralize_C>(),
            ModelDb.Card<Survivor_C>(),

            // ── Common Attacks ──
            ModelDb.Card<Bane_C>(),
            ModelDb.Card<DaggerSpray_C>(),
            ModelDb.Card<DaggerThrow_C>(),
            ModelDb.Card<FlyingKnee_C>(),
            ModelDb.Card<PoisonedStab_C>(),
            ModelDb.Card<QuickSlash_C>(),
            ModelDb.Card<Slice_C>(),
            ModelDb.Card<SneakyStrike_C>(),
            ModelDb.Card<SuckerPunch_C>(),

            // ── Common Skills ──
            ModelDb.Card<Acrobatics_C>(),
            ModelDb.Card<Backflip_C>(),
            ModelDb.Card<BladeDance_C>(),
            ModelDb.Card<CloakAndDagger_C>(),
            ModelDb.Card<DeadlyPoison_C>(),
            ModelDb.Card<Deflect_C>(),
            ModelDb.Card<DodgeAndRoll_C>(),
            ModelDb.Card<Outmaneuver_C>(),
            ModelDb.Card<Prepared_C>(),

            // ── Uncommon Attacks ──
            ModelDb.Card<AllOutAttack_C>(),
            ModelDb.Card<Backstab_C>(),
            ModelDb.Card<ChokeHold_C>(),
            ModelDb.Card<Dash_C>(),
            ModelDb.Card<EndlessAgony_C>(),
            ModelDb.Card<Eviscerate_C>(),
            ModelDb.Card<Finisher_C>(),
            ModelDb.Card<FanOfKnives_C>(),
            ModelDb.Card<Flechettes_C>(),
            ModelDb.Card<GlassKnife_C>(),
            ModelDb.Card<HeelHook_C>(),
            ModelDb.Card<MasterfulStab_C>(),
            ModelDb.Card<Predator_C>(),
            ModelDb.Card<RiddleWithHoles_C>(),
            ModelDb.Card<Skewer_C>(),

            // ── Uncommon Skills ──
            ModelDb.Card<Blur_C>(),
            ModelDb.Card<BouncingFlask_C>(),
            ModelDb.Card<CalculatedGamble_C>(),
            ModelDb.Card<Catalyst_C>(),
            ModelDb.Card<Concentrate_C>(),
            ModelDb.Card<CorpseExplosion_C>(),
            ModelDb.Card<CripplingPoison_C>(),
            ModelDb.Card<Distraction_C>(),
            ModelDb.Card<EscapePlan_C>(),
            ModelDb.Card<Expertise_C>(),
            ModelDb.Card<LegSweep_C>(),
            ModelDb.Card<PiercingWail_C>(),
            ModelDb.Card<PhantasmalKiller_C>(),
            ModelDb.Card<Reflex_C>(),
            ModelDb.Card<Setup_C>(),
            ModelDb.Card<Tactician_C>(),
            ModelDb.Card<Terror_C>(),

            // ── Uncommon Powers ──
            ModelDb.Card<Accuracy_C>(),
            ModelDb.Card<Caltrops_C>(),
            ModelDb.Card<Envenom_C>(),
            ModelDb.Card<Footwork_C>(),
            ModelDb.Card<InfiniteBlades_C>(),
            ModelDb.Card<NoxiousFumes_C>(),
            ModelDb.Card<WellLaidPlans_C>(),

            // ── Rare Attacks ──
            ModelDb.Card<DieDieDie_C>(),
            ModelDb.Card<GrandFinale_C>(),
            ModelDb.Card<Unload_C>(),

            // ── Rare Skills ──
            ModelDb.Card<Adrenaline_C>(),
            ModelDb.Card<Alchemize_C>(),
            ModelDb.Card<BulletTime_C>(),
            ModelDb.Card<Burst_C>(),
            ModelDb.Card<Doppelganger_C>(),
            ModelDb.Card<Malaise_C>(),
            ModelDb.Card<Nightmare_C>(),
            ModelDb.Card<StormOfSteel_C>(),

            // ── Rare Powers ──
            ModelDb.Card<AThousandCuts_C>(),
            ModelDb.Card<Afterimage_C>(),
            ModelDb.Card<ToolsOfTheTrade_C>(),
            ModelDb.Card<WraithForm_C>(),
        };

        return classicCards
        .Concat(ModelDb.CardPool<SilentCardPool>().AllCards.Where(c => c.Rarity == CardRarity.Ancient))
        .GroupBy(c => c.Id)
        .Select(g => g.First())
        .ToArray();
    }
}
