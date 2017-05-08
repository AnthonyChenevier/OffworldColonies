using System;
using System.Collections.Generic;
using OffworldColonies.HexTiles;
using OffworldColonies.Utilities;
using PQSModLoader;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {
    public class Colony : MonoBehaviour {

        public SurfaceStructure SurfaceAnchor { get; private set; }

        public HexTileGrid HexGrid { get; private set; }

        public string ColonyName { get; private set; }
        public int ColonyID { get; private set; }

        public BodySurfacePosition BodyCoordinates { get; private set; }
        public Vector3 WorldPosition => BodyCoordinates.WorldPosition;
        public string BodyName => BodyCoordinates.BodyName;
        public CelestialBody Body => BodyCoordinates.Body;

        public float AltitudeOffset { get; private set; }
        public float RotationOffset { get; private set; }

        public List<BuildOrder> BuildOrders { get; } = new List<BuildOrder>();

        private List<HexTile> _tiles;

        private void Awake() {
            ModLogger.Log($"Initialising {gameObject.name}...");
            _tiles = new List<HexTile>();
        }

        private void OnDestroy() {
            ModLogger.Log($"Destroying {gameObject.name}");
            if (ColonyManager.Instance.Contains(this))
                ColonyManager.Instance.Remove(this);
            //destroy our surface anchor too
            Destroy(SurfaceAnchor);
        }

        public void Init(SurfaceStructure colonyCity, string colonyName, int colonyID, BodySurfacePosition bodyCoordinates, float altitudeOffset, float rotationOffset, HexTileGrid hexGrid) {
            ColonyName = colonyName;
            ColonyID = colonyID;
            SurfaceAnchor = colonyCity;
            BodyCoordinates = bodyCoordinates;
            AltitudeOffset = altitudeOffset;
            RotationOffset = rotationOffset;
            HexGrid = hexGrid;
        }

        public void Refresh() {
            SurfaceAnchor.Refresh();
        }

        public void DestroyHanging() {
            ModLogger.LogWarning($"{gameObject.name} does not have a corresponding node in persistent.sfs (hanging reference) and will be destroyed.");
            Destroy(this);
        }

        /// <summary>
        ///     Design goal:
        ///     Add Base Part must be able to build parts over time, using a placement ghost as a
        ///     placeholder and consuming resources (Ore or CRP) at a given rate. The placeholder
        ///     will have a collider as soon as building begins. This means that static base parts
        ///     should be cheap and fast to build compared to active parts (with their own
        ///     behaviour/functionality) to prevent exploitation of unfinished base parts as a
        ///     cheap way of making flat landing areas.
        /// </summary>
        /// <param name="hexTile"></param>
        public void AddTile(HexTile hexTile) { _tiles.Add(hexTile); }

        public void Load(ConfigNode node) {
            //none of this is required for this phase of development
            //string colonyName = node.GetValue("colonyName");
            //int colonyID = int.Parse(node.GetValue("colonyID"));
            //BodySurfacePosition bodyCoordinates = new BodySurfacePosition(node.GetNode("bodyCoordinates"));
            //float altitudeOffset = float.Parse(node.GetValue("altitudeOffset"));
            //float rotationOffset = float.Parse(node.GetValue("rotationOffset"));

            //Init(SurfaceAnchor, colonyName, colonyID, bodyCoordinates, altitudeOffset, rotationOffset, HexGrid);
        }


        /// <summary>
        /// Requires the node be an existing "COLONY" node
        /// </summary>
        /// <param name="node"></param>
        public void Save(ConfigNode node) {
            node.AddValue("colonyName", ColonyName);
            node.AddValue("colonyID", ColonyID);
            node.AddValue("altitudeOffset", AltitudeOffset);
            node.AddValue("rotationOffset", RotationOffset);

            BodySurfacePosition coordinates = new BodySurfacePosition(BodyCoordinates);
            coordinates.Altitude -= AltitudeOffset;
            coordinates.Save(node.GetNode("SURFACE_POSITION") ?? node.AddNode("SURFACE_POSITION"));

            foreach (HexTile hexTile in _tiles)
                hexTile.Save(node.AddNode("TILE"));
        }


        public bool HasBuildOrder(int orderIndex) {
            return BuildOrders.Count > orderIndex && BuildOrders[orderIndex] != null;
        }

        public int StartBuildOrder(BuildOrder buildOrder) {
            if (!buildOrder.Start())
                return -1;

            BuildOrders.Add(buildOrder);
            return BuildOrders.Count - 1;
        }

        public bool ProcessBuildOrder(int orderIndex) {
            return BuildOrders[orderIndex].Process();
        }

        public void PauseBuildOrder(int orderIndex, bool doPause) {
            BuildOrders[orderIndex].Pause(doPause);
        }

        public bool CancelBuildOrder(int orderIndex) {
            BuildOrders[orderIndex].Cancel();
            BuildOrders.RemoveAt(orderIndex);
            return true;
        }

        #region Factory Methods
        /// <summary>
        /// Creates a new colony.
        /// </summary>
        /// <remarks>
        /// - Creates a new PQSCity2 at the given coordinates with the
        ///   given models.
        /// - Creates a new Colony object for the new base 
        ///   and adds it to the PQSCity2's GameObject.
        /// </remarks>
        /// <param name="colonyName">Name for the colony</param>
        /// <param name="colonyID"></param>
        /// <param name="bodyCoordinates">The body and location to place the colony</param>
        /// <param name="altitudeOffset"></param>
        /// <param name="rotationOffset"></param>
        /// <returns>The newly created base controller</returns>
        public static Colony Create(string colonyName,
                                    int colonyID,
                                    BodySurfacePosition bodyCoordinates,
                                    float altitudeOffset,
                                    float rotationOffset) {

            //create the initial PQSCity2. This will be the anchor point for the entire base 
            //from which new base sections can be added (as child models/lodObjects)
            bodyCoordinates.Altitude += altitudeOffset;
            //create the PQSCity2 instance that anchor us to the planet and add the inital base
            SurfaceStructure colonyAnchor = SurfaceStructure.Create(colonyName, bodyCoordinates, rotationOffset);
            //add it to the runtime pqs injector for this planet so it stays updated
            BodyAnchorLoader.Instance.Anchors[bodyCoordinates.BodyName].Add(colonyAnchor);

            //add and initialise the colony controller component
            Colony newColony = colonyAnchor.gameObject.AddComponent<Colony>();
            HexTileGrid hexTileGrid = new HexTileGrid(newColony.transform, 2, ColonyManager.Instance.BaseRadius);
            newColony.Init(colonyAnchor, colonyName, colonyID, bodyCoordinates, altitudeOffset, rotationOffset, hexTileGrid);

            return newColony;
        }

        public static Colony LoadNew(ConfigNode colonyNode) {
            string colonyName = colonyNode.GetValue("colonyName");
            int colonyID = int.Parse(colonyNode.GetValue("colonyID"));
            float altitudeOffset = float.Parse(colonyNode.GetValue("altitudeOffset"));
            float rotationOffset = float.Parse(colonyNode.GetValue("rotationOffset"));
            BodySurfacePosition bodyCoordinates = new BodySurfacePosition(colonyNode.GetNode("SURFACE_POSITION"));

            Colony newColony = Create(colonyName, colonyID, bodyCoordinates, altitudeOffset, rotationOffset);

            //add base parts if they exist
            ConfigNode[] tileConfigs = colonyNode.GetNodes("TILE");
            foreach (ConfigNode node in tileConfigs) {
                HexTile hexTile = HexTile.LoadNew(newColony, node);
                newColony.AddTile(hexTile);
            }

            return newColony;
        }
        #endregion
    }
}