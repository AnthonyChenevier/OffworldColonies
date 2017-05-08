using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UIBridge {

    [RequireComponent(typeof(RectTransform))]
    public class UIHooksUnity : MonoBehaviour {
        [SerializeField]
        private UITextHandler _instructionText;
        [SerializeField]
        private CanvasGroup _buildPanel;
        [SerializeField]
        private UITextHandler _progressText;
        [SerializeField]
        private Slider _progressSlider;

        [SerializeField]
        private CanvasGroup _selectionPanel;
        [SerializeField]
        private UITextHandler _costText;
        [SerializeField]
        private UITextHandler _timeText;
        [SerializeField]
        private UITextHandler _previewTitleText;
        [SerializeField]
        private UITextHandler _previewDescText;
        [SerializeField]
        private UIAnimatedImage _previewAnimator;


        //KSP-side button listeners
        public UnityEvent OnSelectEmpty { get; } = new UnityEvent();
        public UnityEvent OnSelectHab { get; } = new UnityEvent();
        public UnityEvent OnSelectAg { get; } = new UnityEvent();
        public UnityEvent OnSelectPow { get; } = new UnityEvent();
        public UnityEvent OnSelectNursery { get; } = new UnityEvent();
        public UnityEvent OnSelectRec { get; } = new UnityEvent();
        public UnityEvent OnSelectEdu { get; } = new UnityEvent();
        public UnityEvent OnSelectCom { get; } = new UnityEvent();
        public UnityEvent OnSelectCR { get; } = new UnityEvent();
        public UnityEvent OnSelectLP { get; } = new UnityEvent();
        public UnityEvent OnSelectAir { get; } = new UnityEvent();
        public UnityEvent OnSelectCancel { get; } = new UnityEvent();

        public Toggle.ToggleEvent OnBuildPause { get; } = new Toggle.ToggleEvent();
        public UnityEvent OnBuildCancel { get; } = new UnityEvent();
        public UnityEvent OnStyleChange { get; } = new UnityEvent();

        //Unity-side button hooks
        public void SelectEmpty() { OnSelectEmpty.Invoke(); }
        public void SelectHab() { OnSelectHab.Invoke(); }
        public void SelectAg() { OnSelectAg.Invoke(); }
        public void SelectPower() { OnSelectPow.Invoke();  }
        public void SelectNursery() { OnSelectNursery.Invoke(); }
        public void SelectRec() { OnSelectRec.Invoke(); }
        public void SelectEdu() { OnSelectEdu.Invoke(); }
        public void SelectCom() { OnSelectCom.Invoke(); }
        public void SelectCP() { OnSelectCR.Invoke(); }
        public void SelectLP() { OnSelectLP.Invoke(); }
        public void SelectAir() { OnSelectAir.Invoke(); }
        public void SelectCancel() { OnSelectCancel.Invoke(); }

        public void BuildPause(bool toggle) { OnBuildPause.Invoke(toggle); }
        public void BuildCancel() { OnBuildCancel.Invoke(); }
        public void StyleChange() { OnStyleChange.Invoke(); }

        public void ShowPreview(bool show) {
            _previewDescText.gameObject.SetActive(show);
            _previewTitleText.gameObject.SetActive(show);
            _previewAnimator.gameObject.SetActive(show);

            //if (!show) {
            //    _previewAnimator.Stop();
            //    _previewAnimator.gameObject.SetActive(false);
            //}
            //else {
            //    _previewAnimator.gameObject.SetActive(true);
            //    _previewAnimator.StartPlayback();
            //}
        }

        public void UpdateCost(string costString) {
            _costText.UpdateText(costString);
        }

        public void UpdateProgress(int percent, string overrideText = null) {
            _progressText.UpdateText(string.IsNullOrEmpty(overrideText) ? $"{percent}%" : overrideText);
            _progressSlider.value = percent;
        }

        public void UpdateTime(string timeString) {
            _timeText.UpdateText(timeString);
        }

        public void UpdateInstructions(string instruction) {
            _instructionText.UpdateText(instruction);
        }

        public void UpdatePreview(int animIndex, string tileName, string tileDesc, string buildTime, string tileCost) {
            _previewAnimator.Play(_previewAnimator.AnimationNames[animIndex]);

            _previewDescText.UpdateText(tileDesc);
            _previewTitleText.UpdateText(tileName);

            UpdateCost(tileCost);
            UpdateTime(buildTime);
        }

        public void ShowSelectionPanel() {
            _selectionPanel.alpha = 1;
            _selectionPanel.interactable = true;
            _selectionPanel.blocksRaycasts = true;
            _buildPanel.alpha = 0;
            _buildPanel.interactable = false;
            _buildPanel.blocksRaycasts = false;
        }

        public void ShowBuildPanel() {
            _selectionPanel.alpha = 0;
            _selectionPanel.interactable = false;
            _selectionPanel.blocksRaycasts = false;
            _buildPanel.alpha = 1;
            _buildPanel.interactable = true;
            _buildPanel.blocksRaycasts = true;
        }
    }
}
