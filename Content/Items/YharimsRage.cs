using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Core.Globals;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Items
{
    public class YharimsRage : ModItem
    {
        public override string Texture => "YharimEX/Assets/Items/YharimsRage";
        public override void SetStaticDefaults()
        {
            //Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(3, 11));
            //ItemID.Sets.AnimatesAsSoul[Item.type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = false;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
        }
        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 52;
            Item.rare = ItemRarityID.Purple;
            Item.maxStack = 20;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                Item.consumable = true;
            }
            else
            {
                Item.consumable = false;
            }
            Item.value = Item.buyPrice(1);
        }

        public override bool CanUseItem(Player player) 
        {
            if (!YharimEXCrossmodSystem.FargowiltasSouls.Loaded && NPC.AnyNPCs(ModContent.NPCType<YharimEXBoss>()))
                return false;
            return player.Center.Y / 16 < Main.worldSurface;
        }

        public override bool? UseItem(Player player)
        {
            YharimEXGlobalUtilities.SpawnBossNetcoded(player, ModContent.NPCType<YharimEXBoss>());
            return true;
        }

        //public override Color? GetAlpha(Color lightColor) => Color.White;
    }
}