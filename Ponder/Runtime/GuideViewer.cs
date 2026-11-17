using Ships.Interface.Settings;
using TMPro;
using UI.BuildMenu;
using UnityEngine;

namespace Ponder.Runtime
{
    internal static class GuideViewer
    {
        private static GuideSession _session;
        private static int _closedFrame = -1;
        internal static bool Opened => _session?.Opened == true;

        internal static void Open(BuildMenuPartInfo owner, PartSettings part, RegisteredGuide[] guides,
            TMP_FontAsset font, Color text)
        {
            if (Opened || guides.Length == 0)
            {
                return;
            }

            var session = new GuideSession();
            _session = session;
            session.Open(owner, part, guides, font, text);
        }

        internal static void Update() => _session?.Update();
        internal static void LateUpdate() => _session?.LateUpdate();
        internal static void FocusChanged(bool focused)
        {
            _session?.FocusChanged(focused);
            BuildMenuHooks.CancelHold();
        }

        internal static void Toggle() => _session?.Toggle();
        internal static void Replay() => _session?.Replay();
        internal static void Seek(float progress) => _session?.Seek(progress);
        internal static void Orbit(Vector2 delta) => _session?.Orbit(delta);
        internal static void Pan(Vector2 delta) => _session?.Pan(delta);
        internal static void Zoom(float scroll) => _session?.Zoom(scroll);
        internal static void ResetView() => _session?.ResetView();
        internal static void OwnerClosed(BuildMenuPartInfo owner) => _session?.OwnerClosed(owner);
        internal static bool NativeClose()
        {
            if (Opened)
            {
                Close();
                return false;
            }

            return _closedFrame != Time.frameCount;
        }

        internal static void Close()
        {
            var session = _session;
            if (session == null || !session.Opened)
            {
                return;
            }

            _session = null;
            _closedFrame = Time.frameCount;
            session.Dispose();
        }
    }
}
