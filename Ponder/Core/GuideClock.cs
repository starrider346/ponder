using System;

namespace Ponder.Core
{
    internal sealed class GuideClock
    {
        private readonly double[] _starts;
        internal double Duration
        {
            get;
        }
        internal double Time
        {
            get; private set;
        }
        internal bool Playing { get; private set; } = true;

        internal int Step
        {
            get
            {
                for (var i = _starts.Length - 1; i > 0; i--)
                {
                    if (Time >= _starts[i])
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

        internal GuideClock(float[] durations)
        {
            if (durations == null || durations.Length == 0)
            {
                throw new ArgumentException("A guide needs at least one step.");
            }

            _starts = new double[durations.Length];
            for (var i = 0; i < durations.Length; i++)
            {
                if (float.IsNaN(durations[i]) || float.IsInfinity(durations[i]) || durations[i] <= 0)
                {
                    throw new ArgumentException("Step durations must be finite and positive.");
                }

                _starts[i] = Duration;
                Duration += durations[i];
            }

            if (Duration > float.MaxValue)
            {
                throw new ArgumentException("Guide duration is too large.");
            }
        }

        internal void Advance(float delta, bool focused)
        {
            if (!Playing || !focused || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            Time = Math.Min(Duration, Time + Math.Max(0, delta));
            if (Time >= Duration)
            {
                Playing = false;
            }
        }

        internal void Toggle()
        {
            if (Time >= Duration)
            {
                Replay();
            }
            else
            {
                Playing = !Playing;
            }
        }

        internal void Replay()
        {
            Time = 0;
            Playing = true;
        }

        internal void Jump(int step)
        {
            Time = _starts[Math.Max(0, Math.Min(_starts.Length - 1, step))];
        }

        internal void Seek(float progress)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress))
            {
                return;
            }

            progress = Math.Max(0, Math.Min(1, progress));
            Time = Duration * progress;
            for (var step = 1; step < _starts.Length; step++)
            {
                if (progress == (float)(_starts[step] / Duration))
                {
                    Time = _starts[step];
                    break;
                }
            }

            if (Time >= Duration)
            {
                Playing = false;
            }
        }
    }
}
