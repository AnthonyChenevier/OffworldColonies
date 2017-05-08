using OffworldColonies.UI;
using OffworldColonies.Utilities;

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
                    ModLogger.Log("Adding FlightUIController and Colony Managers");
                    gameObject.AddComponent<FlightUIController>();
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
            if (ColonyManager.Instance != null) Destroy(ColonyManager.Instance);
            if (FlightUIController.Instance != null) Destroy(FlightUIController.Instance);
            //Clean up instance
            if (Instance != null && Instance == this) Instance = null;
        }


        public override void OnLoad(ConfigNode gameNode) {
            base.OnLoad(gameNode);

            if (ColonyManager.Instance != null)
                ColonyManager.Instance.Load(gameNode.GetNode("OFFWORLD_COLONIES"));

            ModLogger.Log("ColonyManager ScenarioModule loaded");
        }


        public override void OnSave(ConfigNode gameNode) {
            base.OnSave(gameNode);

            //get the scenario node's data if it exists or create a new node if not
            if (ColonyManager.Instance != null)
                ColonyManager.Instance.Save(gameNode.GetNode("OFFWORLD_COLONIES") ??
                                            gameNode.AddNode("OFFWORLD_COLONIES"));

            ModLogger.Log("ColonyManager ScenarioModule saved");
        }
    }
}