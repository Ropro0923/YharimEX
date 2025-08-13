using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YharimEX.Content.Items
{
    public class YharimsJournal : ModItem
    {
        public override string Texture => "YharimEX/Assets/Items/Journal";

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.value = 165;
            Item.rare = ItemRarityID.White;
            Item.maxStack = 1;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
        }
    }
}
