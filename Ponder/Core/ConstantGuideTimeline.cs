using System;
using System.Globalization;

namespace Ponder.Core
{
    internal static class ConstantGuideTimeline
    {
        internal const float IntroEnd = 7;
        internal const float FloorStart = IntroEnd;
        internal const float FloorDuration = .4f;
        internal const float PlaceStart = IntroEnd + .4f;
        internal const float PlaceDuration = .35f;
        internal const float DisplayStart = IntroEnd + .65f;
        internal const float DisplayDuration = .35f;
        internal const float CableStart = IntroEnd + 1.1f;
        internal const float CableDuration = 1.2f;
        internal const float EditStart = 16;
        internal const float FirstBackspace = 22;
        internal const float EditInterval = 2;
        internal const float BlankDuration = .5f;
        internal const float Duration = 37;
        internal static ConstantValueFrame ValueAt(float seconds)
        {
            if (seconds < FirstBackspace)
            {
                return new ConstantValueFrame("0", 0, false);
            }

            var index = (int)Math.Floor((seconds - FirstBackspace) / EditInterval);
            if (index >= 6)
            {
                return new ConstantValueFrame("6", 6, false);
            }

            var blank = seconds - FirstBackspace - index * EditInterval < BlankDuration;
            var value = blank ? index : index + 1;
            return new ConstantValueFrame(blank ? "" : value.ToString(CultureInfo.InvariantCulture), value, true);
        }
    }

    internal readonly struct ConstantValueFrame
    {
        internal readonly string Input;
        internal readonly int Output;
        internal readonly bool Editing;
        internal ConstantValueFrame(string input, int output, bool editing)
        {
            Input = input;
            Output = output;
            Editing = editing;
        }
    }
}
