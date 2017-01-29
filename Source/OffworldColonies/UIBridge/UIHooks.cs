using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UIBridge {

    [RequireComponent(typeof(RectTransform))]
    public class UIHooks : MonoBehaviour {
        [SerializeField]
        private UITextHandler _instructionText;
        [SerializeField]
        private UITextHandler _costText;
        [SerializeField]
        private UITextHandler _timeText;
        [SerializeField]
        private UITextHandler _previewTitleText;
        [SerializeField]
        private UITextHandler _previewDescText;
        [SerializeField]
        private Animator _previewAnimator;
        [SerializeField]
        private List<string> _animationNames;

        //KSP-side button listeners
        public UnityEvent OnSelectEmpty { get; } = new UnityEvent();
        public UnityEvent OnSelectHab { get; } = new UnityEvent();
        public UnityEvent OnSelectAg { get; } = new UnityEvent();
        public UnityEvent OnSelectPow { get; } = new UnityEvent();
        public UnityEvent OnSelectNursery { get; } = new UnityEvent();
        public UnityEvent OnSelectRec { get; } = new UnityEvent();
        public UnityEvent OnSelectEdu { get; } = new UnityEvent();
        public UnityEvent OnSelectCom { get; } = new UnityEvent();
        public UnityEvent OnSelectLP { get; } = new UnityEvent();
        public UnityEvent OnSelectAir { get; } = new UnityEvent();
        public UnityEvent OnSelectCancel { get; } = new UnityEvent();

        //Unity-side button hooks
        public void SelectEmpty() { OnSelectEmpty.Invoke(); }
        public void SelectHab() { OnSelectHab.Invoke(); }
        public void SelectAg() { OnSelectAg.Invoke(); }
        public void SelectPower() { OnSelectPow.Invoke();  }
        public void SelectNursery() { OnSelectNursery.Invoke(); }
        public void SelectRec() { OnSelectRec.Invoke(); }
        public void SelectEdu() { OnSelectEdu.Invoke(); }
        public void SelectCom() { OnSelectCom.Invoke(); }
        public void SelectLP() { OnSelectLP.Invoke(); }
        public void SelectAir() { OnSelectAir.Invoke(); }
        public void SelectCancel() { OnSelectCancel.Invoke(); }
        
        public void ShowPreview(bool show) {
            _previewAnimator.gameObject.SetActive(show);
            _previewDescText.gameObject.SetActive(show);
            _previewTitleText.gameObject.SetActive(show);
        }

        public void UpdateCost(string costString) {
            _costText.OnTextUpdate.Invoke(costString);
        }

        public void UpdateTime(string timeString) {
            _timeText.OnTextUpdate.Invoke(timeString);
        }

        public void UpdateInstructions(string instruction) {
            _instructionText.OnTextUpdate.Invoke(instruction);
        }

        public void UpdatePreview(int animIndex, string tileName, string tileDesc, string buildTime, string tileCost) {
            _previewAnimator.Play(_animationNames[animIndex]);
            _previewDescText.OnTextUpdate.Invoke(tileDesc);
            _previewTitleText.OnTextUpdate.Invoke(tileName);
            UpdateCost(tileCost);
            UpdateTime(buildTime);
        }
    }
}
