using System.Collections.Generic;
using System.Linq;

namespace OffworldColonies.HexTiles {
    public class HexTileRecipe {
        public Dictionary<int, RecipeResource> Resources { get; private set; }
        public double BuildTime { get; private set; }

        public HexTileRecipe(ConfigNode configNode) {
            BuildTime = double.Parse(configNode.GetValue("BuildTime"));
            Resources = new Dictionary<int, RecipeResource>();

            foreach (ConfigNode node in configNode.GetNodes("RESOURCE")) {
                RecipeResource resource = new RecipeResource(node, BuildTime);
                Resources.Add(resource.ID, resource);
            }
        }

        public override string ToString() {
            return $"Build time = {BuildTime}. Ingredients::{Resources.Select(r => r.Value.ToString()).Aggregate((w, c) => $"{w}, {c}")}";
        }
    }

    public struct RecipeResource {
        public readonly PartResourceDefinition PartResourceDef;
        public readonly double UnitsRequired;
        public readonly double FlowSpeed;

        public int ID => PartResourceDef.id;
        public string Name => PartResourceDef.name;

        public RecipeResource(ConfigNode node, double buildTime) {
            PartResourceDef = PartResourceLibrary.Instance.GetDefinition(node.GetValue("Name"));
            UnitsRequired = double.Parse(node.GetValue("UnitsRequired"));
            FlowSpeed = UnitsRequired / buildTime;
        }

        public override string ToString() {
            return $"({UnitsRequired} {Name} @ {FlowSpeed}units/s)";
        }
    }
}