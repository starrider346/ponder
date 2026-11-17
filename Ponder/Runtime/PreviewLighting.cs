using System;
using Rendering.Services;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ponder.Runtime
{
    internal sealed class PreviewLighting : IDisposable
    {
        private static readonly int SpaceAmbient = Shader.PropertyToID("_SpaceAmbientLight");
        private static readonly int DirectMap = Shader.PropertyToID("_DirectLightTex");
        private static readonly int AmbientMap = Shader.PropertyToID("_AmbientLightTex");
        private readonly Color _spaceAmbient;
        private readonly Color _ambientColor;
        private readonly Texture _directMap;
        private readonly Texture _ambientMap;
        private readonly AmbientMode _ambientMode;
        private readonly SphericalHarmonicsL2 _ambientProbe;
        private readonly float _ambientIntensity;
        private readonly Light _light;
        private readonly float _lightIntensity;
        private readonly int _lightMask;
        private readonly bool _lightEnabled;
        private readonly LightShadows _shadows;
        private bool _disposed;
        internal PreviewLighting(SceneLighting lighting, int layer)
        {
            _spaceAmbient = Shader.GetGlobalColor(SpaceAmbient);
            _directMap = Shader.GetGlobalTexture(DirectMap);
            _ambientMap = Shader.GetGlobalTexture(AmbientMap);
            _ambientMode = RenderSettings.ambientMode;
            _ambientColor = RenderSettings.ambientLight;
            _ambientIntensity = RenderSettings.ambientIntensity;
            _ambientProbe = RenderSettings.ambientProbe;
            _light = lighting != null ? (Light)NativeFields.Find(typeof(SceneLighting), "sceneLight").GetValue(lighting) : null;
            if (_light != null)
            {
                _lightIntensity = _light.intensity;
                _lightMask = _light.cullingMask;
                _lightEnabled = _light.enabled;
                _shadows = _light.shadows;
            }

            try
            {
                Shader.SetGlobalColor(SpaceAmbient, Color.white);
                Shader.SetGlobalTexture(DirectMap, Texture2D.whiteTexture);
                Shader.SetGlobalTexture(AmbientMap, Texture2D.whiteTexture);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white;
                RenderSettings.ambientIntensity = 1;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(Color.white);
                RenderSettings.ambientProbe = probe;
                if (_light != null)
                {
                    _light.intensity = .35f;
                    _light.cullingMask = 1 << layer;
                    _light.shadows = LightShadows.None;
                    _light.enabled = true;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                Shader.SetGlobalColor(SpaceAmbient, _spaceAmbient);
                Shader.SetGlobalTexture(DirectMap, _directMap);
                Shader.SetGlobalTexture(AmbientMap, _ambientMap);
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientLight = _ambientColor;
                RenderSettings.ambientIntensity = _ambientIntensity;
                RenderSettings.ambientProbe = _ambientProbe;
            }
            finally
            {
                if (_light != null)
                {
                    _light.intensity = _lightIntensity;
                    _light.cullingMask = _lightMask;
                    _light.shadows = _shadows;
                    _light.enabled = _lightEnabled;
                }
            }
        }
    }
}
