using Ships.Interface.Settings;
using StellarStriderLib;
using UnityEngine;

namespace Ponder.Guides
{
    internal static class GuideGeometry
    {
        internal static Bounds BoundsOf(GameObject part)
        {
            var bounds = new Bounds();
            var found = false;
            foreach (var filter in part.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var root = part.transform;
                while (root.parent != null && root.parent.name != "Ponder preview scene")
                {
                    root = root.parent;
                }

                var matrix = root.parent.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var local = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!found)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return bounds;
        }

        internal static GameObject CreateFloor(PonderScene scene, PartSettings settings, out Bounds bounds)
        {
            var floor = scene.CreateObject("floor");
            bounds = default;
            for (var x = 0; x < 5; x++)
            {
                for (var z = 0; z < 3; z++)
                {
                    var tile = scene.AddPart("floor_" + x + "_" + z, PartRef.Native(settings.id));
                    var local = BoundsOf(tile);
                    tile.transform.SetParent(floor.transform, false);
                    tile.transform.localPosition = new Vector3((x - 2) * local.size.x - local.center.x, -local.max.y, (z - 1) * local.size.z - local.center.z);
                    var placed = BoundsOf(tile);
                    if (x == 0 && z == 0)
                    {
                        bounds = placed;
                    }
                    else
                    {
                        bounds.Encapsulate(placed);
                    }
                }
            }

            return floor;
        }
    }
}
