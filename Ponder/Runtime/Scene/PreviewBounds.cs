using System.Collections.Generic;
using UnityEngine;

namespace Ponder.Runtime
{
    internal static class PreviewBounds
    {
        internal static Bounds ForRenderers(IEnumerable<Renderer> renderers, Vector3 fallback)
        {
            var bounds = new Bounds(fallback, Vector3.one * .15f);
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer is LineRenderer)
                {
                    continue;
                }

                var filter = renderer.GetComponent<MeshFilter>();
                Bounds actual;
                if ((filter != null && filter.sharedMesh != null) || renderer is SkinnedMeshRenderer)
                {
                    var local = renderer is SkinnedMeshRenderer skinned ? skinned.localBounds : filter.sharedMesh.bounds;
                    var matrix = renderer.localToWorldMatrix;
                    var x = matrix.MultiplyVector(new Vector3(local.extents.x, 0, 0));
                    var y = matrix.MultiplyVector(new Vector3(0, local.extents.y, 0));
                    var z = matrix.MultiplyVector(new Vector3(0, 0, local.extents.z));
                    var extents = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                    actual = new Bounds(matrix.MultiplyPoint3x4(local.center), extents * 2);
                }
                else
                {
                    actual = renderer.bounds;
                }

                if (!found)
                {
                    bounds = actual;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(actual);
                }
            }

            return bounds;
        }
    }
}
