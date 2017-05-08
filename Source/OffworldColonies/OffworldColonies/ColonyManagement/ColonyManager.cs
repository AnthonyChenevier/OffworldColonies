using System;
using System.Collections.Generic;
using System.Linq;
using OffworldColonies.HexTiles;
using OffworldColonies.Part;
using OffworldColonies.UI;
using OffworldColonies.Utilities;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {
    public class ColonyManager : MonoBehaviour {
        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static ColonyManager Instance { get; private set; }


        //Settings loaded from save file and/or global settings config
        public List<Vessel.Situations> AllowedSituations { get; private set; } = new List<Vessel.Situations> { Vessel.Situations.LANDED };
        public float MaxBuildRange { get; private set; } = 500f;
        public float LinkRange => MaxBuildRange * 4;
        public float MinElevationOffset { get; private set; } = -50f;
        public float MaxElevationOffset { get; private set; } = 50f;
        public float RotationSpeed { get; private set; } = 5f;
        public float BaseRadius { get; private set; } = 20f;
        public int MaxColonyTileRadius { get; private set; } = 2;
        public bool IgnoreSeaLevelCollision { get; private set; } = false;
        public Vector3 LastUIPosition { get; set; } = Vector3.zero;


        /// <summary>
        /// Contains all of the hextile prototype definitions
        /// </summary>
        public Dictionary<HexTileType, HexTileDefinition> HexDefinitions { get; private set; }

        /// <summary>
        /// A collection of every colony instance in the game, separated by body name
        /// </summary>
        private Dictionary<string, List<Colony>> _colonies;

        /// <summary>
        /// The default setting config node
        /// </summary>
        private ConfigNode _settingsConfig;

        public void Awake() {
            ModLogger.Log($"ColonyManager: Awake in {HighLogic.LoadedScene}");
            if (Instance != null && Instance != this) {
                ModLogger.Log("Overwriting existing instance.");
                Destroy(Instance);
            }
            Instance = this;

            //Load HexTile definitions from mod config once
            HexDefinitions = ParseHexDefintions(GameDatabase.Instance.GetConfigNodes("HEXTILE_DEFINITION"));
        }

        public void OnDestroy() {
            ModLogger.Log($"ColonyManager: OnDestroy in {HighLogic.LoadedScene}");

            if (Instance != null && Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Searches the PSystem instance for any Colony Components that are attached.
        /// Necessary because PQSMods (Like PQSCity2, to which the Colony component is attached)
        /// are persistent across save/load.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, List<Colony>> GetColoniesOnPSystem() {
            Dictionary<string, List<Colony>> existingColonies = new Dictionary<string, List<Colony>>();
            List<CelestialBody> celestialBodies = PSystemManager.Instance.localBodies;
            foreach (CelestialBody body in celestialBodies.FindAll(cb => cb.pqsController != null)) {
                ModLogger.Log($"Searching for existing colony components in {body.bodyName}");

                Colony[] colonies = body.pqsController.GetComponentsInChildren<Colony>();
                ModLogger.Log($"Found {colonies.Length} existing colonies");

                foreach (Colony colony in colonies)
                    ModLogger.Log($"Found colony '{colony.gameObject.name}'");

                if (existingColonies.ContainsKey(body.bodyName))
                    existingColonies[body.bodyName].AddRange(colonies);
                else
                    existingColonies.Add(body.bodyName, colonies.ToList());
            }

            return existingColonies;
        }

        /// <summary>
        /// Checks the distance to any colonies on the given body
        /// and returns the closest one within range of the given position.
        /// </summary>
        /// <param name="bodyName"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Colony GetClosestColonyInRange(string bodyName, Vector3 position) {
            float shortestDist = LinkRange;
            Colony closest = null;

            List<Colony> bodyColonies = _colonies[bodyName];
            foreach (Colony colony in bodyColonies) {
                Vector3 cPos = colony.SurfaceAnchor.transform.position;
                float d = Vector3.Distance(cPos, position);

                if (d >= shortestDist)
                    continue;

                shortestDist = d;
                closest = colony;
            }

            return closest;
        }

        /// <summary>
        /// Get a unique ID for a new colony
        /// </summary>
        /// <returns></returns>
        public int NextID() {
            //TODO: can this be simplified? ID has to be the same for save/load, and unique to the 
            //TODO: colony so hashing the name may not work if players can name the colony themselves
            if (_colonies == null || _colonies.Count == 0)
                return 0;

            int[] ids = _colonies.SelectMany(b => b.Value.Select(c => c.ColonyID)).ToArray();

            if (ids.Length > 0)
                return ids.Max() + 1;

            return 0;
        }

        /// <summary>
        /// Creates a new colony at the given position and
        /// begins the build process for the first base part at that 
        /// position.
        /// </summary>
        /// <remarks>
        /// - Creates a colony object to start building the initial tile on 
        /// </remarks>
        /// <remarks>
        /// - This colony object isn't added to the colony manager until building is complete
        /// </remarks>
        /// <remarks>
        /// - A cancel build action will destroy the colony
        /// </remarks>
        /// <remarks>
        /// - Will begin the build process regardless of resource availability 
        /// </remarks>
        /// <param name="printer"></param>
        /// <param name="selectedCoords">The coordinates to build at</param>
        /// <param name="altitudeOffset">The tile altitude offset</param>
        /// <param name="rotationOffset">The tile rotation offset</param>
        public void NewColonyBuildOrder(HextilePrinterModule printer, BodySurfacePosition selectedCoords, float altitudeOffset, float rotationOffset) {
            //TODO: name input window
            int colonyID = NextID();
            string colonyName = $"{selectedCoords.BodyName} Colony #{colonyID:x4}";

            Colony colony = Colony.Create(colonyName, colonyID, selectedCoords, altitudeOffset, rotationOffset);
            if (colony == null || colony.SurfaceAnchor == null) {
                ModLogger.LogError($"New Colony creation FAILED for '{colonyName}'");
                return;
            }
            colony.Refresh();
            printer.CurrentColony = colony;

            StartBuildOrder(printer, printer.CurrentColony, printer.SelectedTile, 0, selectedCoords, 0f);
        }


        public void AddTileBuildOrder(HextilePrinterModule printer, int hexID, float rotationOffset) {
            BodySurfacePosition bodyPosition = new BodySurfacePosition(printer.CurrentColony.BodyCoordinates);
            Vector3 position;
            printer.CurrentColony.HexGrid.GetCell(hexID, out position);
            bodyPosition.WorldPosition = position;
            StartBuildOrder(printer, printer.CurrentColony, printer.SelectedTile, hexID, bodyPosition, rotationOffset);
        }


        private void StartBuildOrder(HextilePrinterModule printer, Colony colony, HexTileDefinition selectedTile, int gridPosition, BodySurfacePosition worldPosition, float rotationOffset) {
            int orderID = colony.StartBuildOrder(new BuildOrder(printer, selectedTile, gridPosition, worldPosition, rotationOffset,
                                                          printer.OnBuildCompleted, printer.OnBuildCanceled,
                                                          printer.OnResourceLack, printer.OnBuildPaused));

            if (orderID < 0) {
                ModLogger.LogError($"New build order:{orderID} FAILED for {colony.ColonyName} at grid index {gridPosition}");
                return;
            }

            printer.SetCurrentOrderID(orderID);
            FlightUIController.Instance.StopPlacementMode();
            FlightUIController.Instance.StartBuildMode(selectedTile);

            ModLogger.Log($"New build order:{orderID} created for {colony.ColonyName} at grid index {gridPosition}");
            ModLogger.Log($"{selectedTile} Recipe: {selectedTile.Recipe}");
            FlightUIController.Instance.PostMessages($"Beginning Construction of {selectedTile} for colony '{colony.ColonyName}'", "Please wait, printing...");
        }



        public bool CancelBuildOrder(Colony colony, int orderID) {
            if (!colony.CancelBuildOrder(orderID))
                return false;
            FlightUIController.Instance.StopBuildMode();
            FlightUIController.Instance.StartSelectionMode();
            return true;
        }

        public void PauseBuildOrder(Colony colony, int orderID, bool doPause) {
            colony.PauseBuildOrder(orderID, doPause);
            FlightUIController.Instance.SetBuildProgress(colony.BuildOrders[0].CompletionProgress, doPause);
        }

        public bool ProcessBuildOrder(Colony colony, int orderID) {
            if (!colony.ProcessBuildOrder(orderID))
                return false;
            FlightUIController.Instance.SetBuildProgress(colony.BuildOrders[0].CompletionProgress);
            return true;
        }



        #region Save_Load_Methods
        public void Save(ConfigNode toNode) {
            //save our data to the node
            SaveSettings(toNode);
            SaveColonyData(toNode);
        }

        private void SaveSettings(ConfigNode toNode) {
            toNode.AddValue("AllowedSituations", AllowedSituationsToString());
            toNode.AddValue("MaxBuildRange", MaxBuildRange);
            toNode.AddValue("MinElevationOffset", MinElevationOffset);
            toNode.AddValue("MaxElevationOffset", MaxElevationOffset);
            toNode.AddValue("RotationSpeed", RotationSpeed);
            toNode.AddValue("BaseRadius", BaseRadius);
            toNode.AddValue("IgnoreSeaLevelCollision", IgnoreSeaLevelCollision);
            toNode.AddValue("MaxColonyTileRadius", MaxColonyTileRadius);
            toNode.AddValue("UIPosition", LastUIPosition.ToString().Trim('(', ')'));
        }

        private void SaveColonyData(ConfigNode toNode) {
            foreach (KeyValuePair<string, List<Colony>> pair in _colonies) {
                foreach (Colony colony in pair.Value)
                    colony.Save(toNode.AddNode("COLONY"));
            }
        }

        private static ConfigNode LoadSettingsConfigNode() {
            ConfigNode[] settingsNodes = GameDatabase.Instance.GetConfigNodes("OC_DEFAULT_SETTINGS");
            if (settingsNodes.Length <= 1)
                return settingsNodes.Length == 0 ? null : settingsNodes[0];

            ModLogger.LogWarning("Multiple OC_DEFAULT_SETTINGS nodes found in configs, using last.");
            return settingsNodes.Last();
        }

        public void Load(ConfigNode node) {
            //load default settings node
            _settingsConfig = LoadSettingsConfigNode();

            if (_settingsConfig == null) {
                ModLogger.LogError("Default settings congfig node failed to load.");
                return;
            }
            LoadSettings(node);

            _colonies = new Dictionary<string, List<Colony>>();

            if (node == null || !node.HasData)
                return;

            LoadColonies(node);
        }

        /// <summary>
        /// Loads settings from node if the given setting exists, otherwise 
        /// </summary>
        /// <param name="fromNode"></param>
        private void LoadSettings(ConfigNode fromNode) {
            AllowedSituations = ParseAllowedSituations(LoadValueOrDefault(fromNode, "AllowedSituations"));
            MaxBuildRange = float.Parse(LoadValueOrDefault(fromNode, "MaxBuildRange"));
            MinElevationOffset = float.Parse(LoadValueOrDefault(fromNode, "MinElevationOffset"));
            MaxElevationOffset = float.Parse(LoadValueOrDefault(fromNode, "MaxElevationOffset"));
            RotationSpeed = float.Parse(LoadValueOrDefault(fromNode, "RotationSpeed"));
            BaseRadius = float.Parse(LoadValueOrDefault(fromNode, "BaseRadius"));
            IgnoreSeaLevelCollision = bool.Parse(LoadValueOrDefault(fromNode, "IgnoreSeaLevelCollision"));
            MaxColonyTileRadius = int.Parse(LoadValueOrDefault(fromNode, "MaxColonyTileRadius"));
            LastUIPosition = ConfigNode.ParseVector3(LoadValueOrDefault(fromNode, "UIPosition"));
        }


        private string LoadValueOrDefault(ConfigNode fromNode, string settingName) {
            if (fromNode != null && fromNode.HasValue(settingName))
                return fromNode.GetValue(settingName);

            if (_settingsConfig.HasValue(settingName))
                return _settingsConfig.GetValue(settingName);

            //TODO: not sure of a more elegant way to handle this right now...
            throw new ArgumentOutOfRangeException(nameof(settingName), $"setting named '{settingName}' not found in save file or default config");
        }

        private void LoadColonies(ConfigNode fromNode) {
            ModLogger.Log("Loading Colony nodes from save file...");
            List<ConfigNode> colonyNodes = fromNode.GetNodes("COLONY").ToList();

            //check for existing colonies (PSystem is flagged as DontDestroyOnLoad)
            //so we don't double up on colony instances over scene changes
            Dictionary<string, List<Colony>> existingColonies = GetColoniesOnPSystem();
            LoadOrDestroyExisting(colonyNodes, existingColonies);

            //parse the stripped-down list
            _colonies = LoadColonyList(colonyNodes);
            ModLogger.Log("Merging existing and newly-created colony lists");
            //and add the perexisiting colonies to the list
            foreach (KeyValuePair<string, List<Colony>> pair in existingColonies) {
                if (_colonies.ContainsKey(pair.Key))
                    _colonies[pair.Key].AddRange(pair.Value);
                else
                    _colonies.Add(pair.Key, pair.Value);
            }
            ModLogger.Log("Loading Colony nodes complete");
        }

        /// <summary>
        /// Searches through the existing colony list
        /// </summary>
        /// <param name="colonyNodes"></param>
        /// <param name="existingColonies"></param>
        private static void LoadOrDestroyExisting(List<ConfigNode> colonyNodes, IDictionary<string, List<Colony>> existingColonies) {
            ModLogger.Log("Searching for matching save-file colony nodes for pre-exisitng Colony components on PSystem");
            foreach (KeyValuePair<string, List<Colony>> pair in existingColonies) {
                List<Colony> colonies = pair.Value;
                int i = colonies.Count;
                while (i-- > 0) {
                    Colony colony = colonies[i];
                    ConfigNode node = colonyNodes.Find(n => n.HasValue("colonyID") && int.Parse(n.GetValue("colonyID")) == colony.ColonyID);

                    if (node == null) {
                        ModLogger.Log($"No save file node exists for component {colony.name}. This colony is a remenant " +
                            "(from an incomplete build order being loaded over or something) and will be destroyed");
                        //this colony object does not have a corresponding 
                        //saved node and should be destroyed
                        colony.DestroyHanging();
                        existingColonies[pair.Key].Remove(colony);
                    }
                    else {
                        ModLogger.Log($"Save file node found for component {colony.name}. Loading data" +
                            "into existing colony and removing node from list to prevent regeneration.");
                        //purge nodes that correspond to the existing colony instance 
                        //after loading any required values to it
                        colony.Load(node);
                        //remove this node from the list to be parsed
                        colonyNodes.Remove(node);
                    }
                }
            }
        }

        // <summary>
        // Simple test for creating new City on load
        // </summary>
        //private static Colony LoadTestCity() {
        //    //these six variables will be saved in persistence file in real colonies
        //    string colonyName = "Runtime Test Colony";
        //    CelestialBody kerbinBody = PSystemManager.Instance.localBodies.Find(p => p.bodyName == "Kerbin");
        //    BodySurfacePosition coordinates = new BodySurfacePosition(0.0605426247461956, -74.5827920941057f, 65, kerbinBody);
        //    float altitudeOffset = 0f;
        //    float rotation = 0f;
        //    int colonyID = Instance.NextID();

        //    Colony newColony = Colony.Create(colonyName, colonyID, coordinates, altitudeOffset, rotation);

        //    //now add a bunch of new default bases
        //    int hexCount = 1 + (6 * 1) + (6 * 2) + (6 * 3);
        //    for (int positionIndex = 0; positionIndex < hexCount; positionIndex++) 
        //        newColony.AddTile(positionIndex, HexTileType.Default);

        //    newColony.Refresh();
        //    return newColony;
        //}


        #endregion

        #region Node_Parsers_and_Serializers
        /// <summary>
        /// Parses an array of colony config nodes into a list of Colony objects
        /// </summary>
        /// <param name="colonyNodes"></param>
        /// <returns></returns>
        private static Dictionary<string, List<Colony>> LoadColonyList(ICollection<ConfigNode> colonyNodes) {
            ModLogger.Log("Creating Colony instantes from save file nodes....");
            if (colonyNodes == null || colonyNodes.Count == 0)
                return new Dictionary<string, List<Colony>>();

            Dictionary<string, List<Colony>> colonies = new Dictionary<string, List<Colony>>();
            foreach (ConfigNode colonyNode in colonyNodes) {
                Colony newColony = Colony.LoadNew(colonyNode);
                newColony.Refresh();

                if (colonies.ContainsKey(newColony.BodyName))
                    colonies[newColony.BodyName].Add(newColony);
                else
                    colonies.Add(newColony.BodyName, new List<Colony> { newColony });
            }
            ModLogger.Log("Colony Creation complete");
            return colonies;
        }


        /// <summary>
        /// Parses a string list (delimited with commas) to a list of Vessel.Situations
        /// </summary>
        /// <param name="situationsString"></param>
        /// <returns></returns>
        private static List<Vessel.Situations> ParseAllowedSituations(string situationsString) {
            List<Vessel.Situations> output;
            try {
                output = situationsString.Split(',').Select(situation => (Vessel.Situations)Enum.Parse(typeof(Vessel.Situations), situation.Trim())).ToList();
            }
            catch (Exception ex) {
                ModLogger.LogError($"Error parsing allowed situations, given value: '{situationsString}, exception {ex}");
                output = new List<Vessel.Situations> { Vessel.Situations.LANDED };
            }
            return output;
        }

        private string AllowedSituationsToString() {
            string situations = "";
            foreach (Vessel.Situations situation in AllowedSituations) {
                situations += situation.ToString();
                if (situation != AllowedSituations.Last())
                    situations += ", ";
            }

            return situations;
        }

        /// <summary>
        /// Creates a dictionary of ProtoHexTiles from config nodes which can be used to instantiate new base parts.
        /// </summary>
        /// <param name="defNodes"></param>
        /// <returns></returns>
        private static Dictionary<HexTileType, HexTileDefinition> ParseHexDefintions(ConfigNode[] defNodes) {
            //we haven't loaded it yet, so get all base model definition nodes from the database
            return defNodes.ToDictionary(node => HexTiles.HexTile.ParseType(node.GetValue("HexType")), node => new HexTileDefinition(node));
        }
        #endregion

        #region Colony_List_Management

        /// <summary>
        /// Adds a Colony to the Manager
        /// </summary>
        /// <param name="colony">The colony to add</param>
        public void Add(Colony colony) {
            if (_colonies.ContainsKey(colony.BodyName))
                _colonies[colony.BodyName].Add(colony);
            else
                _colonies.Add(colony.BodyName, new List<Colony> { colony });
        }

        /// <summary>
        /// Check if the colony exists in the Manager
        /// </summary>
        /// <param name="colony">The colony to check for</param>
        /// <returns>True if the colony exists in the Manager, otherwise false</returns>
        public bool Contains(Colony colony) {
            return _colonies.ContainsKey(colony.BodyName) && _colonies[colony.BodyName].Contains(colony);
        }

        /// <summary>
        /// Remove the colony from the Manager
        /// </summary>
        /// <param name="colony">The colony to remove</param>
        public void Remove(Colony colony) {
            if (!Contains(colony))
                return;

            _colonies[colony.BodyName].Remove(colony);
            if (_colonies[colony.BodyName].Count == 0)
                _colonies.Remove(colony.BodyName);
        }

        #endregion
    }
}
