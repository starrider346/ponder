using System;
using System.Linq;
using MelonLoader;
using Ponder.Core;
using Ships.Interface.Settings;
using StellarStriderLib;
using TMPro;
using UI.BuildMenu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ponder.Runtime
{
    internal sealed class GuideSession : IDisposable
    {
        private BuildMenuPartInfo _owner;
        private PartSettings _part;
        private RegisteredGuide[] _guides;
        private int _index;
        private ViewerView _view;
        private InputCapture _input;
        private PreviewRenderer _renderer;
        private PonderScene _scene;
        private GuideClock _clock;
        private bool _skipAdvance;
        internal bool Opened
        {
            get; private set;
        }

        internal void Open(BuildMenuPartInfo owner, PartSettings part, RegisteredGuide[] guides, TMP_FontAsset font,
            Color text)
        {
            if (Opened || guides.Length == 0)
            {
                return;
            }

            Opened = true;
            _owner = owner;
            _part = part;
            _guides = guides;
            _index = 0;
            _skipAdvance = true;
            try
            {
                _input = new InputCapture();
                if (!Opened || _owner == null)
                {
                    _input.Dispose();
                    _input = null;
                    return;
                }

                _view = new ViewerView(font, text);
                Select(0);
            }
            catch (Exception error)
            {
                MelonLogger.Error("Ponder viewer could not open: " + error);
                GuideViewer.Close();
            }
        }

        private void Select(int index)
        {
            ReleasePreview();
            _index = index;
            var guide = _guides[index];
            _view.Guide(_part.fullLabel, guide);
            _clock = new GuideClock(guide.Steps.Select(step => step.Duration).ToArray());
            try
            {
                _renderer = new PreviewRenderer();
                _scene = new PonderScene(_renderer.Layer);
                guide.Build(_scene);
                _scene.Seal();
                _scene.Evaluate(0, guide.Sample);
                _renderer.Fit(_scene.Bounds);
                _view.Status(guide, _clock, true);
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        internal void Update()
        {
            if (!Opened)
            {
                return;
            }

            if (_owner == null || !_owner.isActiveAndEnabled || !StriderRegistry.WorldReady)
            {
                GuideViewer.Close();
                return;
            }

            if (Application.isFocused && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                GuideViewer.Close();
                return;
            }

            var advance = Application.isFocused && !_skipAdvance;
            var visible = _view != null && _view.FadeComplete;
            if (advance)
            {
                _view?.AdvanceFade(Time.unscaledDeltaTime);
            }

            if (_view != null && _view.FadeComplete)
            {
                _input?.HideCanvases();
            }

            _clock?.Advance(Time.unscaledDeltaTime, advance && visible && !_view.Scrubbing);
            _renderer?.UpdateCompletion(_clock != null && _clock.Time >= _clock.Duration, advance && visible ? Time.unscaledDeltaTime : 0);
            var mouse = Mouse.current;
            if (Application.isFocused && (Keyboard.current?.anyKey.wasPressedThisFrame == true || mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed || mouse.scroll.ReadValue() != Vector2.zero)))
            {
                _renderer?.StopCompletionOrbit();
            }

            _skipAdvance = false;
        }

        internal void FocusChanged(bool focused)
        {
            _skipAdvance = true;
            _view?.CancelScrub();
            BuildMenuHooks.CancelHold();
        }

        internal void LateUpdate()
        {
            if (!Opened)
            {
                return;
            }

            _input?.HoldCursor();
            if (_scene == null || !Application.isFocused)
            {
                return;
            }

            try
            {
                var guide = _guides[_index];
                _scene.Evaluate((float)_clock.Time, guide.Sample);
                _renderer.Render(_scene, _view.PixelWidth, _view.PixelHeight);
                _view.Present(_renderer, _scene, (float)_clock.Time);
                _view.Status(guide, _clock, true);
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        private void Fail(Exception error)
        {
            MelonLogger.Error("Ponder guide failed [" + _guides[_index].Key + "]: " + error);
            ReleasePreview();
            _view.Status(_guides[_index], _clock, false);
            _view.Error("This guide could not be displayed. " + error.GetBaseException().Message);
        }

        internal void Toggle()
        {
            if (_scene == null)
            {
                return;
            }

            if (_clock.Time >= _clock.Duration)
            {
                Replay();
            }
            else
            {
                _clock.Toggle();
            }
        }

        internal void Replay()
        {
            if (_scene == null)
            {
                return;
            }

            _clock.Replay();
            _renderer.UpdateCompletion(false, 0);
            _renderer.Reset();
        }

        internal void Seek(float progress)
        {
            if (!Opened || _scene == null || !_view.FadeComplete)
            {
                return;
            }

            _clock.Seek(progress);
            _renderer.UpdateCompletion(_clock.Time >= _clock.Duration, 0);
            _renderer.StopCompletionOrbit();
            _skipAdvance = true;
            _view.Status(_guides[_index], _clock, true);
        }

        internal void Orbit(Vector2 delta)
        {
            if (Opened)
            {
                _renderer?.Orbit(delta);
            }
        }

        internal void Pan(Vector2 delta)
        {
            if (Opened)
            {
                _renderer?.Pan(delta);
            }
        }

        internal void Zoom(float scroll)
        {
            if (Opened)
            {
                _renderer?.Zoom(scroll);
            }
        }

        internal void ResetView()
        {
            if (Opened)
            {
                _renderer?.Reset();
            }
        }

        internal void OwnerClosed(BuildMenuPartInfo owner)
        {
            if (Opened && _owner == owner)
            {
                GuideViewer.Close();
            }
        }

        public void Dispose()
        {
            if (!Opened)
            {
                return;
            }

            Opened = false;
            try
            {
                ReleasePreview();
            }
            finally
            {
                try
                {
                    _view?.Dispose();
                }
                finally
                {
                    _view = null;
                    _input?.Dispose();
                    _input = null;
                }

                _owner = null;
                _part = null;
                _guides = null;
                _clock = null;
                BuildMenuHooks.CancelHold();
            }
        }

        private void ReleasePreview()
        {
            try
            {
                _scene?.Dispose();
            }
            finally
            {
                _scene = null;
                _renderer?.Dispose();
                _renderer = null;
            }
        }
    }
}
