using TMPro;
using UIBridge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OffworldColonies.UIBridgeKSP {
    public class TextMeshProKSPUI: TextMeshProUGUI, IUITextListener {
        private UITextHandler _textHandler;

        #region IUITextListener implementation

        public void SetListener() {
            UnityEvent<string> updateEvent = _textHandler.OnTextUpdateEvent;
            updateEvent.RemoveAllListeners();
            updateEvent.AddListener(UpdateText);
        }

        public void UpdateText(string t) { text = t; }

        #endregion

        public static TextMeshProKSPUI Replace(Text textUGUI) {
            //Cache all of the relevent information from the Text element
            string t = textUGUI.text;
            Color c = textUGUI.color;
            int i = textUGUI.fontSize;
            bool bestFit = textUGUI.resizeTextForBestFit;
            int maxSize = textUGUI.resizeTextMaxSize;
            int minSize = textUGUI.resizeTextMinSize;
            bool r = textUGUI.raycastTarget;
            FontStyles sty = TMPProUtil.FontStyle(textUGUI.fontStyle);
            TextAlignmentOptions align = TMPProUtil.TextAlignment(textUGUI.alignment);
            float spacing = textUGUI.lineSpacing;
            GameObject obj = textUGUI.gameObject;

            //The existing Text element must by destroyed since Unity will not allow two UI elements to be placed on the same GameObject
            DestroyImmediate(textUGUI);

            TextMeshProKSPUI textMPro = obj.AddComponent<TextMeshProKSPUI>();
            textMPro._textHandler = textMPro.GetComponent<UITextHandler>() ?? textMPro.gameObject.AddComponent<UITextHandler>();
            textMPro.SetListener();
            //Populate the TextMeshPro fields with the cached data from the old Text element
            textMPro.text = t;
            textMPro.color = c;
            textMPro.raycastTarget = r;
            textMPro.fontSize = i;
            textMPro.enableAutoSizing = bestFit;
            textMPro.fontSizeMin = minSize;
            textMPro.fontSizeMax = maxSize;
            textMPro.alignment = align;
            textMPro.fontStyle = sty;
            textMPro.lineSpacing = spacing;

            //default TMP Font
            textMPro.font = UISkinManager.TMPFont;
            textMPro.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;

            textMPro.enableWordWrapping = true;
            textMPro.isOverlay = false;
            textMPro.richText = true;

            return textMPro;
        }

        public void SetStyle(UIStyle style) {
            //the fonts used in KSP are unknown to me, use the default 
            //provided as that seems to be the way ksp does it too
            font = UISkinManager.TMPFont;

            if (!_textHandler.KeepFontSize) fontSize = style.fontSize != 0 ? (float)style.fontSize : 12f;
            if (!_textHandler.KeepFontStyle) fontStyle = TMPProUtil.FontStyle(style.fontStyle);
            if (!_textHandler.KeepTextColor) color = style.normal.textColor;
            if (!_textHandler.KeepTextAlignment) alignment = TMPProUtil.TextAlignment(style.alignment);
        }
    }
}
