using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OffworldColonies.ColonyManagement;
using UnityEngine;

namespace OffworldColonies.Part {

    public interface IColonyLinked {
        ColonyLinkModule LinkModule { get; set; }
    }

    public class ColonyLinkModule: PartModule {
        [KSPField(guiActive = true, guiName = "Linked Colony")]
        private string _currentColonyInfo = "No Link";

        public bool CheckForRange { get; set; } = true;

        public Colony CurrentColony { get; set; } = null;


        public override string GetInfo() {
            string info = "Maintains link with closest Colony site in range.";
            if (ColonyManager.Instance == null) return info;

            info += "\n";
            info += $"Link Range: {ColonyManager.Instance.LinkRange}m";
            info += "\n";
            info += $"Build Range: {ColonyManager.Instance.MaxBuildRange}m";

            return info;
        }

        public override void OnStart(StartState state) {
            if (!HighLogic.LoadedSceneIsFlight) return;

            SetupLinkedModules();
            //Get closest colony may return null if none in range
            CurrentColony = ColonyManager.Instance.GetClosestColonyInRange(vessel.mainBody.bodyName, vessel.vesselTransform.position);
            _currentColonyInfo = ColonyNameAndDistance();

            base.OnStart(state);
        }

        private void SetupLinkedModules() {
            List<IColonyLinked> modules = part.FindModulesImplementing<IColonyLinked>();
            foreach (IColonyLinked colonyLinked in modules) {
                colonyLinked.LinkModule = this;
            }
        }

        public override void OnUpdate() {
            if (HighLogic.LoadedSceneIsEditor) return;

            //Get closest colony may return null if none in range
            if (CheckForRange && (CurrentColony == null || DistanceFromCurrentColony() > ColonyManager.Instance.LinkRange))
                CurrentColony = ColonyManager.Instance.GetClosestColonyInRange(vessel.mainBody.bodyName, vessel.vesselTransform.position);
            
            _currentColonyInfo = ColonyNameAndDistance();
            base.OnUpdate();
        }

        /// <summary>
        /// Returns the distance from the vessel to the currently linked colony in metres.
        /// </summary>
        /// <returns></returns>
        public float DistanceFromCurrentColony() {
            return CurrentColony == null ? 0f : Vector3.Distance(vessel.vesselTransform.position, CurrentColony.SurfaceAnchor.transform.position);
        }
        /// <summary>
        /// Returns a string containing the currently 
        /// linked colony name and distance to it (in km).
        /// </summary>
        /// <returns></returns>
        public string ColonyNameAndDistance() {
            return CurrentColony == null ? "No Link" : $"{CurrentColony.ColonyName} ({DistanceFromCurrentColony() / 1000:#0.00}km)";
        }
    }
}
