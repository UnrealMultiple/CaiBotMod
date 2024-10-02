using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CaibotExtension.Common
{
    internal class 法律鱼:ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 999;
            ItemID.Sets.CanBePlacedOnWeaponRacks[Type] = true; // All vanilla fish can be placed in a weapon rack.
            
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine tooltip = new TooltipLine(Mod, "CaiBot扩展", $"卧槽爆金币了，感觉这鱼至少能爆50金币!") { OverrideColor = Color.Red };
            tooltips.Add(tooltip);
        }
        public override void SetDefaults()
        {
            // DefaultToQuestFish sets quest fish properties.
            // Of note, it sets rare to ItemRarityID.Quest, which is the special rarity for quest items.
            // It also sets uniqueStack to true, which prevents players from picking up a 2nd copy of the item into their inventory.
            Item.DefaultToQuestFish();
            Item.width = 16;
            Item.height = 16;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(5,50);

        }

        public override bool IsQuestFish() => true; // Makes the item a quest fish

        public override bool IsAnglerQuestAvailable() => true; // Makes the quest only appear in hard mode. Adding a '!' before Main.hardMode makes it ONLY available in pre-hardmode.

        public override void AnglerQuestChat(ref string description, ref string catchLocation)
        {
            // How the angler describes the fish to the player.
            description = "我靠我突然感觉到了爆金币的力量，难道这就是传说中的法律鱼?!";
            // What it says on the bottom of the angler's text box of how to catch the fish.
            catchLocation = "法律是无处不在的...";
        }
    }
}
