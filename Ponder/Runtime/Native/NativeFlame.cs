using Ships.Parts.ChemicalThruster;
using UnityEngine;

namespace Ponder.Runtime
{
    internal static class NativeFlame
    {
        internal static void Prepare(ThrusterFlameVFX effect, ParticleSystem[] particles)
        {
            NativeFields.Find(typeof(ThrusterFlameVFX), "_particleSystem").SetValue(effect, effect.GetComponentInChildren<ParticleSystem>(true));
            foreach (var particle in particles)
            {
                particle.useAutoRandomSeed = false;
                particle.randomSeed = 731;
            }
        }
    }
}
