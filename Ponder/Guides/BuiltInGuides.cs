using System.Linq;
using Ponder.Core;
using Ponder.Guides.Constant;
using Ponder.Guides.ControlSeat;
using Ponder.Guides.FluidTank;
using Ponder.Guides.Thruster;
using Ships.Interface.Services;
using Ships.Interface.Settings;
using StellarStriderLib;

namespace Ponder.Guides
{
    internal static class BuiltInGuides
    {
        internal static void Ensure(PartSettings part)
        {
            if (part != null && part.name == "FluidTank")
            {
                EnsureFluidTank(part);
            }

            if (part != null && part.name == "ChemicalThruster")
            {
                EnsureThruster(part);
            }

            if (part != null && (part.name == "BasicPilotSeat" || part.name == "MainControlAdvanced"))
            {
                EnsureControlSeats();
            }

            if (part == null || part.name != "Constant" || PonderRegistry.Catalog.Contains("ponder:constant_signal"))
            {
                return;
            }

            var reference = PartRef.Native(part.id);
            PonderRegistry.Create("ponder", typeof(Plugin).Assembly).RegisterGuide(new GuideDefinition
            {
                Id = "constant_signal",
                Title = "Constant: values and signal output",
                NativeParts = new[] { reference },
                Steps = new[] { new GuideStep { Duration = ConstantGuideTimeline.IntroEnd, Caption = ConstantGuidePresentation.PortExplanation }, new GuideStep { Duration = ConstantGuideTimeline.EditStart - ConstantGuideTimeline.IntroEnd, Caption = "Place a Constant and a Signal Display on iron hull, then connect their signal ports with a cable." }, new GuideStep { Duration = ConstantGuideTimeline.Duration - ConstantGuideTimeline.EditStart, Caption = "Change the Constant's value. The connected Signal Display follows its output." } },
                BuildScene = scene =>
            {
                var presentation = scene.CreateObject("guide_presentation").AddComponent<ConstantGuidePresentation>();
                presentation.Build(scene, reference);
            },
                Sample = (scene, seconds) => scene.GetObject("guide_presentation").GetComponent<ConstantGuidePresentation>().Sample(scene, seconds)
            });
        }

        private static void EnsureControlSeats()
        {
            if (PonderRegistry.Catalog.Contains("ponder:control_seats"))
            {
                return;
            }

            var parts = new PartSettingsProvider().GetAllPartSettings();
            var basic = parts.FirstOrDefault(p => p.name == "BasicPilotSeat");
            var advanced = parts.FirstOrDefault(p => p.name == "MainControlAdvanced");
            if (basic == null || advanced == null)
            {
                return;
            }

            PonderRegistry.Create("ponder", typeof(Plugin).Assembly).RegisterGuide(new GuideDefinition { Id = "control_seats", Title = "Control seats: movement outputs and gyroscopes", NativeParts = new[] { PartRef.Native(basic.id), PartRef.Native(advanced.id) }, Steps = new[] { new GuideStep { Duration = 8, Caption = "The wooden Basic Pilot Seat has two movement outputs and a small built-in gyroscope." }, new GuideStep { Duration = 12, Caption = "Forward output: W gives +1, S gives -1, and releasing gives 0. These are the default keys." }, new GuideStep { Duration = 12, Caption = "Vertical output: Space gives +1, Ctrl gives -1, and releasing gives 0 with the default bindings." }, new GuideStep { Duration = 12, Caption = "The built-in gyroscope turns the ship from your steering input. The movement outputs can control propulsion." }, new GuideStep { Duration = 8, Caption = "Advanced Controls has no built-in gyroscope. Its six signal outputs give you more control over connected equipment." }, new GuideStep { Duration = 12, Caption = "Three movement outputs: forward/back, up/down, and left/right. The extra lateral output allows sideways propulsion control." }, new GuideStep { Duration = 12, Caption = "Three rotation outputs: roll, yaw, and pitch. Each reports the corresponding pilot input independently." }, new GuideStep { Duration = 8, Caption = "Add a separate Gyroscope on the same ship. Supply power through the cable leading off the platform." }, new GuideStep { Duration = 8, Caption = "Connect the seat's Roll output to the Gyroscope's roll input." }, new GuideStep { Duration = 8, Caption = "Connect the seat's Yaw output to the Gyroscope's yaw input." }, new GuideStep { Duration = 8, Caption = "Connect the seat's Pitch output to the Gyroscope's pitch input. Match the axes, not socket order." }, new GuideStep { Duration = 12, Caption = "Now pilot roll, yaw, and pitch reach the powered Gyroscope. It provides the turning force for Advanced Controls." }, new GuideStep { Duration = 8, Caption = "Release the controls and the signals return to zero. Movement outputs remain available for propulsion controls." } }, BuildScene = scene => scene.CreateObject("guide_presentation").AddComponent<ControlSeatGuidePresentation>().Build(scene, PartRef.Native(basic.id), PartRef.Native(advanced.id)), Sample = (scene, seconds) => scene.GetObject("guide_presentation").GetComponent<ControlSeatGuidePresentation>().Sample(scene, seconds) });
        }

