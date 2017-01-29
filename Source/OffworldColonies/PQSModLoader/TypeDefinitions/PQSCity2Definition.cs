using System.Collections.Generic;
using UnityEngine;

namespace PQSModLoader.TypeDefinitions
{
    public class PQSCity2Definition
    {
        public BodySurfacePosition Coordinates { get; private set; }
        public double Rotation { get; private set; }
        public List<LODDefinition> LODDefines { get; private set; }
        public Vector3 UpVector { get; private set; }
        public bool SurfaceSnap { get; private set; }
        public double SnapHeightOffset { get; private set; }
        public string LocationName { get; private set; }

        public PQSCity2Definition(ConfigNode node) {
            Load(node);
        }

        public void Load(ConfigNode node) {
            //non-optional values
            LocationName = node.GetValue("LocationName");

            Coordinates = new BodySurfacePosition(node.GetNode("COORDINATES"));

            ConfigNode lodModels = node.GetNode("LODMODELS");
            LODDefines = new MultiLODModelDefinition(lodModels).LODDefines;

            //optional values
            Rotation = node.HasValue("Rotation") ? double.Parse(node.GetValue("Rotation")) : 0;
            SurfaceSnap = node.HasValue("SurfaceSnap") && bool.Parse(node.GetValue("SurfaceSnap"));
            SnapHeightOffset = node.HasValue("SnapHeightOffset") ? double.Parse(node.GetValue("SnapHeightOffset")) : 0;
            UpVector = node.HasValue("UpVector") ? ConfigNode.ParseVector3(node.GetValue("UpVector")) : Vector3.up;
        }
    }
}