using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIBridge
{
    /// <summary>
    /// Defines the type of element this is in Unity for 
    /// use in UI style replacement in KSP.
    /// </summary>
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
            //the following are children of above types and 
            //do not require their own UIElement flag
            //HorizontalScrollbarLeftButton,
            //HorizontalScrollbarRightButton,
            //HorizontalScrollbarThumb,
            //HorizontalSliderThumb,
            //VerticalScrollbarDownButton,
            //VerticalScrollbarThumb,
            //VerticalScrollbarUpButton,
            //VerticalSliderThumb
            VerticalRangeBarCustom, //a custom element type to emulate a vertical resource/loading range bar (non-interactable)
            HorizontalRangeBarCustom, //a custom element type to emulate a horizontal resource/loading range bar (non-interactable)
            AnimatedButtonCustom,
        }

        /// <summary>
        /// Unity Inspector field for type selection
        /// </summary>
        [SerializeField]
        private ElementTypes _elementType = ElementTypes.None;

        /// <summary>
        /// Returns the selected (read-only on KSP side) element type 
        /// </summary>
        public ElementTypes ElementType => _elementType;

        [SerializeField]
        private string _skinOverride;

        public string SkinOverride => _skinOverride;
    }
}
