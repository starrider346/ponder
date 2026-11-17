using System;

namespace Ponder.Core
{
    internal sealed class HoldGesture
    {
        private object _target;
        private bool _armed;
        private bool _fired;
        private float _elapsed;
        internal const float Duration = 1f;
        internal float Progress => Math.Min(1, _elapsed / Duration);

        internal bool Update(object target, bool held, bool pressed, float delta)
        {
            if (!Equals(target, _target))
            {
                Cancel();
                _target = target;
            }

            if (target == null || !held)
            {
                _armed = _fired = false;
                _elapsed = 0;
                return false;
            }

            if (pressed && !_fired)
            {
                _armed = true;
                _elapsed = 0;
            }

            if (!_armed || _fired)
            {
                return false;
            }

            _elapsed += Math.Max(0, delta);
            if (_elapsed < Duration)
            {
                return false;
            }

            _fired = true;
            return true;
        }

        internal void Cancel()
        {
            _target = null;
            _armed = _fired = false;
            _elapsed = 0;
        }
    }
}
