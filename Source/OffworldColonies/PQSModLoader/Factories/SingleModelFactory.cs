using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader.Factories
{
    public static class SingleModelFactory {

        public static GameObject Create(ConfigNode fromNode, Transform parent) {
            return Create(new SingleModelDefinition(fromNode), parent);
        }

        public static GameObject Create(SingleModelDefinition modelDefinition, Transform parent) {
            return Create(parent, modelDefinition.ModelName, modelDefinition.ModelPath, modelDefinition.LocalPosition, modelDefinition.LocalRotationEuler, modelDefinition.LocalScale);
        }

        public static GameObject Create(Transform parent, string modelBaseName, string modelPath, Vector3 localPosition, Vector3 localRotationEuler, float localScale) {
            GameObject model = GameDatabase.Instance.GetModel(modelPath);
            if (parent != null) {
                model.name = $"{modelBaseName}_{parent.childCount}";
                Debug.Log($"{Mod.Name}{parent.name}: Adding model instance({model.name}) from path '{modelPath}'");
            }
            else {
                model.name = modelBaseName;
                Debug.Log($"{Mod.Name}Creating model instance({model.name}) from path '{modelPath}'");
            }
            model.SetActive(true);
            model.transform.SetParent(parent, false);
            model.transform.localPosition = localPosition;
            model.transform.localRotation = Quaternion.Euler(localRotationEuler);
            model.transform.localScale = Vector3.one * localScale;

            return model;
        }
    }
}