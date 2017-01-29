using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModUtilities;
using OffworldColonies.Debug;
using PQSModLoader;
using PQSModLoader.Factories;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {
    public class Colony : MonoBehaviour {
        private static ColonyManager CM => ColonyManager.Instance;

        public RuntimePQSCity2 AnchorObject { get; private set; }
        public List<HexTile> Tiles { get; private set; }
        public HexTileGrid HexGrid { get; private set; }

        private int _colonyID;
        public int ColonyID => _colonyID;


        private string _colonyName;
        private BodySurfacePosition _bodyCoordinates;
        private float _altitudeOffset;
        private float _rotationOffset;
        private HexTileType _initialHexTile;

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
        /// <param name="initialHexTile">The inital hex base type</param>
        /// <returns>The newly created base controller</returns>
        public static Colony Create(string colonyName,
            int colonyID,
            BodySurfacePosition bodyCoordinates,
            float altitudeOffset,
            float rotationOffset,
            HexTileType initialHexTile = HexTileType.Default) {

            //create the initial PQSCity2. This will be the anchor point for the entire base 
            //from which new base sections can be added (as child models/lodObjects)
            bodyCoordinates.Altitude += altitudeOffset;
            //create the PQSCity2 instance that anchor us to the planet and add the inital base
            RuntimePQSCity2 colonyCity = PQSCity2Factory.Create(colonyName, bodyCoordinates, rotationOffset);
            //add it to the runtime pqs injector for this planet so it stays updated
            RuntimePQSLoader.Instance.ModInjectors[colonyCity.sphere.name].AddMod(colonyCity);

            //add and initialise the colony controller component
            Colony newColony = colonyCity.gameObject.AddComponent<Colony>();
            newColony._colonyName = colonyName;
            newColony._bodyCoordinates = bodyCoordinates;
            newColony._altitudeOffset = altitudeOffset;
            newColony._rotationOffset = rotationOffset;
            newColony._initialHexTile = initialHexTile;
            newColony._colonyID = colonyID;
            newColony.AnchorObject = colonyCity;
            newColony.HexGrid = new HexTileGrid(4);

            //now add our initial hex base lod models
            MultiLODModelDefinition modelDefinition = CM.HexDefinitions[initialHexTile].ModelDefinition;
            Vector3 positionOffset = newColony.HexGrid.Positions[0];
            MultiLODObject lodObject = PQSCity2Factory.AddMultiLODModelTo(colonyCity, modelDefinition, positionOffset, Vector3.zero, 0);
            HexTile hexTile = new HexTile(newColony, initialHexTile, lodObject, 0);
            newColony.Tiles.Add(hexTile);
            
            return newColony;
        }

        public static Colony Create(ConfigNode colonyNode) {
            string colonyName = colonyNode.GetValue("colonyName");
            int colonyID = int.Parse(colonyNode.GetValue("colonyID"));
            BodySurfacePosition bodyCoordinates = new BodySurfacePosition(colonyNode.GetNode("bodyCoordinates"));
            float altitudeOffset = float.Parse(colonyNode.GetValue("altitudeOffset"));
            float rotationOffset = float.Parse(colonyNode.GetValue("rotationOffset"));
            HexTileType initialHexTile = HexTile.ParseType(colonyNode.GetValue("initialHexTile"));
            Colony newColony = Create(colonyName, colonyID, bodyCoordinates, altitudeOffset, rotationOffset, initialHexTile);

            ConfigNode[] baseParts = colonyNode.GetNodes("basePart");
            foreach (ConfigNode node in baseParts)
                newColony.AddNewHexTile(int.Parse(node.GetValue("hexPosIndex")), HexTile.ParseType(node.GetValue("partType")));
            
            return newColony;
        }

        public void Refresh() {
            AnchorObject.Refresh();
        }


        private void Awake() {
            ModLogger.Log($"Initialising {gameObject.name}...");
            Tiles = new List<HexTile>();
        }

        private void Start() {
            ModLogger.Log($"{gameObject.name} starting");
        }

        private void OnDestroy() {
            ModLogger.Log($"Destroying {gameObject.name}");
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
        /// <param name="hexPosIndex"></param>
        /// <param name="partType"></param>
        public void AddNewHexTile(int hexPosIndex, HexTileType partType) {
            Tiles.Add(HexTile.Create(this, hexPosIndex, partType));
        }

        public ConfigNode ToNode() {
            ConfigNode colonyNode = new ConfigNode("COLONY");

            colonyNode.AddValue("colonyName", _colonyName);
            colonyNode.AddValue("colonyID", _colonyID);
            BodySurfacePosition coordinates = new BodySurfacePosition(_bodyCoordinates);
            coordinates.Altitude -= _altitudeOffset;
            colonyNode.AddNode("bodyCoordinates", coordinates.ToNode());
            colonyNode.AddValue("altitudeOffset", _altitudeOffset);
            colonyNode.AddValue("rotationOffset", _rotationOffset);
            colonyNode.AddValue("initialHexTile", _initialHexTile.ToString());

            foreach (HexTile hexTile in Tiles) {
                if (hexTile == Tiles[0]) continue;
                colonyNode.AddNode("basePart", hexTile.ToNode());
            }

            return colonyNode;
        }
    }
}