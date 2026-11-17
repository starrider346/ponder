using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ponder.Runtime
{
    internal sealed class TimelineSlider : Slider
    {
        internal bool Scrubbing
        {
            get; private set;
        }

        internal void CancelScrub() => Scrubbing = false;
        public override void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable())
            {
                return;
            }

            Scrubbing = true;
            base.OnPointerDown(data);
        }

        public override void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                Scrubbing = false;
            }

            base.OnPointerUp(data);
        }

        protected override void OnDisable()
        {
            Scrubbing = false;
            base.OnDisable();
        }
    }
}
