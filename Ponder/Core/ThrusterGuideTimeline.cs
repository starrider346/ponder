namespace Ponder.Core
{
    internal static class ThrusterGuideTimeline
    {
        internal const float IntroEnd = 8;
        internal const float OxygenStart = 16;
        internal const float BurnStart = 24;
        internal const float ValveStart = 32;
        internal const float SignalStart = 40;
        internal const float OffStart = 48;
        internal const float QuarterStart = 56;
        internal const float HalfStart = 64;
        internal const float ThreeQuarterStart = 72;
        internal const float MaxStart = 80;
        internal const float ShutdownStart = 88;
        internal const float Duration = 96;
        internal const float MaxConsumption = .05f;
        internal static ThrusterFrame ValueAt(float seconds)
        {
            var throttle = seconds < BurnStart ? 0 : seconds < ValveStart ? 1 : seconds < SignalStart + .5f ? 0 : seconds < OffStart ? 1 : seconds < QuarterStart ? 0 : seconds < HalfStart ? .25f : seconds < ThreeQuarterStart ? .5f : seconds < MaxStart ? .75f : seconds < ShutdownStart ? 1 : 0;
            return new ThrusterFrame(throttle);
        }
    }

    internal readonly struct ThrusterFrame
    {
        internal readonly float Throttle;
        internal float Flow => Throttle * ThrusterGuideTimeline.MaxConsumption;
        internal bool Burning => Throttle > 0;

        internal ThrusterFrame(float throttle)
        {
            Throttle = throttle;
        }
    }
}
