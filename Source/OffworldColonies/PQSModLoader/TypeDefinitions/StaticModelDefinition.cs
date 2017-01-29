using UnityEngine;

namespace PQSModLoader.TypeDefinitions
{
    public class StaticModelDefinition
    {
        public string ModelName { get; set; }
        public string ModelPath { get; private set; }
        public Vector3 LocalPosition { get; private set; } = Vector3.zero;
        public Vector3 LocalRotationEuler { get; private set; } = Vector3.zero;
        public float LocalScale { get; private set; } = 1;

        public StaticModelDefinition(ConfigNode node) {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            ModelName = node.GetValue("ModelName");
            ModelPath = node.GetValue("ModelPath");

            if (node.HasValue("LocalPosition"))
                LocalPosition = ConfigNode.ParseVector3(node.GetValue("LocalPosition"));

            if (node.HasValue("LocalRotationEuler"))
                LocalRotationEuler = ConfigNode.ParseVector3(node.GetValue("LocalRotationEuler"));

            if (node.HasValue("LocalScale"))
                LocalScale = float.Parse(node.GetValue("LocalScale"));
        }
    }
}