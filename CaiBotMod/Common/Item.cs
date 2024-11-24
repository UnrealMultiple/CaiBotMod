using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CaiBotMod.Common;

internal class 法律鱼 : ModItem
{
    public override void SetStaticDefaults()
    {
        this.Item.ResearchUnlockCount = 666;
        ItemID.Sets.CanBePlacedOnWeaponRacks[this.Type] = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        TooltipLine tooltip = new (this.Mod, "CaiBot扩展", "卧槽爆金币了，感觉这鱼至少能爆50金币!") { OverrideColor = Color.Red };
        tooltips.Add(tooltip);
    }

    public override void SetDefaults()
    {
        this.Item.DefaultToQuestFish();
        this.Item.width = 16;
        this.Item.height = 16;
        this.Item.rare = ItemRarityID.Master;
        this.Item.value = Item.buyPrice(5, 50);
    }

    public override bool IsQuestFish()
    {
        return true;
    }

    public override bool IsAnglerQuestAvailable()
    {
        return true;
    }

    public override void AnglerQuestChat(ref string description, ref string catchLocation)
    {
        description = "我靠我突然感觉到了爆金币的力量，难道这就是传说中的法律鱼?!\n" +
                      "*其实这是Cai群里一个管理,CaiBot其实一开始是Cai自己用的,后面就莫名其妙公开了,这段奇妙的物品代码也是,算是个彩蛋吧233...";
        catchLocation = "法律是无处不在的...";
    }
}