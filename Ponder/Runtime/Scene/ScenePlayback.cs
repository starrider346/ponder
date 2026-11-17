using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ponder.Runtime
{
    internal sealed class ScenePlayback
    {
        private readonly List<Action<float>> _tracks = new();
        private readonly List<ScenePose> _poses = new();
        internal void Add(Action<float> track) => _tracks.Add(track);
        internal void Capture(Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != root)
                {
                    _poses.Add(new ScenePose(transform));
                }
            }
        }

        internal void Evaluate(float seconds)
        {
            foreach (var pose in _poses)
            {
                pose.Restore();
            }

            foreach (var track in _tracks)
            {
                track(seconds);
            }
        }

        internal void Clear()
        {
            _tracks.Clear();
            _poses.Clear();
        }
    }
}
