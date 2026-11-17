using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Ponder.Runtime
{
    internal sealed class InputCapture : IDisposable
    {
        private readonly List<InputAction> _actions = new();
        private readonly List<EventSystem> _events = new();
        private readonly List<Canvas> _canvases = new();
        private readonly EventSystem _previous;
        private readonly GameObject _selection;
        private readonly CursorLockMode _lock;
        private readonly bool _cursor;
        private bool _disposed;
        private bool _canvasesHidden;
        internal InputCapture()
        {
            _previous = EventSystem.current;
            _selection = _previous != null ? _previous.currentSelectedGameObject : null;
            _lock = Cursor.lockState;
            _cursor = Cursor.visible;
            try
            {
                _actions.AddRange(InputSystem.ListEnabledActions());
                foreach (var action in _actions)
                {
                    action.Disable();
                }

                foreach (var system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                {
                    if (system.isActiveAndEnabled)
                    {
                        _events.Add(system);
                        system.enabled = false;
                    }
                }

                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas.isActiveAndEnabled && canvas.renderMode != RenderMode.WorldSpace)
                    {
                        _canvases.Add(canvas);
                    }
                }

                HoldCursor();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void HoldCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        internal void HideCanvases()
        {
            if (_canvasesHidden)
            {
                return;
            }

            foreach (var canvas in _canvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = false;
                }
            }

            _canvasesHidden = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                foreach (var canvas in _canvases)
                {
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }

                foreach (var system in _events)
                {
                    if (system != null)
                    {
                        system.enabled = true;
                    }
                }

                if (_previous != null && _previous.isActiveAndEnabled)
                {
                    EventSystem.current = _previous;
                    if (_selection != null && _selection.activeInHierarchy)
                    {
                        _previous.SetSelectedGameObject(_selection);
                    }
                }
            }
            catch (Exception error)
            {
                MelonLoader.MelonLogger.Warning("Ponder could not restore UI selection: " + error.Message);
            }

            foreach (var action in _actions)
            {
                try
                {
                    action.Enable();
                }
                catch (Exception error)
                {
                    MelonLoader.MelonLogger.Warning("Ponder could not restore an input action: " + error.Message);
                }
            }

            _actions.Clear();
            _events.Clear();
            _canvases.Clear();
            Cursor.lockState = _lock;
            Cursor.visible = _cursor;
        }
    }
}
