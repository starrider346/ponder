using System;

namespace Ponder.Core
{
    internal sealed class CompletionOrbit
    {
        private float _elapsed;
        internal bool Finished
        {
            get; private set;
        }
        internal bool Automatic
        {
            get; private set;
        }
        internal float Angle
        {
            get; private set;
        }
        internal float Blend => _elapsed * _elapsed * (3 - 2 * _elapsed);

        internal void Update(bool finished, float delta)
        {
            if (!finished)
            {
                Finished = Automatic = false;
                _elapsed = Angle = 0;
                return;
            }

            if (!Finished)
            {
                Finished = Automatic = true;
                return;
            }

            if (!Automatic || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            delta = Math.Max(0, delta);
            var fitting = Math.Min(1 - _elapsed, delta);
            _elapsed += fitting;
            Angle = (Angle + (delta - fitting) * 8) % 360;
        }

        internal void Stop()
        {
            Automatic = false;
        }
    }
}
