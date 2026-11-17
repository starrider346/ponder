using System;

namespace Ponder.Core
{
    internal static class FluidTankGuideTimeline
    {
        internal const float IntroEnd = 7;
        internal const float AssemblyEnd = 14;
        internal const float PlaceDuration = .35f;
        internal const float CameraDuration = .6f;
        internal const float CableDuration = 1.2f;
        internal const float FillPipeStart = IntroEnd + 1.1f;
        internal const float FillStart = AssemblyEnd;
        internal const float FillEnd = 24;
        internal const float ThrusterStart = 27;
        internal const float FuelPipeStart = 27.5f;
        internal const float OxygenStart = 29;
        internal const float DrainStart = 37;
        internal const float HalfFull = 47;
        internal const float DisplayStart = 47.3f;
        internal const float SignalStart = 47.9f;
        internal const float SignalReady = SignalStart + CableDuration;
        internal const float ReadoutStart = 54;
        internal const float Resume = 63;
        internal const float Empty = 73;
        internal const float Duration = 80;
        internal static FluidTankFrame ValueAt(float seconds)
        {
            var percent = seconds switch
            {
                < FillStart => 0f,
                < FillEnd => 100 * Phase(seconds, FillStart, FillEnd),
                < DrainStart => 100f,
                < HalfFull => 100 - 50 * Phase(seconds, DrainStart, HalfFull),
                < Resume => 50f,
                _ => 50 * (1 - Phase(seconds, Resume, Empty))
            };

            return new FluidTankFrame(percent, seconds >= FillStart && seconds < FillEnd, (seconds >= DrainStart && seconds < HalfFull) || (seconds >= Resume && seconds < Empty));
        }

        private static float Phase(float seconds, float start, float end) => Math.Max(0, Math.Min(1, (seconds - start) / (end - start)));
    }

    internal readonly struct FluidTankFrame
    {
        internal readonly float Percent;
        internal readonly bool Producing;
        internal readonly bool Thrusting;
        internal int Signal => (int)Math.Round(Percent, MidpointRounding.AwayFromZero);

        internal FluidTankFrame(float percent, bool producing, bool thrusting)
        {
            Percent = percent;
            Producing = producing;
            Thrusting = thrusting;
        }
    }
}
