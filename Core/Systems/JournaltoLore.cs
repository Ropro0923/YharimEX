using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod.Items.LoreItems;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Content.Items;

namespace YharimEX.Core.Systems
{
    public class JournaltoLore : ModSystem
    {
        public override void AddRecipes()
        {
            foreach (var item in Main.item)
            {
                //this isn't working... ill fix it eventually.
                if (item.ModItem is LoreItem)
                {
                    Recipe.Create(item.type)
                        .AddIngredient(ModContent.ItemType<YharimsJournal>())
                        .Register();
                }
            }
        }
    }
}
