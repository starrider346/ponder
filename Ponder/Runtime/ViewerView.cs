using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ponder.Runtime
{
    internal sealed class ViewerView : IDisposable
    {
        private GameObject _root;
        private CanvasGroup _fade;
        private float _fadeTime;
        private readonly List<InputActionReference> _references = new();
        private readonly CalloutLayout _callouts;
        private DefaultInputActions _actions;
        private EventSystem _events;
        private readonly TMP_FontAsset _font;
        private readonly Color _text;
        private TMP_Text _title;
        private TMP_Text _part;
        private TMP_Text _caption;
        private TMP_Text _pauseLabel;
        private Button _pause;
        private Button _replay;
        private Button _reset;
        private RectTransform _fill;
        private RectTransform _stepMarkers;
        private TimelineSlider _timeline;
        internal RectTransform Annotations
        {
            get; private set;
        }

        private RawImage _image;
        internal static readonly Color Panel = new(.025f, .055f, .075f, .96f);
        internal static readonly Color Accent = new(.25f, .78f, 1, 1);
        internal static readonly Color SceneBackground = new(.018f, .032f, .045f, 1);
        internal static readonly UnityEngine.Rect SceneArea = new(.035f, .245f, .665f, .61f);
        internal RectTransform Viewport
        {
            get; private set;
        }
        internal bool FadeComplete => _fade.alpha >= 1;
        internal bool Scrubbing => _timeline.Scrubbing;
        internal int PixelWidth => Mathf.RoundToInt(Viewport.rect.width * _root.GetComponent<Canvas>().scaleFactor);
        internal int PixelHeight => Mathf.RoundToInt(Viewport.rect.height * _root.GetComponent<Canvas>().scaleFactor);

        internal ViewerView(TMP_FontAsset font, Color text)
        {
            _callouts = new CalloutLayout(this);
            _font = font != null ? font : TMP_Settings.defaultFontAsset;
            if (_font == null)
            {
                throw new InvalidOperationException("StellarDrive's UI font is unavailable.");
            }

            _text = text;
            _text.a = 1;
            try
            {
                Create();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Create()
        {
            _root = new GameObject("Ponder guide viewer", typeof(RectTransform));
            _root.SetActive(false);
            _fade = _root.AddComponent<CanvasGroup>();
            _fade.alpha = 0;
            _fade.interactable = false;
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32766;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            _root.AddComponent<GraphicRaycaster>();
            Box(_root.transform, "Guide background", Vector2.zero, Vector2.one, SceneBackground);
            Viewport = Rect(_root.transform, "Part scene", Vector2.zero, Vector2.one);
            _image = Viewport.gameObject.AddComponent<RawImage>();
            _image.color = Color.white;
            Viewport.gameObject.AddComponent<PreviewPointer>();
            var header = Box(_root.transform, "Header", new Vector2(.035f, .87f), new Vector2(.965f, .97f), Panel);
            _part = Label(header, "Part", new Vector2(.015f, .55f), new Vector2(.80f, .95f), 19);
            _part.color = Accent;
            _title = Label(header, "Guide title", new Vector2(.015f, .04f), new Vector2(.80f, .58f), 28);
            Button(header, "Close  [Esc]", new Vector2(.84f, .18f), new Vector2(.985f, .82f), GuideViewer.Close);
            Annotations = Rect(_root.transform, "Scene annotations", new Vector2(.035f, .245f), new Vector2(.965f, .855f));
            var caption = Box(_root.transform, "Explanation", new Vector2(.12f, .155f), new Vector2(.88f, .235f), Panel);
            _caption = Label(caption, "Caption", new Vector2(.025f, .12f), new Vector2(.975f, .88f), 20);
            _caption.textWrappingMode = TextWrappingModes.Normal;
            var controls = Box(_root.transform, "Playback controls", new Vector2(.035f, .035f), new Vector2(.965f, .115f), Panel);
            _pause = Button(controls, "Pause", new Vector2(.01f, .17f), new Vector2(.17f, .84f), GuideViewer.Toggle);
            _pauseLabel = _pause.GetComponentInChildren<TMP_Text>();
            _replay = Button(controls, "Replay", new Vector2(.18f, .17f), new Vector2(.34f, .84f), GuideViewer.Replay);
            _reset = Button(controls, "Reset view", new Vector2(.35f, .17f), new Vector2(.53f, .84f), GuideViewer.ResetView);
            var hint = Label(controls, "Camera controls", new Vector2(.60f, .1f), new Vector2(.99f, .9f), 12);
            hint.text = "Left drag: turn\nRight drag: pan\nScroll: zoom";
            hint.alignment = TextAlignmentOptions.Center;
            var track = Box(_root.transform, "Timeline", new Vector2(.035f, .12f), new Vector2(.965f, .142f), new Color(.10f, .22f, .29f, 1));
            _fill = Box(track, "Progress", Vector2.zero, new Vector2(0, 1), Accent);
            _fill.GetComponent<Image>().raycastTarget = false;
            _stepMarkers = Rect(track, "Step markers", Vector2.zero, Vector2.one);
            var playhead = Box(track, "Playhead", Vector2.zero, new Vector2(0, 1), _text);
            playhead.sizeDelta = new Vector2(4, 6);
            playhead.GetComponent<Image>().raycastTarget = false;
            _timeline = track.gameObject.AddComponent<TimelineSlider>();
            _timeline.targetGraphic = track.GetComponent<Image>();
            _timeline.fillRect = _fill;
            _timeline.handleRect = playhead;
            _timeline.minValue = 0;
            _timeline.maxValue = 1;
            _timeline.wholeNumbers = false;
            _timeline.direction = Slider.Direction.LeftToRight;
            _timeline.interactable = false;
            _timeline.onValueChanged.AddListener(GuideViewer.Seek);
            var eventObject = new GameObject("Ponder input");
            eventObject.transform.SetParent(_root.transform, false);
            _events = eventObject.AddComponent<EventSystem>();
            var input = eventObject.AddComponent<InputSystemUIInputModule>();
            _actions = new DefaultInputActions();
            input.actionsAsset = _actions.asset;
            input.point = Reference(_actions.UI.Point);
            input.leftClick = Reference(_actions.UI.Click);
            input.rightClick = Reference(_actions.UI.RightClick);
            input.middleClick = Reference(_actions.UI.MiddleClick);
            input.scrollWheel = Reference(_actions.UI.ScrollWheel);
            input.move = Reference(_actions.UI.Navigate);
            input.submit = Reference(_actions.UI.Submit);
            input.cancel = Reference(_actions.UI.Cancel);
            _root.SetActive(true);
            EventSystem.current = _events;
            _actions.UI.Enable();
        }

        private InputActionReference Reference(InputAction action)
        {
            var reference = InputActionReference.Create(action);
            _references.Add(reference);
            return reference;
        }

        internal void AdvanceFade(float delta)
        {
            _fadeTime = Mathf.Min(.5f, _fadeTime + Mathf.Max(0, delta));
            _fade.alpha = _fadeTime / .5f;
            _fade.interactable = FadeComplete;
        }

        internal void CancelScrub() => _timeline.CancelScrub();
        internal void Guide(string part, RegisteredGuide guide)
        {
            CancelScrub();
            _part.text = "PONDERING  /  " + part;
            _title.text = guide.Title;
            foreach (Transform marker in _stepMarkers)
            {
                marker.gameObject.SetActive(false);
                Object.Destroy(marker.gameObject);
            }

            var total = guide.Steps.Sum(step => (double)step.Duration);
            var elapsed = 0d;
            for (var step = 1; step < guide.Steps.Length; step++)
            {
                elapsed += guide.Steps[step - 1].Duration;
                var position = (float)(elapsed / total);
                var marker = Box(_stepMarkers, "Step " + (step + 1), new Vector2(position, 0), new Vector2(position, 1), Panel);
                marker.sizeDelta = new Vector2(2, 0);
                marker.GetComponent<Image>().raycastTarget = false;
            }

            _timeline.SetValueWithoutNotify(0);
            _timeline.interactable = false;
            _callouts.Hide();
            _image.texture = null;
        }

        internal void Status(RegisteredGuide guide, Core.GuideClock clock, bool ready)
        {
            _pause.interactable = _replay.interactable = _reset.interactable = _timeline.interactable = ready;
            if (!ready)
            {
                CancelScrub();
                return;
            }

            var index = clock.Step;
            _caption.text = guide.Steps[index].Caption;
            _pauseLabel.text = clock.Playing ? "Pause" : clock.Time >= clock.Duration ? "Play again" : "Resume";
            if (!Scrubbing)
            {
                _timeline.SetValueWithoutNotify((float)(clock.Time / clock.Duration));
            }
        }

        internal void Error(string text)
        {
            CancelScrub();
            _caption.text = text;
            _timeline.interactable = false;
            _timeline.SetValueWithoutNotify(0);
            _image.texture = null;
            _callouts.Hide();
        }

        internal void Present(PreviewRenderer renderer, PonderScene scene, float seconds)
        {
            if (!string.IsNullOrWhiteSpace(scene.SubjectLabel))
            {
                _part.text = "PONDERING  /  " + scene.SubjectLabel;
            }

            _image.texture = renderer.Texture;
            _callouts.Present(renderer, scene, seconds);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(parent, name, min, max);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        internal TMP_Text Label(Transform parent, string name, Vector2 min, Vector2 max, float size)
        {
            var text = Rect(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.fontSize = size;
            text.color = _text;
            text.raycastTarget = false;
            text.richText = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private Button Button(Transform parent, string title, Vector2 min, Vector2 max, Action clicked)
        {
            var rect = Box(parent, title, min, max, new Color(.10f, .22f, .29f, 1));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => clicked());
            var label = Label(rect, "Label", new Vector2(.04f, .04f), new Vector2(.96f, .96f), 16);
            label.text = title;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        public void Dispose()
        {
            _actions?.Disable();
            if (_root != null)
            {
                _root.SetActive(false);
                Object.Destroy(_root);
                _root = null;
            }

            foreach (var reference in _references)
            {
                if (reference != null)
                {
                    Object.Destroy(reference);
                }
            }

            _references.Clear();
            _callouts.Clear();
            _actions?.Dispose();
            _actions = null;
        }
    }
}
