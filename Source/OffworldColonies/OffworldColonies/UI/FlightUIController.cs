using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using KSP.UI;
using KSPAssets;
using KSPAssets.Loaders;
using OffworldColonies.ColonyManagement;
using OffworldColonies.HexTiles;
using OffworldColonies.Part;
using OffworldColonies.UIBridgeKSP;
using OffworldColonies.Utilities;

namespace OffworldColonies.UI {
    public class FlightUIController: UIHooksKSP {
        private const string ControlsString = "Controls: [Shift + Drag = Elevation] [Ctrl + Drag = Rotation] [Left Click = Place] [Right Click = Cancel]";
        
        public static FlightUIController Instance { get; private set; }

        private bool _loadingComplete;
        private bool _loadingNow;

        private GameObject _uiObjectPrefab;
        private GameObject _uiObjectInstance;

        private readonly List<string> _skins = new List<string> {
                                          "MainMenuSkin",
                                          "KSP window 7",
                                          "MiniSettingsSkin",
                                          "KSP window 1",
                                          "FlagBrowserSkin",
                                          "uiSkinName"
                                      };

        private int _selectedSkinIndex = 0;


        public enum UIMode { None, Selection, Placement, Build }
        public UIMode CurrentMode { get; private set; }
        public bool IsUIOpen { get; private set; }


        /// <summary>
        /// Starts loading the UI Prefab asyncronously. Anonymous callback
        /// sets the loaded prefab and the UI ready flag when complete.
        /// </summary>
        /// <remarks>
        /// - _loadingNow is true as long as the loader has not returned an object
        /// </remarks>
        /// <remarks>
        /// - _loadingComplete will be set to true when the ui object is loaded
        /// </remarks>
        public void Awake() {
            ModLogger.Log($"FlightUIController: Awake in {HighLogic.LoadedScene}");

            //set up our static instance
            if (Instance != null && Instance != this) {
                ModLogger.LogWarning("FlightUIController is overwriting an existing Singleton instance");
                Destroy(Instance);
            }
            Instance = this;
            
            //Load our UI prefab
            _loadingComplete = false;
            _loadingNow = false;

            ModLogger.Log("FlightUIController: Loading UI prefab...");
            AssetDefinition uiDefinition = AssetLoader.AssetDefinitions.Find(a => a.name == "BuilderUIPanel");
            if (uiDefinition != null)
                _loadingNow = AssetLoader.LoadAssets(loader => {
                    if (loader.objects[0] == null) {
                        ModLogger.LogError("Asset Loader could not load BuilderUIPanel asset");
                        _loadingNow = false;
                        return;
                    }
                    _uiObjectPrefab = loader.objects[0] as GameObject;
                    _loadingComplete = true;
                    _loadingNow = false;
                }, uiDefinition);
            else
                ModLogger.LogError("AssetLoader could not find BuilderUIPanel asset definition");
        }


        /// <summary>
        /// Destroys everything
        /// </summary>
        public void OnDestroy() {
            ModLogger.Log($"FlightUIContoller: OnDestroy in {HighLogic.LoadedScene}");
            CloseWindow();
            Destroy(_uiObjectPrefab);
        }

        /// <summary>
        /// Coroutine to wait for prefab loading to complete.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CreateUIWhenLoaded(UIMode startMode) {
            while (!_loadingComplete) {
                ModLogger.Log("FlightUIController: Waiting for UI prefab to load...");
                yield return null;
            }
            CreateUIInstance(startMode);
        }

