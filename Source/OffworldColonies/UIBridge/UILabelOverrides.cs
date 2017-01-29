using UnityEngine;

namespace UIBridge {
    [RequireComponent(typeof(UITextHandler))]
    [RequireComponent(typeof(UIElement))]
    public class UILabelOverrides: MonoBehaviour {
        public bool ignoreStyle = false;
        public bool ignoreSize = false;
        public bool ignoreAlignment = false;
        public bool ignoreColor = false;
    }
}