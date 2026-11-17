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
using Ships.Parts.Constant;
using Ships.Parts.FluidValve;
using Ships.Parts.FuelTank;
using StellarStriderLib;
using TMPro;
using UnityEngine;

namespace Ponder.Guides.Thruster
{
    internal sealed class ThrusterGuidePresentation : MonoBehaviour
    {
        private static readonly Color FuelColor = new(.4f, .8f, .5058824f);
        private static readonly Color OxygenColor = new(.55f, .8f, .95f);
        private readonly List<Action<float>> _pipes = new();
        private TMP_InputField _input;
        private Transform _handle;
        private ParticleSystem[] _flames;
        private ThrusterFlameVFX _flameEffect;
        private Bounds _introBounds;
        private Bounds _assemblyBounds;
        private Bounds _controlBounds;
        private Vector3 _thrusterPosition;
        internal void Build(PonderScene scene, PartRef reference)
        {
            var parts = new PartSettingsProvider().GetAllPartSettings();
            PartSettings Native(string partName) => GuideParts.Find(parts, partName);
            var thruster = scene.AddPart("thruster", reference);
            var tankSettings = Native("FluidTank");
            var fuel = scene.AddPart("fuel_source", PartRef.Native(tankSettings.id), rotation: new Vector3(0, -90, 0));
            var oxygen = scene.AddPart("oxygen_source", PartRef.Native(tankSettings.id), rotation: new Vector3(0, -90, 0));
            var valve = scene.AddPart("valve", PartRef.Native(Native("FluidValve").id), rotation: new Vector3(0, 90, 0));
            var constantSettings = Native("Constant");
            var constant = scene.AddPart("constant", PartRef.Native(constantSettings.id), rotation: new Vector3(0, 180, 0));
            var floorSettings = GuideParts.IronFloor(parts);
            var floor = GuideGeometry.CreateFloor(scene, floorSettings, out var floorBounds);
            floor.SetActive(false);
            scene.Show("floor", ThrusterGuideTimeline.IntroEnd);
            scene.Move("floor", Vector3.down * .85f, Vector3.zero, ThrusterGuideTimeline.IntroEnd, .4f);
            void Place(string id, GameObject part, Vector3 position, float at, bool hidden)
            {
                position.y = -BoundsOf(part).min.y + .004f;
                part.transform.localPosition = position;
                scene.Move(id, position + Vector3.up * .4f, position, at, .35f);
                if (hidden)
                {
                    part.SetActive(false);
                    scene.Show(id, at);
                }
            }

            Place("thruster", thruster, new Vector3(1.25f, 0, 0), ThrusterGuideTimeline.IntroEnd, false);
            Place("fuel_source", fuel, new Vector3(-1.5f, 0, .5f), ThrusterGuideTimeline.IntroEnd, true);
            Place("oxygen_source", oxygen, new Vector3(-1.5f, 0, -.85f), ThrusterGuideTimeline.OxygenStart, true);
            Place("valve", valve, new Vector3(-.2f, 0, .125f), ThrusterGuideTimeline.ValveStart, true);
            Place("constant", constant, new Vector3(-.2f, 0, .85f), ThrusterGuideTimeline.SignalStart, true);
            _introBounds = BoundsOf(thruster);
            _thrusterPosition = thruster.transform.localPosition;
            _assemblyBounds = floorBounds;
            foreach (var part in new[]
            {
                thruster,
                fuel,
                oxygen,
                valve,
                constant
            }

            )
            {
                _assemblyBounds.Encapsulate(BoundsOf(part));
            }

            _assemblyBounds.Encapsulate(new Bounds(new Vector3(1.25f, 1.8f, 0), new Vector3(.9f, 2.2f, .9f)));
            _controlBounds = BoundsOf(constant);
            _controlBounds.Encapsulate(BoundsOf(valve));
            _controlBounds.Encapsulate(_introBounds);
            _controlBounds.Encapsulate(new Bounds(new Vector3(1.25f, 1.8f, 0), new Vector3(.9f, 2.2f, .9f)));
            PrepareTank(scene, fuel, FuelColor);
            PrepareTank(scene, oxygen, OxygenColor);
            _input = constant.GetComponentInChildren<ConstantVisuals>(true)?.InputField;
            _handle = Field<Transform>(valve.GetComponentInChildren<FluidValveVisual>(true), "valveHandle");
            _flames = thruster.GetComponentsInChildren<ParticleSystem>(true);
            _flameEffect = thruster.GetComponentInChildren<ThrusterFlameVFX>(true);
            if (_input?.textComponent == null || _handle == null || _flames.Length == 0 || _flameEffect == null)
            {
                throw new InvalidOperationException("Native Constant, Valve handle or thruster flame is unavailable.");
            }

            if (_input.placeholder != null)
            {
                _input.placeholder.gameObject.SetActive(false);
            }

            NativeFlame.Prepare(_flameEffect, _flames);
            Pipe(scene, "direct_fuel_pipe", scene.GetSocket("fuel_source", 0), scene.GetSocket("thruster", 0), ThrusterGuideTimeline.IntroEnd + .5f, ThrusterGuideTimeline.ValveStart, FuelColor);
            Pipe(scene, "oxygen_pipe", scene.GetSocket("oxygen_source", 0), scene.GetSocket("thruster", 1), ThrusterGuideTimeline.OxygenStart + .5f, float.PositiveInfinity, OxygenColor);
            Pipe(scene, "valve_supply_pipe", scene.GetSocket("fuel_source", 0), scene.GetSocket("valve", 2), ThrusterGuideTimeline.ValveStart + .5f, float.PositiveInfinity, FuelColor);
            Pipe(scene, "valve_fuel_pipe", scene.GetSocket("valve", 3), scene.GetSocket("thruster", 0), ThrusterGuideTimeline.ValveStart + .5f, float.PositiveInfinity, FuelColor);
            var output = constantSettings.GetSockets().Where(s => s.Type == CableType.Signal).OrderBy(s => s.Id).First().Id;
            scene.AddSignalCable("throttle_signal", "constant", output, "valve", 0, ThrusterGuideTimeline.SignalStart + .5f, 1);
            scene.Callout("thruster", new Vector3(0, .5f, 0), "Burn ethanol and O2 from separate fluid networks.", 0, ThrusterGuideTimeline.IntroEnd);
            scene.Callout("fuel_source", new Vector3(0, .6f, 0), "Ethanol source: a prefilled Fluid Tank.", ThrusterGuideTimeline.IntroEnd, ThrusterGuideTimeline.OxygenStart);
            scene.CalloutSocket("thruster", 0, "Fuel connects to this fluid network.", ThrusterGuideTimeline.IntroEnd, ThrusterGuideTimeline.OxygenStart);
            scene.Callout("oxygen_source", new Vector3(0, .6f, 0), "O2 source: a separate prefilled Fluid Tank.", ThrusterGuideTimeline.OxygenStart, ThrusterGuideTimeline.BurnStart);
            scene.CalloutSocket("thruster", 1, "Oxygen connects to the other fluid network.", ThrusterGuideTimeline.OxygenStart, ThrusterGuideTimeline.BurnStart);
            scene.Callout("thruster", new Vector3(0, .8f, 0), "Full burn uses 0.05 ethanol and 0.05 O2 per second.", ThrusterGuideTimeline.BurnStart, ThrusterGuideTimeline.ValveStart);
            scene.Callout("valve", new Vector3(0, .15f, 0), "Put the Valve in the fuel line. Keep oxygen unrestricted.", ThrusterGuideTimeline.ValveStart, ThrusterGuideTimeline.SignalStart);
            scene.Highlight("valve", ThrusterGuideTimeline.ValveStart, ThrusterGuideTimeline.SignalStart);
            scene.CalloutSocket("valve", 0, "The Constant sets the fuel flow limit per second.", ThrusterGuideTimeline.SignalStart, ThrusterGuideTimeline.OffStart);
            scene.HighlightSocket("valve", 0, ThrusterGuideTimeline.SignalStart, ThrusterGuideTimeline.OffStart);
            void Setting(float start, float end, string text)
            {
                scene.Callout(_input.textComponent.transform, Vector3.zero, text, start, end);
            }

            Setting(ThrusterGuideTimeline.OffStart, ThrusterGuideTimeline.QuarterStart, "0 = closed. After buffered fuel is consumed, thrust is off.");
            Setting(ThrusterGuideTimeline.QuarterStart, ThrusterGuideTimeline.HalfStart, "0.0125 fuel/s = 25% steady thrust.");
            Setting(ThrusterGuideTimeline.HalfStart, ThrusterGuideTimeline.ThreeQuarterStart, "0.025 fuel/s = 50% steady thrust.");
            Setting(ThrusterGuideTimeline.ThreeQuarterStart, ThrusterGuideTimeline.MaxStart, "0.0375 fuel/s = 75% steady thrust.");
            Setting(ThrusterGuideTimeline.MaxStart, ThrusterGuideTimeline.ShutdownStart, "0.05 fuel/s = 100% thrust. Higher flow limits add no thrust.");
            Setting(ThrusterGuideTimeline.ShutdownStart, ThrusterGuideTimeline.Duration + 1, "0 = off again. Buffers and supply shortages affect the response time.");
            scene.Highlight(_input.transform, ThrusterGuideTimeline.OffStart, ThrusterGuideTimeline.Duration + 1);
        }

