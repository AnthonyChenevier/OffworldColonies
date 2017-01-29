using System.Collections;
using KSP.UI;
using KSPAssets;
using KSPAssets.Loaders;
using ModUtilities;
using OffworldColonies.ColonyManagement;
using OffworldColonies.Part;
using PQSModLoader.TypeDefinitions;
using UIBridge;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OffworldColonies.UI {
    public class BuildUIManager: MonoBehaviour {
        private static ColonyManager CM => ColonyManager.Instance;
        public static BuildUIManager Instance { get; private set; }

        public const ControlTypes BuildControlLocks = 
            ControlTypes.ALL_SHIP_CONTROLS | 
            ControlTypes.CAMERAMODES |
            ControlTypes.TIMEWARP | 
            ControlTypes.QUICKSAVE | 
            ControlTypes.VESSEL_SWITCHING |
            ControlTypes.TARGETING |
            ControlTypes.MAP_TOGGLE |
            ControlTypes.MAP_UI |
            ControlTypes.MANNODE_ADDEDIT |
            ControlTypes.MANNODE_DELETE;

        private bool _ready;
        private bool _loading;

        private GameObject _uiPrefab;

        private GameObject _uiObject;
        private UIHooks _uiHooks;

        private BaseBuilderPart _activeBuilderPart;

        private GhostController _placementGhost;
        private UIDragPanel _dragPanel;
        public GhostController PlacementGhost => _placementGhost ?? (_placementGhost = GhostController.Create());

        public void Awake() {
            ModLogger.Log($"BuildUIManager: Awake in {HighLogic.LoadedScene}");
            if (Instance != null && Instance != this) {
                ModLogger.LogWarning("BuildUIManager is overwriting an existing instance");
                Destroy(Instance);
            }
            Instance = this;

            //Load our UI prefab once here.
            _ready = false;
            _loading = false;
            AssetDefinition panelDef = AssetLoader.AssetDefinitions.Find(a => a.name == "BuilderUIPanel");
            if (panelDef == null) {
                ModLogger.LogError("Could not find BuilderUIPanel asset definition");
                return;
            }
            _loading = AssetLoader.LoadAssets(OnUILoaded, panelDef);
        }


        public void OnDestroy() {
            ModLogger.Log($"BuildUIManager: OnDestroy in {HighLogic.LoadedScene}");
            CancelBuildMode();

            if (PlacementGhost != null) Destroy(PlacementGhost);
            if (_uiHooks != null) {
                _uiHooks.OnSelectEmpty.RemoveListener(OnSelectEmpty);
                //_uiHooks.OnSelectHab.RemoveListener(OnSelectHab);
                //_uiHooks.OnSelectAg.RemoveListener(OnSelectAg);
                //_uiHooks.OnSelectPow.RemoveListener(OnSelectPow);
                //_uiHooks.OnSelectRec.RemoveListener(OnSelectRec);
                //_uiHooks.OnSelectNursery.RemoveListener(OnSelectNursery);
                //_uiHooks.OnSelectEdu.RemoveListener(OnSelectEdu);
                //_uiHooks.OnSelectCom.RemoveListener(OnSelectCom);
                //_uiHooks.OnSelectLP.RemoveListener(OnSelectLP);
                //_uiHooks.OnSelectAir.RemoveListener(OnSelectAir);
                _uiHooks.OnSelectCancel.RemoveListener(OnSelectCancel);
            }

            _dragPanel.onEndDrag.RemoveListener(OnEndDrag);
            _dragPanel.onEndDrag.RemoveListener(OnDrag);

            if (_uiPrefab != null) Destroy(_uiPrefab);
        }

        private void OnUILoaded(AssetLoader.Loader loader) {
            if (loader == null || loader.objects[0] == null)
                return;

            _uiPrefab = loader.objects[0] as GameObject;
            _ready = true;
            _loading = false;
        }

        private IEnumerator InstantiateUIAsync() {
            while (!_ready)
                yield return null;
            InstantiateUI();
        }

        private void InstantiateUI() {
            _uiObject = UIFactory.Instance.Create(_uiPrefab, "MainMenuSkin");
            _uiObject.transform.SetParent(UIMasterController.Instance.mainCanvas.transform, false);
            if (CM.LastUIPosition != Vector3.zero) _uiObject.transform.position = CM.LastUIPosition;
            _dragPanel = _uiObject.AddComponent<UIDragPanel>();
            _dragPanel.dragAnchor = _uiObject.transform;
            //_dragPanel.tmpReplaceOnDrag = new GameObject("tmp");
            _dragPanel.canvasGroup = _uiObject.GetComponent<CanvasGroup>();
            _dragPanel.manualDragOffset = Vector3.zero;
            _dragPanel.onDrag.AddListener(OnDrag);
            _dragPanel.onEndDrag.AddListener(OnEndDrag);

            _uiHooks = _uiObject.GetComponent<UIHooks>();

            _uiHooks.OnSelectEmpty.AddListener(OnSelectEmpty);
            //_uiHooks.OnSelectHab.AddListener(OnSelectHab);
            //_uiHooks.OnSelectAg.AddListener(OnSelectAg);
            //_uiHooks.OnSelectPow.AddListener(OnSelectPow);
            //_uiHooks.OnSelectRec.AddListener(OnSelectRec);
            //_uiHooks.OnSelectNursery.AddListener(OnSelectNursery);
            //_uiHooks.OnSelectEdu.AddListener(OnSelectEdu);
            //_uiHooks.OnSelectCom.AddListener(OnSelectCom);
            //_uiHooks.OnSelectLP.AddListener(OnSelectLP);
            //_uiHooks.OnSelectAir.AddListener(OnSelectAir);
            _uiHooks.OnSelectCancel.AddListener(OnSelectCancel);

            ClearSelected();
        }

        private void OnDrag(PointerEventData eventData) { CM.LastUIPosition = _dragPanel.transform.position; }

        private void OnEndDrag(PointerEventData eventData) { _dragPanel.transform.position = CM.LastUIPosition; }


        private void OnSelectEmpty() {
            ProtoHexTile tile = CM.HexDefinitions[HexTileType.Default];

            if (_uiHooks == null) return;

            _uiHooks.UpdatePreview(tile.PreviewIndex, tile.Name, tile.Desc, tile.TimeString, tile.CostString);
            _uiHooks.ShowPreview(true);

            StartPlacementMode();
        }

        //private void OnSelectAir() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectLP() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectCom() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectEdu() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectNursery() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectRec() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectPow() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectAg() {
        //    throw new NotImplementedException();
        //}

        //private void OnSelectHab() {
        //    throw new NotImplementedException();
        //}

        private void OnSelectCancel() {
            CancelBuildMode();
        }

        public void ClearSelected() {
            if (_uiHooks == null) return;

            _uiHooks.ShowPreview(false);
            _uiHooks.UpdateTime("-");
            _uiHooks.UpdateCost("-");
        }
        
        /// <summary>
        /// Creates a new colony at the given position and
        /// begins the build process for the first base part at that 
        /// position.
        /// </summary>
        /// <param name="selectedCoords">The coordinates </param>
        /// <param name="altitudeOffset"></param>
        /// <param name="rotationOffset"></param>
        /// <param name="initialPartType"></param>
        public void BuildNewColony(BodySurfacePosition selectedCoords, float altitudeOffset, float rotationOffset, HexTileType initialPartType = HexTileType.Default) {

            //TODO: This function is intended as the build process entry point, move 
            //TODO: creation process to end in case of cancelation and begin the build process instead

            string colonyName = "New Colony";
            int colonyID = CM.NextID();
            ScreenMessages.PostScreenMessage($"Beginning Construction of new colony '{colonyName}'", 2f, ScreenMessageStyle.UPPER_CENTER);
            Colony newColony = Colony.Create(colonyName, colonyID, selectedCoords, altitudeOffset, rotationOffset, initialPartType);

            _activeBuilderPart.CurrentColony = newColony;

            newColony.Refresh();
            CM.AddColony(newColony);
        }

        /// <summary>
        ///     Begins the colony creation process.
        /// </summary>
        /// <remarks>
        ///     Design goal and discussion:
        ///     Begins the base selection procedure. This process should:
        ///     - bring up a hexBase ghost model centred under the cursor
        ///     - color the placeholder for valid (blue) and invalid (red)
        ///       placement (over any vessel/object other than the ground,
        ///       too far from builder, etc)
        ///     - allow some control over placement (height & rotation) of
        ///       the initial hex. Other hexes will be built off this in a
        ///       fairly rigid structure (on one of the six sides, no changes
        ///       to altitude, only allow placement if there isn't something
        ///       blocking it, etc)
        ///     - Each subsequent hex is aligned to the initial piece. This
        ///       means that over large distances the furthest pieces will
        ///       have a higher altitude relative to the sphere than the initial
        ///       piece, and that the angle of gravity will change as to the
        ///       gravity well it seems like the euclidean flat structure is
        ///       actually sloping upwards at an increasing angle. A stretch goal
        ///       to combat this is to allow each hex to align itself to the
        ///       surface normal so that the base is always flat *relative to the
        ///       sphere* rather than relative to the initial piece. This would
        ///       necessitate one or more pentagonal pieces, and may be impacted
        ///       by the radii of the different spheres (and even altitude on a
        ///       single sphere) requiring different sized hexes to cover - hard maths :/
        ///       Instead the base size will be limited to a certain radius/number
        ///       of hexes - testing on each body is required to see what the
        ///       gravity differential is for different max-sizes - the smallest, densest
        ///       body vs the largest, rarest body are good test targets.
        ///     Testing/alpha functionality:
        ///     1. Show a ghost at mouse position on sphere (DONE)
        ///     2. Follow mouse on body surface (DONE)
        ///     3. Color ghost for forbidden (red) and allowed (green) placement range (DONE)
        ///     4. Lock out other user controls and pause time during placement (All guis minus
        ///        pause menu and return to space centre/recover vessel (these will have to 
        ///        handle cancelling the action as well)
        /// </remarks>
        public void StartBuildMode(BaseBuilderPart baseBuilderPart) {
            _activeBuilderPart = baseBuilderPart;
            //hide the part UI
            UIPartActionController.Instance.Deselect(true);
            InputLockManager.SetControlLock(BuildControlLocks, "OCSelectColonySite");

            if (_loading || _ready) {
                if (!_loading && _ready)
                    InstantiateUI(); //our prefab is gtg now
                else if (_loading && !_ready)
                    StartCoroutine(InstantiateUIAsync()); //wait for the prefab to finish loading first
            }
            else ModLogger.LogError("StartBuildMode called without loading assets.");
        }


        public void CancelBuildMode() {
            //ScreenMessages.PostScreenMessage("Build Mode Ended", 2f, ScreenMessageStyle.UPPER_CENTER);

            //hide our build UI
            CancelPlacementMode();
            //Restore normal controls
            InputLockManager.RemoveControlLock("OCSelectColonySite");

            if (_uiObject == null) return;

            Destroy(_uiObject);
            _uiHooks = null;
        }

        public void StartPlacementMode() {
            ScreenMessages.PostScreenMessage("Select a starting location for your colony", 5f, ScreenMessageStyle.UPPER_CENTER);
            //Get hex ghost instance and begin tracking the mouse position
            PlacementGhost.Enable(_activeBuilderPart.vessel);
            //inform the player they are now in build mode. Use a ridiculous time because I want this to be active until I destroy it.
            //_activeMsg = ScreenMessages.PostScreenMessage("Build Mode Active", float.MaxValue, ScreenMessageStyle.UPPER_CENTER);
        }

        public void CancelPlacementMode() {
            ClearSelected();
            PlacementGhost.Disable();
        }

        /// <summary>
        /// Deny placement to the player.
        /// </summary>
        public void DenyPlacement() {
            ScreenMessages.PostScreenMessage("<color=red>You may not build in that location</color>", 5f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
