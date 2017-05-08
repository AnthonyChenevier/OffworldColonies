using System;
using System.Linq;
using OffworldColonies.ColonyManagement;
using OffworldColonies.Utilities;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.HexTiles {
    public class HexTile {
        public Colony Colony { get; set; }
        public SurfaceStructure AnchorStructure => Colony.SurfaceAnchor;

        public int HexID { get; private set; }
        public HexTileType TileType { get; private set; }


        private MultiLODModelDefinition _modelDefinition;
        private MultiLODModel _lodModelInstance;
        private HexTile[] _connectedTiles;
        private float _rotation;

        public HexTile() {
            Colony = null;
            HexID = -1;
            TileType = HexTileType.Default;

            _rotation = 0;
            _modelDefinition = null;
            _lodModelInstance = null;
            _connectedTiles = new HexTile[6];
        }

        public HexTile(HexTileType partType, int hexID, float rotation) : this() {
            TileType = partType;
            HexID = hexID;
            _rotation = rotation;
            _modelDefinition = ColonyManager.Instance.HexDefinitions[TileType].ModelDefinition;
        }

        public void Init(Colony colony) {
            Vector3 localPosition;
            colony.HexGrid.GetCellLocal(HexID, out localPosition, true);
            Colony = colony;
            _lodModelInstance = AnchorStructure.AddModelTo(_modelDefinition,
                                                           localPosition,
                                                           new Vector3(0, _rotation, 0),
                                                           0);
        }

        public void Save(ConfigNode node) {
            node.AddValue("tileType", TileType.ToString());
            node.AddValue("gridPosIndex", HexID);
            node.AddValue("tileRotation", _rotation);
        }

        public void Load(ConfigNode node) {
            TileType = ParseType(node.GetValue("tileType"));
            HexID = int.Parse(node.GetValue("gridPosIndex"));
            _rotation = float.Parse(node.GetValue("tileRotation"));
            _modelDefinition = ColonyManager.Instance.HexDefinitions[TileType].ModelDefinition;
        }

        /// <summary>
        /// Sets the colour of the base part of the model.
        /// </summary>
        /// <param name="color"></param>
        public void SetBaseColor(Color color) {
            foreach (PQSCity2.LodObject lodObject in _lodModelInstance)
                lodObject.objects[0].GetComponentInChildren<MeshRenderer>().material.color = color;
        }

        public void Link(HexTile other, int onSide) {
            if (IsLinked(onSide)) {
                ModLogger.LogError($"Can't connect tiles {this} and {other}, another hex tile already has that link: {GetLinked(onSide)}");
                return;
            }

            _connectedTiles[onSide] = other;
            int otherSide = (onSide + 3) % 6 + 1;
            other._connectedTiles[otherSide] = this;
        }

        public bool IsLinked(int onSide) {
            return _connectedTiles[onSide] != null;
        }

        public HexTile GetLinked(int onSide) {
            return _connectedTiles[onSide];
        }

        public void Unlink(HexTile other) {
            if (!_connectedTiles.Contains(other))
                return;

            _connectedTiles[_connectedTiles.IndexOf(other)] = null;
            other._connectedTiles[other._connectedTiles.IndexOf(this)] = null;
        }

        public static HexTileType ParseType(string partString) {
            switch (partString) {
            case "Habitation":
                return HexTileType.Habitation;
            case "Agriculture":
                return HexTileType.Agriculture;
            case "Nursery":
                return HexTileType.Nursery;
            case "Power":
                return HexTileType.Power;
            case "Recreation":
                return HexTileType.Recreation;
            case "Education":
                return HexTileType.Education;
            case "CommRelay":
                return HexTileType.CommRelay;
            case "Launchpad":
                return HexTileType.Launchpad;
            case "Airstrip":
                return HexTileType.Airstrip;
            case "Commissary":
                return HexTileType.Commissary;
            //case "Empty":
            default:
                return HexTileType.Default;
            }
        }

        public static HexTile Create(Colony colony, int hexID, HexTileType partType, float rotation) {
            HexTile hexTile = new HexTile(partType, hexID, rotation);
            hexTile.Init(colony);
            return hexTile;
        }

        public static HexTile LoadNew(Colony colony, ConfigNode node) {
            HexTile hexTile = new HexTile();
            hexTile.Load(node);
            hexTile.Init(colony);

            return hexTile;
        }
    }
}