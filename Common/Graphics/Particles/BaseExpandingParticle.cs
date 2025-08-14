using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;

namespace YharimEX.Common.Graphics.Particles
{
    public abstract class BaseExpandingParticle : Particle
    {
        public readonly Vector2 StartScale;

        public readonly Vector2 EndScale;

        public Color BloomColor;

        public readonly bool UseBloom;

        public override string AtlasTextureName => "YharimEX.Bloom";

        public virtual Vector2 DrawScale => Scale * 0.3f;

        public BaseExpandingParticle(Vector2 position, Vector2 velocity, Color drawColor, Vector2 startScale, Vector2 endScale, int lifetime, bool useExtraBloom = false, Color? extraBloomColor = null)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = drawColor;
            Scale = (StartScale = startScale);
            EndScale = endScale;
            Lifetime = lifetime;
            UseBloom = useExtraBloom;
            Color valueOrDefault = extraBloomColor.GetValueOrDefault();
            if (!extraBloomColor.HasValue)
            {
                valueOrDefault = Color.White;
                extraBloomColor = valueOrDefault;
            }

            BloomColor = extraBloomColor.Value;
        }

        public sealed override void Update()
        {
            Opacity = MathHelper.Lerp(1f, 0f, FargoSoulsUtil.SineInOut(base.LifetimeRatio));
            Scale = Vector2.Lerp(StartScale, EndScale, FargoSoulsUtil.SineInOut(base.LifetimeRatio));
        }

        public sealed override void Draw(SpriteBatch spriteBatch)
        {
            AtlasTexture texture = base.Texture;
            Vector2 position = Position - Main.screenPosition;
            Rectangle? frame = Frame;
            Color drawColor = DrawColor;
            drawColor.A = 0;
            spriteBatch.Draw(texture, position, frame, drawColor * Opacity, Rotation, null, DrawScale, Direction.ToSpriteDirection());
            if (UseBloom)
            {
                AtlasTexture texture2 = base.Texture;
                Vector2 position2 = Position - Main.screenPosition;
                drawColor = BloomColor;
                drawColor.A = 0;
                spriteBatch.Draw(texture2, position2, null, drawColor * 0.4f * Opacity, Rotation, null, DrawScale * 0.66f, Direction.ToSpriteDirection());
            }
        }
    }
}
