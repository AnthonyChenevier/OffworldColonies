using OffworldColonies.ColonyManagement;
using UnityEngine;

namespace OffworldColonies.HexGrid {
    public class HexGrid {
        private readonly Transform _parent;

        private readonly int _width;
        private readonly int _height;

        private readonly int _zStart;
        private readonly int _xStart;

        private HexCoordinates _coordinateStart;
    
        private HexCell[] _cells;

        private float innerRadius;
        private float outerRadius;

        public HexGrid(Transform parent, int width, int height) {
            _parent = parent;
            _width = width;
            _height = height;
            innerRadius = ColonyManager.Instance.BaseRadius;
            outerRadius = innerRadius / (Mathf.Sqrt(3) * .5f);

            _zStart = -_height / 2;
            _xStart = -_width / 2;
            _coordinateStart = HexCoordinates.FromOffsetCoordinates(_xStart, _zStart);
            GenerateHexGrid();
        }


        private void GenerateHexGrid() {
            _cells = new HexCell[_height * _width];
            //split start and end so that hex 0,0,0 is centred
        
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
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(ox+_xStart, oz+_zStart);

            if (ox > 0)
                cell.SetNeighbor(HexDirection.W, _cells[cellIndex - 1]);

            if (oz <= 0) return;

            if ((oz & 1) == 0) { //even rows 
                cell.SetNeighbor(HexDirection.SE, _cells[cellIndex - _width]);
                if (ox > 0) {
                    cell.SetNeighbor(HexDirection.SW, _cells[cellIndex - _width - 1]);
                }
            }
            else { //odd rows
                cell.SetNeighbor(HexDirection.SW, _cells[cellIndex - _width]);
                if (ox < _width - 1) {
                    cell.SetNeighbor(HexDirection.SE, _cells[cellIndex - _width + 1]);
                }
            }
        }


        //private void HandleInput() {
        //    //if (!Input.GetMouseButtonDown(0))
        //    //    return;
        //    Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    RaycastHit hit;
        //    if (!Physics.Raycast(inputRay, out hit))
        //        return;


        //    HexCell cell = CellAtPosition(hit.point);
        //    //HexCell cell = hit.collider.GetComponent<HexCell>();//CellAtPosition(hit.point);
        //    if (!cell)
        //        return;

        //    //int index = _cells.ToList().IndexOf(cell);
        //    //Debug.Log("Hit cell " + cell.coordinates + ", cells index["+index+"]");

        //    if (ShowNeighbours)
        //        HighlightNeighbors(cell);
        //    else
        //        SelectCells(cell);
        //}


        //private void SelectCells(HexCell center) {
        //    int centerX = center.coordinates.X;
        //    int centerZ = center.coordinates.Z;
        //    int v = (brushSize - 1);

        //    //negative z to center
        //    for (int r = 0, z = centerZ - v; z <= centerZ; z++, r++) {
        //        //negative x from centrex-r (current brushSize level)
        //        for (int x = centerX - r; x <= centerX + v; x++) {
        //            HexCell cell = GetCell(new HexCoordinates(x, z));
        //            if (cell)
        //                cell.Color = HighlightColor;
        //        }
        //    }
        //    //positive z to center (excluding center)
        //    for (int r = 0, z = centerZ + v; z > centerZ; z--, r++) {
        //        for (int x = centerX - v; x <= centerX + r; x++) {
        //            HexCell cell = GetCell(new HexCoordinates(x, z));
        //            if (cell)
        //                cell.Color = HighlightColor;
        //        }
        //    }
        //}

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

        //private void HighlightNeighbors(HexCell cell) {
        //    cell.Color = HighlightColor;
        //    for (HexDirection i = 0; i <= (HexDirection)5; i++) {
        //        HexCell neighbor = cell.Neighbors[(int)i];
        //        if (!neighbor) continue;

        //        Color highlight;
        //        switch (i) {
        //            case HexDirection.NE:
        //                highlight = NEColor;
        //                break;
        //            case HexDirection.E:
        //                highlight = EColor;
        //                break;
        //            case HexDirection.SE:
        //                highlight = SEColor;
        //                break;
        //            case HexDirection.SW:
        //                highlight = SWColor;
        //                break;
        //            case HexDirection.W:
        //                highlight = WColor;
        //                break;
        //            case HexDirection.NW:
        //                highlight = NWColor;
        //                break;
        //            default:
        //                Debug.LogWarning("Unknown HexDirection used");
        //                highlight = HighlightColor;
        //                break;
        //        }
        //        neighbor.Color = highlight;
        //    }
        //}
    }
}
