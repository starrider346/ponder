using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ponder.Runtime
{
    internal sealed class SceneResources : IDisposable
    {
        private readonly HashSet<Object> _assets = new();
        internal GameObject Root { get; } = new("Ponder preview scene");

        internal SceneResources()
        {
            Root.SetActive(false);
            Root.transform.position = Vector3.zero;
        }

        internal T Own<T>(T asset)
            where T : Object
        {
            _assets.Add(asset);
            return asset;
        }

        public void Dispose()
        {
            if (Root != null)
            {
                Root.SetActive(false);
                Object.Destroy(Root);
            }

            foreach (var asset in _assets)
            {
                if (asset != null)
                {
                    Object.Destroy(asset);
                }
            }

            _assets.Clear();
        }
    }
}
