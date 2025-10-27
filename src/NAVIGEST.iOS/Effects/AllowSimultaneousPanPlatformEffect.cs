#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;
using UIKit;

[assembly: ResolutionGroupName("NAVIGEST")]
[assembly: ExportEffect(typeof(NAVIGEST.iOS.Effects.AllowSimultaneousPanPlatformEffect), NAVIGEST.iOS.Effects.AllowSimultaneousPanEffect.EffectName)]

namespace NAVIGEST.iOS.Effects
{
    /// <summary>
    /// iOS platform effect that keeps our pan gesture running alongside the CollectionView scroll.
    /// </summary>
    public sealed class AllowSimultaneousPanPlatformEffect : PlatformEffect
    {
    private readonly List<UIPanGestureRecognizer> _trackedRecognizers = new();
    private UIView? _nativeRoot;

        protected override void OnAttached()
        {
            _nativeRoot = Control ?? Container;
            if (_nativeRoot is null)
                return;

            AttachToHierarchy(_nativeRoot);

            // Cells are virtualized; run again shortly to catch late-bound subviews/recognizers.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                if (_nativeRoot is null)
                    return;

                AttachToHierarchy(_nativeRoot);
            });
        }

        protected override void OnDetached()
        {
            foreach (var recognizer in _trackedRecognizers)
            {
                recognizer.ShouldRecognizeSimultaneously -= ShouldRecognizeSimultaneously;
            }

            _trackedRecognizers.Clear();
            _nativeRoot = null;
        }

        private void AttachToHierarchy(UIView view)
        {
            AttachRecognizers(view);

            if (view.Subviews is null || view.Subviews.Length == 0)
                return;

            foreach (var subview in view.Subviews)
            {
                AttachToHierarchy(subview);
            }
        }

        private void AttachRecognizers(UIView view)
        {
            if (view.GestureRecognizers is null)
                return;

            foreach (var recognizer in view.GestureRecognizers.OfType<UIPanGestureRecognizer>())
            {
                recognizer.CancelsTouchesInView = false;
                AttachRecognizer(recognizer);
            }
        }

        private void AttachRecognizer(UIPanGestureRecognizer recognizer)
        {
            if (_trackedRecognizers.Contains(recognizer))
                return;

            recognizer.ShouldRecognizeSimultaneously += ShouldRecognizeSimultaneously;
            _trackedRecognizers.Add(recognizer);
        }

        private bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer) => true;
    }
}
#endif
