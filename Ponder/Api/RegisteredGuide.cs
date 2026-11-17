using System;

namespace Ponder
{
    internal sealed class RegisteredGuide
    {
        internal readonly string Key;
        internal readonly string Title;
        internal readonly GuideStep[] Steps;
        internal readonly Action<PonderScene> Build;
        internal readonly Action<PonderScene, float> Sample;
        internal RegisteredGuide(string key, string title, GuideStep[] steps, Action<PonderScene> build, Action<PonderScene, float> sample)
        {
            Key = key;
            Title = title;
            Steps = steps;
            Build = build;
            Sample = sample;
        }
    }
}