        internal void Sample(PonderScene scene, float seconds)
        {
            var frame = ThrusterGuideTimeline.ValueAt(seconds);
            var value = frame.Flow.ToString("0.####", CultureInfo.InvariantCulture);
            _input.SetTextWithoutNotify(value);
            _input.textComponent.text = value;
            _handle.localRotation = Quaternion.AngleAxis(frame.Flow * 180, Vector3.up);
            foreach (var pipe in _pipes)
            {
                pipe(seconds);
            }

            _flameEffect.SetIntensity(frame.Throttle);
            foreach (var flame in _flames)
            {
                var started = seconds < ThrusterGuideTimeline.ValveStart ? ThrusterGuideTimeline.BurnStart : seconds < ThrusterGuideTimeline.OffStart ? ThrusterGuideTimeline.SignalStart + .5f : seconds < ThrusterGuideTimeline.HalfStart ? ThrusterGuideTimeline.QuarterStart : seconds < ThrusterGuideTimeline.ThreeQuarterStart ? ThrusterGuideTimeline.HalfStart : seconds < ThrusterGuideTimeline.MaxStart ? ThrusterGuideTimeline.ThreeQuarterStart : ThrusterGuideTimeline.MaxStart;
                var elapsed = Mathf.Max(0, seconds - started);
                scene.SampleParticles(flame, frame.Burning ? Mathf.Min(elapsed, 1 + Mathf.Repeat(elapsed, 1)) : 0);
            }

            SampleCamera(scene, seconds);
        }

