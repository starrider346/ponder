using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ponder.Core;
using Ponder.Runtime;
using Ships.Interface.Model.Parts;
using Ships.Interface.Services;
using Ships.Interface.Settings;
using Ships.Parts.Gyroscope;
using Ships.Parts.SignalDisplay;
using StellarStriderLib;
using TMPro;
using UnityEngine;

namespace Ponder.Guides.ControlSeat
{
    internal sealed class ControlSeatGuidePresentation : MonoBehaviour
    {
        private readonly TMP_Text[] _displays = new TMP_Text[6];
        private readonly List<Action<ControlSeatFrame>> _sticks = new();
        private Transform _basicBar;
        private Transform _basicWheel;
        private Transform _gyroBar;
        private Transform _gyroWheel;
        private Quaternion _basicBarRotation;
        private Quaternion _basicWheelRotation;
        private Quaternion _gyroBarRotation;
        private Quaternion _gyroWheelRotation;
        private Bounds _seatBounds;
        private Bounds _basicBounds;
        private Bounds _advancedBounds;
        private Bounds _connectedBounds;
        private Bounds _builtInBounds;
        internal void Build(PonderScene scene, PartRef basicRef, PartRef advancedRef)
        {
            var parts = new PartSettingsProvider().GetAllPartSettings();
            PartSettings Native(string partName) => GuideParts.Find(parts, partName);
            var basic = scene.AddPart("basic_seat", basicRef);
            var advanced = scene.AddPart("advanced_seat", advancedRef);
            var gyro = scene.AddPart("gyroscope", PartRef.Native(Native("Gyroscope").id), rotation: new Vector3(0, 180, 0));
            var displaySettings = Native("SignalDisplay");
            var floorSettings = GuideParts.IronFloor(parts);
            var floor = scene.CreateObject("floor");
            Bounds baseFloor = default;
            Bounds fullFloor = default;
            var baseFound = false;
            var fullFound = false;
            for (var x = 0; x < 3; x++)
            {
                for (var z = 1; z < 4; z++)
                {
                    var tile = scene.AddPart("floor_" + x + "_" + z, PartRef.Native(floorSettings.id));
                    var local = BoundsOf(tile);
                    tile.transform.SetParent(floor.transform, false);
                    tile.transform.localPosition = new Vector3((x - 1.5f) * local.size.x - local.center.x, -local.max.y, (z - 1.5f) * local.size.z - local.center.z);
                    var placed = BoundsOf(tile);
                    if (!fullFound)
                    {
                        fullFloor = placed;
                        fullFound = true;
                    }
                    else
                    {
                        fullFloor.Encapsulate(placed);
                    }

                    if (z == 1 || z == 2)
                    {
                        if (!baseFound)
                        {
                            baseFloor = placed;
                            baseFound = true;
                        }
                        else
                        {
                            baseFloor.Encapsulate(placed);
                        }
                    }
                    else
                    {
                        tile.SetActive(false);
                        scene.Show("floor_" + x + "_" + z, ControlSeatGuideTimeline.GyroStart);
                    }
                }
            }

            floor.SetActive(false);
            scene.Show("floor", ControlSeatGuideTimeline.ForwardStart);
            void Place(string id, GameObject part, Vector3 position, float start)
            {
                position.y = -BoundsOf(part).min.y + .004f;
                part.transform.localPosition = position;
                if (start <= 0)
                {
                    return;
                }

                part.SetActive(false);
                scene.Show(id, start);
                scene.Move(id, position + Vector3.up * .4f, position, start, .35f);
            }

            Place("basic_seat", basic, new Vector3(0, 0, .25f), 0);
            Place("advanced_seat", advanced, new Vector3(0, 0, .25f), ControlSeatGuideTimeline.AdvancedStart);
            Place("gyroscope", gyro, new Vector3(-1.45f, 0, 1.3f), ControlSeatGuideTimeline.GyroStart);
            scene.Show("basic_seat", ControlSeatGuideTimeline.AdvancedStart, false);
            foreach (var seat in new[]
            {
                basic,
                advanced
            }

            )
            {
                foreach (var child in seat.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "VelocityHologram" || child.name == "HudPos")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            _seatBounds = BoundsOf(basic);
            _basicBounds = baseFloor;
            _basicBounds.Encapsulate(_seatBounds);
            _advancedBounds = baseFloor;
            _advancedBounds.Encapsulate(BoundsOf(advanced));
            _connectedBounds = fullFloor;
            _connectedBounds.Encapsulate(BoundsOf(advanced));
            _connectedBounds.Encapsulate(BoundsOf(gyro));
            var input = displaySettings.GetSockets().Where(s => s.Type == CableType.Signal).OrderBy(s => s.Id).First().Id;
            for (byte i = 0; i < 6; i++)
            {
                var id = "output_" + i;
                var display = scene.AddPart(id, PartRef.Native(displaySettings.id), rotation: new Vector3(0, 180, 0));
                var advancedZ = scene.Root.transform.InverseTransformPoint(scene.GetSocket("advanced_seat", i).position).z;
                var initialZ = i < 2 ? scene.Root.transform.InverseTransformPoint(scene.GetSocket("basic_seat", i).position).z : advancedZ;
                Place(id, display, new Vector3(-1.25f, 0, initialZ), i < 2 ? ControlSeatGuideTimeline.ForwardStart : ControlSeatGuideTimeline.TranslationStart);
                _displays[i] = Field<TMP_Text>(display.GetComponentInChildren<SignalDisplayVisuals>(true), "displayedText") ?? throw new InvalidOperationException("The native Signal Display text is unavailable.");
                if (i < 2)
                {
                    _basicBounds.Encapsulate(BoundsOf(display));
                }

                if (i < 2)
                {
                    var initial = display.transform.localPosition;
                    scene.Move(id, initial, new Vector3(initial.x, initial.y, advancedZ), ControlSeatGuideTimeline.TranslationStart, .35f);
                }

                _advancedBounds.Encapsulate(BoundsOf(display));
                if (i < 3)
                {
                    _connectedBounds.Encapsulate(BoundsOf(display));
                }

                scene.Show(id, ControlSeatGuideTimeline.GyroStart, false);
                if (i < 2)
                {
                    scene.AddSignalCable("basic_signal_" + i, "basic_seat", i, id, input, ControlSeatGuideTimeline.ForwardStart + .5f, 1);
                    scene.Show("basic_signal_" + i, ControlSeatGuideTimeline.AdvancedStart, false);
                }

                scene.AddSignalCable("advanced_signal_" + i, "advanced_seat", i, id, input, ControlSeatGuideTimeline.TranslationStart + .5f, 1);
                scene.Show("advanced_signal_" + i, ControlSeatGuideTimeline.GyroStart, false);
            }

            for (var i = 0; i < 2; i++)
            {
                scene.Show("output_" + i, ControlSeatGuideTimeline.AdvancedStart, false);
                scene.Show("output_" + i, ControlSeatGuideTimeline.TranslationStart);
                scene.Show("output_" + i, ControlSeatGuideTimeline.GyroStart, false);
            }

            var basicAnimator = basic.GetComponentInChildren<GyroscopeAnimator>(true);
            var gyroAnimator = gyro.GetComponentInChildren<GyroscopeAnimator>(true);
            _basicBar = Field<Transform>(basicAnimator, "gyroBar");
            _basicWheel = Field<Transform>(basicAnimator, "gyroWheel");
            _gyroBar = Field<Transform>(gyroAnimator, "gyroBar");
            _gyroWheel = Field<Transform>(gyroAnimator, "gyroWheel");
            if (_basicBar == null || _basicWheel == null || _gyroBar == null || _gyroWheel == null)
            {
                throw new InvalidOperationException("Native built-in or standalone gyroscope geometry is unavailable.");
            }

            _basicBarRotation = _basicBar.localRotation;
            _basicWheelRotation = _basicWheel.localRotation;
            _gyroBarRotation = _gyroBar.localRotation;
            _gyroWheelRotation = _gyroWheel.localRotation;
            _builtInBounds = new Bounds(scene.Root.transform.InverseTransformPoint(_basicBar.position), Vector3.one * .8f);
            AddSticks(basic, false);
            AddSticks(advanced, true);
            scene.AddSignalCable("roll_signal", "advanced_seat", ControlSeatGuideTimeline.Roll, "gyroscope", ControlSeatGuideTimeline.GyroRoll, ControlSeatGuideTimeline.RollStart + .5f, 1);
            scene.AddSignalCable("yaw_signal", "advanced_seat", ControlSeatGuideTimeline.Yaw, "gyroscope", ControlSeatGuideTimeline.GyroYaw, ControlSeatGuideTimeline.YawStart + .5f, 1);
            scene.AddSignalCable("pitch_signal", "advanced_seat", ControlSeatGuideTimeline.Pitch, "gyroscope", ControlSeatGuideTimeline.GyroPitch, ControlSeatGuideTimeline.PitchStart + .5f, 1);
            PowerSupply(scene, scene.GetSocket("gyroscope", ControlSeatGuideTimeline.GyroPower));
            scene.Callout("basic_seat", new Vector3(0, .7f, 0), "Basic Pilot Seat: two movement signals and a built-in gyroscope.", 0, ControlSeatGuideTimeline.ForwardStart);
            scene.CalloutSocket("basic_seat", ControlSeatGuideTimeline.BasicForward, "W = +1; S = -1; release = 0 (default keys).", ControlSeatGuideTimeline.ForwardStart, ControlSeatGuideTimeline.VerticalStart);
            scene.HighlightSocket("basic_seat", ControlSeatGuideTimeline.BasicForward, ControlSeatGuideTimeline.ForwardStart, ControlSeatGuideTimeline.VerticalStart);
            scene.Callout(_displays[1].transform, Vector3.zero, "The connected display shows the forward/back signal.", ControlSeatGuideTimeline.ForwardStart, ControlSeatGuideTimeline.VerticalStart);
            scene.CalloutSocket("basic_seat", ControlSeatGuideTimeline.BasicVertical, "Space = +1; Ctrl = -1; release = 0 (default keys).", ControlSeatGuideTimeline.VerticalStart, ControlSeatGuideTimeline.BuiltInGyroStart);
            scene.HighlightSocket("basic_seat", ControlSeatGuideTimeline.BasicVertical, ControlSeatGuideTimeline.VerticalStart, ControlSeatGuideTimeline.BuiltInGyroStart);
            scene.Callout(_displays[0].transform, Vector3.zero, "The other display shows the up/down signal.", ControlSeatGuideTimeline.VerticalStart, ControlSeatGuideTimeline.BuiltInGyroStart);
            scene.Callout(_basicBar, Vector3.zero, "This small built-in gyroscope turns the ship from your steering input.", ControlSeatGuideTimeline.BuiltInGyroStart, ControlSeatGuideTimeline.AdvancedStart);
            scene.Highlight(_basicBar, ControlSeatGuideTimeline.BuiltInGyroStart, ControlSeatGuideTimeline.AdvancedStart);
            scene.Callout("advanced_seat", new Vector3(0, .5f, 0), "Advanced Controls: six outputs, with no built-in gyroscope.", ControlSeatGuideTimeline.AdvancedStart, ControlSeatGuideTimeline.TranslationStart);
            var movementLabels = new[]
            {
                "Forward / back: W / S (default keys).",
                "Up / down: Space / Ctrl (default keys).",
                "Left / right: lateral pilot input."
            };
            var rotationLabels = new[]
            {
                "Roll",
                "Yaw",
                "Pitch"
            };
            for (byte i = 0; i < 3; i++)
            {
                scene.CalloutSocket("advanced_seat", i, movementLabels[i], ControlSeatGuideTimeline.TranslationStart, ControlSeatGuideTimeline.RotationStart);
                scene.HighlightSocket("advanced_seat", i, ControlSeatGuideTimeline.TranslationStart, ControlSeatGuideTimeline.RotationStart);
                scene.CalloutSocket("advanced_seat", (byte)(i + 3), rotationLabels[i] + " output: the pilot's rotation command.", ControlSeatGuideTimeline.RotationStart, ControlSeatGuideTimeline.GyroStart);
                scene.HighlightSocket("advanced_seat", (byte)(i + 3), ControlSeatGuideTimeline.RotationStart, ControlSeatGuideTimeline.GyroStart);
            }

            scene.CalloutSocket("gyroscope", ControlSeatGuideTimeline.GyroPower, "Supply power to the separate Gyroscope from your ship's powered network.", ControlSeatGuideTimeline.GyroStart, ControlSeatGuideTimeline.RollStart);
            void Axis(byte output, byte target, string axis, float start, float end)
            {
                scene.CalloutSocket("advanced_seat", output, axis + " output: connect this signal to the matching gyro axis.", start, end);
                scene.CalloutSocket("gyroscope", target, axis + " input on the powered Gyroscope.", start, end);
                scene.HighlightSocket("advanced_seat", output, start, end);
                scene.HighlightSocket("gyroscope", target, start, end);
            }

            Axis(ControlSeatGuideTimeline.Roll, ControlSeatGuideTimeline.GyroRoll, "Roll", ControlSeatGuideTimeline.RollStart, ControlSeatGuideTimeline.YawStart);
            Axis(ControlSeatGuideTimeline.Yaw, ControlSeatGuideTimeline.GyroYaw, "Yaw", ControlSeatGuideTimeline.YawStart, ControlSeatGuideTimeline.PitchStart);
            Axis(ControlSeatGuideTimeline.Pitch, ControlSeatGuideTimeline.GyroPitch, "Pitch", ControlSeatGuideTimeline.PitchStart, ControlSeatGuideTimeline.SteeringStart);
            scene.Callout(_gyroBar, Vector3.zero, "Pilot roll, yaw and pitch now drive this powered gyroscope.", ControlSeatGuideTimeline.SteeringStart, ControlSeatGuideTimeline.ReleaseStart);
            scene.Highlight(_gyroBar, ControlSeatGuideTimeline.SteeringStart, ControlSeatGuideTimeline.ReleaseStart);
            scene.Callout("advanced_seat", new Vector3(0, .6f, 0), "Release returns the signals to 0. Use the movement outputs for propulsion controls.", ControlSeatGuideTimeline.ReleaseStart, ControlSeatGuideTimeline.Duration + 1);
        }

        internal void Sample(PonderScene scene, float seconds)
        {
            var frame = ControlSeatGuideTimeline.ValueAt(seconds);
            scene.SubjectLabel = frame.Advanced ? "Advanced Controls" : "Basic Pilot Seat";
            for (var i = 0; i < _displays.Length; i++)
            {
                _displays[i].text = frame.Output(i).ToString("0", CultureInfo.InvariantCulture);
            }

            foreach (var sticks in _sticks)
            {
                sticks(frame);
            }

            var basicPhase = frame.Advanced ? 8 : frame.GyroSeconds;
            AnimateGyro(_basicBar, _basicWheel, _basicBarRotation, _basicWheelRotation, basicPhase);
            AnimateGyro(_gyroBar, _gyroWheel, _gyroBarRotation, _gyroWheelRotation, frame.Advanced ? frame.GyroSeconds : 0);
            SampleCamera(scene, seconds);
        }

        private void SampleCamera(PonderScene scene, float seconds)
        {
            var assembly = Smooth(seconds, ControlSeatGuideTimeline.ForwardStart);
            var advanced = Smooth(seconds, ControlSeatGuideTimeline.AdvancedStart);
            var outputs = Smooth(seconds, ControlSeatGuideTimeline.TranslationStart);
            var connected = Smooth(seconds, ControlSeatGuideTimeline.GyroStart);
            var bounds = Blend(_seatBounds, _basicBounds, assembly);
            bounds = Blend(bounds, _advancedBounds, advanced * (1 - outputs));
            bounds = Blend(bounds, _advancedBounds, outputs);
            bounds = Blend(bounds, _connectedBounds, connected);
            var gyroFocus = Smooth(seconds, ControlSeatGuideTimeline.BuiltInGyroStart) * (1 - Smooth(seconds, ControlSeatGuideTimeline.AdvancedStart - .6f));
            bounds = Blend(bounds, _builtInBounds, gyroFocus);
            scene.SetCameraFrame(bounds, 55, Mathf.Lerp(36, 14, gyroFocus), 1.25f);
        }

        private void AddSticks(GameObject seat, bool advanced)
        {
            var animator = seat.GetComponentInChildren<FlightSticksAnimator>(true);
            var left = Field<Transform>(animator, "leftStick");
            var right = Field<Transform>(animator, "rightStick");
            if (left == null || right == null)
            {
                throw new InvalidOperationException("Native pilot seat control sticks are unavailable.");
            }

            var position = left.localPosition;
            var leftRotation = left.localRotation;
            var rightRotation = right.localRotation;
            _sticks.Add(frame =>
            {
                var active = frame.Advanced == advanced;
                left.localPosition = position + Vector3.up * (active ? frame.Up * .08f : 0);
                left.localRotation = leftRotation * Quaternion.Euler(active ? frame.Forward * 30 : 0, 0, active ? -frame.Right * 30 : 0);
                right.localRotation = rightRotation * Quaternion.Euler(active ? frame.Pitch * 30 : 0, active ? frame.Yaw * 30 : 0, active ? -frame.Roll * 30 : 0);
            });
        }

        private static void AnimateGyro(Transform bar, Transform wheel, Quaternion barRotation, Quaternion wheelRotation, float seconds)
        {
            bar.localRotation = barRotation * Quaternion.Euler(0, 0, 38 * Mathf.Sin(seconds * 1.4f));
            wheel.localRotation = wheelRotation * Quaternion.Euler(seconds * 540, 0, 0);
        }

        private static void PowerSupply(PonderScene scene, Transform socket)
        {
            var group = scene.CreateObject("gyro_power");
            var line = group.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 32;
            line.widthMultiplier = .045f;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            var material = scene.Own(new Material(Shader.Find("Sprites/Default")));
            material.color = Color.white;
            line.sharedMaterial = material;
            var color = new Color(.65f, .52f, .25f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .65f), new GradientAlphaKey(0, 1) });
            line.colorGradient = gradient;
            var a = socket.position;
            var b = a - socket.forward * .24f;
            var c = scene.Root.transform.TransformPoint(new Vector3(-2.25f, .075f, -1));
            for (var i = 0; i < line.positionCount; i++)
            {
                var t = i / (float)(line.positionCount - 1);
                var u = 1 - t;
                line.SetPosition(i, u * u * a + 2 * u * t * b + t * t * c);
            }

            group.SetActive(false);
            scene.Show("gyro_power", ControlSeatGuideTimeline.GyroStart + .5f);
        }

        private static float Smooth(float seconds, float start) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - start) / .6f));
        private static Bounds Blend(Bounds a, Bounds b, float phase) => new(Vector3.Lerp(a.center, b.center, phase), Vector3.Lerp(a.size, b.size, phase));
        private static T Field<T>(object instance, string name)
            where T : class => instance == null ? null : NativeFields.Find(instance.GetType(), name)?.GetValue(instance) as T;
        private static Bounds BoundsOf(GameObject part) => GuideGeometry.BoundsOf(part);
    }
}
