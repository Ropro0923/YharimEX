using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace YharimEX.Core.Systems
{
    public class YharimWorldFlags : ModSystem
    {
        public enum Downed
        {
            TimberChampion,
            TerraChampion,
            EarthChampion,
            NatureChampion,
            LifeChampion,
            ShadowChampion,
            SpiritChampion,
            WillChampion,
            CosmosChampion,
            TrojanSquirrel,
            Lifelight,
            CursedCoffin,
            BanishedBaron,
            Magmaw
        }
        internal static bool downedYharimEX;
        internal static bool angryYharimEX;
        internal static int skipYharimEXP1;
        internal static bool downedAnyBoss;
        internal static bool[] downedBoss = new bool[Enum.GetValues(typeof(Downed)).Length];
        public static int SkipYharimEXP1 { get => skipYharimEXP1; set => skipYharimEXP1 = value; }
        public static bool[] DownedBoss { get => downedBoss; set => downedBoss = value; }
        public static bool DownedYharimEX { get => downedYharimEX; set => downedYharimEX = value; }
        public static bool AngryYharimEX { get => angryYharimEX; set => angryYharimEX = value; }        
        public override void Unload() => DownedBoss = null;

        private static void ResetFlags()
        {
            DownedYharimEX = false;
            AngryYharimEX = false;
            SkipYharimEXP1 = 0;
            for (int i = 0; i < DownedBoss.Length; i++)
                DownedBoss[i] = false;
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
            for (int i = 0; i < DownedBoss.Length; i++)
            {
                if (DownedBoss[i])
                    downed.Add("downedBoss" + i.ToString());
            }
            tag.Add("downed", downed);
            tag.Add("YharimEXP1", SkipYharimEXP1);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> downed = tag.GetList<string>("downed");
            DownedYharimEX = downed.Contains("downedYharimEX");
            AngryYharimEX = downed.Contains("AngryYharimEX");
            for (int i = 0; i < DownedBoss.Length; i++)
                DownedBoss[i] = downed.Contains($"downedBoss{i}") || downed.Contains($"downedChampion{i}");
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
            for (int i = 0; i < DownedBoss.Length; i++)
            {
                int bits = i % 8;
                if (bits == 0)
                    flags = reader.ReadByte();

                DownedBoss[i] = flags[bits];
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(SkipYharimEXP1);

            writer.Write(new BitsByte
            {
                [1] = DownedYharimEX,
                [2] = AngryYharimEX,
            });

            BitsByte bitsByte = new();
            for (int i = 0; i < DownedBoss.Length; i++)
            {
                int bit = i % 8;

                if (bit == 0 && i != 0)
                {
                    writer.Write(bitsByte);
                    bitsByte = new BitsByte();
                }

                bitsByte[bit] = DownedBoss[i];
            }
        }
    }
}
