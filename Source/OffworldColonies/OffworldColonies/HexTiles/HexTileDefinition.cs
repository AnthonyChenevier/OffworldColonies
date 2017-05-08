using System;
using System.Linq;
using PQSModLoader.TypeDefinitions;

namespace OffworldColonies.HexTiles {
    /// <summary>
    /// HexTile definition. Holds all required information for creating
    /// a HexTile with the BuildUI and HexTilePrinterModule.
    /// </summary>
    public class HexTileDefinition: IConfigNode {
        /// <summary>
        /// The name of the HexTile for use in the UI
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Description of the HexTile for use in the UI
        /// </summary>
        public string Desc { get; private set; }
        /// <summary>
        /// This tile's UI preview image index
        /// </summary>
        public int PreviewIndex { get; private set; }
        /// <summary>
        /// The HexTileType to use for creation
        /// </summary>
        public HexTileType TileType { get; private set; }
        /// <summary>
        /// The recipe for this tile type
        /// </summary>
        public HexTileRecipe Recipe { get; private set; }
        /// <summary>
        /// The model definition for this tile type
        /// </summary>
        public MultiLODModelDefinition ModelDefinition { get; private set; }
        /// <summary>
        /// Returns a string containing the amounts of each resource required
        /// </summary>
        public string CostString {
            get {
                return Recipe.Resources.Select(r => $"{r.Value.UnitsRequired} {r.Value.Name}").Aggregate((w, c) => $"{w}, {c}");
            }
        }
        /// <summary>
        /// Returns the build time for this tile as a string in 00h:00m:00s format
        /// </summary>
        public string TimeString {
            get {
                TimeSpan t = TimeSpan.FromSeconds(Recipe.BuildTime);
                return $"{t.Hours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s";
            }
        }

        /// <summary>
        /// ConfigNode Constructor
        /// </summary>
        /// <param name="node"></param>
        public HexTileDefinition(ConfigNode node) {
            Load(node);
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        /// <param name="hexType"></param>
        /// <param name="previewIndex"></param>
        /// <param name="recipe"></param>
        /// <param name="modelDefinition"></param>
        public HexTileDefinition(string name, string desc, HexTileType hexType, int previewIndex, HexTileRecipe recipe, MultiLODModelDefinition modelDefinition) {
            Name = name;
            Desc = desc;
            TileType = hexType;
            PreviewIndex = previewIndex;
            Recipe = recipe;
            ModelDefinition = modelDefinition;
        }

        /// <summary>
        /// ConfigNode Loader
        /// </summary>
        /// <param name="node"></param>
        public void Load(ConfigNode node) {
            Name = node.GetValue("Name");
            Desc = node.GetValue("Desc");
            TileType = HexTile.ParseType(node.GetValue("HexType"));
            PreviewIndex = int.Parse(node.GetValue("PreviewIndex"));

            Recipe = new HexTileRecipe(node.GetNode("TILE_RECIPE"));

            ModelDefinition = new MultiLODModelDefinition();
            ModelDefinition.Load(node.GetNode("LODMODELS"));
        }

        public void Save(ConfigNode node) {
            throw new NotImplementedException();
        }

        public override string ToString() { return $"{Name}: type={TileType}"; }
    }
}