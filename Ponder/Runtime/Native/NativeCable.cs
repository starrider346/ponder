using System;
using System.Linq;
using Core.Services;
using Ships.Cables;
using Ships.Cables.Model;
using Ships.Cables.VisualObjects;
using Ships.Interface.Model.Parts;
using Ships.Interface.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ponder.Runtime
{
    internal sealed class NativeCable
    {
        private readonly PonderScene _scene;
        private readonly CableVisual _cable;
        private readonly Transform _from;
        private readonly Transform _to;
        private readonly Transform[] _covers;
        internal GameObject Group
        {
            get;
        }
        internal Renderer Fluid
        {
            get;
        }

        internal NativeCable(PonderScene scene, string id, CableType type, Transform from, Transform to)
        {
            _scene = scene;
            _from = from;
            _to = to;
            var provider = ServiceLocator.GetService<ICablesVisualsProvider>() as CablesVisuals;
            var configs = NativeFields.Read<CableVisualConfig[]>(provider, "cableConfigs");
            var prefab = configs?.FirstOrDefault(config => config.type == type).visualPrefab;
            if (prefab == null)
            {
                throw new InvalidOperationException("The native " + type + " cable is unavailable.");
            }

            Group = scene.CreateObject(id);
            _cable = Object.Instantiate(prefab, Group.transform);
            _cable.gameObject.SetActive(true);
            var picking = NativeFields.Read<Renderer>(_cable, "idRenderer");
            if (picking != null)
            {
                picking.enabled = false;
            }

            NativeFields.Find(typeof(CableVisual), "_cableDynamicMesh").SetValue(_cable, null);
            Fluid = NativeFields.Read<Renderer>(_cable, "scrollingCord");
            _covers = from.GetComponentsInChildren<Transform>(true).Concat(to.GetComponentsInChildren<Transform>(true)).Where(transform => transform.name == "Cover").ToArray();
            Update();
            Group.SetActive(false);
        }

        internal void Update()
        {
            _cable.SetTransform(_from.position, _from.rotation, _to.position, _to.rotation);
            var meshes = (Mesh[])NativeFields.Find(typeof(CableVisual), "_cableDynamicMesh").GetValue(_cable);
            foreach (var mesh in meshes ?? Array.Empty<Mesh>())
            {
                if (mesh != null)
                {
                    _scene.Own(mesh);
                }
            }
        }

        internal void HideCovers()
        {
            foreach (var cover in _covers)
            {
                cover.gameObject.SetActive(false);
            }
        }
    }
}
