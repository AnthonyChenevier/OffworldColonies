using UnityEngine;
using UnityEngine.Events;

namespace UIBridge {
    public class UITextHandler: MonoBehaviour {
        public class OnTextEvent : UnityEvent<string> { }

		private OnTextEvent _onTextUpdate = new OnTextEvent();

		public UnityEvent<string> OnTextUpdate
		{
			get { return _onTextUpdate; }
		}
    }
}
