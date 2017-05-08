using UnityEngine;
using UnityEngine.Events;

namespace UIBridge {
    /// <summary>
    /// Provides a Text/TextMeshProUGUI agnostic interface for updating text in KSP
    /// by using OnTextUpdate(text) Event. The KSP-side implementer must add 
    /// a listener to the OnTextUpdate event to actually update the correct 
    /// type's .text field. The Unity 
    /// </summary>
    [RequireComponent(typeof(UIElement))]
    public class UITextHandler: MonoBehaviour {
        private class TextUpdateEvent : UnityEvent<string> { }
		public UnityEvent<string> OnTextUpdateEvent { get; private set; } = new TextUpdateEvent();
        public bool KeepFontStyle = false;
        public bool KeepFontSize = false;
        public bool KeepTextAlignment = false;
        public bool KeepTextColor = false;

        public void UpdateText(string text) { OnTextUpdateEvent.Invoke(text); }
    }

    public interface IUITextListener {
        void SetListener();
        void UpdateText(string text);
    }
}
