using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIBridge
{
    [RequireComponent(typeof(RectTransform))]
    public class UIElement: MonoBehaviour
    {
        public enum ElementTypes {
            None,
            Window,
            Box,
            Button,
            Toggle,
            Label,
            TextArea,
            TextField,
            ScrollView,
            HorizontalScrollbar,
            HorizontalSlider,
            VerticalScrollbar,
            VerticalSlider,
            //the following are children of above types
            //HorizontalScrollbarLeftButton,
            //HorizontalScrollbarRightButton,
            //HorizontalScrollbarThumb,
            //HorizontalSliderThumb,
            //VerticalScrollbarDownButton,
            //VerticalScrollbarThumb,
            //VerticalScrollbarUpButton,
            //VerticalSliderThumb
        }

        [SerializeField]
        private ElementTypes _elementType = ElementTypes.None;

        public UILabelOverrides LabelOverrides { get; private set; }

        public ElementTypes ElementType {
            get { return _elementType; }
        }



        private void Awake() {
            if (_elementType == ElementTypes.Label) {
                LabelOverrides = gameObject.GetComponent<UILabelOverrides>() ??
                                  gameObject.AddComponent<UILabelOverrides>();
            }
        }

        private void SetSelectable(Sprite normal, Sprite highlight, Sprite active, Sprite inactive) {
            Selectable select = GetComponent<Selectable>();

            if (select == null)
                return;

            select.image.sprite = normal;
            select.image.type = Image.Type.Sliced;
            select.transition = Selectable.Transition.SpriteSwap;

            SpriteState spriteState = select.spriteState;
            spriteState.highlightedSprite = highlight;
            spriteState.pressedSprite = active;
            spriteState.disabledSprite = inactive;
            select.spriteState = spriteState;
        }

        public void SetImage(Sprite sprite, Image.Type type) {
            Image image = GetComponent<Image>();

            if (image == null)
                return;

            image.sprite = sprite;
            image.type = type;
        }

        public void SetButton(Sprite normal, Sprite highlight, Sprite active, Sprite inactive) {
            SetSelectable(normal, highlight, active, inactive);
        }

        public void SetToggle(Sprite normal, Sprite highlight, Sprite active, Sprite inactive) {
            SetSelectable(normal, highlight, active, inactive);

            Toggle toggle = GetComponent<Toggle>();

            if (toggle == null)
                return;

            //The "checkmark" sprite is replaced with the "active" sprite; this is only displayed when the toggle is in the true state
            Image toggleImage = toggle.graphic as Image;

            if (toggleImage == null)
                return;

            toggleImage.sprite = active;
            toggleImage.type = Image.Type.Sliced;
        }

        public void SetSlider(Sprite background, Sprite thumb, Sprite thumbHighlight, Sprite thumbActive, Sprite thumbInactive) {
            //The slider thumb is the selectable component
            SetSelectable(thumb, thumbHighlight, thumbActive, thumbInactive);

            if (background == null)
                return;

            Slider slider = GetComponent<Slider>();

            if (slider == null)
                return;

            Image back = slider.GetComponentInChildren<Image>();

            if (back == null)
                return;

            back.sprite = background;
            back.type = Image.Type.Sliced;
        }


        public void SetScrollView(Sprite background, Sprite vScrollBackground, Sprite vThumbBackground, Sprite hScrollBackground, Sprite hThumbBackground) {
            ScrollRect scrollRect = GetComponent<ScrollRect>();

            if (scrollRect == null)
                return;

            if (scrollRect.vertical) {
                scrollRect.verticalScrollbar.GetComponent<Image>().sprite = vScrollBackground;
                scrollRect.verticalScrollbar.GetComponent<Scrollbar>().targetGraphic.GetComponent<Image>().sprite = vThumbBackground;
            }
            if (scrollRect.horizontal) {
                scrollRect.horizontalScrollbar.GetComponent<Image>().sprite = hScrollBackground;
                scrollRect.horizontalScrollbar.GetComponent<Scrollbar>().targetGraphic.GetComponent<Image>().sprite = hThumbBackground;
            }

            Image back = GetComponent<Image>();

            if (back == null)
                return;

            back.sprite = background;
            back.type = Image.Type.Sliced;
        }

    }
}
