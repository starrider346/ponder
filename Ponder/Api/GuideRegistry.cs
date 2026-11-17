using System;
using System.Linq;
using System.Reflection;
using Ponder.Core;
using StellarStriderLib;

namespace Ponder
{
    /// <summary>A mod-owned guide collection. Does not register content or save-owned IDs in SSLib.</summary>
    public sealed class GuideRegistry
    {
        public string ModId
        {
            get;
        }
        public Assembly Assembly
        {
            get;
        }

        internal GuideRegistry(string id, Assembly assembly)
        {
            ModId = id;
            Assembly = assembly;
        }

        /// <summary>Registers one titled guide on its target SSLib content keys.
        /// Targets resolve through the active world's mapping when parts are viewed.</summary>
        public void RegisterGuide(GuideDefinition definition)
        {
            PonderRegistry.MainThread();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            PonderRegistry.Identifier(definition.Id);
            if (definition.NativeParts != null && (ModId != "ponder" || Assembly != typeof(PonderRegistry).Assembly))
            {
                throw new ArgumentException("Native targets are reserved for Ponder's bundled guides.");
            }

            if (definition.NativeParts != null && definition.Parts?.Length > 0)
            {
                throw new ArgumentException("A bundled guide cannot mix native targets with SSLib target keys.");
            }

            var targets = definition.NativeParts ?? definition.Parts?.Select(key =>
            {
                var segments = key?.Split(':');
                if (segments == null || segments.Length != 2)
                {
                    throw new ArgumentException("Guide targets must be full SSLib content keys, such as owner:part.");
                }

                PonderRegistry.Identifier(segments[0]);
                PonderRegistry.Identifier(segments[1]);
                return PartRef.Mod(key);
            }).ToArray();
            if (string.IsNullOrWhiteSpace(definition.Title) || definition.BuildScene == null || targets == null || targets.Length == 0 || definition.Steps == null || definition.Steps.Any(s => s == null))
            {
                throw new ArgumentException("A guide needs a title, target parts, scene builder and timed steps.");
            }

            var steps = definition.Steps.Select(s => new GuideStep { Caption = s.Caption ?? "", Duration = s.Duration }).ToArray();
            _ = new GuideClock(steps.Select(s => s.Duration).ToArray());
            var entry = new RegisteredGuide(ModId + ":" + definition.Id, definition.Title, steps, definition.BuildScene, definition.Sample);
            PonderRegistry.Catalog.Add(entry.Key, targets, entry);
        }
    }
}
