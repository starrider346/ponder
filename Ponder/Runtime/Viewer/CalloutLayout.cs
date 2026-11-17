using System.Collections.Generic;
using System.Linq;
using Ponder.Core;
using UnityEngine;

namespace Ponder.Runtime
{
    internal sealed class CalloutLayout
    {
        private readonly ViewerView _view;
        private readonly List<CalloutView> _callouts = new();
        private RectTransform Viewport => _view.Viewport;
        private RectTransform Annotations => _view.Annotations;

        internal CalloutLayout(ViewerView view) => _view = view;
        internal void Hide()
        {
            foreach (var callout in _callouts)
            {
                callout.Hide();
            }
        }

        internal void Clear() => _callouts.Clear();
        internal void Present(PreviewRenderer renderer, PonderScene scene, float seconds)
        {
            while (_callouts.Count < scene.Callouts.Count)
            {
                _callouts.Add(new CalloutView(_view));
            }

            var visible = new List<(CalloutView View, string Text, Vector2 Target)>();
            for (var i = 0; i < _callouts.Count; i++)
            {
                if (i >= scene.Callouts.Count)
                {
                    _callouts[i].Hide();
                    continue;
                }

                var callout = scene.Callouts[i];
                if (seconds < callout.Start || seconds >= callout.End || callout.Target == null || !WithinScene(callout.Target, scene.Root.transform))
                {
                    _callouts[i].Hide();
                    continue;
                }

                var point = renderer.Camera.WorldToViewportPoint(callout.Target.TransformPoint(callout.Point));
                if (point.z <= 0 || point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1)
                {
                    _callouts[i].Hide();
                    continue;
                }

                var world = Viewport.TransformPoint(new Vector3((point.x - Viewport.pivot.x) * Viewport.rect.width, (point.y - Viewport.pivot.y) * Viewport.rect.height, 0));
                var local = (Vector2)Annotations.InverseTransformPoint(world) + Vector2.Scale(Annotations.rect.size, Annotations.pivot);
                visible.Add((_callouts[i], callout.Text, local));
            }

            var size = Annotations.rect.size;
            var maximumHeight = (size.y - 16 - 12 * (visible.Count - 1)) / Mathf.Max(1, visible.Count);
            var heights = visible.Select(item => item.View.Measure(item.Text, size, maximumHeight)).ToArray();
            var centers = CalloutPacking.Centers(visible.Select(item => item.Target.y + 35).ToArray(), heights, size.y);
            for (var i = 0; i < visible.Count; i++)
            {
                visible[i].View.CenterY = centers[i];
                visible[i].View.Show(visible[i].Text, visible[i].Target, size);
            }
        }

        private static bool WithinScene(Transform transform, Transform root)
        {
            for (var t = transform; t != root; t = t.parent)
            {
                if (t == null || !t.gameObject.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
