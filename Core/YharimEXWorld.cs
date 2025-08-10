using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace YharimEX.Core.Systems
{
    public class YharimEXWorld : ModSystem
    {
        internal static bool yharimEXEnraged;
        public static bool YharimEXEnraged { get => yharimEXEnraged; set => yharimEXEnraged = value; }
        internal static bool YharimEXDowned;
        private static void ResetFlags()
        {
            YharimEXEnraged = false;
        }
        public override void SaveWorldData(TagCompound tag)
        {
            List<string> downed = new List<string>();
            if (YharimEXEnraged)
                downed.Add("YharimEXEnraged");
        }
        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> downed = tag.GetList<string>("downed");
            YharimEXEnraged = downed.Contains("YharimEXEnraged");
        }
        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            YharimEXEnraged = flags[6];
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(new BitsByte {[6] = YharimEXEnraged});
        }
    }
}