        private void SampleCamera(PonderScene scene, float seconds)
        {
            var overview = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - ThrusterGuideTimeline.IntroEnd) / .6f));
            var controls = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - ThrusterGuideTimeline.SignalStart) / .6f));
            var intro = _introBounds;
            intro.center += scene.GetObject("thruster").transform.localPosition - _thrusterPosition;
            var bounds = new Bounds(Vector3.Lerp(intro.center, _assemblyBounds.center, overview), Vector3.Lerp(intro.size, _assemblyBounds.size, overview));
            bounds = new Bounds(Vector3.Lerp(bounds.center, _controlBounds.center, controls), Vector3.Lerp(bounds.size, _controlBounds.size, controls));
            var valueFocus = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - ThrusterGuideTimeline.OffStart) / .6f)) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - ThrusterGuideTimeline.QuarterStart + .6f) / .6f)));
            var valuePoint = scene.Root.transform.InverseTransformPoint(_input.textComponent.transform.position);
            bounds = new Bounds(Vector3.Lerp(bounds.center, valuePoint, valueFocus), Vector3.Lerp(bounds.size, Vector3.one * .5f, valueFocus));
            scene.SetCameraFrame(bounds, -25, Mathf.Lerp(Mathf.Lerp(24, 42, overview), 68, valueFocus), 1.2f);
        }

        private static void PrepareTank(PonderScene scene, GameObject tank, Color color)
        {
            var visual = tank.GetComponentInChildren<FluidTankVisuals>(true);
            var window = Field<Renderer>(visual, "windowRenderer");
            var liquid = Field<Material>(visual, "liquidGlassMat");
            var gauge = tank.GetComponentInChildren<Gauge>(true);
            if (window == null || liquid == null || gauge == null)
            {
                throw new InvalidOperationException("The native supply tank visuals are unavailable.");
            }

            window.sharedMaterial = scene.Own(new Material(liquid));
            Liquid(window, color, 1, 0);
            gauge.UpdateGauge(100, 100);
        }

        private void Pipe(PonderScene scene, string id, Transform from, Transform to, float start, float end, Color color)
        {
            var cable = new NativeCable(scene, id, CableType.Fluid, from, to);
            _pipes.Add(seconds =>
            {
                var visible = seconds >= start && seconds < end;
                cable.Group.SetActive(visible);
                if (!visible)
                {
                    return;
                }

                cable.HideCovers();
                cable.Update();
                if (cable.Fluid == null)
                {
                    return;
                }

                var flow = ThrusterGuideTimeline.ValueAt(seconds).Flow;
                Liquid(cable.Fluid, color, 1, -(seconds - start) * flow);
            });
        }

        private static T Field<T>(object instance, string name)
            where T : class => instance == null ? null : NativeFields.Find(instance.GetType(), name)?.GetValue(instance) as T;
        private static void Liquid(Renderer renderer, Color color, float density, float position) => FluidMaterials.Apply(renderer, color, density, position);
        private static Bounds BoundsOf(GameObject part) => GuideGeometry.BoundsOf(part);
    }
}
