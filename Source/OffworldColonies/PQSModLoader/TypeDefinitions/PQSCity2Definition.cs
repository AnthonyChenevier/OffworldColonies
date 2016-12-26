using System.Collections.Generic;
using UnityEngine;

namespace PQSModLoader.TypeDefinitions
{
    public class PQSCity2Definition : IConfigNode
    {
        public BodySurfacePosition Coordinates { get; set; }
        public double Rotation { get; set; }
        public List<LODDefinition> LODDefines { get; set; }
        public Vector3 UpVector { get; set; }
        public bool SurfaceSnap { get; set; }
        public double SnapHeightOffset { get; set; }
        public string LocationName { get; set; }

        public void Load(ConfigNode node)
        {
            //non-optional values
            LocationName = node.GetValue("LocationName");

            Coordinates = new BodySurfacePosition();
            Coordinates.Load(node.GetNode("COORDINATES"));

            ConfigNode lodModels = node.GetNode("LODMODELS");
            MultiLODModelDefinition modDef = new MultiLODModelDefinition();
            modDef.Load(lodModels);
            LODDefines = modDef.LODDefines;

            //optional values
            Rotation = node.HasValue("Rotation") ? double.Parse(node.GetValue("Rotation")) : 0;
            SurfaceSnap = node.HasValue("SurfaceSnap") && bool.Parse(node.GetValue("SurfaceSnap"));
            SnapHeightOffset = node.HasValue("SnapHeightOffset") ? double.Parse(node.GetValue("SnapHeightOffset")) : 0;
            UpVector = node.HasValue("UpVector") ? ConfigNode.ParseVector3(node.GetValue("UpVector")) : Vector3.up;
        }

        public void Save(ConfigNode node)
        {

        }
    }
}