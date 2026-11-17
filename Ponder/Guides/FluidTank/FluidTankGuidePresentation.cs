using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ponder.Core;
using Ponder.Runtime;
using Ships.Interface.Model.Parts;
using Ships.Interface.Services;
using Ships.Interface.Settings;
using Ships.Parts.ChemicalThruster;
using Ships.Parts.Common.Gauge;
using Ships.Parts.EthanolDistiller;
using Ships.Parts.FuelTank;
using Ships.Parts.SignalDisplay;
using StellarStriderLib;
using TMPro;
using UnityEngine;

namespace Ponder.Guides.FluidTank
{
    internal sealed class FluidTankGuidePresentation : MonoBehaviour
    {
        internal const string PortExplanation = "The Fluid Tank can store fluids for later use.";
        private static readonly Color FuelColor = new(.4f, .8f, .5058824f);
        private readonly List<Action<float>> _connections = new();
        private Gauge _gauge;
        private Renderer _tankWindow;
        private Renderer _distillerWindow;
        private TMP_Text _display;
        private ParticleSystem[] _flames;
        private ThrusterFlameVFX _flameEffect;
        private Bounds _tankBounds;
        private Bounds _assemblyBounds;
        private Vector3 _initialTankPosition;
        private float _introYaw;
        internal void Build(PonderScene scene, PartRef reference)
        {
            var parts = new PartSettingsProvider().GetAllPartSettings();
            PartSettings Native(string partName) => GuideParts.Find(parts, partName);
            var tank = scene.AddPart("tank", reference, rotation: new Vector3(0, -90, 0));
            var distillerSettings = Native("EthanolDistiller");
            var thrusterSettings = Native("ChemicalThruster");
            var displaySettings = Native("SignalDisplay");
            var floorSettings = GuideParts.IronFloor(parts);
            var floor = GuideGeometry.CreateFloor(scene, floorSettings, out var floorBounds);
            floor.SetActive(false);
            scene.Show("floor", FluidTankGuideTimeline.IntroEnd);
            scene.Move("floor", Vector3.down * .85f, Vector3.zero, FluidTankGuideTimeline.IntroEnd, .4f);
            var distiller = scene.AddPart("distiller", PartRef.Native(distillerSettings.id));
            var thruster = scene.AddPart("thruster", PartRef.Native(thrusterSettings.id));
            var display = scene.AddPart("signal_display", PartRef.Native(displaySettings.id));
            void Land(string id, GameObject part, Vector3 position, float start, bool initiallyHidden)
            {
                position.y = -BoundsOf(part).min.y + .004f;
                part.transform.localPosition = position + Vector3.up * .45f;
                scene.Move(id, part.transform.localPosition, position, start, FluidTankGuideTimeline.PlaceDuration);
                if (initiallyHidden)
                {
                    part.SetActive(false);
                    scene.Show(id, start);
                }
            }

            Land("tank", tank, Vector3.zero, FluidTankGuideTimeline.IntroEnd + .4f, false);
            Land("distiller", distiller, new Vector3(-1.5f, 0, -.5f), FluidTankGuideTimeline.IntroEnd + .65f, true);
            Land("thruster", thruster, new Vector3(1.5f, 0, -.5f), FluidTankGuideTimeline.ThrusterStart, true);
            Land("signal_display", display, new Vector3(0, 0, -1.1f), FluidTankGuideTimeline.DisplayStart, true);
            _tankBounds = BoundsOf(tank);
            _initialTankPosition = tank.transform.localPosition;
            _assemblyBounds = floorBounds;
            foreach (var part in new[]
            {
                tank,
                distiller,
                thruster,
                display
            }

            )
            {
                _assemblyBounds.Encapsulate(BoundsOf(part));
            }

            _assemblyBounds.Encapsulate(new Vector3(1.5f, 3, -.5f));
            var tankVisual = tank.GetComponentInChildren<FluidTankVisuals>(true);
            _gauge = tank.GetComponentInChildren<Gauge>(true);
            _tankWindow = Field<Renderer>(tankVisual, "windowRenderer");
            var liquid = Field<Material>(tankVisual, "liquidGlassMat");
            if (liquid != null && _tankWindow != null)
            {
                _tankWindow.sharedMaterial = scene.Own(new Material(liquid));
            }

            _distillerWindow = Field<Renderer>(distiller.GetComponentInChildren<EthanolDistillerVisuals>(true), "glassContainerRenderer");
            _display = Field<TMP_Text>(display.GetComponentInChildren<SignalDisplayVisuals>(true), "displayedText");
            if (_gauge == null || _tankWindow == null || _distillerWindow == null || _display == null)
            {
                throw new InvalidOperationException("Native tank dial, fluid windows or Signal Display are unavailable.");
            }

            _flames = thruster.GetComponentsInChildren<ParticleSystem>(true);
            _flameEffect = thruster.GetComponentInChildren<ThrusterFlameVFX>(true);
            if (_flames.Length == 0 || _flameEffect == null)
            {
                throw new InvalidOperationException("The native thruster flame is unavailable.");
            }

            NativeFlame.Prepare(_flameEffect, _flames);
            var inlet = scene.GetSocket("tank", 1);
            var outward = Vector3.ProjectOnPlane(inlet.position - tank.transform.position, Vector3.up);
            _introYaw = Quaternion.LookRotation(-outward.normalized, Vector3.up).eulerAngles.y;
            scene.CalloutSocket("tank", 1, PortExplanation, 0, FluidTankGuideTimeline.IntroEnd);
            NativePipe(scene, "fill_pipe", scene.GetSocket("distiller", 1), inlet, FluidTankGuideTimeline.FillPipeStart);
            NativePipe(scene, "fuel_pipe", scene.GetSocket("tank", 0), scene.GetSocket("thruster", 0), FluidTankGuideTimeline.FuelPipeStart);
            EdgeSupply(scene, "oxygen_pipe", scene.GetSocket("thruster", 1), new Vector3(.8f, .075f, floorBounds.min.z), FluidTankGuideTimeline.OxygenStart, new Color(.55f, .8f, .95f));
            EdgeSupply(scene, "distiller_power", scene.GetSocket("distiller", 2), new Vector3(floorBounds.min.x, .075f, -.625f), FluidTankGuideTimeline.FillPipeStart, new Color(.65f, .52f, .25f));
            var signalInput = displaySettings.GetSockets().Where(s => s.Type == CableType.Signal).OrderBy(s => s.Id).First().Id;
            scene.AddSignalCable("signal_cable", "tank", 2, "signal_display", signalInput, FluidTankGuideTimeline.SignalStart, FluidTankGuideTimeline.SignalReady - FluidTankGuideTimeline.SignalStart);
            scene.Callout("distiller", new Vector3(0, .8f, -.3f), "The powered Ethanol Distiller produces fuel.", FluidTankGuideTimeline.FillPipeStart, FluidTankGuideTimeline.FillPipeStart + 7);
            scene.Callout(_gauge.transform, Vector3.zero, "Fluid flows in and is stored internally. The dial rises as the tank fills.", FluidTankGuideTimeline.FillStart, FluidTankGuideTimeline.ThrusterStart);
            scene.Highlight(_gauge.transform, FluidTankGuideTimeline.FillStart, FluidTankGuideTimeline.ThrusterStart);
            scene.CalloutSocket("thruster", 1, "Supply oxygen to the second port.", FluidTankGuideTimeline.OxygenStart, FluidTankGuideTimeline.DrainStart);
            scene.Callout(_gauge.transform, Vector3.zero, "With fuel and oxygen supplied, the thruster operates automatically and draws fuel from the tank.", FluidTankGuideTimeline.DrainStart, FluidTankGuideTimeline.HalfFull);
            scene.Highlight(_gauge.transform, FluidTankGuideTimeline.DrainStart, FluidTankGuideTimeline.HalfFull);
            scene.CalloutSocket("tank", 2, "The tank's signal port outputs its current fuel percentage.", FluidTankGuideTimeline.HalfFull, FluidTankGuideTimeline.ReadoutStart);
            scene.HighlightSocket("tank", 2, FluidTankGuideTimeline.HalfFull, FluidTankGuideTimeline.ReadoutStart);
            scene.Callout(_display.transform, Vector3.zero, "The tank outputs its current fuel percentage: 50 means half full.", FluidTankGuideTimeline.ReadoutStart, FluidTankGuideTimeline.Resume);
            scene.Callout(_display.transform, Vector3.zero, "As stored fuel is consumed, the percentage falls with the dial and reaches 0 when empty.", FluidTankGuideTimeline.Resume, FluidTankGuideTimeline.Empty);
            scene.Callout(_gauge.transform, Vector3.zero, "The tank is empty. Without fuel, the thruster stops producing thrust.", FluidTankGuideTimeline.Empty, FluidTankGuideTimeline.Duration + 1);
        }

