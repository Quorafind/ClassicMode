using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

public sealed class BottledEnchantment : EnchantmentModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Innate)];

    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Innate);
    }
}
