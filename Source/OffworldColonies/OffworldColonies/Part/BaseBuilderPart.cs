using System.Collections.Generic;
using ModUtilities;
using OffworldColonies.ColonyManagement;
using OffworldColonies.Debug;
using OffworldColonies.UI;
using UnityEngine;

namespace OffworldColonies.Part {
    /// <summary>
    ///     BaseBuilderPart is the part that allows a vessel to create new bases.
    ///     This class handles the positioning of a new base, including checks for distance to other existing bases.
    ///     It also handles adding new base pieces onto existing bases within range.
    ///     Checking for the position of a new base means showing a ghost version of the
    ///     base model and allowing the user to adjust its position. The initial piece takes
    ///     All subsequent base pieces in the same base
    ///     will use the inital piece as a reference and must be connected to one of the existing pieces.
    /// </summary>
    public class BaseBuilderPart : PartModule, IResourceConsumer {
        private BaseEvent _baseEvent;
        private BaseEvent _deployEvent;

        public Colony CurrentColony { get; set; }
        
        [KSPField]
        private bool _useResources = true;
        [KSPField(isPersistant = true)]
        private bool _consumingResources;
        [KSPField(guiActive = true, guiName = "Printer:")]
        private string _status = "Packed";


        private List<PartResourceDefinition> _consumedResources;
        private float _resourceFraction;
        private bool _started;

        public List<PartResourceDefinition> GetConsumedResources() {
            return _consumedResources;
        }

        public override void OnAwake() {
            if (_consumedResources == null)
                _consumedResources = new List<PartResourceDefinition>();
            else
                _consumedResources.Clear();

            if (_useResources) {
                int count = resHandler.inputResources.Count;
                for (int index = 0; index < count; ++index)
                    _consumedResources.Add(PartResourceLibrary.Instance.GetDefinition(resHandler.inputResources[index].name));
            }
        }

        public override void OnStart(StartState state) {
            _baseEvent = Events["CreateBaseHere"];
            _deployEvent = Events["DeployBuilder"];
            _baseEvent.active = false;
            _started = true;
        }

        public override void OnLoad(ConfigNode node) {
            //if (resHandler.inputResources.Count != 0) return;
            //resHandler.inputResources.Add(new ModuleResource {
            //    name = resourceName,
            //    title = KSPUtil.PrintModuleName(resourceName),
            //    id = resourceName.GetHashCode(),
            //    rate = resourceAmount
            //});
        }

        public override string GetInfo() {
            string empty = string.Empty;
            if (_useResources)
                empty += resHandler.PrintModuleResources();
            return empty;
        }

        public override void OnFixedUpdate() {
            if (HighLogic.LoadedSceneIsEditor || !_started) return;

            ProcessConsumption();
        }

        private void ProcessConsumption() {
            if (!_useResources || !_consumingResources) return;

            _resourceFraction = (float)resHandler.UpdateModuleResourceInputs(ref _status, 1.0, 0.99, false, false, true);
            ModLogger.Log($"Resource usage: {_resourceFraction}");

            if (_resourceFraction < 0.990000009536743) return;

            if (_status != "Nominal")
                _status = "Nominal";
        }

        private void ConsumeResources() {
            _consumingResources = !_consumingResources;

        }

        #region Buttons

        [KSPEvent(guiName = "Deploy", active = true, guiActive = true)]
        public void DepolyBuilder() {
            //do deploy animation

            //toggle the create base button after animation has ended
            _baseEvent.active = !_baseEvent.active;
            _baseEvent.guiActive = _baseEvent.active;
        }

        /// <summary>
        ///     Builds a new base if there are no active bases in
        ///     range, adds new parts to a base if there is.
        /// </summary>
        [KSPEvent(guiName = "Create Base", active = false, guiActive = false)]
        public void CreateBaseHere() {
            if (!ColonyManager.Instance.AllowedSituations.Contains(FlightGlobals.ActiveVessel.situation) || FlightGlobals.ActiveVessel.horizontalSrfSpeed > 0.05) {
                ScreenMessages.PostScreenMessage("<color=red>You must be at rest to use </color>", 4f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            BuildUIManager.Instance.StartBuildMode(this);
        }

        //[KSPEvent(guiName = "Test Function", active = true, guiActive = true)]
        //public void TestingButton() {
        //    //DebugTools.TestMaterialColour(vessel);
        //    //DebugTools.DumpHeirarchy();
        //    //DebugTools.TestMisc();
        //    //DebugTools.DumpLayerMatrix();
        //}

        #endregion
    }
}