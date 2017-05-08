using OffworldColonies.ColonyManagement;
using OffworldColonies.HexTiles;
using OffworldColonies.UI;
using OffworldColonies.Utilities;
using PQSModLoader.TypeDefinitions;

namespace OffworldColonies.Part {
    /// <summary>
    ///     HextilePrinterModule is the part that allows a vessel to create new bases.
    ///     This class handles the positioning of a new base, including checks for distance to other existing bases.
    ///     It also handles adding new base pieces onto existing bases within range.
    ///     Checking for the position of a new base means showing a ghost version of the
    ///     base model and allowing the user to adjust its position. The initial piece takes
    ///     All subsequent base pieces in the same base
    ///     will use the inital piece as a reference and must be connected to one of the existing pieces.
    /// </summary>
    public class HextilePrinterModule : PartModule, IAnimatedModule, IColonyLinked, ISharedResourceProvider {

        //for cheating
        [KSPField] private bool _useResources = true;

        //Printer state display
        [KSPField(guiActive = true, guiName = "Status")]
        private string _status = "ready to print";
        private BaseEvent _openUIButton;
        
        private bool _isValidSituation;
        private bool _brakingSetFlag;

        private bool _userPaused;
        private bool _reportedResourceLack;
        private bool _printingEnabled;

        [KSPField(isPersistant = true)]
        private bool _printerWorking;

        public Colony CurrentColony {
            get { return LinkModule ? LinkModule.CurrentColony : null; }
            set {
                if (LinkModule == null) ModLogger.LogError("LinkModule not set.");
                else LinkModule.CurrentColony = value;
            }
        }

        private PlacementPlaceholder _placementPlaceholder;

        public ColonyLinkModule LinkModule { get; set; }

        public HexTileDefinition SelectedTile { get; set; }

        private int _currentOrderID;
        private ModuleAnimationGroup _animator;
        public int CurrentOrderID => _currentOrderID;

        public void SetCurrentOrderID(int value) {
            _currentOrderID = value;
            SetWorking(true);
        }
        private bool IsVesselMoving() => part.vessel.horizontalSrfSpeed > 0.05;


        public override void OnStart(StartState state) {
            if (!HighLogic.LoadedSceneIsFlight) return;

            _openUIButton = Events["OpenPrinterUI"];
            RefreshStatus();
            base.OnStart(state);
        }


        public override void OnLoad(ConfigNode node) { }


        public override void OnUpdate() {
            if (!HighLogic.LoadedSceneIsFlight) return;

            RefreshStatus();

            //TODO: maybe not every frame, maybe check for resources before retrying?
            //if we have reported a lack of resources then try again
            //if (_reportedResourceLack) {
            //    _reportedResourceLack = false;
            //    SetWorking(true);
            //}

            base.OnUpdate();
        }


        public void FixedUpdate() {
            if (!HighLogic.LoadedSceneIsFlight || !_printerWorking ||
                CurrentColony == null || !CurrentColony.HasBuildOrder(_currentOrderID))
                return;

            if (!ColonyManager.Instance.ProcessBuildOrder(CurrentColony, _currentOrderID))
                ModLogger.Log($"Processing build order {_currentOrderID} failed for '{CurrentColony.ColonyName}'");
        }

        public void OnDestroy() {
            if (_placementPlaceholder != null) Destroy(_placementPlaceholder);
        }

        private void RefreshStatus() {
            //bool lCanUse = _printingEnabled;
            _isValidSituation = IsSituationValid();
            //_printingEnabled = _isValidSituation && !FlightUIController.Instance.IsUIOpen;
            //if (_printingEnabled != lCanUse)
            //    _openUIButton.active = _printingEnabled;

            string lStatus = _status;
            _status = StatusString();
            if (_status != lStatus)
                MonoUtilities.RefreshContextWindows(part);
        }


        public override string GetInfo() {
            return "Print surface structures with local or imported resources";
        }


        private string StatusString() {
            if (!_isValidSituation)
                return "locked (invalid situation)";
            if (IsVesselMoving())
                return "locked (moving)";
            if (_userPaused)
                return "paused";
            if (_reportedResourceLack)
                return "not enough resources";

            return _printerWorking ? "working" : "ready to print";
        }


        /// <summary>
        /// Activates/deactivates the brakes action group, but only if 
        /// it isn't already set active by the player. 
        /// </summary>
        /// <param name="active"></param>
        public void SetVesselBrakes(bool active) {
            //if we are setting the brakes and they aren't already 
            //set remember that we set them ourselves 
            if (active && !vessel.ActionGroups[KSPActionGroup.Brakes]) {
                _brakingSetFlag = true;
                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            }
            //if we are unsetting the brakes and the flag is set, then unset them
            else if (!active && _brakingSetFlag) {
                _brakingSetFlag = false;
                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
            }
        }

