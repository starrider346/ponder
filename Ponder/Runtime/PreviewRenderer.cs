using System;
using Core.Services;
using HarmonyLib;
using Ponder.Core;
using Rendering.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ponder.Runtime
{
    internal sealed class PreviewRenderer : IDisposable
    {
        private Camera _camera;
        private RenderTexture _texture;
        private Vector3 _center;
        private Vector3 _pan;
        private Vector3 _authoredCenter;
        private float _viewDistance;
        private float _inputHeight = 1;
        private bool _orbitFromView;
        private readonly CompletionOrbit _completion = new();
        private Vector3 _completionStartCenter;
        private Quaternion _completionStartRotation;
        private float _completionStartSize;
        private float _completionStartDistance;
        private Bounds? _completionBounds;
        private float _radius;
        private float _yaw = -35;
        private float _pitch = 24;
        private float _zoom = 1;
        internal Camera Camera => _camera;
        internal RenderTexture Texture => _texture;
        internal int Layer
        {
            get; private set;
        }

        internal PreviewRenderer()
        {
            var holder = new GameObject("Ponder camera setup");
            holder.SetActive(false);
            try
            {
                Layer = LayerMask.NameToLayer("PartThumbnailRender");
                if (Layer < 0)
                {
                    throw new InvalidOperationException("The game's part thumbnail layer is missing.");
                }

                var setup = holder.AddComponent<ObjectThumbnailRenderer>();
                NativeFields.Find(typeof(ObjectThumbnailRenderer), "_layer").SetValue(setup, Layer);
                try
                {
                    AccessTools.Method(typeof(ObjectThumbnailRenderer), "SetupRenderCamera").Invoke(setup, null);
                }
                finally
                {
                    _camera = (Camera)NativeFields.Find(typeof(ObjectThumbnailRenderer), "_renderCamera").GetValue(setup);
                }

                if (_camera == null)
                {
                    throw new InvalidOperationException("Native preview camera setup failed.");
                }

                _camera.name = "Ponder preview camera";
                _camera.cullingMask = 1 << Layer;
                _camera.nearClipPlane = .01f;
                _camera.farClipPlane = 1000;
                _camera.enabled = false;
                _camera.allowHDR = true;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = ViewerView.SceneBackground;
            }
            catch
            {
                Dispose();
                throw;
            }
            finally
            {
                Object.Destroy(holder);
            }
        }

        internal void Fit(Bounds bounds)
        {
            _center = bounds.center;
            _radius = Mathf.Max(.1f, bounds.extents.magnitude);
            Reset();
        }

        internal void Reset()
        {
            _yaw = -35;
            _pitch = 24;
            _zoom = 1;
            _pan = Vector3.zero;
            _orbitFromView = _completion.Finished;
            _completion.Stop();
        }

        internal void UpdateCompletion(bool finished, float delta)
        {
            var wasFinished = _completion.Finished;
            _completion.Update(finished, delta);
            if (!wasFinished && _completion.Finished)
            {
                _completionStartCenter = _camera.transform.position + _camera.transform.forward * _viewDistance;
                _completionStartRotation = _camera.transform.rotation;
                _completionStartSize = _camera.orthographicSize;
                _completionStartDistance = _viewDistance;
                _completionBounds = null;
                _yaw = -35;
                _pitch = 24;
                _zoom = 1;
                _pan = Vector3.zero;
                _orbitFromView = true;
            }
            else if (wasFinished && !_completion.Finished)
            {
                Reset();
            }
        }

        internal void StopCompletionOrbit() => _completion.Stop();
        internal void Orbit(Vector2 delta)
        {
            StopCompletionOrbit();
            if (!_orbitFromView && _texture != null)
            {
                _pan = _camera.transform.position + _camera.transform.forward * _viewDistance - _authoredCenter;
                _orbitFromView = true;
            }

            _yaw += delta.x * .3f;
            _pitch = Mathf.Clamp(_pitch - delta.y * .3f, -80, 80);
        }

        internal void Pan(Vector2 delta)
        {
            StopCompletionOrbit();
            if (_texture == null)
            {
                return;
            }

            var unitsPerPixel = 2 * _camera.orthographicSize / _inputHeight;
            _pan -= (_camera.transform.right * delta.x + _camera.transform.up * delta.y) * unitsPerPixel;
        }

        internal void Zoom(float scroll)
        {
            StopCompletionOrbit();
            _zoom = Mathf.Clamp(_zoom * Mathf.Exp(-scroll * .0015f), .35f, 3.5f);
        }

        internal void Render(PonderScene scene, int width, int height)
        {
            _inputHeight = Mathf.Max(1, height);
            var scale = Mathf.Min(1, 2048f / Mathf.Max(1, Mathf.Max(width, height)));
            width = Mathf.Max(64, Mathf.RoundToInt(width * scale));
            height = Mathf.Max(64, Mathf.RoundToInt(height * scale));
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                ReleaseTexture();
                _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf)
                {
                    name = "Ponder guide",
                    antiAliasing = 1
                };
                if (!_texture.Create())
                {
                    throw new InvalidOperationException("Could not allocate the guide render texture.");
                }
            }

            _camera.aspect = (float)width / height;
            if (_completion.Finished)
            {
                ApplyCompletionCamera(scene);
            }
            else
            {
                var authored = scene.CameraView;
                var size = authored?.Size ?? _radius * 1.25f;
                _authoredCenter = authored.HasValue ? scene.Root.transform.TransformPoint(authored.Value.Target) : _center;
                var center = _authoredCenter + _pan;
                var yaw = authored.HasValue ? authored.Value.Yaw + _yaw + 35 : _yaw;
                var pitch = authored.HasValue ? Mathf.Clamp(authored.Value.Pitch + _pitch - 24, -85, 85) : _pitch;
                _camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
                var frame = authored?.Frame;
                var composition = ViewerView.SceneArea;
                var contentAspect = _camera.aspect * composition.width / composition.height;
                if (frame.HasValue)
                {
                    var extents = frame.Value.extents;
                    var x = scene.Root.transform.TransformVector(Vector3.right * extents.x);
                    var y = scene.Root.transform.TransformVector(Vector3.up * extents.y);
                    var z = scene.Root.transform.TransformVector(Vector3.forward * extents.z);
                    var framing = Quaternion.Euler(authored?.Pitch ?? 24, authored?.Yaw ?? -35, 0);
                    var right = framing * Vector3.right;
                    var up = framing * Vector3.up;
                    var horizontal = Mathf.Abs(Vector3.Dot(x, right)) + Mathf.Abs(Vector3.Dot(y, right)) + Mathf.Abs(Vector3.Dot(z, right));
                    var vertical = Mathf.Abs(Vector3.Dot(x, up)) + Mathf.Abs(Vector3.Dot(y, up)) + Mathf.Abs(Vector3.Dot(z, up));
                    size = OrthographicFraming.HalfHeight(horizontal, vertical, contentAspect, authored.Value.Padding);
                }
                else
                {
                    size /= Mathf.Min(1, contentAspect);
                }

                _camera.orthographicSize = size * _zoom / composition.height;
                if (!_orbitFromView)
                {
                    center -= _camera.transform.right * ((composition.center.x - .5f) * 2 * _camera.orthographicSize * _camera.aspect) + _camera.transform.up * ((composition.center.y - .5f) * 2 * _camera.orthographicSize);
                }

                var radius = frame.HasValue ? frame.Value.extents.magnitude : authored.HasValue ? size : _radius;
                var distance = Mathf.Max(5, radius * 4);
                _viewDistance = distance;
                _camera.farClipPlane = Mathf.Max(10, distance + radius * 8);
                _camera.transform.position = center - _camera.transform.forward * distance;
            }

            var previous = RenderTexture.active;
            var lighting = ServiceLocator.GetService<SceneLighting>();
            try
            {
                scene.Root.SetActive(true);
                scene.PrepareParticles();
                scene.PrepareDisplays(_camera);
                lighting?.SetTmpSceneLighting(_camera.transform.forward, Color.white);
                using (new PreviewLighting(lighting, Layer))
                {
                    _camera.targetTexture = _texture;
                    _camera.Render();
                }
            }
            finally
            {
                _camera.targetTexture = null;
                scene.Root.SetActive(false);
                RenderTexture.active = previous;
                lighting?.RestoreSceneLighting();
            }
        }

        private void ApplyCompletionCamera(PonderScene scene)
        {
            var bounds = _completionBounds ??= scene.VisibleBounds;
            var extents = bounds.extents;
            var composition = ViewerView.SceneArea;
            var aspect = _camera.aspect * composition.width / composition.height;
            var size = OrthographicFraming.OrbitHalfHeight(extents.x, extents.y, extents.z, 24, aspect, 1.2f) / composition.height;
            var rotation = Quaternion.Slerp(_completionStartRotation, Quaternion.Euler(24, _completionStartRotation.eulerAngles.y + _completion.Angle, 0), _completion.Blend);
            var target = bounds.center - rotation * Vector3.right * ((composition.center.x - .5f) * 2 * size * _camera.aspect) - rotation * Vector3.up * ((composition.center.y - .5f) * 2 * size);
            _authoredCenter = Vector3.Lerp(_completionStartCenter, target, _completion.Blend);
            var angles = rotation.eulerAngles;
            var pitch = Mathf.DeltaAngle(0, angles.x);
            _camera.transform.rotation = Quaternion.Euler(Mathf.Clamp(pitch + _pitch - 24, -85, 85), angles.y + _yaw + 35, 0);
            _camera.orthographicSize = Mathf.Lerp(_completionStartSize, size, _completion.Blend) * _zoom;
            var radius = Mathf.Max(.1f, extents.magnitude);
            _viewDistance = Mathf.Lerp(_completionStartDistance, Mathf.Max(5, radius * 4), _completion.Blend);
            _camera.farClipPlane = Mathf.Max(10, _viewDistance + radius * 8);
            _camera.transform.position = _authoredCenter + _pan - _camera.transform.forward * _viewDistance;
        }

        private void ReleaseTexture()
        {
            if (_texture == null)
            {
                return;
            }

            _texture.Release();
            Object.Destroy(_texture);
            _texture = null;
        }

        public void Dispose()
        {
            if (_camera != null)
            {
                _camera.targetTexture = null;
                Object.Destroy(_camera.gameObject);
                _camera = null;
            }

            ReleaseTexture();
        }
    }
}
