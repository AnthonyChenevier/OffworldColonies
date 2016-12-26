using System.Collections.Generic;

namespace PQSModLoader.TypeDefinitions
{
    /// <summary>
    /// Definition for a multi-LOD model. Similar to a PQSCity2Define 
    /// but only contains a List of LODDefine. For use when adding a new
    /// model (including all LODs for that model) to an existing PQSCity2.
    /// </summary>
    public class MultiLODModelDefinition: IConfigNode
    {
        public List<LODDefinition> LODDefines { get; set; }

        public void Save(ConfigNode node)
        {
            //no need for this
        }

        public void Load(ConfigNode node)
        {
            this.LODDefines = new List<LODDefinition>();
            ConfigNode[] lodNodes = node.GetNodes("LOD");
            foreach (ConfigNode lodNode in lodNodes)
            {
                LODDefinition def = new LODDefinition();
                def.Load(lodNode);
                this.LODDefines.Add(def);
            }

            this.LODDefines.Sort((p, q) => p.VisibleRange.CompareTo(q.VisibleRange));
        }
    }
}