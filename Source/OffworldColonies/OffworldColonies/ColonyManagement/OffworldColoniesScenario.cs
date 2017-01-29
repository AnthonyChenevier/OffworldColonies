using ModUtilities;
using OffworldColonies.UI;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {
    /// <summary>
    ///     The offworld colonies main plugin handles saving and
    ///     loading config data and persistent save data, managing persistent
    ///     base progression, resource usage and needs for all developed colonies
    ///     and providing UI hooks for certain functions
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT,
         GameScenes.TRACKSTATION)]
    public class OffworldColoniesScenario : ScenarioModule {
        private static ColonyManager CM => ColonyManager.Instance;

        /// <summary>Singleton Instance</summary>
        public static OffworldColoniesScenario Instance { get; private set; }

        /// <summary>
        /// Entry point for the Offworld Colonies mod, creates Instances of 
        /// ColonyManager and BuildUIManager (as components of this script's
        /// gameObject) in the appropriate scenes here so they are ready for
        /// calls to Load and Save later.
        /// </summary>
        public override void OnAwake() {
            ModLogger.Title = "OC ColonyManager";
            ModLogger.Log($"OffworldColoniesScenario: OnAwake in {HighLogic.LoadedScene}");

            if (Instance != null && Instance != this) {
                ModLogger.Log("Overwriting existing instance.");
                Destroy(Instance);
            }
            Instance = this;

            ModLogger.Log("Instance created");

            //create managers for loading/saving as required by the scene
            switch (HighLogic.LoadedScene) {
                case GameScenes.SPACECENTER:
                    ModLogger.Log("Adding ColonyManager");
                    gameObject.AddComponent<ColonyManager>();
                    break;
                case GameScenes.TRACKSTATION:
                    ModLogger.Log("Adding ColonyManager");
                    gameObject.AddComponent<ColonyManager>();
                    break;
                case GameScenes.FLIGHT:
                    ModLogger.Log("Adding BuildUI and Colony Managers");
                    gameObject.AddComponent<BuildUIManager>();
                    gameObject.AddComponent<ColonyManager>();
                    break;
                default:
                    ModLogger.Log("Scene does not require Managers");
                    break;
            }
        }

        /// <summary>
        /// Destroys itself and any managers that were created in this scene.
        /// </summary>
        public void OnDestroy() {
            ModLogger.Log($"OffworldColoniesScenario: OnDestroy in {HighLogic.LoadedScene}");
            //Destroy BuildUI and Colony Managers
            if (CM != null) Destroy(CM);
            if (BuildUIManager.Instance != null) Destroy(BuildUIManager.Instance);
            //Clean up instance
            if (Instance != null && Instance == this) Instance = null;
        }


        public override void OnLoad(ConfigNode gameNode) {
            base.OnLoad(gameNode);

            if (CM == null) return;

            CM.Load(gameNode);

            ModLogger.Log("ColonyManager ScenarioModule loaded");
        }


        public override void OnSave(ConfigNode gameNode) {
            base.OnSave(gameNode);

            if (CM == null) return;
            //Save ColonyManager data to the node
            CM.Save(gameNode);
            ModLogger.Log("ColonyManager ScenarioModule saved");
        }
    }
}