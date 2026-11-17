using UnityEngine;
using UnityEngine.EventSystems;

namespace Ponder.Runtime
{
    internal sealed class PreviewPointer : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public void OnDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                GuideViewer.Orbit(data.delta);
            }
            else if (data.button == PointerEventData.InputButton.Right)
            {
                GuideViewer.Pan(data.delta);
            }
        }

        public void OnScroll(PointerEventData data) => GuideViewer.Zoom(data.scrollDelta.y * 120);
    }
}
