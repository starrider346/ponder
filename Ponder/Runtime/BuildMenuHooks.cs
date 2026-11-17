using System;
using HarmonyLib;
using Ponder.Core;
using Ponder.Guides;
using Ships.Interface.Settings;
using StellarStriderLib;
using TMPro;
using UI.BuildMenu;
using UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NativePalette = global::Core.Values.GameColorPalette;
using Object = UnityEngine.Object;

namespace Ponder.Runtime
{
    internal static class BuildMenuHooks
    {
        private static readonly HoldGesture Hold = new();
        private static BuildMenuPartInfo _owner;
        private static PartSettings _part;
        private static GameObject _prompt;
        private static RectTransform _fill;
        private static TMP_FontAsset _font;
        private static Color _text;
        private static Color _background;
        private static Color _accent;
        internal static void Install(HarmonyLib.Harmony harmony)
        {
            Patch(harmony, typeof(BuildMenuPartInfo), "Show", nameof(Shown));
            Patch(harmony, typeof(BuildMenuPartInfo), "Hide", nameof(Hidden));
            Patch(harmony, typeof(BuildMenu), "OnDisable", nameof(MenuClosed));
            var close = AccessTools.Method(typeof(MenuTracker), "CloseAllOpenMenus") ?? throw new MissingMethodException(typeof(MenuTracker).FullName, "CloseAllOpenMenus");
            harmony.Patch(close, prefix: new HarmonyMethod(typeof(GuideViewer), nameof(GuideViewer.NativeClose)));
        }

        private static void Patch(HarmonyLib.Harmony harmony, Type type, string method, string callback)
        {
            var target = AccessTools.Method(type, method) ?? throw new MissingMethodException(type.FullName, method);
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(BuildMenuHooks), callback));
        }

        private static void Shown(BuildMenuPartInfo __instance, PartSettings __0, Transform ___elementsList, TMP_Text ___description, NativePalette ___palette, GameObject ___wrapper)
        {
            try
            {
                BuiltInGuides.Ensure(__0);
                var guides = PonderRegistry.Find(__0.id);
                if (guides.Length == 0)
                {
                    Hidden(__instance);
                    return;
                }

                if (_owner != __instance || _prompt == null)
                {
                    Clear();
                    _owner = __instance;
                    _font = ___description.font;
                    _text = ___description.color;
                    _background = ___palette != null ? ___palette.uiBackgroundColor : new Color(.015f, .025f, .04f, 1);
                    _accent = ___palette != null ? ___palette.uiLineColor : _text;
                    CreatePrompt(___wrapper.transform, ___description.fontSize);
                }

                if (_part != __0)
                {
                    Hold.Cancel();
                }

                _part = __0;
                _prompt.SetActive(true);
            }
            catch (Exception error)
            {
                Clear();
                MelonLoader.MelonLogger.Error("Ponder build-menu prompt failed: " + error);
            }
        }

        private static void CreatePrompt(Transform parent, float size)
        {
            _prompt = new GameObject("Hold Z To Ponder", typeof(RectTransform));
            _prompt.transform.SetParent(parent, false);
            _prompt.AddComponent<LayoutElement>().ignoreLayout = true;
            var bar = (RectTransform)_prompt.transform;
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1, 0);
            bar.offsetMin = new Vector2(8, 8);
            bar.offsetMax = new Vector2(-8, 8 + Mathf.Max(52, size * 3.2f));
            var background = _prompt.AddComponent<Image>();
            background.color = _background;
            background.raycastTarget = false;
            var border = _prompt.AddComponent<Outline>();
            border.effectColor = _accent;
            border.effectDistance = new Vector2(1, -1);
            border.useGraphicAlpha = false;
            var textRect = (RectTransform)new GameObject("Ponder hint", typeof(RectTransform)).transform;
            textRect.SetParent(bar, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 4);
            textRect.offsetMax = new Vector2(-16, -8);
            var label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.fontSize = size * 1.1f;
            label.text = "Hold <b>Z</b> To Ponder";
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = size * 1.1f;
            label.color = _text;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            var track = (RectTransform)new GameObject("Hold progress", typeof(RectTransform)).transform;
            track.SetParent(bar, false);
            track.anchorMin = new Vector2(0, 1);
            track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(0, -4);
            track.offsetMax = Vector2.zero;
            var image = track.gameObject.AddComponent<Image>();
            image.color = new Color(_accent.r, _accent.g, _accent.b, _accent.a * .2f);
            image.raycastTarget = false;
            _fill = (RectTransform)new GameObject("Progress", typeof(RectTransform)).transform;
            _fill.SetParent(track, false);
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(0, 1);
            _fill.offsetMin = _fill.offsetMax = Vector2.zero;
            var fillImage = _fill.gameObject.AddComponent<Image>();
            fillImage.color = _accent;
            fillImage.raycastTarget = false;
        }

        internal static void Update()
        {
            if (_owner == null || _prompt == null || _part == null)
            {
                CancelHold();
                return;
            }

            var keyboard = Keyboard.current;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var typing = selected != null && (selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<InputField>() != null);
            var eligible = !GuideViewer.Opened && Application.isFocused && _owner.isActiveAndEnabled && _prompt.activeInHierarchy && StriderRegistry.WorldReady && !typing && keyboard != null;
            var fired = Hold.Update(eligible ? _part : null, eligible && keyboard.zKey.isPressed, eligible && keyboard.zKey.wasPressedThisFrame, Time.unscaledDeltaTime);
            _fill.anchorMax = new Vector2(Hold.Progress, 1);
            if (fired)
            {
                var guides = PonderRegistry.Find(_part.id);
                GuideViewer.Open(_owner, _part, guides, _font, _text);
            }
        }

        internal static void CancelHold()
        {
            Hold.Cancel();
            if (_fill != null)
            {
                _fill.anchorMax = new Vector2(0, 1);
            }
        }

        private static void Hidden(BuildMenuPartInfo __instance)
        {
            if (_owner != __instance)
            {
                return;
            }

            GuideViewer.OwnerClosed(__instance);
            _part = null;
            CancelHold();
            if (_prompt != null)
            {
                _prompt.SetActive(false);
            }
        }

        private static void MenuClosed(BuildMenu __instance)
        {
            if (_owner == null || _owner.transform.IsChildOf(__instance.transform))
            {
                Clear();
            }
        }

        internal static void Clear()
        {
            if (_owner != null)
            {
                GuideViewer.OwnerClosed(_owner);
            }

            CancelHold();
            if (_prompt != null)
            {
                _prompt.SetActive(false);
                Object.Destroy(_prompt);
            }

            _prompt = null;
            _fill = null;
            _owner = null;
            _part = null;
        }
    }
}
