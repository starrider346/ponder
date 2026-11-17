using UnityEngine;

namespace Ponder.Runtime
{
    internal sealed class ScenePose
    {
        private readonly Transform _transform;
        private readonly Vector3 _position;
        private readonly Vector3 _scale;
        private readonly Quaternion _rotation;
        private readonly bool _active;
        private readonly RectTransform _rect;
        private readonly Vector2 _anchorMin;
        private readonly Vector2 _anchorMax;
        private readonly Vector2 _pivot;
        private readonly Vector2 _size;
        private readonly Vector3 _anchoredPosition;
        internal ScenePose(Transform transform)
        {
            _transform = transform;
            _position = transform.localPosition;
            _scale = transform.localScale;
            _rotation = transform.localRotation;
            _active = transform.gameObject.activeSelf;
            _rect = transform as RectTransform;
            if (_rect != null)
            {
                _anchorMin = _rect.anchorMin;
                _anchorMax = _rect.anchorMax;
                _pivot = _rect.pivot;
                _size = _rect.sizeDelta;
                _anchoredPosition = _rect.anchoredPosition3D;
            }
        }

        internal void Restore()
        {
            if (_transform == null)
            {
                return;
            }

            if (_rect != null)
            {
                _rect.anchorMin = _anchorMin;
                _rect.anchorMax = _anchorMax;
                _rect.pivot = _pivot;
                _rect.sizeDelta = _size;
                _rect.anchoredPosition3D = _anchoredPosition;
            }
            else
            {
                _transform.localPosition = _position;
            }

            _transform.localRotation = _rotation;
            _transform.localScale = _scale;
            _transform.gameObject.SetActive(_active);
        }
    }
}
