using Microsoft.Maui.Controls;

namespace NAVIGEST.iOS.Effects
{
    public sealed class AllowSimultaneousPanEffect : RoutingEffect
    {
        public const string EffectName = nameof(AllowSimultaneousPanEffect);

        public AllowSimultaneousPanEffect()
            : base($"NAVIGEST.{EffectName}")
        {
        }
    }
}
