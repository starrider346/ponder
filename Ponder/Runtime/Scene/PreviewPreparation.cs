using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ponder.Runtime
{
    internal sealed class PreviewPreparation
    {
        private readonly PonderScene _scene;
        private readonly int _layer;
        private readonly List<Action<Camera>> _displayOrientations = new();
        private readonly Dictionary<ParticleSystem, float> _particleSamples = new();
        private GameObject Root => _scene.Root;

        internal PreviewPreparation(PonderScene scene, int layer)
        {
            _scene = scene;
            _layer = layer;
        }

        private T Own<T>(T asset)
            where T : Object => _scene.Own(asset);
        internal void SampleParticles(ParticleSystem particles, float seconds) => _particleSamples[particles] = seconds;
        internal void BeginSample() => _particleSamples.Clear();
        internal void Clear()
        {
            _displayOrientations.Clear();
            _particleSamples.Clear();
        }

        internal void Prepare()
        {
            var materials = new Dictionary<Material, Material>();
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(Color.white);
            var probes = new[]
            {
                ambient
            };
            foreach (var transform in Root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = _layer;
            }

            foreach (var behaviour in Root.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour is Canvas canvas)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    continue;
                }

                if (behaviour is TMP_Text || behaviour is Graphic)
                {
                    continue;
                }

                var type = behaviour.GetType();
                if (type.Name == "LookAtPlayerFlat4Sides" || type.Name == "LookAtPlayerFlat2Sides")
                {
                    var surface = NativeFields.Find(type, "surface")?.GetValue(behaviour) as Transform;
                    if (surface != null && surface.IsChildOf(Root.transform))
                    {
                        var target = behaviour.transform;
                        var initialUp = Quaternion.Inverse(surface.rotation) * target.up;
                        var fourSides = type.Name == "LookAtPlayerFlat4Sides";
                        _displayOrientations.Add(camera =>
                        {
                            var up = Vector3.ProjectOnPlane(camera.transform.up, surface.up);
                            if (up.sqrMagnitude < .00001f)
                            {
                                up = surface.forward;
                            }

                            var local = Quaternion.Inverse(surface.rotation) * up.normalized;
                            Vector3 snapped;
                            if (fourSides)
                            {
                                snapped = Mathf.Abs(local.z) > Mathf.Abs(local.x) ? (local.z >= 0 ? Vector3.forward : Vector3.back) : (local.x >= 0 ? Vector3.right : Vector3.left);
                            }
                            else
                            {
                                snapped = Vector3.Dot(local, initialUp) >= 0 ? initialUp : -initialUp;
                            }

                            target.rotation = Quaternion.LookRotation(-surface.up, surface.rotation * snapped);
                        });
                    }
                }

                behaviour.enabled = false;
            }

            foreach (var text in Root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.maskable = false;
                text.overflowMode = TextOverflowModes.Overflow;
                if (text.font != null && text.font.material != null)
                {
                    var material = Own(new Material(text.font.material));
                    material.DisableKeyword("UNITY_UI_CLIP_RECT");
                    material.DisableKeyword("UNITY_UI_ALPHACLIP");
                    material.DisableKeyword("MASK_SOFT");
                    material.DisableKeyword("MASK_HARD");
                    material.SetVector(ShaderProperties.ClipRect, new Vector4(-100000, -100000, 100000, 100000));
                    material.SetFloat(ShaderProperties.StencilComp, 8);
                    material.SetFloat(ShaderProperties.Stencil, 0);
                    text.fontSharedMaterial = material;
                }

                if (text is TextMeshProUGUI ui)
                {
                    ui.canvasRenderer.DisableRectClipping();
                    ui.canvasRenderer.cull = false;
                }
            }

            foreach (var collider in Root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (var body in Root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            foreach (var particle in Root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.playOnAwake = false;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            foreach (var renderer in Root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.CopySHCoefficientArraysFrom(probes);
                properties.SetColor(ShaderProperties.SpaceAmbientLight, Color.white);
                properties.SetTexture(ShaderProperties.DirectLightTex, Texture2D.whiteTexture);
                properties.SetTexture(ShaderProperties.AmbientLightTex, Texture2D.whiteTexture);
                renderer.SetPropertyBlock(properties);
                var copies = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                    {
                        return null;
                    }

                    if (!materials.TryGetValue(material, out var copy))
                    {
                        copy = Own(new Material(material));
                        materials.Add(material, copy);
                        copy.EnableKeyword("PLANET_RING_DISABLED");
                        copy.EnableKeyword("PLANET_CLOUDS_DISABLED");
                        copy.SetVector(ShaderProperties.PlanetPositionWS, new Vector4(100000, 0, 0, 0));
                        copy.SetFloat(ShaderProperties.SceneScaling, 1);
                        copy.SetColor(ShaderProperties.SpaceAmbientLight, Color.white);
                        copy.SetTexture(ShaderProperties.DirectLightTex, Texture2D.whiteTexture);
                        copy.SetTexture(ShaderProperties.AmbientLightTex, Texture2D.whiteTexture);
                        copy.SetFloat(ShaderProperties.PlanetRadius, 1);
                        copy.SetFloat(ShaderProperties.PlanetAtmosphereShellThickness, 1);
                    }

                    return copy;
                }).ToArray();
                renderer.sharedMaterials = copies;
            }

            foreach (var rect in Root.GetComponentsInChildren<RectTransform>(true))
            {
                rect.ForceUpdateRectTransforms();
            }
        }

        internal void PrepareDisplays(Camera camera)
        {
            foreach (var orientation in _displayOrientations)
            {
                orientation(camera);
            }

            foreach (var canvas in Root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = camera;
            }

            foreach (var rect in Root.GetComponentsInChildren<RectTransform>(true))
            {
                rect.ForceUpdateRectTransforms();
            }

            Canvas.ForceUpdateCanvases();
            foreach (var text in Root.GetComponentsInChildren<TMP_Text>())
            {
                if (text.isActiveAndEnabled)
                {
                    text.ForceMeshUpdate();
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        internal void PrepareParticles()
        {
            foreach (var sample in _particleSamples)
            {
                var particles = sample.Key;
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (sample.Value <= 0)
                {
                    continue;
                }

                particles.Simulate(sample.Value, false, true, false);
                particles.Pause(false);
            }
        }
    }
}
