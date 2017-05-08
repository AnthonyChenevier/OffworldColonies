using System;
using KSP.UI;
using UIBridge;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OffworldColonies.UIBridgeKSP {
    public static class KSPUIFactory {
        public static GameObject Create(GameObject uiPrefab, string defaultSkinName) {
            GameObject uiObject = Object.Instantiate(uiPrefab);
            UISkinDef defaultSkin = UISkinManager.GetSkin(defaultSkinName);

            UIElement[] uiElements = uiObject.GetComponentsInChildren<UIElement>(true);
            if (uiElements == null) return uiObject;

            foreach (UIElement uiElement in uiElements) {
                if (uiElement == null || defaultSkin == null) continue;

                UISkinDef elementSkin;
                if (!string.IsNullOrEmpty(uiElement.SkinOverride))
                    elementSkin = UISkinManager.GetSkin(uiElement.SkinOverride) ?? defaultSkin;
                else
                    elementSkin = defaultSkin;

                switch (uiElement.ElementType) {
                    case UIElement.ElementTypes.Window:
                        uiElement.StyleImage(elementSkin.window.normal.background, Image.Type.Sliced);
                        break;
                    case UIElement.ElementTypes.Box:
                        uiElement.StyleImage(elementSkin.box.normal.background, Image.Type.Sliced);
                        break;
                    case UIElement.ElementTypes.Label:
                        uiElement.StyleLabel(elementSkin.label);
                        break;
                    case UIElement.ElementTypes.Button:
                        uiElement.StyleButton(elementSkin.button);
                        break;
                    case UIElement.ElementTypes.Toggle:
                        uiElement.StyleToggleButton(elementSkin.button);
                        break;
                    case UIElement.ElementTypes.HorizontalSlider:
                        uiElement.StyleSlider(elementSkin.horizontalSlider, elementSkin.horizontalSliderThumb);
                        break;
                    case UIElement.ElementTypes.VerticalSlider:
                        uiElement.StyleSlider(elementSkin.verticalSlider, elementSkin.verticalSliderThumb);
                        break;
                    case UIElement.ElementTypes.ScrollView:
                        uiElement.StyleScrollView(elementSkin.scrollView, elementSkin.verticalScrollbar,
                                                elementSkin.verticalScrollbarThumb, elementSkin.horizontalScrollbar,
                                                elementSkin.horizontalScrollbarThumb);
                        break;
                    case UIElement.ElementTypes.HorizontalScrollbar:
                        uiElement.StyleScrollbar(elementSkin.horizontalScrollbar, elementSkin.horizontalScrollbarThumb,
                                               elementSkin.horizontalScrollbarLeftButton,
                                               elementSkin.horizontalScrollbarRightButton);
                        break;
                    case UIElement.ElementTypes.VerticalScrollbar:
                        uiElement.StyleScrollbar(elementSkin.verticalScrollbar, elementSkin.verticalScrollbarThumb,
                                               elementSkin.verticalScrollbarUpButton,
                                               elementSkin.verticalScrollbarDownButton);
                        break;
                    case UIElement.ElementTypes.VerticalRangeBarCustom:
                        uiElement.StyleRangeBar(elementSkin.verticalSlider, elementSkin.verticalSliderThumb);
                        break;
                case UIElement.ElementTypes.HorizontalRangeBarCustom:
                    uiElement.StyleRangeBar(elementSkin.horizontalSlider, elementSkin.horizontalSliderThumb);
                    break;
                case UIElement.ElementTypes.AnimatedButtonCustom:
                    uiElement.StyleAnimatedButton(elementSkin.button);
                    break;
                default: throw new ArgumentOutOfRangeException();
                }
            }

            return uiObject;
        }

        public static void StyleImage(this UIElement uiElement, Sprite sprite, Image.Type type) {
            Image image = uiElement.GetComponent<Image>();
            if (image == null) return;

            image.sprite = sprite;
            image.type = type;
        }



        public static void StyleLabel(this UIElement uiElement, UIStyle labelStyle) {
            Text oldText = uiElement.GetComponent<Text>();
            if (oldText == null) return;

            TextMeshProKSPUI.Replace(oldText).SetStyle(labelStyle);
        }



        public static void StyleSelectable(this UIElement uiElement, UIStyle selectableStyle) {
            Selectable select = uiElement.GetComponent<Selectable>();
            if (select == null)
                return;

            Sprite normal = selectableStyle.normal.background;
            Sprite highlight = selectableStyle.highlight.background;
            Sprite active = selectableStyle.active.background;
            Sprite inactive = selectableStyle.disabled.background;

            select.image.sprite = normal;
            select.image.type = Image.Type.Sliced;
            select.transition = Selectable.Transition.SpriteSwap;

            SpriteState spriteState = select.spriteState;
            spriteState.highlightedSprite = highlight;
            spriteState.pressedSprite = active;
            spriteState.disabledSprite = inactive;
            select.spriteState = spriteState;
        }



        public static void StyleAnimatedButton(this UIElement uiElement, UIStyle buttonStyle) {
            uiElement.StyleButton(buttonStyle);

            UIAnimatedImage animator = uiElement.GetComponentInChildren<UIAnimatedImage>();
            if (animator == null) return;

            //add a KSP UIOnHover listener. very convenient :)
            //AnimationName[1] is the active animation, AnimationName[0] is the idle animation
            UIOnHover hoverListener = animator.gameObject.AddComponent<UIOnHover>();
            hoverListener.onEnter.AddListener(() => { animator.Play(animator.AnimationNames[1]); });
            hoverListener.onExit.AddListener(() => { animator.Play(animator.AnimationNames[0]); });
        }



        public static void StyleButton(this UIElement uiElement, UIStyle buttonStyle) {
            uiElement.StyleSelectable(buttonStyle);
        }



        public static void StyleToggleButton(this UIElement uiElement, UIStyle buttonStyle) {
            uiElement.StyleSelectable(buttonStyle);

            Toggle toggle = uiElement.GetComponent<Toggle>();
            if (toggle == null) return;

            //The "checkmark" sprite is replaced with the "active" sprite; this is only displayed when the toggle is in the true state
            Image toggleImage = toggle.graphic as Image;
            if (toggleImage == null) return;

            toggleImage.sprite = buttonStyle.active.background;
            toggleImage.type = Image.Type.Sliced;
        }



        public static void StyleSlider(this UIElement uiElement, UIStyle sliderStyle, UIStyle thumbStyle) {
            //The slider thumb is the selectable component
            uiElement.StyleSelectable(thumbStyle);

            Sprite background = sliderStyle.normal.background;
            if (background == null) return;

            Slider slider = uiElement.GetComponent<Slider>();
            if (slider == null) return;

            Image back = slider.GetComponentInChildren<Image>();
            if (back == null) return;

            back.sprite = background;
            back.type = Image.Type.Sliced;
        }



        public static void StyleRangeBar(this UIElement uiElement, UIStyle scrollbarStyle, UIStyle thumbStyle) {
            Slider slider = uiElement.GetComponent<Slider>();
            if (slider == null) return;

            Sprite background = scrollbarStyle.normal.background;
            Image back = slider.GetComponentInChildren<Image>();
            if (background != null && back != null) {
                back.sprite = background;
                back.type = Image.Type.Sliced;
            }

            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill == null) return;

            Sprite fillSprite = thumbStyle.normal.background;
            if (fillSprite == null) return;

            fill.sprite = fillSprite;
            fill.type = Image.Type.Sliced;
        }



        private static void StyleScrollbar(this Scrollbar scrollbar, UIStyle scrollbarStyle, UIStyle sbThumbStyle, UIStyle sbBackStyle, UIStyle sbForwardStyle) {
            Sprite background = scrollbarStyle.normal.background;
            if (background == null) return;

            Image image = scrollbar.GetComponent<Image>();
            if (image == null) return;

            image.sprite = background;

            Sprite thumb = sbThumbStyle.normal.background;
            if (thumb == null) return;

            Image tImage = scrollbar.targetGraphic.GetComponent<Image>();
            if (tImage == null) return;

            tImage.sprite = thumb;
        }



        public static void StyleScrollbar(this UIElement uiElement, UIStyle scrollbarStyle, UIStyle sbThumbStyle, UIStyle sbBackStyle, UIStyle sbForwardStyle) {
            Scrollbar scrollbar = uiElement.GetComponent<Scrollbar>();
            if (scrollbar == null) return;
            scrollbar.StyleScrollbar(scrollbarStyle, sbThumbStyle, sbBackStyle, sbForwardStyle);
        }



        public static void StyleScrollView(this UIElement uiElement, UIStyle scrollViewStyle, UIStyle vScrollStyle, UIStyle vScrollThumbStyle, UIStyle hScrollStyle, UIStyle hScrollThumbStyle) {
            Sprite background = scrollViewStyle.normal.background;
            if (background == null) return;

            ScrollRect scrollRect = uiElement.GetComponent<ScrollRect>();
            if (scrollRect == null) return;

            if (scrollRect.vertical)
                scrollRect.verticalScrollbar.StyleScrollbar(vScrollStyle, vScrollThumbStyle, null, null);
            if (scrollRect.horizontal)
                scrollRect.horizontalScrollbar.StyleScrollbar(hScrollStyle, hScrollThumbStyle, null, null);

            Image back = scrollRect.GetComponent<Image>();
            if (back == null) return;
            
            back.sprite = background;
            back.type = Image.Type.Sliced;
        }
    }
}