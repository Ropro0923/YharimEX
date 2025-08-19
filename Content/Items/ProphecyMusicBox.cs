using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Content.Tilles;
using CalamityMod.Items.Placeables.Furniture.BossRelics;

namespace YharimEX.Content.Items
{
    public class ProphecyMusicBox : ModItem
    {
        public override string Texture => "YharimEX/Assets/Items/ProphecyMusicBox";
        public override void SetStaticDefaults()
        {
            if (Main.dedServ)
                return;
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.CanGetPrefixes[Type] = false;
            ItemID.Sets.ShimmerTransformToItem[Type] = 576;
            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Music/TheRealityoftheProphecy"), ModContent.ItemType<ProphecyMusicBox>(), ModContent.TileType<ProphecyMusicBoxTile>(), 0);
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<ProphecyMusicBoxTile>();
            Item.width = 32;
            Item.height = 48;
            Item.rare = ItemRarityID.Master;
            Item.value = 100000;
            Item.accessory = true;
        }

        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("FargowiltasSouls", out Mod Souls))
            {
                ModItem mutantRelic = Souls.Find<ModItem>("MutantRelic");
                if (mutantRelic != null)
                {
                    Recipe Fargos = CreateRecipe();
                    Fargos.AddIngredient(mutantRelic);
                    Fargos.AddIngredient(ItemID.MusicBox);
                    Fargos.AddTile(TileID.HeavyWorkBench);
                    Fargos.Register();
                }
            }
            else
            {
                Recipe Base = CreateRecipe();
                Base.AddIngredient<DraedonRelic>();
                Base.AddIngredient<CalamitasRelic>();
                Base.AddIngredient(ItemID.Zenith);
                Base.AddIngredient(ItemID.MusicBox);
                Base.AddTile(TileID.HeavyWorkBench);
                Base.Register();
            }
        }
    }
}