﻿using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace YharimEX.Core.Systems
{
    public class YharimEXWorldFlags : ModSystem
    {
        internal static bool infernumMode;
        internal static bool downedYharimEX;
        internal static bool angryYharimEX = false;
        internal static int skipYharimEXP1;
        public static int SkipYharimEXP1 { get => skipYharimEXP1; set => skipYharimEXP1 = value; }
        public static bool DownedYharimEX { get => downedYharimEX; set => downedYharimEX = value; }
        public static bool AngryYharimEX { get => angryYharimEX; set => angryYharimEX = value; }

        public static bool RevengenceMode
        {
            get
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    return calamity.Call("GetDifficultyActive", "revengeance") is bool b && b;
                }
                return false;
            }
        }

        public static bool DeathMode
        {
            get
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    return calamity.Call("GetDifficultyActive", "death") is bool b && b;
                }
                return false;
            }
        }
        public static bool InfernumMode
        {
            get
            {
                if (YharimEXCrossmodSystem.InfernumMode.Loaded)
                {
                    return YharimEXCrossmodSystem.InfernumMode.Mod.Call("GetInfernumActive") is bool b && b;
                }
                return false;
            }
        }

        public static bool EternityMode
        {
            get
            {
                if (!YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    return false;
                }

                return YharimEXCrossmodSystem.FargowiltasSouls.Mod.Call("EternityMode") is bool active && active;
            }
        }

        public static bool MasochistModeReal
        {
            get
            {
                if (!YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    return false;
                }

                return YharimEXCrossmodSystem.FargowiltasSouls.Mod.Call("MasochistMode") is bool active && active;
            }
        }


        private static void ResetFlags()
        {
            DownedYharimEX = false;
            AngryYharimEX = false;
            SkipYharimEXP1 = 0;
        }

        public override void OnWorldLoad() => ResetFlags();

        public override void OnWorldUnload() => ResetFlags();

        public override void SaveWorldData(TagCompound tag)
        {
            List<string> downed = [];
            if (DownedYharimEX)
                downed.Add("downedYharimEX");
            if (AngryYharimEX)
                downed.Add("AngryYharimEX");
            tag.Add("downed", downed);
            tag.Add("YharimEXP1", SkipYharimEXP1);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> downed = tag.GetList<string>("downed");
            DownedYharimEX = downed.Contains("downedYharimEX");
            AngryYharimEX = downed.Contains("AngryYharimEX");
            if (tag.ContainsKey("YharimEXP1"))
                SkipYharimEXP1 = tag.GetAsInt("YharimEXP1");
        }

        public override void NetReceive(BinaryReader reader)
        {
            SkipYharimEXP1 = reader.ReadInt32();

            BitsByte flags = reader.ReadByte();
            DownedYharimEX = flags[5];
            AngryYharimEX = flags[6];            
            flags = reader.ReadByte();
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(SkipYharimEXP1);

            writer.Write(new BitsByte
            {
                [1] = DownedYharimEX,
                [2] = AngryYharimEX,
            });
        }
    }
}
