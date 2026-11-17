using UnityEngine;

namespace Ponder.Runtime
{
    internal static class ShaderProperties
    {
        internal static readonly int AmbientLightTex = Shader.PropertyToID("_AmbientLightTex");
        internal static readonly int ClipRect = Shader.PropertyToID("_ClipRect");
        internal static readonly int Color = Shader.PropertyToID("_Color");
        internal static readonly int DirectLightTex = Shader.PropertyToID("_DirectLightTex");
        internal static readonly int LiquidDensity = Shader.PropertyToID("_LiquidDensity");
        internal static readonly int LiquidPos = Shader.PropertyToID("_LiquidPos");
        internal static readonly int PlanetAtmosphereShellThickness = Shader.PropertyToID("_PlanetAtmosphereShellThickness");
        internal static readonly int PlanetPositionWS = Shader.PropertyToID("_PlanetPositionWS");
        internal static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
        internal static readonly int SceneScaling = Shader.PropertyToID("_SceneScaling");
        internal static readonly int SpaceAmbientLight = Shader.PropertyToID("_SpaceAmbientLight");
        internal static readonly int Stencil = Shader.PropertyToID("_Stencil");
        internal static readonly int StencilComp = Shader.PropertyToID("_StencilComp");
        internal static readonly int UseArray = Shader.PropertyToID("_UseArray");
    }
}
