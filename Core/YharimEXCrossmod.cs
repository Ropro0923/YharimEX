using System;
using Terraria.ModLoader;

namespace YharimEX.Core
{
    public static class YharimEXCrossmod
    {
        public static class FargowiltasSouls
        {
            public const string Name = "FargowiltasSouls";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
        public static class Fargowiltas
        {
            public const string Name = "FargowiltasSouls";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
        public static class Calamity
        {
            public const string Name = "Calamity";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
    }
}