using UnityEngine;

namespace PQSModLoader.TypeDefinitions
{
    public class StaticModelDefinition: IConfigNode
    {
        public string ModelName { get; set; }
        public string ModelPath { get; set; }
        public Vector3 LocalPosition { get; set; } = Vector3.zero;
        public Vector3 LocalRotationEuler { get; set; } = Vector3.zero;
        public float LocalScale { get; set; } = 1;

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

        public void Save(ConfigNode node)
        {
            //nothing
        }
    }
}