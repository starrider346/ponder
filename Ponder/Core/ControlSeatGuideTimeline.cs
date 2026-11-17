using System;

namespace Ponder.Core
{
    internal static class ControlSeatGuideTimeline
    {
        internal const float ForwardStart = 8;
        internal const float VerticalStart = 20;
        internal const float BuiltInGyroStart = 32;
        internal const float AdvancedStart = 44;
        internal const float TranslationStart = 52;
        internal const float RotationStart = 64;
        internal const float GyroStart = 76;
        internal const float RollStart = 84;
        internal const float YawStart = 92;
        internal const float PitchStart = 100;
        internal const float SteeringStart = 108;
        internal const float ReleaseStart = 120;
        internal const float Duration = 128;
        internal const byte BasicVertical = 0;
        internal const byte BasicForward = 1;
        internal const byte Forward = 0;
        internal const byte Vertical = 1;
        internal const byte Lateral = 2;
        internal const byte Roll = 3;
        internal const byte Yaw = 4;
        internal const byte Pitch = 5;
        internal const byte GyroPitch = 0;
        internal const byte GyroYaw = 1;
        internal const byte GyroRoll = 2;
        internal const byte GyroPower = 3;
        internal static ControlSeatFrame ValueAt(float seconds)
        {
            float forward = 0;
            float up = 0;
            float right = 0;
            float pitch = 0;
            float yaw = 0;
            float roll = 0;
            if (seconds >= ForwardStart && seconds < VerticalStart)
            {
                forward = Pair(seconds - ForwardStart);
            }

            if (seconds >= VerticalStart && seconds < BuiltInGyroStart)
            {
                up = Pair(seconds - VerticalStart);
            }

            if (seconds >= BuiltInGyroStart && seconds < AdvancedStart)
            {
                yaw = Pair(seconds - BuiltInGyroStart);
            }

            if (seconds >= TranslationStart && seconds < RotationStart)
            {
                right = Pair(seconds - TranslationStart);
            }

            if (seconds >= RotationStart && seconds < GyroStart)
            {
                if (seconds < RotationStart + 4)
                {
                    roll = 1;
                }
                else if (seconds < RotationStart + 8)
                {
                    yaw = 1;
                }
                else
                {
                    pitch = 1;
                }
            }

            if (seconds >= SteeringStart && seconds < ReleaseStart)
            {
                var elapsed = seconds - SteeringStart;
                var sign = elapsed % 4 < 2 ? 1 : -1;
                if (elapsed < 4)
                {
                    roll = sign;
                }
                else if (elapsed < 8)
                {
                    yaw = sign;
                }
                else
                {
                    pitch = sign;
                }
            }

            var basic = seconds >= BuiltInGyroStart && seconds < AdvancedStart;
            var external = seconds >= SteeringStart && seconds < ReleaseStart;
            var gyroSeconds = basic ? Math.Max(0, Math.Min(8, seconds - BuiltInGyroStart)) : seconds >= SteeringStart ? Math.Max(0, Math.Min(12, seconds - SteeringStart)) : 0;
            return new ControlSeatFrame(seconds >= AdvancedStart, forward, up, right, pitch, yaw, roll, basic || external, gyroSeconds);
        }

        private static float Pair(float elapsed) => elapsed < 4 ? 1 : elapsed < 8 ? -1 : 0;
    }

    internal readonly struct ControlSeatFrame
    {
        internal readonly bool Advanced;
        internal readonly bool Steering;
        internal readonly float Forward;
        internal readonly float Up;
        internal readonly float Right;
        internal readonly float Pitch;
        internal readonly float Yaw;
        internal readonly float Roll;
        internal readonly float GyroSeconds;
        internal ControlSeatFrame(bool advanced, float forward, float up, float right, float pitch, float yaw, float roll, bool steering, float gyroSeconds)
        {
            Advanced = advanced;
            Forward = forward;
            Up = up;
            Right = right;
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
            Steering = steering;
            GyroSeconds = gyroSeconds;
        }

        internal float Output(int socket) => Advanced ? socket switch
        {
            0 => Forward,
            1 => Up,
            2 => Right,
            3 => Roll,
            4 => Yaw,
            5 => Pitch,
            _ => 0
        } : socket == ControlSeatGuideTimeline.BasicVertical ? Up : socket == ControlSeatGuideTimeline.BasicForward ? Forward : 0;
    }
}