        internal void Sample(PonderScene scene, float seconds)
        {
            var frame = FluidTankGuideTimeline.ValueAt(seconds);
            _gauge.UpdateGauge(frame.Percent, 100);
            _display.text = frame.Signal.ToString(CultureInfo.InvariantCulture);
            var produced = 100 * Mathf.Clamp01((seconds - FluidTankGuideTimeline.FillStart) / (FluidTankGuideTimeline.FillEnd - FluidTankGuideTimeline.FillStart));
            Liquid(_tankWindow, frame.Percent <= 0 ? 0 : .1f + frame.Percent / 90, -produced * .04f + (produced - frame.Percent) * .04f);
            var productionTime = Mathf.Clamp(seconds - FluidTankGuideTimeline.FillStart, 0, FluidTankGuideTimeline.FillEnd - FluidTankGuideTimeline.FillStart);
            Liquid(_distillerWindow, frame.Producing ? 1 : 0, -productionTime * .5f);
            foreach (var connection in _connections)
            {
                connection(seconds);
            }

            _flameEffect.SetIntensity(frame.Thrusting ? 1 : 0);
            foreach (var flame in _flames)
            {
                var started = seconds < FluidTankGuideTimeline.HalfFull ? FluidTankGuideTimeline.DrainStart : FluidTankGuideTimeline.Resume;
                var elapsed = Mathf.Max(0, seconds - started);
                scene.SampleParticles(flame, frame.Thrusting ? Mathf.Min(elapsed, 1 + Mathf.Repeat(elapsed, 1)) : 0);
            }

            SampleCamera(scene, seconds);
        }

