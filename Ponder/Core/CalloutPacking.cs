using System;
using System.Linq;

namespace Ponder.Core
{
    internal static class CalloutPacking
    {
        internal static float[] Centers(float[] desired, float[] heights, float height, float margin = 8, float gap = 12)
        {
            var order = Enumerable.Range(0, desired.Length).OrderByDescending(i => desired[i]).ToArray();
            var centers = new float[desired.Length];
            var ceiling = height - margin;
            foreach (var i in order)
            {
                centers[i] = Math.Min(Math.Max(desired[i], margin + heights[i] / 2), ceiling - heights[i] / 2);
                ceiling = centers[i] - heights[i] / 2 - gap;
            }

            var floor = margin;
            for (var n = order.Length - 1; n >= 0; n--)
            {
                var i = order[n];
                centers[i] = Math.Max(centers[i], floor + heights[i] / 2);
                floor = centers[i] + heights[i] / 2 + gap;
            }

            return centers;
        }
    }
}
