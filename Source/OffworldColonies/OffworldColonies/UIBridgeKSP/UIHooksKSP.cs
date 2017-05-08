using System.Collections.Generic;
using OffworldColonies.Utilities;
using UIBridge;
using UnityEngine;
using UnityEngine.Events;

namespace OffworldColonies.UIBridgeKSP {
    public class UIHooksKSP: MonoBehaviour {
        /// <summary>
        /// Links to the UIHooksUnity script on the UI object
        /// </summary>
        private UIHooksUnity _uiHooksUnity;
        /// <summary>
        /// Setup all UI button listeners 
        /// </summary>
        protected virtual bool UISetupHooks(GameObject uiObject, Dictionary<string, UnityAction> actions) {
            _uiHooksUnity = uiObject.GetComponent<UIHooksUnity>();
            if (_uiHooksUnity == null) {
                ModLogger.LogError("FlightUIController: UIHooksUnity not on prefab");
                return false;
            }

            _uiHooksUnity.OnSelectEmpty.AddListener(actions["SelectEmpty"]);
            _uiHooksUnity.OnSelectHab.AddListener(actions["SelectHab"]);
            _uiHooksUnity.OnSelectAg.AddListener(actions["SelectAg"]);
            _uiHooksUnity.OnSelectPow.AddListener(actions["SelectPow"]);
            _uiHooksUnity.OnSelectRec.AddListener(actions["SelectRec"]);
            _uiHooksUnity.OnSelectNursery.AddListener(actions["SelectNursery"]);
            _uiHooksUnity.OnSelectEdu.AddListener(actions["SelectEdu"]);
            _uiHooksUnity.OnSelectCom.AddListener(actions["SelectCom"]);
            _uiHooksUnity.OnSelectCR.AddListener(actions["SelectCR"]);
            _uiHooksUnity.OnSelectLP.AddListener(actions["SelectLP"]);
            _uiHooksUnity.OnSelectAir.AddListener(actions["SelectAir"]);
            _uiHooksUnity.OnSelectCancel.AddListener(actions["SelectCancel"]);
            _uiHooksUnity.OnBuildPause.AddListener(b => {
                                                       if (b) actions["BuildPause"]();
                                                       else actions["BuildUnpause"]();
                                                   });
            _uiHooksUnity.OnBuildCancel.AddListener(actions["BuildCancel"]);
            _uiHooksUnity.OnStyleChange.AddListener(actions["SwitchStyle"]);

            return true;
        }

        /// <summary>
        /// Cleanup all UIHooksUnity button listeners
        /// </summary>
        protected virtual void UIRemoveHooks() {
            if (_uiHooksUnity == null) return; //already destroyed

            _uiHooksUnity.OnSelectEmpty.RemoveAllListeners();
            _uiHooksUnity.OnSelectHab.RemoveAllListeners();
            _uiHooksUnity.OnSelectAg.RemoveAllListeners();
            _uiHooksUnity.OnSelectPow.RemoveAllListeners();
            _uiHooksUnity.OnSelectRec.RemoveAllListeners();
            _uiHooksUnity.OnSelectNursery.RemoveAllListeners();
            _uiHooksUnity.OnSelectEdu.RemoveAllListeners();
            _uiHooksUnity.OnSelectCom.RemoveAllListeners();
            _uiHooksUnity.OnSelectCR.RemoveAllListeners();
            _uiHooksUnity.OnSelectLP.RemoveAllListeners();
            _uiHooksUnity.OnSelectAir.RemoveAllListeners();
            _uiHooksUnity.OnSelectCancel.RemoveAllListeners();
            _uiHooksUnity.OnBuildCancel.RemoveAllListeners();
            _uiHooksUnity.OnStyleChange.RemoveAllListeners();

            _uiHooksUnity = null;
        }

        protected virtual void UIClearPreview() {
            if (_uiHooksUnity == null) return;
            _uiHooksUnity.ShowPreview(false);
            _uiHooksUnity.UpdateTime("-");
            _uiHooksUnity.UpdateCost("-");
        }

        protected virtual void UIShowSelectionPanel() {
            if (_uiHooksUnity == null) return;
            _uiHooksUnity.ShowSelectionPanel();
        }


        protected virtual void UIShowBuildPanel() {
            if (_uiHooksUnity == null) return;
            _uiHooksUnity.ShowBuildPanel();
        }

        protected virtual void UISetPreviewTile(int tilePreviewIndex, string tileName, string tileDesc, string tileTimeString, string tileCostString) {
            if (_uiHooksUnity == null) return;

            _uiHooksUnity.ShowPreview(true);
            _uiHooksUnity.UpdatePreview(tilePreviewIndex, tileName, tileDesc, tileTimeString, tileCostString);
        }

        protected virtual void UISetInstructions(string instructions) {
            if (_uiHooksUnity == null) return;
            _uiHooksUnity.UpdateInstructions(instructions);
        }

        protected virtual void UISetProgress(float percentComplete, bool paused) {
            if (_uiHooksUnity == null) return;
            _uiHooksUnity.UpdateProgress((int)(percentComplete * 100), paused ? "<color=red>Paused</color>" : null);
        }
    }
}