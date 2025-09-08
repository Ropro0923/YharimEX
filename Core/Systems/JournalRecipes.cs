using CalamityMod.Items.LoreItems;
using Terraria.ID;
using YharimEX.Content.Items;

namespace YharimEX.Core.Systems
{
    public class JournalRecipes : ModSystem
    {
        public override void PostAddRecipes()
        {
            Recipe[] originalRecipes = new Recipe[Recipe.numRecipes];
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                originalRecipes[i] = Main.recipe[i];
            }

            foreach (Recipe recipe in originalRecipes)
            {
                if (recipe?.createItem?.ModItem is LoreItem)
                {
                    Recipe JournalRecipe = Recipe.Create(recipe.createItem.type);
                    JournalRecipe.AddIngredient(ModContent.ItemType<YharimsJournal>());
                    foreach (int tile in recipe.requiredTile)
                    {
                        if (tile > 0)
                            JournalRecipe.AddTile(tile);
                    }
                    JournalRecipe.Register();
                }
            }
        }
    }
}