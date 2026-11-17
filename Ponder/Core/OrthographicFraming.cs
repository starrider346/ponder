using System;

namespace Ponder.Core
{
    internal static class OrthographicFraming
    {
        internal static float HalfHeight(float projectedHalfWidth, float projectedHalfHeight, float aspect, float padding) => Math.Max(.02f, Math.Max(projectedHalfHeight, projectedHalfWidth / Math.Max(.01f, aspect))) * padding;
        internal static float OrbitHalfHeight(float halfWidth, float halfHeight, float halfDepth, float pitch, float aspect, float padding)
        {
            var horizontal = (float)Math.Sqrt(halfWidth * halfWidth + halfDepth * halfDepth);
            var radians = pitch * Math.PI / 180;
            var vertical = halfHeight * (float)Math.Abs(Math.Cos(radians)) + horizontal * (float)Math.Abs(Math.Sin(radians));
            return HalfHeight(horizontal, vertical, aspect, padding);
        }
    }
}
