using System.Collections.Generic;
using System.Linq;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader.Factories
{
    public class LODModelFactory {
        public static PQSCity2.LodObject Create(ConfigNode fromNode, Transform parent) {
            return Create(new LODModelDefinition(fromNode), parent);
        }

        public static PQSCity2.LodObject Create(LODModelDefinition lodModelDefinition, Transform parent) {
            Debug.Log($"{Mod.Name}{parent.name}: Adding models to new LOD = {lodModelDefinition.VisibleRange}m");

            PQSCity2.LodObject lodObject = new PQSCity2.LodObject {
                                               visibleRange = lodModelDefinition.VisibleRange,
                                               objects = lodModelDefinition.Models.Select(m => SingleModelFactory.Create(m, parent)).ToArray()
                                           };

            return lodObject;
        }

        public static PQSCity2.LodObject Create(LODModelDefinition modelLODModel, Transform parent, Vector3 positionOffset, Vector3 rotationOffset, float scaleOffset) {
            Debug.Log($"{Mod.Name}{parent.name}: Adding models to new LOD = {modelLODModel.VisibleRange}m with offsets P({positionOffset}) R({rotationOffset}) S({scaleOffset})");

            return new PQSCity2.LodObject {
                visibleRange = modelLODModel.VisibleRange,
                objects = (from m in modelLODModel.Models
                           let pos = m.LocalPosition + positionOffset
                           let rot = m.LocalRotationEuler + rotationOffset
                           let scale = m.LocalScale + scaleOffset
                           select SingleModelFactory.Create(parent, m.ModelName, m.ModelPath, pos, rot, scale)).ToArray()
            };
        }
    }
}