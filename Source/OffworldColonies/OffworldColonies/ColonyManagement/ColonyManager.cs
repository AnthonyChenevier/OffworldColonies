using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModUtilities;
using OffworldColonies.UI;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {

    class ColonyManager: MonoBehaviour, IConfigNode {
        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static ColonyManager Instance { get; private set; }

        //Settings loaded from save file and/or global settings config
        public List<Vessel.Situations> AllowedSituations { get; private set; } = new List<Vessel.Situations> {Vessel.Situations.LANDED};
        public float MaxBuildRange { get; private set; } = 500f;
        public float MinElevationOffset { get; private set; } = -50f;
        public float MaxElevationOffset { get; private set; } = 50f;
        public float RotationSpeed { get; private set; } = 5f;
        public float BaseRadius { get; private set; } = 20f;
        public bool IgnoreSeaLevelCollision { get; private set; } = false;
        public Vector3 LastUIPosition { get; set; }

        /// <summary>
        /// Contains all of the hextile prototype definitions
        /// </summary>
        public Dictionary<HexTileType, ProtoHexTile> HexDefinitions { get; private set; }

        /// <summary>
        /// A collection of every colony instance in the game
        /// </summary>
        private List<Colony> Colonies { get; set; }

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


        private static ConfigNode LoadDefaultConfig() {
            ConfigNode[] settingsNodes = GameDatabase.Instance.GetConfigNodes("OC_DEFAULT_SETTINGS");
            if (settingsNodes.Length <= 1)
                return settingsNodes.Length == 0 ? null : settingsNodes[0];

            ModLogger.LogWarning("Multiple OC_DEFAULT_SETTINGS nodes found in configs, using last.");
            return settingsNodes.Last();
        }

        public void Load(ConfigNode fromNode) {
            //we have data, load it
            if (fromNode.HasNode("OFFWORLD_COLONIES")) {
                ConfigNode pDataNode = fromNode.GetNode("OFFWORLD_COLONIES");
                LoadSettings(pDataNode);
                LoadColonyData(pDataNode);
            }
            //no data, create new data and load default settings
            else {
                LoadSettings(LoadDefaultConfig());
                Colonies = new List<Colony>();
            }
        }

        private void LoadColonyData(ConfigNode fromNode) {
            List<ConfigNode> colonyNodes = fromNode.GetNodes("COLONY").ToList();

            List<Colony> existingColonies = GetColoniesOnPSystem();

            foreach (Colony colony in existingColonies) {
                ConfigNode node = colonyNodes.Find(n => n.HasValue("colonyID") && int.Parse(n.GetValue("colonyID")) == colony.ColonyID);
                if (node != null) colonyNodes.Remove(node);
            }

            Colonies = ParseColonies(colonyNodes);
            Colonies.AddRange(existingColonies);
        }

        private static List<Colony> GetColoniesOnPSystem() {
            List<Colony> existingColonies = new List<Colony>();
            List<CelestialBody> celestialBodies = PSystemManager.Instance.localBodies;
            foreach (CelestialBody body in celestialBodies.FindAll(cb => cb.pqsController != null)) {
                ModLogger.Log($"Searching for existing colony components in {body.bodyName}");

                Colony[] colonies = body.pqsController.GetComponentsInChildren<Colony>();
                ModLogger.Log($"Found {colonies.Length} existing colonies");

                foreach (Colony colony in colonies)
                    ModLogger.Log($"Found colony '{colony.gameObject.name}'");

                existingColonies.AddRange(colonies);
            }

            return existingColonies;
        }

        private void LoadSettings(ConfigNode fromNode) {
            AllowedSituations = ParseSituations(fromNode.GetValue("AllowedSituations"));
            MaxBuildRange = float.Parse(fromNode.GetValue("MaxBuildRange"));
            MinElevationOffset = float.Parse(fromNode.GetValue("MinElevationOffset"));
            MaxElevationOffset = float.Parse(fromNode.GetValue("MaxElevationOffset"));
            RotationSpeed = float.Parse(fromNode.GetValue("RotationSpeed"));
            BaseRadius = float.Parse(fromNode.GetValue("BaseRadius"));
            IgnoreSeaLevelCollision = bool.Parse(fromNode.GetValue("IgnoreSeaLevelCollision"));
            if (fromNode.HasValue("UIPosition"))
                LastUIPosition = ConfigNode.ParseVector3(fromNode.GetValue("UIPosition"));
            else
                LastUIPosition = Vector3.zero;
        }

        public void Save(ConfigNode toNode) {
            //get the scenario node's data if it exists or create a new node if not
            ConfigNode pDataNode = toNode.HasNode("OFFWORLD_COLONIES")
                                                ? toNode.GetNode("OFFWORLD_COLONIES")
                                                : toNode.AddNode("OFFWORLD_COLONIES");
            //save our settings to the node
            SaveSettings(pDataNode);
            SaveColonyData(pDataNode);
        }

        private void SaveColonyData(ConfigNode toNode) {
            foreach (Colony colony in Colonies)
                toNode.AddNode(colony.ToNode());
        }

        private void SaveSettings(ConfigNode toNode) {
            toNode.AddValue("AllowedSituations", GetSituationNodeValue());
            toNode.AddValue("MaxBuildRange", MaxBuildRange);
            toNode.AddValue("MinElevationOffset", MinElevationOffset);
            toNode.AddValue("MaxElevationOffset", MaxElevationOffset);
            toNode.AddValue("RotationSpeed", RotationSpeed);
            toNode.AddValue("BaseRadius", BaseRadius);
            toNode.AddValue("IgnoreSeaLevelCollision", IgnoreSeaLevelCollision);
            toNode.AddValue("UIPosition", LastUIPosition.ToString().Trim('(', ')'));
        }

        private string GetSituationNodeValue() {
            string situations = "";
            foreach (Vessel.Situations situation in AllowedSituations) {
                situations += situation.ToString();
                if (situation != AllowedSituations.Last())
                    situations += ", ";
            }

            return situations;
        }

        /// <summary>
        /// Parses an array of colony config nodes into a list of instantiated Colonies
        /// </summary>
        /// <param name="colonyNodes"></param>
        /// <returns></returns>
        private static List<Colony> ParseColonies(ICollection<ConfigNode> colonyNodes) {
            if (colonyNodes == null || colonyNodes.Count == 0) return new List<Colony>();

            List<Colony> colonies = new List<Colony>();
            foreach (ConfigNode colonyNode in colonyNodes) {
                Colony newColony = Colony.Create(colonyNode);
                newColony.Refresh();
                colonies.Add(newColony);
            }

            return colonies;
        }

        private static ConfigNode[] ColonyConfigs(IEnumerable<Colony> colonies) {
            return colonies.Select(colony => colony.ToNode()).ToArray();
        }
        /// <summary>
        /// Parses a string list (delimited with commas) to a list of Vessel.Situations
        /// </summary>
        /// <param name="situationsString"></param>
        /// <returns></returns>
        private static List<Vessel.Situations> ParseSituations(string situationsString) {
            List<Vessel.Situations> allowedSituations = new List<Vessel.Situations>();
            string[] situations = situationsString.Split(',');
            foreach (string situation in situations) {
                switch (situation.ToUpper().Trim()) {
                case "LANDED":
                    allowedSituations.Add(Vessel.Situations.LANDED);
                    break;
                case "DOCKED":
                    allowedSituations.Add(Vessel.Situations.DOCKED);
                    break;
                case "ESCAPING":
                    allowedSituations.Add(Vessel.Situations.ESCAPING);
                    break;
                case "FLYING":
                    allowedSituations.Add(Vessel.Situations.FLYING);
                    break;
                case "ORBITING":
                    allowedSituations.Add(Vessel.Situations.ORBITING);
                    break;
                case "PRELAUNCH":
                    allowedSituations.Add(Vessel.Situations.PRELAUNCH);
                    break;
                case "SUB_ORBITAL":
                    allowedSituations.Add(Vessel.Situations.SUB_ORBITAL);
                    break;
                case "SPLASHED":
                    allowedSituations.Add(Vessel.Situations.SPLASHED);
                    break;
                default:
                    ModLogger.LogWarning($"Failed parsing unknown situation '{situation}'");
                    break;
                }
            }
            return allowedSituations;
        }

        /// <summary>
        /// Creates a dictionary of ProtoHexTiles from config nodes which can be used to instantiate new base parts.
        /// </summary>
        /// <param name="defNodes"></param>
        /// <returns></returns>
        private static Dictionary<HexTileType, ProtoHexTile> ParseHexDefintions(ConfigNode[] defNodes) {
            //we haven't loaded it yet, so get all base model definition nodes from the database
            return defNodes.ToDictionary(node => HexTile.ParseType(node.GetValue("HexType")), node => new ProtoHexTile(node));
        }

        /// <summary>
        /// Adds a Colony to the Manager Instance
        /// </summary>
        /// <param name="colony"></param>
        public void AddColony(Colony colony) {
            Colonies.Add(colony);
        }


        /// <summary>
        /// Simple test for creating new City on load
        /// </summary>
        private static Colony LoadTestCity() {
            //these six variables will be saved in persistence file in real colonies
            string colonyName = "Runtime Test Colony";
            CelestialBody kerbinBody = PSystemManager.Instance.localBodies.Find(p => p.bodyName == "Kerbin");
            BodySurfacePosition coordinates = new BodySurfacePosition(0.0605426247461956, -74.5827920941057f, 65, kerbinBody);
            HexTileType initialHexTileType = HexTileType.Default;
            float altitudeOffset = 0f;
            float rotation = 0f;
            int colonyID = Instance.NextID();

            Colony newColony = Colony.Create(colonyName, colonyID, coordinates, altitudeOffset, rotation, initialHexTileType);
            //newColony.Tiles[0].SetBaseColor(XKCDColors.BrightOlive);

            //now add 24 new bases to the inital one on each side
            int hexCount = 1 + (6 * 1) + (6 * 2) + (6 * 3);
            for (int positionIndex = 1; positionIndex < hexCount; positionIndex++) 
                newColony.AddNewHexTile(positionIndex, HexTileType.Default);

            newColony.Refresh();
            return newColony;
        }

        public int NextID() {
            if (Colonies == null || Colonies.Count == 0) return 0;

            int[] ids = Colonies.Select(c => c.ColonyID).ToArray();

            if (ids.Length > 0) return ids.Max()+1;

            return 0;
        }
    }
}
