using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Ponder.Core;
using StellarStriderLib;

namespace Ponder
{
    /// <summary>Client-local guide registration. Call from OnInitializeMelon on Unity's main thread.</summary>
    public static class PonderRegistry
    {
        private static readonly Dictionary<string, GuideRegistry> Owners = new(StringComparer.Ordinal);
        internal static readonly GuideCatalog<PartRef, RegisteredGuide> Catalog = new();
        private static int _thread;
        internal static void MainThread()
        {
            if (_thread == 0)
            {
                _thread = Thread.CurrentThread.ManagedThreadId;
            }

            if (_thread != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("Ponder APIs must run on Unity's main thread.");
            }
        }

        public static GuideRegistry Create(string modId, Assembly assembly)
        {
            MainThread();
            Identifier(modId);
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (modId == "ponder" && assembly != typeof(PonderRegistry).Assembly)
            {
                throw new ArgumentException("The ponder owner ID is reserved for Ponder's bundled guides.");
            }

            if (Owners.TryGetValue(modId, out var existing))
            {
                return existing.Assembly == assembly ? existing : throw new ArgumentException("Mod ID already owned: " + modId);
            }

            var registry = new GuideRegistry(modId, assembly);
            Owners.Add(modId, registry);
            return registry;
        }

        internal static void Identifier(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Trim() != id || id.Contains(":"))
            {
                throw new ArgumentException("Identifiers must be nonempty, unpadded and contain no colon.");
            }
        }

        internal static RegisteredGuide[] Find(ushort id)
        {
            MainThread();
            if (!StriderRegistry.WorldReady)
            {
                return Array.Empty<RegisteredGuide>();
            }

            return Catalog.Find(id, reference => StriderRegistry.TryGetPart(reference, out var part) ? part.id : (ushort?)null);
        }
    }
}
