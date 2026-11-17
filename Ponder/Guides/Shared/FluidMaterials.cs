using Ponder.Runtime;
using UnityEngine;

namespace Ponder.Guides
{
    internal static class FluidMaterials
    {
        internal static void Apply(Renderer renderer, Color color, float density, float position)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(ShaderProperties.Color, color);
            properties.SetFloat(ShaderProperties.LiquidDensity, density);
            properties.SetFloat(ShaderProperties.LiquidPos, position);
            renderer.SetPropertyBlock(properties);
        }
    }
}