        private static void EnsureThruster(PartSettings part)
        {
            if (PonderRegistry.Catalog.Contains("ponder:chemical_thruster"))
            {
                return;
            }

            var reference = PartRef.Native(part.id);
            PonderRegistry.Create("ponder", typeof(Plugin).Assembly).RegisterGuide(new GuideDefinition { Id = "chemical_thruster", Title = "Chemical Thruster: fuel, oxygen and throttle", NativeParts = new[] { reference }, Steps = new[] { new GuideStep { Duration = 8, Caption = "The Chemical Thruster burns ethanol and oxygen supplied through separate fluid networks." }, new GuideStep { Duration = 8, Caption = "Connect a tank of ethanol to one fluid port." }, new GuideStep { Duration = 8, Caption = "Connect a separate oxygen tank to the other fluid port. Keep the two fluids separate." }, new GuideStep { Duration = 8, Caption = "With both fluids supplied, the thruster burns automatically. Maximum consumption is 0.05 of each fluid per second." }, new GuideStep { Duration = 8, Caption = "Insert a Valve into the fuel line to limit ethanol flow. Leave oxygen unrestricted." }, new GuideStep { Duration = 8, Caption = "Wire a Constant to the Valve's signal input. Its value limits fuel transfer per second." }, new GuideStep { Duration = 8, Caption = "Set 0 to close the Valve: no fuel flow, no thrust once buffered fuel is consumed." }, new GuideStep { Duration = 8, Caption = "Set 0.0125 for 25% throttle: one quarter of the maximum fuel consumption." }, new GuideStep { Duration = 8, Caption = "Set 0.025 for 50% throttle. With enough oxygen, halving the fuel flow halves steady thrust." }, new GuideStep { Duration = 8, Caption = "Set 0.0375 for 75% throttle. The flame grows as more fuel reaches the thruster." }, new GuideStep { Duration = 8, Caption = "Set 0.05 for maximum throttle: 0.05 ethanol and 0.05 oxygen per second for this single thruster." }, new GuideStep { Duration = 8, Caption = "Return to 0 to shut down. These are steady-state examples; pipe buffers and supply shortages affect response." } }, BuildScene = scene => scene.CreateObject("guide_presentation").AddComponent<ThrusterGuidePresentation>().Build(scene, reference), Sample = (scene, seconds) => scene.GetObject("guide_presentation").GetComponent<ThrusterGuidePresentation>().Sample(scene, seconds) });
        }

        private static void EnsureFluidTank(PartSettings part)
        {
            if (PonderRegistry.Catalog.Contains("ponder:fluid_tank"))
            {
                return;
            }

            var reference = PartRef.Native(part.id);
            PonderRegistry.Create("ponder", typeof(Plugin).Assembly).RegisterGuide(new GuideDefinition { Id = "fluid_tank", Title = "Fluid Tank: storing and using fluids", NativeParts = new[] { reference }, Steps = new[] { new GuideStep { Duration = FluidTankGuideTimeline.IntroEnd, Caption = FluidTankGuidePresentation.PortExplanation }, new GuideStep { Duration = FluidTankGuideTimeline.AssemblyEnd - FluidTankGuideTimeline.IntroEnd, Caption = "Connect an Ethanol Distiller to a fluid port on the tank." }, new GuideStep { Duration = FluidTankGuideTimeline.ThrusterStart - FluidTankGuideTimeline.AssemblyEnd, Caption = "Fluid flows in and is stored internally. The dial rises as the tank fills." }, new GuideStep { Duration = FluidTankGuideTimeline.DrainStart - FluidTankGuideTimeline.ThrusterStart, Caption = "Connect the tank's other end to a Chemical Thruster. Supply oxygen to its second port." }, new GuideStep { Duration = FluidTankGuideTimeline.HalfFull - FluidTankGuideTimeline.DrainStart, Caption = "With fuel and oxygen supplied, the thruster operates automatically and consumes stored fuel. The tank's dial drops." }, new GuideStep { Duration = FluidTankGuideTimeline.Resume - FluidTankGuideTimeline.HalfFull, Caption = "The tank's signal port outputs its current fuel percentage to a connected Signal Display." }, new GuideStep { Duration = FluidTankGuideTimeline.Empty - FluidTankGuideTimeline.Resume, Caption = "As stored fuel is consumed, the display follows the fuel percentage down to zero." }, new GuideStep { Duration = FluidTankGuideTimeline.Duration - FluidTankGuideTimeline.Empty, Caption = "The tank is empty. Without fuel, the thruster stops producing thrust." } }, BuildScene = scene => scene.CreateObject("guide_presentation").AddComponent<FluidTankGuidePresentation>().Build(scene, reference), Sample = (scene, seconds) => scene.GetObject("guide_presentation").GetComponent<FluidTankGuidePresentation>().Sample(scene, seconds) });
        }
    }
}
