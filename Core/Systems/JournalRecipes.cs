using CalamityMod.Items.LoreItems;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Content.Items;

namespace YharimEX.Core.Systems
{
    public class JournalRecipes : ModSystem
    {
        public override void PostSetupRecipes()
        {
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe != null && recipe.createItem.type == ModContent.ItemType<LoreItem>())
                {
                    recipe.AddIngredient(ModContent.ItemType<YharimsJournal>());
                }
            }
        }
    }
}