        /// <summary>
        /// Create a UI Instance from the loaded prefab.
        /// </summary>
        private void CreateUIInstance(UIMode startMode) {
            ModLogger.Log("FlightUIController: UI prefab is loaded. Instantiating UI and setting style");
            string skinName = _skins[_selectedSkinIndex];
            _uiObjectInstance = KSPUIFactory.Create(_uiObjectPrefab, skinName);
            _uiObjectInstance.transform.SetParent(UIMasterController.Instance.mainCanvas.transform, false);
            ModLogger.Log("FlightUIController: Hooking into UI Callbacks");
            Dictionary<string, UnityAction> uiActions = new Dictionary<string, UnityAction> {
                {"SelectEmpty",() => { Instance.SelectTile(HexTileType.Default); }},
                {"SelectLP",() => { Instance.SelectTile(HexTileType.Launchpad); }},
                {"SelectCR",() => { Instance.SelectTile(HexTileType.CommRelay); }},
                {"SelectAir",() => { Instance.SelectTile(HexTileType.Airstrip); }},
                {"SelectHab",() => { Instance.SelectTile(HexTileType.Habitation); }},
                {"SelectPow",() => { Instance.SelectTile(HexTileType.Power); }},
                {"SelectAg",() => { Instance.SelectTile(HexTileType.Agriculture); }},
                {"SelectNursery",() => { Instance.SelectTile(HexTileType.Nursery); }},
                {"SelectRec",() => { Instance.SelectTile(HexTileType.Recreation); }},
                {"SelectEdu",() => { Instance.SelectTile(HexTileType.Education); }},
                {"SelectCom",() => { Instance.SelectTile(HexTileType.Commissary); }},
                {"SelectCancel", () => { Instance.CancelSelection(); }},
                {"BuildPause", () => { Instance.PauseBuild(true);}},
                {"BuildUnpause", () => { Instance.PauseBuild(false);}},
                {"BuildCancel", () => { Instance.CancelBuild(); }},
                {"SwitchStyle", () => { Instance.SwitchStyle(); }}
            };
            if (_uiObjectInstance == null || !UISetupHooks(_uiObjectInstance, uiActions)) {
                ModLogger.LogError("FlightUIController: UI instantiation or hooking up callbacks failed!");
                return;
            }

            GameEvents.onVesselChange.Add(OnVesselChange);

            SetupDragPanel();

            IsUIOpen = true; //we are now open, the rest is setting the current state

            ClearSelected();
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster == null) {
                StartSelectionMode();
                ModLogger.Log("FlightUIController: Window Opened");
                return;
            }
            //clear and show the selection screen to start with
            switch (startMode) {
                case UIMode.Placement:
                StartPlacementMode(printersMaster.SelectedTile);
                    break;
                case UIMode.Build:
                StartBuildMode(printersMaster.SelectedTile);
                    break;
                case UIMode.None:
                case UIMode.Selection:
                default:
                    StartSelectionMode();
                    break;
            }
            ModLogger.Log("FlightUIController: Window Opened");
        }


        private List<HextilePrinterModule> FindLinkedPrinters() {
            return FlightGlobals.ActiveVessel.FindPartModulesImplementing<HextilePrinterModule>();
        }

        private HextilePrinterModule LinkedPrintersMaster() {
            List<HextilePrinterModule> linkedPrinters = FindLinkedPrinters();
            if (linkedPrinters.Count > 0) return FindLinkedPrinters().First();
            PostMessages("No Printer found on Vessel", "ERROR");
            return null;
        }


        /// <summary>
        /// Locks/unlocks most normal flight controls and 
        /// enables/disables the vessel brakes (if they 
        /// aren't already enabled by the player).
        /// Notably does not lock camera controls.
        /// </summary>
        /// <param name="doLock"></param>
        public void LockControls(bool doLock) {
            HextilePrinterModule printer = LinkedPrintersMaster();
            if (printer == null) {
                ModLogger.Log($"FlightUIController: {(doLock ? "L" : "Unl")}ocking Controls failed, no printer master");
                return;
            }
            if (doLock) InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS |
                                                ControlTypes.CAMERAMODES |
                                                ControlTypes.TIMEWARP |
                                                ControlTypes.QUICKSAVE |
                                                ControlTypes.VESSEL_SWITCHING |
                                                ControlTypes.TARGETING |
                                                ControlTypes.MAP_TOGGLE |
                                                ControlTypes.MAP_UI |
                                                ControlTypes.MANNODE_ADDEDIT |
                                                ControlTypes.MANNODE_DELETE, "OCBuildUILock");
            else InputLockManager.RemoveControlLock("OCBuildUILock");

            printer.SetVesselBrakes(doLock);
            ModLogger.Log($"FlightUIController: Controls {(doLock?"L":"Unl")}ocked");
        }


        /// <summary>
        /// Clears the selected tile
        /// </summary>
        public void ClearSelected() {
            UIClearPreview();

            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster != null) printersMaster.SelectedTile = null;
        }


        /// <summary>
        /// Sets the build progress on the UI and the tile placeholder
        /// </summary>
        /// <param name="percentComplete"></param>
        /// <param name="paused"></param>
        public void SetBuildProgress(float percentComplete, bool paused = false) {
            if (CurrentMode != UIMode.Build) return;
            UISetProgress(percentComplete, paused);
        }


        /// <summary>
        /// Post messages to the screen and UI instructions label respectively
        /// </summary>
        /// <param name="screenMessage"></param>
        /// <param name="uiInstructions"></param>
        public void PostMessages(string screenMessage, string uiInstructions) {
            if (screenMessage != null)
                ScreenMessages.PostScreenMessage(screenMessage, 5f, ScreenMessageStyle.UPPER_CENTER);

            if (uiInstructions != null)
                UISetInstructions(uiInstructions);
        }


        /// <summary>
        /// Deny placement to the player and print the reason on screen
        /// </summary>
        public void DenyPlacement(string reason) {
            if (CurrentMode != UIMode.Placement) return;
            PostMessages($"<color=red>You may not build in that location ({reason})</color>", "Try Again. " + ControlsString);
        }

        public void OnVesselChange(Vessel vessel) {
            if (FindLinkedPrinters().Count > 0) return;
            CloseWindow();
        }


        /// <summary>
        /// Opens the Colony Builder UI Window for the given printer part
        /// </summary>
        /// <remarks>
        /// As the prefab begins loading on Awake, and this may be called in 
        /// the time between Awake and _loadingComplete the method fires a 
        /// coroutine to wait if the prefab hasn't loaded already
        /// </remarks>
        public void OpenWindow(UIMode startMode = UIMode.Selection) {
            if (IsUIOpen) return;

            if (_loadingNow || _loadingComplete) {
                if (!_loadingNow && _loadingComplete)
                    CreateUIInstance(startMode); //our prefab is gtg now
                else if (_loadingNow && !_loadingComplete)
                    StartCoroutine(CreateUIWhenLoaded(startMode)); //wait for the prefab to finish loading first
            }
            else ModLogger.LogError("FlightUIController: OpenWindow called without loading prefab");
        }

        /// <summary>
        /// Closes the Builder UI Window
        /// </summary>
        public void CloseWindow() {
            if (_uiObjectInstance == null) return;

            StopBuildMode();
            StopSelectionMode();
            StopPlacementMode();
            UIRemoveHooks();
            DestroyDragPanel();

            Destroy(_uiObjectInstance);
            GameEvents.onVesselChange.Remove(OnVesselChange);

            IsUIOpen = false;
            ModLogger.Log("FlightUIController: Window Closed");
        }


        #region Mode Control
        public void StartSelectionMode() {
            ModLogger.Log("FlightUIController: Starting Selection Mode");
            CurrentMode = UIMode.Selection;

            PostMessages(null, "Select a tile");
            UIShowSelectionPanel();
        }



        private void StopSelectionMode() {
            if (CurrentMode != UIMode.Selection) return;
            ModLogger.Log("FlightUIController: Stopping Selection Mode");
            CurrentMode = UIMode.None;
            ClearSelected();
        }


        /// <summary>
        /// Starts placement mode. Placement mode is like selection 
        /// mode- all of the ui controls are the same and work as expected.
        /// In addition, placement mode locks out most controls and enables the tile placement
        /// placeholder and interface for selecting the position, elevation and
        /// rotation of the selected tile.
        /// </summary>
        public void StartPlacementMode(HexTileDefinition selectedTile) {
            ModLogger.Log("FlightUIController: Starting Placement Mode");
            CurrentMode = UIMode.Placement;
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster == null) return;

            //Get hex ghost instance and begin tracking the mouse position
            LockControls(true);
            PostMessages("Vehicle controls locked", "Select a build location. " + ControlsString);
            printersMaster.EnablePlacementPlaceholder(selectedTile.ModelDefinition.LODDefines[0].Models[0]);
        }


        /// <summary>
        /// Stops placement mode
        /// </summary>
        public void StopPlacementMode() {
            if (CurrentMode != UIMode.Placement) return;
            ModLogger.Log("FlightUIController: Stopping Placement Mode");
            CurrentMode = UIMode.None;
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster == null) return;

            LockControls(false);
            PostMessages("Vehicle controls unlocked", "Select a tile");
            printersMaster.DisablePlacementPlaceholder();
            ClearSelected();
        }


        /// <summary>
        /// Starts build mode
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="bodyPos"></param>
        /// <param name="rotationOffset"></param>
        /// <param name="heightOffset"></param>
        public void StartBuildMode(HexTileDefinition tile) {
            ModLogger.Log("FlightUIController: Starting Build Mode");
            CurrentMode = UIMode.Build;

            UISetProgress(0, false);
            UISetPreviewTile(tile.PreviewIndex, tile.Name, tile.Desc, tile.TimeString, tile.CostString);
            UIShowBuildPanel();
        }


        /// <summary>
        /// Stops build mode
        /// </summary>
        public void StopBuildMode() {
            if (CurrentMode != UIMode.Build) return;
            ModLogger.Log("FlightUIController: Stopping Build Mode");
            CurrentMode = UIMode.None;
            ClearSelected();
        }
        #endregion

        #region DragPanel Implementation
        /// <summary>
        /// Sets up our drag panel stuff
        /// </summary>
        private void SetupDragPanel() {
            UIDragPanel dragPanel = _uiObjectInstance.AddOrGetComponent<UIDragPanel>();
            dragPanel.dragAnchor = _uiObjectInstance.transform;
            dragPanel.canvasGroup = _uiObjectInstance.GetComponent<CanvasGroup>();
            dragPanel.manualDragOffset = Vector3.zero;

            if (ColonyManager.Instance.LastUIPosition != Vector3.zero)
                _uiObjectInstance.transform.position = ColonyManager.Instance.LastUIPosition;

            dragPanel.onDrag.AddListener(e => { ColonyManager.Instance.LastUIPosition = dragPanel.transform.position; });
            dragPanel.onEndDrag.AddListener(e => { dragPanel.transform.position = ColonyManager.Instance.LastUIPosition; });
        }


        /// <summary>
        /// Safely tidies up drag listeners
        /// </summary>
        private void DestroyDragPanel() {
            UIDragPanel dragPanel = _uiObjectInstance.GetComponent<UIDragPanel>();
            if (dragPanel == null)
                return;

            dragPanel.onDrag.RemoveAllListeners();
            dragPanel.onEndDrag.RemoveAllListeners();

            Destroy(dragPanel);
        }
        #endregion


        #region UI Button Listeners
        /// <summary>
        /// Selection button handler. Selects the given tile type, 
        /// updates preview window and begins placement mode.
        /// </summary>
        /// <param name="tileType">The type of tile selected (depends on button pressed)</param>
        private void SelectTile(HexTileType tileType) {
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster == null) return;

            HexTileDefinition selectedTile = ColonyManager.Instance.HexDefinitions[tileType];
            printersMaster.SelectedTile = selectedTile;
            UISetPreviewTile(selectedTile.PreviewIndex, selectedTile.Name, selectedTile.Desc, selectedTile.TimeString, selectedTile.CostString);
            StartPlacementMode(selectedTile);
        }

        /// <summary>
        /// Build panel cancel button
        /// </summary>
        private void CancelBuild() {
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster == null) return;

            ColonyManager.Instance.CancelBuildOrder(printersMaster.CurrentColony, printersMaster.CurrentOrderID);
        }

        /// <summary>
        /// Build panel cancel button
        /// </summary>
        /// <param name="doPause"></param>
        private void PauseBuild(bool doPause) {
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            if (printersMaster == null) return;
            ColonyManager.Instance.PauseBuildOrder(printersMaster.CurrentColony, printersMaster.CurrentOrderID, doPause);
        }


        /// <summary>
        /// Switch style button. changes the current UI style and restarts the UI
        /// </summary>
        private void SwitchStyle() {
            UIMode currentMode = CurrentMode;
            HextilePrinterModule printersMaster = LinkedPrintersMaster();
            HexTileDefinition selectedTile = printersMaster != null ? printersMaster.SelectedTile : null;

            CloseWindow();
            _selectedSkinIndex++;
            if (_selectedSkinIndex >= _skins.Count)
                _selectedSkinIndex = 0;
            PostMessages($"Switching UI skin to '{_skins[_selectedSkinIndex]}'", null);
            OpenWindow();

            if (selectedTile == null) {
                ModLogger.LogError("Selected Tile or printersMaster is NULL. Reverting to Selection Mode");
                StartSelectionMode();
                return;
            }
            
            switch (currentMode) {
            case UIMode.Selection:
                StartSelectionMode();
                break;
            case UIMode.Placement:
                StartPlacementMode(selectedTile);
                break;
            case UIMode.Build:
                StartBuildMode(selectedTile);
                break;
            //case UIMode.None:
            default:
                ModLogger.LogError("UIMode unset during switch!");
                break;
            }
        }

        /// <summary>
        /// Selection panel cancel button. Stops placement mode (if active) and closes the UI
        /// </summary>
        private void CancelSelection() {
            StopPlacementMode();
            CloseWindow();
        }
        #endregion
    }
}
