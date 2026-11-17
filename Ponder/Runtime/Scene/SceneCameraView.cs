using UnityEngine;

namespace Ponder.Runtime
{
    internal readonly struct SceneCameraView
    {
        internal readonly Vector3 Target;
        internal readonly float Yaw;
        internal readonly float Pitch;
        internal readonly float Size;
        internal readonly Bounds? Frame;
        internal readonly float Padding;
        internal SceneCameraView(Vector3 target, float yaw, float pitch, float size)
        {
            Target = target;
            Yaw = yaw;
            Pitch = pitch;
            Size = size;
            Frame = null;
            Padding = 1;
        }

        internal SceneCameraView(Bounds bounds, float yaw, float pitch, float padding)
        {
            Target = bounds.center;
            Yaw = yaw;
            Pitch = pitch;
            Size = 0;
            Frame = bounds;
            Padding = padding;
        }
    }
}
