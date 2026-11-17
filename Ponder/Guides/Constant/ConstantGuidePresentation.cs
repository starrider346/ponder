using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Ponder.Core;
using Ponder.Runtime;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Services;
using Ships.Parts.Constant;
using Ships.Parts.SignalDisplay;
using StellarStriderLib;
using TMPro;
using UI.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ponder.Guides.Constant
{
    internal sealed class ConstantGuidePresentation : MonoBehaviour
    {
        internal const string PortExplanation = "The constant is capable of outputting a set value from its signal port.";
        private static readonly FieldInfo DisplayText = NativeFields.Find(typeof(SignalDisplayVisuals), "displayedText");
        private static readonly FieldInfo InteractAction = NativeFields.Find(typeof(InteractionTooltipsActivator), "interactAction");
        private TMP_InputField _input;
        private TMP_Text _display;
        private RectTransform _caret;
        private Transform _port;
        private float _floorTop;
        private float _portYaw;
        private float _portPitch;
        private Bounds _constantBounds;
        private Bounds _assemblyBounds;
        private Bounds _editingBounds;

        internal void Build(PonderScene scene, PartRef constantRef)
        {
            var parts = new PartSettingsProvider().GetAllPartSettings();
            var displaySettings = parts.Where(p => p.name.IndexOf(':') < 0 && (p.name == "SignalDisplay" || p.internalStateClass == nameof(SignalDisplayState) || p.internalStateClass?.Split(',')[0].Trim().EndsWith("." + nameof(SignalDisplayState), StringComparison.Ordinal) == true)).OrderBy(p => p.name == "SignalDisplay" ? 0 : 1).ThenBy(p => p.id).FirstOrDefault() ?? throw new InvalidOperationException("The native Signal Display part is unavailable.");
            var floorSettings = GuideParts.IronFloor(parts);
            var floor = scene.CreateObject("floor");
            var floorBounds = new Bounds();
            const int floorTiles = 1;
            for (var i = 0; i < floorTiles; i++)
            {
                var tile = scene.AddPart("floor_" + i, PartRef.Native(floorSettings.id));
                tile.transform.SetParent(floor.transform, false);
                var tileBounds = LocalBounds(tile);
                var offset = new Vector3((i - (floorTiles - 1) * .5f) * tileBounds.size.x - tileBounds.center.x, -tileBounds.max.y, -tileBounds.center.z);
                tile.transform.localPosition = offset;
                tileBounds.center += offset;
                if (i == 0)
                {
                    floorBounds = tileBounds;
                }
                else
                {
                    floorBounds.Encapsulate(tileBounds);
                }
            }

            _floorTop = floorBounds.max.y;
            floor.SetActive(false);
            scene.Show("floor", ConstantGuideTimeline.FloorStart);
            scene.Move("floor", new Vector3(0, -.85f, 0), Vector3.zero, ConstantGuideTimeline.FloorStart, ConstantGuideTimeline.FloorDuration);
            var constant = scene.AddPart("constant", constantRef);
            var display = scene.AddPart("signal_display", PartRef.Native(displaySettings.id));
            _constantBounds = LocalBounds(constant);
            var displayBounds = LocalBounds(display);
            const float gap = .28f;
            var leftEdge = -(_constantBounds.size.x + displayBounds.size.x + gap) / 2;
            var constantPosition = new Vector3(leftEdge - _constantBounds.min.x, _floorTop - _constantBounds.min.y + .004f, -_constantBounds.center.z);
            var displayPosition = new Vector3(leftEdge + _constantBounds.size.x + gap - displayBounds.min.x, _floorTop - displayBounds.min.y + .004f, -displayBounds.center.z);
            var placedConstant = _constantBounds;
            placedConstant.center += constantPosition;
            var placedDisplay = displayBounds;
            placedDisplay.center += displayPosition;
            _editingBounds = placedConstant;
            _editingBounds.Encapsulate(placedDisplay);
            _assemblyBounds = floorBounds;
            _assemblyBounds.Encapsulate(_editingBounds);
            constant.transform.localPosition = constantPosition + Vector3.up * .35f;
            display.transform.localPosition = displayPosition;
            scene.Move("constant", constant.transform.localPosition, constantPosition, ConstantGuideTimeline.PlaceStart, ConstantGuideTimeline.PlaceDuration);
            display.SetActive(false);
            scene.Show("signal_display", ConstantGuideTimeline.DisplayStart);
            scene.Move("signal_display", displayPosition + Vector3.up * .5f, displayPosition, ConstantGuideTimeline.DisplayStart, ConstantGuideTimeline.DisplayDuration);
            _input = constant.GetComponentInChildren<ConstantVisuals>(true)?.InputField;
            var displayVisual = display.GetComponentInChildren<SignalDisplayVisuals>(true);
            _display = displayVisual != null ? DisplayText?.GetValue(displayVisual) as TMP_Text : null;
            if (_input == null || _input.textComponent == null || _display == null)
            {
                throw new InvalidOperationException("The Constant value field or Signal Display text is unavailable.");
            }

            _input.SetTextWithoutNotify("0");
            _input.textComponent.text = "0";
            _display.text = "0";
            if (_input.placeholder != null)
            {
                _input.placeholder.gameObject.SetActive(false);
            }

            CreateCaret();
            if (!StriderRegistry.TryGetPart(constantRef, out var constantSettings))
            {
                throw new InvalidOperationException("Constant is unavailable.");
            }

            var output = constantSettings.GetSockets().Where(s => s.Type == CableType.Signal).OrderBy(s => s.Id).First().Id;
            var input = displaySettings.GetSockets().Where(s => s.Type == CableType.Signal).OrderBy(s => s.Id).First().Id;
            _port = scene.GetSocket("constant", output);
            var bodyCenter = constant.transform.parent.TransformPoint(_constantBounds.center + constant.transform.localPosition);
            var outward = Vector3.ProjectOnPlane(_port.position - bodyCenter, Vector3.up);
            if (outward.sqrMagnitude < .00001f)
            {
                outward = -_port.forward;
            }

            var direction = Quaternion.LookRotation(-outward.normalized, Vector3.up).eulerAngles;
            _portYaw = direction.y;
            _portPitch = Mathf.DeltaAngle(0, direction.x);
            scene.CalloutSocket("constant", output, PortExplanation, 0, ConstantGuideTimeline.IntroEnd);
            scene.AddSignalCable("signal_cable", "constant", output, "signal_display", input, ConstantGuideTimeline.CableStart, ConstantGuideTimeline.CableDuration);
            scene.Callout(_input.textComponent.transform, _input.textComponent.rectTransform.rect.center, "Push [" + InteractKey() + "] while looking at the top display to modify the value.", ConstantGuideTimeline.EditStart, ConstantGuideTimeline.Duration + 1);
            scene.Highlight(_input.transform, ConstantGuideTimeline.EditStart, ConstantGuideTimeline.Duration + 1);
        }

        internal void Sample(PonderScene scene, float seconds)
        {
            var frame = ConstantGuideTimeline.ValueAt(seconds);
            if (_input.text != frame.Input)
            {
                _input.SetTextWithoutNotify(frame.Input);
            }

            _input.textComponent.text = frame.Input;
            _display.text = frame.Output.ToString(CultureInfo.InvariantCulture);
            _caret.gameObject.SetActive(frame.Editing && (int)(seconds * 2) % 2 == 0);
            var rect = _input.textComponent.rectTransform.rect;
            var glyphs = _input.textComponent.textInfo;
            var right = 0f;
            for (var i = 0; i < glyphs.characterCount; i++)
            {
                if (glyphs.characterInfo[i].isVisible)
                {
                    right = glyphs.characterInfo[i].topRight.x;
                }
            }

            var x = frame.Input.Length == 0 ? rect.center.x : Mathf.Clamp(right + .5f, rect.xMin + .6f, rect.xMax - .6f);
            _caret.anchoredPosition = new Vector2(x - rect.center.x, 0);
            SampleCamera(scene, seconds);
        }

        private void SampleCamera(PonderScene scene, float seconds)
        {
            var overview = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - ConstantGuideTimeline.IntroEnd) / .6f));
            var edit = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - ConstantGuideTimeline.EditStart) / .6f));
            var intro = _constantBounds;
            intro.center += scene.GetObject("constant").transform.localPosition;
            var wide = new Bounds(Vector3.Lerp(intro.center, _assemblyBounds.center, overview), Vector3.Lerp(intro.size, _assemblyBounds.size, overview));
            var frameBounds = new Bounds(Vector3.Lerp(wide.center, _assemblyBounds.center, edit), Vector3.Lerp(wide.size, _assemblyBounds.size, edit));
            var yaw = Mathf.LerpAngle(_portYaw, _portYaw - 22, overview);
            var pitch = Mathf.Lerp(Mathf.Lerp(_portPitch + 8, 36, overview), 48, edit);
            scene.SetCameraFrame(frameBounds, yaw, pitch, Mathf.Lerp(1.45f, 1.3f, overview));
        }

        private void CreateCaret()
        {
            _caret = (RectTransform)new GameObject("Ponder typing caret", typeof(RectTransform)).transform;
            _caret.SetParent(_input.textComponent.transform, false);
            _caret.anchorMin = _caret.anchorMax = new Vector2(.5f, .5f);
            _caret.sizeDelta = new Vector2(.6f, _input.textComponent.fontSize * .85f);
            var image = _caret.gameObject.AddComponent<Image>();
            image.color = _input.textComponent.color;
            image.raycastTarget = false;
            _caret.gameObject.SetActive(false);
        }

        private static string InteractKey()
        {
            foreach (var tooltip in UnityEngine.Object.FindObjectsByType<InteractionTooltipsActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var action = (InteractAction?.GetValue(tooltip) as InputActionReference)?.action;
                if (action == null)
                {
                    continue;
                }

                for (var i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath) || (!binding.effectivePath.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) && !binding.effectivePath.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var label = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        return label;
                    }
                }

                var fallback = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    return fallback;
                }
            }

            return "Interact";
        }

        private static Bounds LocalBounds(GameObject part)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var found = false;
            foreach (var filter in part.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var local = filter.sharedMesh.bounds;
                var matrix = part.transform.parent.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (var i = 0; i < 8; i++)
                {
                    var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!found)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return bounds;
        }
    }
}
