using UnityEngine;
using System.Collections;
using PQSModLoader.TypeDefinitions;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;
    public HexCell[] Neighbors = new HexCell[6];

    public MultiLODModel CellLODModels;
    //private Renderer _renderComponent;
    //private Material _sharedMaterial;

    //private Color _color;
    //public Color Color {
    //    get { return _color; }
    //    set {
    //        if (_sharedMaterial && _sharedMaterial.color != value)
    //            _sharedMaterial.color = value;
    //        _color = value;
    //    }
    //}

    //void Start() {
    //    _renderComponent = GetComponent<Renderer>();
    //    _sharedMaterial = _renderComponent.material;
    //    Color = Color;
    //}

    public HexCell GetNeighbor(HexDirection direction) {
        return Neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        Neighbors[(int)direction] = cell;
        cell.Neighbors[(int)direction.Opposite()] = this;
    }
}
