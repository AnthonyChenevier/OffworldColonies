using UnityEngine;

namespace ModUtilities {
    public class VisualHelper : MonoBehaviour {
        private enum HelperType {
            Sphere,
            Cube,
            Bound
        }

        private HelperType _type;
        private Color _color;
        private Transform _transform;

        private Bounds _bounds;
        private Vector3 _size;
        private Vector3 _posOffset;

        void OnRenderObject() {
            switch (_type) {
                case HelperType.Bound:
                    DrawTools.DrawBounds(_transform, _bounds, _color);
                    break;
                case HelperType.Cube:
                    DrawTools.DrawLocalBox(_transform, _size, _color, _posOffset);
                    break;
                case HelperType.Sphere:
                    DrawTools.DrawSphere(_transform.TransformPoint(_posOffset), _color, _size.y);
                    break;
            }
        }


        public static VisualHelper CreateHelper(Transform parent, Vector3 size, Color color, Vector3 posOffset) {
            VisualHelper helper = parent.gameObject.AddComponent<VisualHelper>();
            helper._type = HelperType.Cube;

            helper._size = size;
            helper._transform = parent;
            helper._color = color;
            helper._posOffset = posOffset;

            return helper;
        }

        public void Change(Transform parent, Vector3 size, Color color, Vector3 posOffset) {
            if (_type == HelperType.Cube) {
                _size = size;
                _transform = parent;
                _color = color;
                _posOffset = posOffset;
            }
        }

        public static VisualHelper CreateHelper(Transform parent, Bounds bounds, Color color) {
            VisualHelper helper = parent.gameObject.AddComponent<VisualHelper>();
            helper._type = HelperType.Bound;
            helper._transform = parent;
            helper._color = color;
            helper._bounds = bounds;

            return helper;
        }

        public void Change(Transform parent, Bounds bounds, Color color) {
            if (_type == HelperType.Bound) {
                _transform = parent;
                _bounds = bounds;
                _color = color;
            }
        }


        public static VisualHelper CreateHelper(Transform parent, float radius, Color color, Vector3 offset) {
            VisualHelper helper = parent.gameObject.AddComponent<VisualHelper>();
            helper._type = HelperType.Sphere;
            helper._transform = parent;
            helper._color = color;
            helper._posOffset = offset;
            helper._size = Vector3.up * radius;

            return helper;
        }

        public void Change(Transform parent, float radius, Color color, Vector3 offset) {
            if (_type == HelperType.Sphere) {
                _transform = parent;
                _size = Vector3.up * radius;
                _color = color;
                _posOffset = offset;
            }
        }
    }
}
