using Terraria.ModLoader;

namespace YharimEX.Core.Systems
{
    public static class YharimEXCrossmodSystem
    {
        public static class FargowiltasSouls
        {
            public const string Name = "FargowiltasSouls";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
        public static class Fargowiltas
        {
            public const string Name = "Fargowiltas";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
        public static class InfernumMode
        {
            public const string Name = "InfernumMode";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
        public static class FargowiltasCrossmod
        {
            public const string Name = "FargowiltasCrossmod ";
            public static bool Loaded => ModLoader.HasMod(Name);
            public static Mod Mod => ModLoader.GetMod(Name);
        }
    }
}