        /// <summary>
        /// Setter also stops animationGroup animation
        /// </summary>
        public void SetWorking(bool isWorking) {
            _printerWorking = isWorking;

            //this *should* be guaranteed to exist as we are an IAnimatedModule.
            //IAnimatedModule and ModuleAnimationGroup's implementation does not stop 
            //an active animation when disabled by design (for good gameplay reasons I think).
            //however, this module's active animation is designed to be an indicator of whether the 
            //process is currently working, so we need to turn the animation off manually
            _animator = GetComponent<ModuleAnimationGroup>();
            if (_animator == null) {
                ModLogger.LogWarning("HextilePrinterModule: No ModuleAnimationGroup found");
                return;
            }
            if (!_printerWorking) _animator.ActiveAnimation.Stop(_animator.activeAnimationName);
        }

        #region IAnimatedModule implementation

        public void EnableModule() { _printingEnabled = true; }

        public void DisableModule() { _printingEnabled = false; }

        //used to enable/disable active animation on ModuleAnimationGroup*
        // *kind of, we also need to manually disable the animation 
        public bool ModuleIsActive() { return _printerWorking; }

        /// <summary>
        /// Only allows ModuleAnimationGroup deployment and 
        /// HextilePrinterModule usage in these situations.
        /// </summary>
        /// <returns></returns>
        public bool IsSituationValid() {
            return ColonyManager.Instance.AllowedSituations.Contains(vessel.situation);
        }

        #endregion
        

        /// <summary>
        /// Opens the Colony Builder window
        /// </summary>
        [KSPEvent(guiName = "Open Printer Menu", active = true, guiActive = true)]
        public void OpenPrinterUI() {
            if (!_isValidSituation || IsVesselMoving()) {
                FlightUIController.Instance.PostMessages("<color=red>You must be at rest to use the Printer</color>", null);
                return;
            }

            FlightUIController.Instance.OpenWindow();
            
            //TODO:can't retract while window is open

            //hide the part action UI
            //UIPartActionController.Instance.Deselect(true);
            RefreshStatus();
        }


        /// <summary>
        /// Event listener. Creates the given tile at the given grid 
        /// position and adds the colony to the Manager
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="hexPosition"></param>
        /// <param name="tileRotation"></param>
        public void OnBuildCompleted(HexTileDefinition tile, int hexPosition, float tileRotation) {
            SetWorking(false);
            TimeWarp.SetRate(0, false);

            FlightUIController.Instance.StopBuildMode();
            FlightUIController.Instance.StartSelectionMode();
            RefreshStatus();

            HexTile hexTile = HexTile.Create(CurrentColony, hexPosition, tile.TileType, tileRotation);
            CurrentColony.AddTile(hexTile);
            CurrentColony.Refresh();
            if (!ColonyManager.Instance.Contains(CurrentColony))
                ColonyManager.Instance.Add(CurrentColony);
            ModLogger.Log("HextilePrinterModule: Build Complete");
        }



        public void OnBuildPaused(bool doPause, float completion) {
            SetWorking(doPause);
            _userPaused = doPause;
            RefreshStatus();
            ModLogger.Log($"HextilePrinterModule: Build {(doPause?"P":"Unp")}aused");
        }



        public void OnBuildCanceled(float completion) {
            //only destroy the colony instance if it isn't an existing one (in the Manager)
            if (!ColonyManager.Instance.Contains(CurrentColony)) {
                Destroy(CurrentColony);
                CurrentColony = null;
            }
            SetWorking(false);
            RefreshStatus();
            ModLogger.Log("HextilePrinterModule: Build Canceled");
        }



        public void OnResourceLack(int resID, double requested, double available) {
            _reportedResourceLack = true;
            SetWorking(false);
            RefreshStatus();
            string resName = PartResourceLibrary.Instance.GetDefinition(resID).name;
            FlightUIController.Instance.PostMessages($"You do not have enough {resName} ({available}/{requested}). Printing Stalled.", "Print Stalled");
            ModLogger.Log($"HextilePrinterModule: Resource lack reported ({resName}). Requested:{requested}, Available:{available}");
        }



        public void EnablePlacementPlaceholder(SingleModelDefinition modelDefinition) {
            if (_placementPlaceholder == null)
                _placementPlaceholder = PlacementPlaceholder.Create();
            _placementPlaceholder.Enable(modelDefinition, this);
        }



        public void DisablePlacementPlaceholder() {
            if (_placementPlaceholder != null)
                _placementPlaceholder.Disable();
        }



        public double RequestResource(int resID, double demand) {
            return part.RequestResource(resID, demand);
        }
    }
}