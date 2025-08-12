using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Content.Items.BossBags;
using Terraria.Localization;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace YharimEX.Core.Players
{
    public class YharimEXPlayer : ModPlayer
    {
        public int MaxLifeReduction;
        int LifeReductionUpdateTimer;
        public int CurrentLifeReduction;
        public override void UpdateDead()
        {
            MaxLifeReduction = 0;
            CurrentLifeReduction = 0;
        }

        public void ManageLifeReduction()
        {
            if (LifeReductionUpdateTimer > 0)
            {
                const int threshold = 30;
                if (LifeReductionUpdateTimer++ > threshold)
                {
                    LifeReductionUpdateTimer = 1;

                    CurrentLifeReduction -= 5;
                    if (MaxLifeReduction > CurrentLifeReduction)
                        MaxLifeReduction = CurrentLifeReduction;
                    CombatText.NewText(Player.Hitbox, Color.DarkGreen, Language.GetTextValue($"Mods.{Mod.Name}.LifeUp"));
                }
            }

            if (CurrentLifeReduction > 0)
            {
                if (CurrentLifeReduction > Player.statLifeMax2 - 100)
                    CurrentLifeReduction = Player.statLifeMax2 - 100;
                Player.statLifeMax2 -= CurrentLifeReduction;
            }
        }
    }
}
