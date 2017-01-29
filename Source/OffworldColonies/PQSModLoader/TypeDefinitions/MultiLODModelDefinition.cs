﻿using System.Collections.Generic;

namespace PQSModLoader.TypeDefinitions
{
    /// <summary>
    /// Definition for a multi-LOD model. Similar to a PQSCity2Define 
    /// but only contains a List of LODDefine. For use when adding a new
    /// model (including all LODs for that model) to an existing PQSCity2.
    /// </summary>
    public class MultiLODModelDefinition
    {
        public List<LODDefinition> LODDefines { get; private set; }

        public MultiLODModelDefinition(ConfigNode node) {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            LODDefines = new List<LODDefinition>();
            ConfigNode[] lodNodes = node.GetNodes("LOD");
            foreach (ConfigNode lodNode in lodNodes)
                LODDefines.Add(new LODDefinition(lodNode));

            LODDefines.Sort((p, q) => p.VisibleRange.CompareTo(q.VisibleRange));
        }
    }
}