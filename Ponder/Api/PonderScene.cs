using System;
using System.Collections.Generic;
using System.Linq;
using Core.Services;
using Ponder.Runtime;
using Ships.Interface.Model.Parts;
using Ships.Interface.Services;
using Ships.Interface.Settings;
using Ships.Parts.Common.Model;
using StellarStriderLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ponder
{
    /// <summary>Owns one viewing's visual objects and absolute-time animation tracks.
    /// Build through the guide's BuildScene callback; never spawn live ship parts here.</summary>
    public sealed class PonderScene : IDisposable
    {
        private readonly Dictionary<string, GameObject> _objects = new(StringComparer.Ordinal);
        private readonly ScenePlayback _playback = new();
        private readonly SceneResources _resources = new();
        private readonly PreviewPreparation _preparation;
        private readonly SceneAnnotations _annotations;
        private bool _sealed;
        private bool _disposed;
        internal GameObject Root => _resources.Root;
        internal List<SceneCallout> Callouts => _annotations.Callouts;

        internal SceneCameraView? CameraView;
        internal string SubjectLabel;
        internal PonderScene(int layer)
        {
            _preparation = new PreviewPreparation(this, layer);
            _annotations = new SceneAnnotations(this);
        }

        /// <summary>Add a native or SSLib part's visual preview. No ship, state or network object is created.</summary>
        public GameObject AddPart(string id, PartRef part, Vector3 position = default, Vector3 rotation = default)
        {
            ValidateObjectCreation(id);
            if (!StriderRegistry.TryGetPart(part, out var settings))
            {
                throw new ArgumentException("Preview part is unavailable: " + id);
            }

            var instancer = ServiceLocator.GetService<IShipPartInstancer>();
            if (instancer == null)
            {
                throw new InvalidOperationException("The game's part preview service is unavailable.");
            }

            var preview = instancer.InstantiatePartPreview(settings, Root.transform);
            if (preview == null)
            {
                throw new InvalidOperationException("Part preview construction failed: " + id);
            }

            preview.name = id;
            preview.transform.localPosition = position;
            preview.transform.localRotation = Quaternion.Euler(rotation);
            if (settings is HullPartSettings generatedHull && !generatedHull.hasItsOwnMesh)
            {
                preview.transform.localRotation *= settings.DefaultRotation;
            }

            _objects.Add(id, preview);
            if (settings is HullPartSettings hull && !hull.hasItsOwnMesh)
            {
                foreach (var filter in preview.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh != null)
                    {
                        Own(filter.sharedMesh);
                    }
                }

                foreach (var renderer in preview.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null && material.HasProperty(ShaderProperties.UseArray) && material.GetInt(ShaderProperties.UseArray) == 1)
                        {
                            Own(material);
                        }
                    }
                }
            }

            return preview;
        }

        /// <summary>Add an empty scene object for authored meshes, lines or other custom visuals.</summary>
        public GameObject CreateObject(string id)
        {
            ValidateObjectCreation(id);
            var item = new GameObject(id);
            item.transform.SetParent(Root.transform, false);
            _objects.Add(id, item);
            return item;
        }

        /// <summary>Show a full native signal cable between two scene-owned sockets at the given start time. Creates visuals only.</summary>
        public GameObject AddSignalCable(string id, string fromPart, byte fromSocket, string toPart, byte toSocket, float start, float duration)
        {
            ValidateTrackTime(start, duration);
            var cable = new NativeCable(this, id, CableType.Signal, GetSocket(fromPart, fromSocket), GetSocket(toPart, toSocket));
            _playback.Add(seconds =>
            {
                cable.Group.SetActive(seconds >= start);
                if (seconds >= start)
                {
                    cable.Update();
                }
            });
            return cable.Group;
        }

        /// <summary>Set the authored camera for this sample. The target is scene-local; size is orthographic half-height.
        /// Player turn, pan and zoom are applied as offsets. Call on every Sample for repeatable camera motion.</summary>
        public void SetCameraView(Vector3 target, float yaw, float pitch, float size)
        {
            EnsureAlive();
            if (size <= 0 || !Finite(size) || !Finite(yaw) || !Finite(pitch) || !Finite(target.x) || !Finite(target.y) || !Finite(target.z))
            {
                throw new ArgumentException("Invalid guide camera view.");
            }

            CameraView = new SceneCameraView(target, yaw, pitch, size);
        }

        /// <summary>Frame scene-local bounds with padding, accounting for the actual viewport aspect ratio.</summary>
        public void SetCameraFrame(Bounds bounds, float yaw, float pitch, float padding = 1.2f)
        {
            EnsureAlive();
            var center = bounds.center;
            var size = bounds.size;
            if (!Finite(center.x) || !Finite(center.y) || !Finite(center.z) || !Finite(size.x) || !Finite(size.y) || !Finite(size.z) || size.x < 0 || size.y < 0 || size.z < 0 || !Finite(yaw) || !Finite(pitch) || !Finite(padding) || padding < 1)
            {
                throw new ArgumentException("Invalid camera framing bounds.");
            }

            CameraView = new SceneCameraView(bounds, yaw, pitch, padding);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        /// <summary>Access a registered preview, including from a custom Sample callback.</summary>
        public GameObject GetObject(string id)
        {
            EnsureAlive();
            return _objects.TryGetValue(id, out var item) ? item : throw new ArgumentException("Unknown scene object: " + id);
        }

        /// <summary>Reconstruct scene-owned particles at an absolute effect time during the next render.
        /// Zero clears the effect. No automatic playback or native gameplay scripts are enabled.</summary>
        public void SampleParticles(ParticleSystem particles, float seconds)
        {
            EnsureAlive();
            if (particles == null || !particles.transform.IsChildOf(Root.transform))
            {
                throw new ArgumentException("Particles must belong to this scene.");
            }

            if (!Finite(seconds) || seconds < 0)
            {
                throw new ArgumentException("Particle time must be finite and nonnegative.");
            }

            _preparation.SampleParticles(particles, seconds);
        }

        /// <summary>Access the preview's native socket transform by socket ID.</summary>
        public Transform GetSocket(string id, byte socketId)
        {
            var tracker = GetObject(id).GetComponentInChildren<PartSocketsTracker>(true);
            if (tracker == null || socketId >= tracker.sockets.Length || tracker.sockets[socketId] == null)
            {
                throw new ArgumentException("Unknown socket " + socketId + " on " + id);
            }

            return tracker.sockets[socketId];
        }

        /// <summary>Transfer a newly created material, mesh or texture to the scene for disposal.
        /// Never pass shared game assets or SSLib's cached assets.</summary>
        public T Own<T>(T asset)
            where T : Object
        {
            EnsureAlive();
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            return _resources.Own(asset);
        }

        public void Move(string id, Vector3 from, Vector3 to, float start, float duration)
        {
            ValidateTrackTime(start, duration);
            var transform = GetObject(id).transform;
            _playback.Add(t =>
            {
                if (t >= start)
                {
                    transform.localPosition = Vector3.Lerp(from, to, Phase(t, start, duration));
                }
            });
        }

        /// <summary>Interpolate Euler angles in degrees, allowing complete revolutions.</summary>
        public void Rotate(string id, Vector3 from, Vector3 to, float start, float duration)
        {
            ValidateTrackTime(start, duration);
            var transform = GetObject(id).transform;
            _playback.Add(t =>
            {
                if (t >= start)
                {
                    transform.localRotation = Quaternion.Euler(Vector3.Lerp(from, to, Phase(t, start, duration)));
                }
            });
        }

        public void Show(string id, float at, bool visible = true)
        {
            ValidateTrackTime(at, 1);
            var item = GetObject(id);
            _playback.Add(t =>
            {
                if (t >= at)
                {
                    item.SetActive(visible);
                }
            });
        }

        public void Highlight(string id, float start, float end, Color? color = null) => AddHighlight(GetObject(id).transform, false, start, end, color ?? Color.cyan);
        /// <summary>Highlight a particular child, such as a value field or moving subassembly.</summary>
        public void Highlight(Transform target, float start, float end, Color? color = null)
        {
            EnsureAlive();
            if (target == null || !target.IsChildOf(Root.transform))
            {
                throw new ArgumentException("Highlight target must belong to this scene.");
            }

            AddHighlight(target, false, start, end, color ?? Color.cyan);
        }

        public void HighlightSocket(string id, byte socketId, float start, float end, Color? color = null) => AddHighlight(GetSocket(id, socketId), true, start, end, color ?? Color.cyan);
        public void Callout(string id, Vector3 localPoint, string text, float start, float end) => AddCallout(GetObject(id).transform, localPoint, text, start, end);
        public void CalloutSocket(string id, byte socketId, string text, float start, float end) => AddCallout(GetSocket(id, socketId), Vector3.zero, text, start, end);
        /// <summary>Attach a callout to a particular scene-owned child, such as a value field.</summary>
        public void Callout(Transform target, Vector3 localPoint, string text, float start, float end)
        {
            EnsureAlive();
            if (target == null || !target.IsChildOf(Root.transform))
            {
                throw new ArgumentException("Callout target must belong to this scene.");
            }

            AddCallout(target, localPoint, text, start, end);
        }

        private void AddCallout(Transform target, Vector3 point, string text, float start, float end)
        {
            ValidateInterval(start, end);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Callout text is empty.");
            }

            Callouts.Add(new SceneCallout { Target = target, Point = point, Text = text, Start = start, End = end });
        }

        internal void Seal()
        {
            EnsureAlive();
            if (_objects.Count == 0)
            {
                throw new InvalidOperationException("A guide must create at least one scene object.");
            }

            _preparation.Prepare();
            _playback.Capture(Root.transform);
            _sealed = true;
        }

        internal void Evaluate(float seconds, Action<PonderScene, float> custom)
        {
            EnsureAlive();
            CameraView = null;
            SubjectLabel = null;
            _preparation.BeginSample();
            _playback.Evaluate(seconds);
            custom?.Invoke(this, seconds);
        }

        internal Bounds Bounds => PreviewBounds.ForRenderers(Root.GetComponentsInChildren<Renderer>(true).Where(r => !(r is LineRenderer)), Root.transform.position);
        internal Bounds VisibleBounds => PreviewBounds.ForRenderers(Root.GetComponentsInChildren<Renderer>(true).Where(renderer =>
        {
            if (!renderer.enabled || renderer is ParticleSystemRenderer)
            {
                return false;
            }

            for (var current = renderer.transform; current != Root.transform; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }), Root.transform.position);

        internal void AddTrack(Action<float> track) => _playback.Add(track);
        internal void PrepareDisplays(Camera camera) => _preparation.PrepareDisplays(camera);
        internal void PrepareParticles() => _preparation.PrepareParticles();
        private void AddHighlight(Transform target, bool socket, float start, float end, Color color)
        {
            ValidateInterval(start, end);
            _annotations.Highlight(target, socket, start, end, color);
        }

        private static float Phase(float time, float start, float duration) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - start) / duration));
        private void EnsureAlive()
        {
            PonderRegistry.MainThread();
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PonderScene));
            }
        }

        private void ValidateObjectCreation(string id)
        {
            EnsureAlive();
            if (_sealed)
            {
                throw new InvalidOperationException("Construct the scene in BuildScene, not Sample.");
            }

            PonderRegistry.Identifier(id);
            if (_objects.ContainsKey(id))
            {
                throw new ArgumentException("Duplicate scene object: " + id);
            }
        }

        private void ValidateTrackTime(float start, float duration)
        {
            EnsureAlive();
            if (_sealed)
            {
                throw new InvalidOperationException("Declare animation tracks in BuildScene.");
            }

            if (float.IsNaN(start) || float.IsInfinity(start) || start < 0 || float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0)
            {
                throw new ArgumentException("Animation times must be finite, with a nonnegative start and positive duration.");
            }
        }

        private void ValidateInterval(float start, float end)
        {
            ValidateTrackTime(start, end - start);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _resources.Dispose();
            _objects.Clear();
            _playback.Clear();
            _preparation.Clear();
            Callouts.Clear();
        }
    }
}
