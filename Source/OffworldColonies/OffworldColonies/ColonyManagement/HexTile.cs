using System.Collections.Generic;
using PQSModLoader.TypeDefinitions;

namespace OffworldColoniesPlugin.ColonyManagement {
    public class HexTile {
        protected BaseController _baseController;
        protected PQSCity2 _basePart;
        public HexTile(PQSCity2 basePart, BaseController controller)
        {
            _baseController = controller;
            _basePart = basePart;
        }
        
        public static void GetModelDataForBaseType(BaseType baseType, out MultiLODModelDefinition baseData)
        {
            //no data to start with
            baseData = new MultiLODModelDefinition();

            //try and find any base model define nodes in loaded configs
            List<ConfigNode> modelDefineNode = new List<ConfigNode>(GameDatabase.Instance.GetConfigNodes("OC_BASE_MODEL_DEFINE"));
            //nothing found, return null
            if (modelDefineNode.Count <= 0)
                return;

            //get the last (for now) instance of the node that corresponds to the type we are looking for
            ConfigNode typeNode = modelDefineNode.FindLast(tNode => tNode.GetValue("HexType") == baseType.ToString());

            //no definition for type found, return null
            if (typeNode == null)
                return;

            //populate base data from the node
            baseData = new MultiLODModelDefinition();
            baseData.Load(typeNode.GetNode("LODMODELS"));

        }

    }
}
