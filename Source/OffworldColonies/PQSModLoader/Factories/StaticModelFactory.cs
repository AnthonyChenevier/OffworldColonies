using ModUtilities;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader.Factories
{
    public static class StaticModelFactory {
        public static GameObject Create(ConfigNode fromNode, Transform parent) {
            return Create(new StaticModelDefinition(fromNode), parent);
        }

        public static GameObject Create(StaticModelDefinition modelDefinition, Transform parent) {
            return Create(parent, modelDefinition.ModelName, modelDefinition.ModelPath, modelDefinition.LocalPosition, modelDefinition.LocalRotationEuler, modelDefinition.LocalScale);
        }

        public static GameObject Create(Transform parent, string modelBaseName, string modelPath, Vector3 localPosition, Vector3 localRotationEuler, float localScale) {
            GameObject model = GameDatabase.Instance.GetModel(modelPath);
            if (parent != null) {
                model.name = $"{modelBaseName}_{parent.childCount}";
                ModLogger.Log($"{parent.name}: Adding model instance({model.name}) from path '{modelPath}'");
            }
            else {
                model.name = modelBaseName;
                ModLogger.Log($"Creating model instance({model.name}) from path '{modelPath}'");
            }
            model.SetActive(true);
            model.transform.parent = parent;
            model.transform.localPosition = localPosition;
            model.transform.localRotation = Quaternion.Euler(localRotationEuler);
            model.transform.localScale = Vector3.one * localScale;

            return model;
        }
    }
}