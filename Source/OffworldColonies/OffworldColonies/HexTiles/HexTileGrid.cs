using System.Collections.Generic;
using System.Linq;
using OffworldColonies.ColonyManagement;
using UnityEngine;

namespace OffworldColonies.HexTiles {
    /// <summary>
    /// Encapsulates a list of local Vector3 positions that define a grid of hexagonal tiles
    /// </summary>
    public class HexTileGrid {
        private readonly Transform _parent;

        private readonly int _width;
        private readonly int _height;

        private readonly int _zStart;
        private readonly int _xStart;

        private HexCoordinates _coordinateStart;

        private HexCell[] _cells;

        private float innerRadius;
        private float outerRadius;

        public HexTileGrid(Transform parent, int width, int height) {
            _parent = parent;
            _width = width;
            _height = height;
            innerRadius = ColonyManager.Instance.BaseRadius;
            outerRadius = innerRadius / (Mathf.Sqrt(3) * .5f);

            _zStart = -_height / 2;
            _xStart = -_width / 2;
            _coordinateStart = HexCoordinates.FromOffsetCoordinates(_xStart, _zStart);

            _cells = new HexCell[_height * _width];
            for (int oz = 0, i = 0; oz < _height; oz++)
                for (int ox = 0; ox < _width; ox++)
                    CreateCell(ox, oz, i++);
        }

        private void CreateCell(int ox, int oz, int cellIndex) {

            Vector3 position;
            position.x = (ox + oz * 0.5f - oz / 2) * (innerRadius * 2f);
            position.y = 0f;
            position.z = oz * (outerRadius * 1.5f);

            HexCell cell = _cells[cellIndex] = Object.Instantiate(CellPrefab);

            cell.name = "HX_" + cellIndex;
            cell.transform.SetParent(_parent, false);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(ox + _xStart, oz + _zStart);

            if (ox > 0)
                cell.SetNeighbor(HexDirection.W, _cells[cellIndex - 1]);

            if (oz <= 0)
                return;

            if ((oz & 1) == 0) { //even rows 
                cell.SetNeighbor(HexDirection.SE, _cells[cellIndex - _width]);
                if (ox > 0)
                    cell.SetNeighbor(HexDirection.SW, _cells[cellIndex - _width - 1]);
            }
            else { //odd rows
                cell.SetNeighbor(HexDirection.SW, _cells[cellIndex - _width]);
                if (ox < _width - 1)
                    cell.SetNeighbor(HexDirection.SE, _cells[cellIndex - _width + 1]);
            }
        }
        private HexCell GetCell(HexCoordinates coordinates) {
            //Debug.Log("Getting cell at " + coordinates);
            int z = coordinates.Z - _coordinateStart.Z;
            if (z < 0 || z >= _height)
                return null;

            int x = (coordinates.X - _coordinateStart.X) + z / 2;
            if (x < 0 || x >= _width)
                return null;

            int i = x + z * _width;
            return _cells[i];
        }
        public HexCell CellAtPosition(Vector3 worldPosition) {
            Vector3 localPosition = _parent.InverseTransformPoint(worldPosition);
            HexCoordinates coordinates = HexCoordinates.FromPosition(localPosition);
            coordinates = new HexCoordinates(coordinates.X + _coordinateStart.X, coordinates.Z + _coordinateStart.Z);
            return GetCell(coordinates);
        }

        /// <summary>
        /// gets the localPosition at hexPosIndex on the grid.
        /// </summary>
        /// <param name="hexPosIndex"></param>
        /// <param name="localPosition"></param>
        /// <param name="allowExpansion"></param>
        /// <returns></returns>
        public bool GetCellLocal(int hexPosIndex, out Vector3 localPosition, bool allowExpansion = false) {
            if (allowExpansion && hexPosIndex > _positions.Count)
                GenerateRingsToIndex(hexPosIndex);
            
            if (hexPosIndex >= _positions.Count) {
                localPosition = Vector3.zero;
                return false;
            }

            localPosition = _positions[hexPosIndex];
            return true;
        }

        public bool GetCell(int hexPosIndex, out Vector3 worldPosition, bool allowExpansion = false) {
            Vector3 localPosition;
            if (GetCellLocal(hexPosIndex, out localPosition, allowExpansion)) {
                worldPosition = _parent.TransformPoint(localPosition);
                return true;
            }
            worldPosition = Vector3.zero;
            return false;
        }

        private Vector3 CellAtLocalPosition(Vector3 localPosition) {
            List<Vector3> positions = new List<Vector3>(_positions);
            positions.Sort((p, q) => Vector3.Distance(p, localPosition).CompareTo(Vector3.Distance(q, localPosition)));
            return positions.First();
        }

        //public Vector3 CellAtPosition(Vector3 worldPosition) {
        //    Vector3 localPosition = _parent.InverseTransformPoint(worldPosition);
        //    Vector3 gridPosition = CellAtLocalPosition(localPosition);
        //    return _parent.TransformPoint(gridPosition);
        //}

        public int CellIndexAtPosition(Vector3 worldPosition) {
            Vector3 localPosition = _parent.InverseTransformPoint(worldPosition);
            Vector3 gridPosition = CellAtLocalPosition(localPosition);
            return _positions.IndexOf(gridPosition);
        }
    }
}