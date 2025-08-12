using Terraria.Audio;

namespace YharimEX.Assets.Sounds
{
    public static class YharimEXSoundRegistry
    {
        public const string SoundsPath = "YharimEX/Assets/Sounds/";
        public static readonly SoundStyle YharimEXUnpredictive = new(SoundsPath + "YharimEXUnpredictive");
        public static readonly SoundStyle YharimEXPredictive = new(SoundsPath + "YharimEXPredictive");
        public static readonly SoundStyle YharimEXSwordThrow = new(SoundsPath + "YharimEXSwordThrow");
        public static readonly SoundStyle YharimEXSwordBlast = new(SoundsPath + "YharimEXSwordBlast");
        public static readonly SoundStyle MutantSword = new(SoundsPath + "YharimEXSword");
    }
}
