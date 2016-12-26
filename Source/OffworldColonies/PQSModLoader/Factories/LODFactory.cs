using System.Collections.Generic;
using ModUtils;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader.Factories
{
    public class LODFactory {
        public static PQSCity2.LodObject Create(ConfigNode fromNode, Transform parent)
        {
            LODDefinition lodDef = new LODDefinition();
            lodDef.Load(fromNode);
            return Create(lodDef, parent);
        }

        public static PQSCity2.LodObject Create(LODDefinition lodDefinition, Transform parent) {
            ModLogger.Log($"-{parent.name}: Adding models to new LOD = {lodDefinition.VisibleRange}m");
            List<GameObject> lodModels = new List<GameObject>();

            foreach (StaticModelDefinition modelDefine in lodDefinition.Models) {
                GameObject model = StaticModelFactory.Create(modelDefine, parent);
                lodModels.Add(model);
            }

            PQSCity2.LodObject lodObject = new PQSCity2.LodObject();
            lodObject.objects = lodModels.ToArray();
            lodObject.visibleRange = lodDefinition.VisibleRange;

            return lodObject;
        }

        public static PQSCity2.LodObject Create(LODDefinition modelLOD, Transform parent, Vector3 positionOffset, Vector3 rotationOffset, float scaleOffset) {
            List<GameObject> lodModels = new List<GameObject>();
            ModLogger.Log($"-{parent.name}: Adding models to new LOD = {modelLOD.VisibleRange}m with offsets P({positionOffset}) R({rotationOffset}) S({scaleOffset})");

            foreach (StaticModelDefinition m in modelLOD.Models) {
                Vector3 pos = m.LocalPosition + positionOffset;
                Vector3 rot = m.LocalRotationEuler + rotationOffset;
                float scale = m.LocalScale + scaleOffset;
                GameObject model = StaticModelFactory.Create(parent, m.ModelName, m.ModelPath, pos, rot, scale);
                lodModels.Add(model);
            }

            PQSCity2.LodObject lodObject = new PQSCity2.LodObject();
            lodObject.objects = lodModels.ToArray();
            lodObject.visibleRange = modelLOD.VisibleRange;

            return lodObject;
        }
    }
}