using System;
using System.Collections.Generic;
using ModUtils;
using UnityEngine;

namespace OffworldColoniesPlugin.ColonyManagement {
    public class BaseController: MonoBehaviour
    {
        private PQSCity2 _city;
        private List<HexTile> tiles;
        public PQSCity2 AnchorObject => _city;

        private void Awake() {
            ModLogger.Log($"Initialising {gameObject.name}...");
            tiles = new List<HexTile>();
        }

        private void Start() {
            ModLogger.Log($"{gameObject.name} starting");
        }

        private void OnDestroy() {
            ModLogger.Log($"Destroying {gameObject.name}");

        }

        public void AddTile(HexTile newTile)
        {
            tiles.Add(newTile);
        }

        public void Init(PQSCity2 pqsCity2)
        {
            _city = pqsCity2;
            tiles.Add(new HexTile(_city, this));
        }
    }
}