using UnityEngine;

namespace ModUtils {
    public class VisualHelper : MonoBehaviour {
        private enum HelperType {
            Sphere,
            Cube,
            Bound
        }
        HelperType _type;

        Bounds _bounds;
        Color _color;
        Transform _transform;
        Vector3 _size;
        Vector3 _centre;
        Vector3 _pos;
        Transform _parent;
        Vector3 _diff;

        void OnRenderObject() {
            switch (_type) {
            case HelperType.Bound:
                Bounds t = _bounds;
                t.center = (_transform.position + _diff);
                DrawTools.DrawBounds(t, _color);
                break;
            case HelperType.Cube:
                DrawTools.DrawLocalCube(_transform, _size, _color, _centre);
                break;
            default:
                break;
            }
        }

        void Update() {
            if (_type == HelperType.Sphere)
                transform.position = _pos + _parent.position;
        }


        public static VisualHelper CreateHelper(Transform trans, Vector3 size, Color color, Vector3 centre = default(Vector3)) {
            VisualHelper helper = trans.gameObject.AddComponent<VisualHelper>();
            helper._type = HelperType.Cube;

            helper._size = size;
            helper._transform = trans;
            helper._color = color;
            helper._centre = centre;

            return helper;
        }

        public static VisualHelper CreateHelper(Transform trans, Bounds bounds, Color color) {
            VisualHelper helper = trans.gameObject.AddComponent<VisualHelper>();
            helper._type = HelperType.Bound;
            helper._diff = bounds.center - trans.position;
            helper._transform = trans;
            helper._color = color;
            helper._bounds = bounds;

            return helper;
        }


        public static VisualHelper CreateHelper(Color color, Transform parent) {
            VisualHelper helper = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<VisualHelper>();
            helper._type = HelperType.Sphere;

            helper._parent = parent;
            helper.GetComponent<Collider>().enabled = false;
            helper.GetComponent<Renderer>().material.color = color;

            return helper;
        }

        public void Change(Color color, Bounds bounds) {
            if (_type == HelperType.Bound) {
                this._bounds = bounds;
                this._color = color;
            }
        }

        public void Change(Transform trans, Vector3 size, Color color, Vector3 centre) {
            if (_type == HelperType.Cube) {
                this._size = size;
                this._transform = trans;
                this._color = color;
                this._centre = centre;
            }
        }

        public void Change(Vector3 newPos, float size) {
            if (_type == HelperType.Sphere) {
                _pos = newPos - _parent.position;
                transform.localScale = Vector3.one * size;
            }
        }
    }
}
