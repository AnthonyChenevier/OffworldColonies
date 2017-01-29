using System.Collections.Generic;

namespace PQSModLoader.TypeDefinitions
{
    /// <summary>
    /// Simple struct to hold LODObject definitions (from config or hard-coded).
    /// TODO: add local position, rotation and scale
    /// </summary>
    public class LODDefinition
    {
        public List<StaticModelDefinition> Models { get; private set; }
        public float VisibleRange { get; private set; }

        public LODDefinition(ConfigNode node) {
            Load(node);
        }

        public void Load(ConfigNode node) {
            VisibleRange = float.Parse(node.GetValue("VisibleRange"));

            Models = new List<StaticModelDefinition>();
            ConfigNode[] models = node.GetNodes("MODEL");
            foreach (ConfigNode modelNode in models)
                Models.Add(new StaticModelDefinition(modelNode));
        }
    }
}