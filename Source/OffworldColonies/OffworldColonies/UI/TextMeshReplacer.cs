using TMPro;
using UIBridge;

namespace OffworldColonies.UI {
    public class TextMeshReplacer: TextMeshProUGUI {
        private UITextHandler _handler;

        private new void Awake() {
            base.Awake();

            _handler = GetComponent<UITextHandler>();

            if (_handler == null)
                return;

            _handler.OnTextUpdate.AddListener(UpdateText);
        }

        private void UpdateText(string t) {
            text = t;
        }

        public void SetStyle(UIStyle style, UILabelOverrides overrides) {
            //the fonts used in KSP are unknown to me, use the default 
            //provided as that seems to be the way ksp does it too
            font = UISkinManager.TMPFont;

            if (!overrides.ignoreSize) fontSize = style.fontSize != 0 ? (float)style.fontSize : 12f;
            if (!overrides.ignoreStyle) fontStyle = TMPProUtil.FontStyle(style.fontStyle);
            if (!overrides.ignoreColor) color = style.normal.textColor;
            if (!overrides.ignoreAlignment) alignment = TMPProUtil.TextAlignment(style.alignment);
        }
    }
}
