using System.Collections.Generic;

namespace PQSModLoader.TypeDefinitions
{
    /// <summary>
    /// Simple struct to hold LODObject definitions (from config or hard-coded).
    /// TODO: add local position, rotation and scale
    /// </summary>
    public class LODModelDefinition
    {
        public List<SingleModelDefinition> Models { get; private set; }
        public float VisibleRange { get; private set; }

        public LODModelDefinition(ConfigNode node) {
            Load(node);
        }

        public void Load(ConfigNode node) {
            VisibleRange = float.Parse(node.GetValue("VisibleRange"));

            Models = new List<SingleModelDefinition>();
            ConfigNode[] models = node.GetNodes("MODEL");
            foreach (ConfigNode modelNode in models)
                Models.Add(new SingleModelDefinition(modelNode));
        }
    }
}