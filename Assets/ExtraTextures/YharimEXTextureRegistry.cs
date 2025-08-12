using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace YharimEX.Assets.ExtraTextures
{
    public static class YharimEXTextureRegistry
    {
        public static Asset<Texture2D> WavyNoise => ModContent.Request<Texture2D>("YharimEX/Assets/ExtraTextures/Noise/WavyNoise");
    }
}
