using System;
using StellarStriderLib;

namespace Ponder
{
    /// <summary>A standalone guide with its own identity, title and target SSLib parts.
    /// Registration copies this definition.</summary>
    public sealed class GuideDefinition
    {
        public string Id
        {
            get; set;
        }
        /// <summary>The guide's own display title, independent of any target part's name.</summary>
        public string Title
        {
            get; set;
        }
        /// <summary>Full SSLib content keys (owner:part) for every part that offers this guide.
        /// Numeric/native targets are reserved for Ponder's bundled guides.</summary>
        public string[] Parts { get; set; } = Array.Empty<string>();
        internal PartRef[] NativeParts
        {
            get; set;
        }
        public GuideStep[] Steps { get; set; } = Array.Empty<GuideStep>();
        /// <summary>Construct preview objects and absolute-time animation tracks. Called for each viewing.</summary>
        public Action<PonderScene> BuildScene
        {
            get; set;
        }
        /// <summary>Optional custom animation, sampled after tracks with absolute guide seconds.
        /// Must set state deterministically, including when time moves backwards.</summary>
        public Action<PonderScene, float> Sample
        {
            get; set;
        }
    }
}
