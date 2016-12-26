using System;
using System.Collections.Generic;

namespace PQSModLoader.TypeDefinitions
{
    /// <summary>
    /// Simple struct to hold LODObject definitions (from config or hard-coded).
    /// TODO: add local position, rotation and scale
    /// </summary>
    public class LODDefinition:IConfigNode
    {
        public List<StaticModelDefinition> Models { get; set; }
        public float VisibleRange { get; set; }

        public void Save(ConfigNode node) {
            //nothing for now
        }

        public void Load(ConfigNode node) {
            this.VisibleRange = float.Parse(node.GetValue("VisibleRange"));
            ConfigNode[] models = node.GetNodes("MODEL");

            this.Models = new List<StaticModelDefinition>();

            foreach (ConfigNode modelNode in models)
            {
                StaticModelDefinition modDef = new StaticModelDefinition();
                modDef.Load(modelNode);
                this.Models.Add(modDef);
            }
        }
    }
}