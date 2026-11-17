using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ponder.Runtime
{
    internal sealed class CalloutView
    {
        private readonly RectTransform _panel;
        private readonly RectTransform _line;
        private readonly TMP_Text _label;
        private float _width;
        private float _height;
        internal float CenterY;
        internal CalloutView(ViewerView view)
        {
            _line = ViewerView.Box(view.Annotations, "Callout leader", Vector2.zero, Vector2.zero, ViewerView.Accent);
            _line.pivot = new Vector2(0, .5f);
            _line.GetComponent<Image>().raycastTarget = false;
            _panel = ViewerView.Box(view.Annotations, "Callout", Vector2.zero, Vector2.zero, ViewerView.Panel);
            _panel.pivot = new Vector2(0, .5f);
            _panel.GetComponent<Image>().raycastTarget = false;
            _label = view.Label(_panel, "Explanation", Vector2.zero, Vector2.one, 17);
            _label.rectTransform.offsetMin = new Vector2(12, 8);
            _label.rectTransform.offsetMax = new Vector2(-12, -8);
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 10;
            _label.fontSizeMax = 17;
            Hide();
        }

        internal float Measure(string text, Vector2 size, float maximumHeight)
        {
            _width = Mathf.Min(340, size.x * .26f);
            _label.text = text;
            _height = Mathf.Min(maximumHeight, Mathf.Max(54, _label.GetPreferredValues(text, _width - 24, 0).y + 20));
            return _height;
        }

        internal void Show(string text, Vector2 target, Vector2 size)
        {
            _panel.gameObject.SetActive(true);
            _line.gameObject.SetActive(true);
            _label.text = text;
            var position = new Vector2(size.x - _width - 12, CenterY);
            _panel.sizeDelta = new Vector2(_width, _height);
            _panel.anchoredPosition = position;
            var end = position;
            var delta = end - target;
            _line.anchoredPosition = target;
            _line.sizeDelta = new Vector2(delta.magnitude, 2);
            _line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        internal void Hide()
        {
            _panel.gameObject.SetActive(false);
            _line.gameObject.SetActive(false);
        }
    }
}
