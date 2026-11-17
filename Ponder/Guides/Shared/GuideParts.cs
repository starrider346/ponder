using System;
using System.Collections.Generic;
using System.Linq;
using Ships.Interface.Model;
using Ships.Interface.Model.Parts;
using Ships.Interface.Settings;

namespace Ponder.Guides
{
    internal static class GuideParts
    {
        internal static PartSettings Find(IEnumerable<PartSettings> parts, string partName) => parts.FirstOrDefault(part => part.name == partName) ?? throw new InvalidOperationException("The native " + partName + " part is unavailable.");
        internal static HullPartSettings IronFloor(IEnumerable<PartSettings> parts) => parts.OfType<HullPartSettings>().Where(part => part.name.IndexOf(':') < 0 && part.hullMaterialType == HullMaterialType.Metal && part.hullMeshSegmentType == HullMeshSegmentType.Flat && part.snappingStyle == SnappingStyle.Floor && !part.hasItsOwnMesh).OrderBy(part => part.id).FirstOrDefault() ?? throw new InvalidOperationException("The native iron floor is unavailable.");
    }
}
