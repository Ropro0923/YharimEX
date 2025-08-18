
namespace YharimEX.Content.Projectiles.MutantAttacks
{
    public class YharimEXSpearThrownFriendly : PenetratorThrown
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXSpear";

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Default;
        }
    }
}