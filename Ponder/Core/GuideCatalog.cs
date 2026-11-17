using System;
using System.Collections.Generic;
using System.Linq;

namespace Ponder.Core
{
    internal sealed class GuideCatalog<TTarget, TGuide>
    {
        private readonly SortedDictionary<string, (TTarget[] Targets, TGuide Guide)> _entries = new(StringComparer.Ordinal);
        internal void Add(string key, TTarget[] targets, TGuide guide)
        {
            if (_entries.ContainsKey(key))
            {
                throw new ArgumentException("Guide already registered: " + key);
            }

            _entries.Add(key, ((TTarget[])targets.Clone(), guide));
        }

        internal bool Contains(string key) => _entries.ContainsKey(key);
        internal TGuide[] Find(ushort id, Func<TTarget, ushort?> resolve) => _entries.Values.Where(e => e.Targets.Any(t => resolve(t) == id)).Select(e => e.Guide).ToArray();
    }
}
