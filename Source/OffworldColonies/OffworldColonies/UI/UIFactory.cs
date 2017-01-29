using System;
using TMPro;
using UIBridge;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OffworldColonies.UI {
    public class UIFactory {
        private static UIFactory _instance;
        public static UIFactory Instance => _instance ?? (_instance = new UIFactory());

        public GameObject Create(GameObject uiPrefab, string skinName) {
            GameObject uiObject = Object.Instantiate(uiPrefab);
            UISkinDef uiSkinDef = UISkinManager.GetSkin(skinName);
            //replace all text elements first
            UITextHandler[] textElements = uiObject.GetComponentsInChildren<UITextHandler>(true);
            if (textElements != null) {
                foreach (UITextHandler textElement in textElements)
                    TMProFromText(textElement);
            }

            UIElement[] uiElements = uiObject.GetComponentsInChildren<UIElement>(true);
            if (uiElements != null) {
                foreach (UIElement element in uiElements) {
                    ProcessUIComponents(element, uiSkinDef);
                }
            }

            return uiObject;
        }

        private void ProcessUIComponents(UIElement uiElement, UISkinDef skin) {
            if (uiElement == null || skin == null) return;

            switch (uiElement.ElementType) {
            case UIElement.ElementTypes.Window:
                uiElement.SetImage(skin.window.normal.background, Image.Type.Sliced);
                break;
            case UIElement.ElementTypes.Box:
                uiElement.SetImage(skin.box.normal.background, Image.Type.Sliced);
                break;
            case UIElement.ElementTypes.Button:
                uiElement.SetButton(skin.button.normal.background, 
                    skin.button.highlight.background, 
                    skin.button.active.background, 
                    skin.button.disabled.background);
                break;
            case UIElement.ElementTypes.Toggle:
                uiElement.SetToggle(skin.button.normal.background, 
                    skin.button.highlight.background, 
                    skin.button.active.background, 
                    skin.button.disabled.background);
                break;
            case UIElement.ElementTypes.HorizontalSlider:
                uiElement.SetSlider(skin.horizontalSlider.normal.background,
                    skin.horizontalSliderThumb.normal.background,
                    skin.horizontalSliderThumb.highlight.background,
                    skin.horizontalSliderThumb.active.background, 
                    skin.horizontalSliderThumb.disabled.background);
                break;
            case UIElement.ElementTypes.VerticalSlider:
                uiElement.SetSlider(skin.verticalSlider.normal.background, 
                    skin.verticalSliderThumb.normal.background, 
                    skin.verticalSliderThumb.highlight.background, 
                    skin.verticalSliderThumb.active.background, 
                    skin.verticalSliderThumb.disabled.background);
                break;
            case UIElement.ElementTypes.ScrollView:
                uiElement.SetScrollView(skin.scrollView.normal.background,
                    skin.verticalScrollbar.normal.background,
                    skin.verticalScrollbarThumb.normal.background,
                    skin.horizontalScrollbar.normal.background, 
                    skin.horizontalScrollbarThumb.normal.background);
                break;
            case UIElement.ElementTypes.Label:
                uiElement.GetComponent<TextMeshReplacer>().SetStyle(skin.label, uiElement.LabelOverrides);
                break;
            }
        }

        private void TMProFromText(UITextHandler handler) {
            if (handler == null) return;

            //The TextHandler element should be attached only to objects with a Unity Text element
            //Note that the "[RequireComponent(typeof(Text))]" attribute cannot be attached to TextHandler since Unity will not allow the Text element to be removed
            Text text = handler.GetComponent<Text>();

            if (text == null) return;

            //Cache all of the relevent information from the Text element
            string t = text.text;
            Color c = text.color;
            int i = text.fontSize;
            bool r = text.raycastTarget;
            FontStyles sty = TMPProUtil.FontStyle(text.fontStyle);
            TextAlignmentOptions align = TMPProUtil.TextAlignment(text.alignment);
            float spacing = text.lineSpacing;
            GameObject obj = text.gameObject;

            //The existing Text element must by destroyed since Unity will not allow two UI elements to be placed on the same GameObject
            Object.DestroyImmediate(text);

            TextMeshReplacer textPro = obj.AddComponent<TextMeshReplacer>();
            //Populate the TextMeshPro fields with the cached data from the old Text element
            textPro.text = t;
            textPro.color = c;
            textPro.raycastTarget = r;
            textPro.fontSize = i;
            textPro.alignment = align;
            textPro.fontStyle = sty;
            textPro.lineSpacing = spacing;

            //default TMP Font
            textPro.font = UISkinManager.TMPFont;
            textPro.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;

            textPro.enableWordWrapping = true;
            textPro.isOverlay = false;
            textPro.richText = true;
        }
    }
}