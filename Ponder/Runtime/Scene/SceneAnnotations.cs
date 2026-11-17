using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ponder.Runtime
{
    internal sealed class SceneAnnotations
    {
        private readonly PonderScene _scene;
        private GameObject Root => _scene.Root;

        internal readonly List<SceneCallout> Callouts = new();
        internal SceneAnnotations(PonderScene scene) => _scene = scene;
        internal void Highlight(Transform target, bool socket, float start, float end, Color color)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var go = new GameObject("Ponder highlight");
            go.transform.SetParent(Root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 16;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("No preview highlight shader is available.");
            }

            var material = _scene.Own(new Material(shader));
            material.color = color;
            line.sharedMaterial = material;
            line.startColor = line.endColor = color;
            line.enabled = false;
            var points = new Vector3[16];
            var order = new[]
            {
                0,
                1,
                2,
                3,
                0,
                4,
                5,
                1,
                5,
                6,
                2,
                6,
                7,
                3,
                7,
                4
            };
            _scene.AddTrack(t =>
            {
                var visible = true;
                for (var parent = target; parent != Root.transform; parent = parent.parent)
                {
                    if (parent == null || !parent.gameObject.activeSelf)
                    {
                        visible = false;
                        break;
                    }
                }

                line.enabled = t >= start && t < end && visible;
                if (!line.enabled)
                {
                    return;
                }

                var bounds = socket ? new Bounds(target.position, Vector3.one * .09f) : PreviewBounds.ForRenderers(renderers, target.position);
                if (!socket && renderers.Length == 0 && target is RectTransform rect)
                {
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    bounds = new Bounds(corners[0], Vector3.zero);
                    foreach (var corner in corners)
                    {
                        bounds.Encapsulate(corner);
                    }
                }

                bounds.Expand(Mathf.Max(.008f, bounds.size.magnitude * .025f));
                line.startWidth = line.endWidth = Mathf.Max(.002f, bounds.size.magnitude * .006f);
                var lo = bounds.min;
                var hi = bounds.max;
                for (var i = 0; i < points.Length; i++)
                {
                    var n = order[i];
                    points[i] = new Vector3((n == 1 || n == 2 || n == 5 || n == 6) ? hi.x : lo.x, n >= 4 ? hi.y : lo.y, (n == 2 || n == 3 || n == 6 || n == 7) ? hi.z : lo.z);
                }

                line.SetPositions(points);
            });
        }
    }
}