        private void SampleCamera(PonderScene scene, float seconds)
        {
            var overview = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - FluidTankGuideTimeline.IntroEnd) / FluidTankGuideTimeline.CameraDuration));
            var intro = _tankBounds;
            intro.center += scene.GetObject("tank").transform.localPosition - _initialTankPosition;
            var bounds = new Bounds(Vector3.Lerp(intro.center, _assemblyBounds.center, overview), Vector3.Lerp(intro.size, _assemblyBounds.size, overview));
            var yaw = Mathf.LerpAngle(_introYaw, -35, overview);
            var pitch = Mathf.Lerp(16, 32, overview);
            var dial = scene.Root.transform.InverseTransformPoint(_gauge.transform.position);
            var display = scene.Root.transform.InverseTransformPoint(_display.transform.position);
            var cameraDuration = FluidTankGuideTimeline.CameraDuration;
            var dialFocus = Mathf.Max(Focus(seconds, FluidTankGuideTimeline.FillStart + 2, FluidTankGuideTimeline.FillStart + 2 + cameraDuration, FluidTankGuideTimeline.FillEnd, FluidTankGuideTimeline.FillEnd + cameraDuration), Focus(seconds, FluidTankGuideTimeline.DrainStart, FluidTankGuideTimeline.DrainStart + cameraDuration, FluidTankGuideTimeline.HalfFull - cameraDuration, FluidTankGuideTimeline.HalfFull));
            dialFocus = Mathf.Max(dialFocus, Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - FluidTankGuideTimeline.Empty) / cameraDuration)));
            var displayFocus = Focus(seconds, FluidTankGuideTimeline.ReadoutStart, FluidTankGuideTimeline.ReadoutStart + cameraDuration, FluidTankGuideTimeline.Resume - cameraDuration / 2, FluidTankGuideTimeline.Resume + cameraDuration / 2);
            var readoutsFocus = Focus(seconds, FluidTankGuideTimeline.Resume, FluidTankGuideTimeline.Resume + cameraDuration, FluidTankGuideTimeline.Empty - cameraDuration, FluidTankGuideTimeline.Empty);
            var readouts = new Bounds(dial, Vector3.one * .2f);
            readouts.Encapsulate(new Bounds(display, Vector3.one * .35f));
            readouts.Expand(.2f);
            bounds = new Bounds(Vector3.Lerp(bounds.center, dial, dialFocus), Vector3.Lerp(bounds.size, Vector3.one * .36f, dialFocus));
            yaw = Mathf.LerpAngle(yaw, -90, dialFocus);
            pitch = Mathf.Lerp(pitch, 34, dialFocus);
            bounds = new Bounds(Vector3.Lerp(bounds.center, display, displayFocus), Vector3.Lerp(bounds.size, Vector3.one * .42f, displayFocus));
            yaw = Mathf.LerpAngle(yaw, -35, displayFocus);
            pitch = Mathf.Lerp(pitch, 68, displayFocus);
            bounds = new Bounds(Vector3.Lerp(bounds.center, readouts.center, readoutsFocus), Vector3.Lerp(bounds.size, readouts.size, readoutsFocus));
            yaw = Mathf.LerpAngle(yaw, -55, readoutsFocus);
            pitch = Mathf.Lerp(pitch, 54, readoutsFocus);
            scene.SetCameraFrame(bounds, yaw, pitch, Mathf.Lerp(1.4f, 1.15f, overview));
        }

        private static float Focus(float seconds, float enterStart, float enterEnd, float leaveStart, float leaveEnd) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - enterStart) / (enterEnd - enterStart))) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - leaveStart) / (leaveEnd - leaveStart))));
        private static T Field<T>(object instance, string name)
            where T : class => instance == null ? null : NativeFields.Find(instance.GetType(), name)?.GetValue(instance) as T;
        private static void Liquid(Renderer renderer, float density, float position) => FluidMaterials.Apply(renderer, FuelColor, density, position);
        private void NativePipe(PonderScene scene, string id, Transform from, Transform to, float start)
        {
            var cable = new NativeCable(scene, id, CableType.Fluid, from, to);
            _connections.Add(seconds =>
            {
                cable.Group.SetActive(seconds >= start);
                if (seconds < start)
                {
                    return;
                }

                cable.HideCovers();
                cable.Update();
                if (cable.Fluid == null)
                {
                    return;
                }

                var frame = FluidTankGuideTimeline.ValueAt(seconds);
                var quantity = id == "fill_pipe" ? 100 * Mathf.Clamp01((seconds - FluidTankGuideTimeline.FillStart) / (FluidTankGuideTimeline.FillEnd - FluidTankGuideTimeline.FillStart)) : 100 - frame.Percent;
                Liquid(cable.Fluid, frame.Percent > 0 ? 1 : 0, -quantity * .04f);
            });
        }

        private void EdgeSupply(PonderScene scene, string id, Transform socket, Vector3 edge, float start, Color color)
        {
            var group = scene.CreateObject(id);
            var line = group.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 32;
            line.widthMultiplier = .045f;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            var material = scene.Own(new Material(Shader.Find("Sprites/Default")));
            material.color = Color.white;
            line.sharedMaterial = material;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .65f), new GradientAlphaKey(0, 1) });
            line.colorGradient = gradient;
            var covers = socket.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cover").ToArray();
            group.SetActive(false);
            _connections.Add(seconds =>
            {
                group.SetActive(seconds >= start);
                if (seconds < start)
                {
                    return;
                }

                foreach (var cover in covers)
                {
                    cover.gameObject.SetActive(false);
                }

                var a = socket.position;
                var b = a - socket.forward * .24f;
                var c = scene.Root.transform.TransformPoint(edge);
                for (var i = 0; i < line.positionCount; i++)
                {
                    var t = i / (float)(line.positionCount - 1);
                    var u = 1 - t;
                    line.SetPosition(i, u * u * a + 2 * u * t * b + t * t * c);
                }
            });
        }

        private static Bounds BoundsOf(GameObject part) => GuideGeometry.BoundsOf(part);
    }
}
