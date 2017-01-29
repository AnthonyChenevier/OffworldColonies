using System.Collections.Generic;
using System.Linq;
using ModUtilities;
using PQSModLoader.Factories;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.ColonyManagement {

    /// <summary>
    /// The different available base types.
    /// TODO: all but default (empty hex)
    /// Default:
    ///     An empty base piece. Useful for creating flat landing 
    ///     areas in hilly terrain or just providing space empty 
    ///     in your colony.
    /// Habitation:
    ///     A basic habitation module that provides living quarters
    ///     for up to 5 kolonists. Upgradable to a spacious habitation module, which
    ///     houses the same number of kerbals, but each spot also comes with an 
    ///     extra unfillable spot, increasing overall colony happiness due to extra space.
    ///     When configured for Kerbonauts, these basically work as hotels 
    ///     for active kerbonauts to rest and relax in a comfortable home-like
    ///     environment before heading back into space. Each space taken by a
    ///     kerbonaut counts the same as a kolonist for all happiness, supply 
    ///     usage and breeding calculations, except they cannot be used to man 
    ///     agricuture or power stations. This hooks into the USI habitation
    ///     timer and can be used to simulate sending a kerbonaut back to Kerbin
    ///     by reducing the lifetime habitation timer back to 0 over time,
    ///     dependent on the overall happiness level of the colony. Kerbonauts 
    ///     can also be instantly converted to kolonists if required, but the
    ///     downside of this is that they lose all of their previous experience
    ///     and speciality for good and must be re-trained from scratch if the
    ///     player wants them to be playable again. Useful for starting colonies 
    ///     with existing kerbonauts in-situ rather than bringing a colonisation
    ///     ship all the way from Kerbin. (TD: make a Kolonist cabin part/MM config!)
    /// Agriculture:
    ///     A space for growing and processing crops into supplies.
    ///     Multiple configurations available to process different 
    ///     parts of the supply chain (Hydrofarm, Waste-Fertilizer Processing, 
    ///     Supply Preperation). Requires kolonists to run and production 
    ///     increases with each additional kolonist manning it.
    /// Power:
    ///     Module to provide power for the colony. Can be configured for 
    ///     solar stations, capacitors or nuclear power stations. Solar 
    ///     work like stock solar on vessels, and would require capacitors 
    ///     (batteries) to store charge for nighttime usage. Nuclear would 
    ///     work like USI reactors, and require periodic uranium input from 
    ///     mining vessels to continue working, but can throttle with usage 
    ///     so that if less power is used less fuel is required. Balance would
    ///     assume the reactor is equipped with a breeder to reduce the amount
    ///     of micro required. Every part of a kolony requires power except for 
    ///     the default base, each with differnt power requirements. Each part 
    ///     requires kolonists to run and maintain.
    /// Nursery:
    ///     Required for kolonists to breed (not that they can't woo-koo at any time, but 
    ///     population must be managed in a colony with limited space and supplies). 
    ///     As long as Kolonists are currently happy, both genders are represented 
    ///     (1 of each per nursery) and there is enough habitation space to hold a
    ///     new kolonist, each nursery will generate 1 new child kolonist at a rate
    ///     of 1 every 6 months. This means that unless the player prevents breeding
    ///     manually by turning off the nursery the colony may grow too much to sustain
    ///     itself with the supplies it generates and kolonist will starve and become
    ///     unhappy, unproductive and die. Child kolonists are like any other kolonist
    ///     with happiness requirements and will even work in the various parts of the kolony, 
    ///     except they cannot be trained and recruited as kerbonauts for at least 1 year.
    /// Recreation:
    ///     Pay a lot of power to keep your kolonists happy. Contains things like theatres, 
    ///     arcades, kerbnet connections back home and low-g laser tag that provide a
    ///     happiness boost needed in larger colonies. Provides 10(upgradable?) recreation 
    ///     spots that kolonists automatically fill if available (think of each available 
    ///     spot as a membership card, kolonists get a membership card which entitles them 
    ///     to use of the facilities. If all the spots are filled then there is not enough 
    ///     entertainment to go around). 
    ///     Happiness is derived from the amount of space per kolonist (no. of extra empty 
    ///     habitation spaces per kolonist), supplies & power (+boost for amount above
    ///     minimum currently stored, -boost for amount below that is 2x as impactful 
    ///     as the +boost), recreation (no of filled recreation spots, has a big +boost), 
    ///     and base diveristy (number of different base types used in the colony). 
    /// Education:
    ///     Akademy that trains Kolonists into recruitable Kerbonauts. Must be staffed
    ///     by a single (3+ star) Kerbonaut to work. Kolonists assigned to education
    ///     live and work in the Akademy for 3 months and are unaffected by happiness:
    ///     They are basically taken out of all kolony equations except for supply and power 
    ///     usage, which is calulated by the akademy itself. Trained Kerbonauts come out of 
    ///     training into kerbonaut-configured habitation and are available to fill nearby
    ///     vessels just like kerbonauts brought from Kerbin. Kerbonauts trained in an 
    ///     akademy are trained as the same type as the instructor (scientist trains 
    ///     scientists, etc) and can have from 0 to the trainers number of stars, using a 
    ///     standard bell curve as the probability of acheiving a high or low grade, so
    ///     most will fall at about half of the trainer's level.
    /// CommRelay:
    ///     Provides a stock KerbNet control point as good as Kerbin ground control for a huge
    ///     power and manpower cost, or as a simpler relay station (like a ground-based relay dish 
    ///     on a vessel, with higher range and power) for a more reasonable power cost and no
    ///     manpower requirement.
    /// Lauchpad:
    ///     A lauchpad that can be used by a nearby manufacturing vessel with Extraplanetary
    ///     Lauchpads. Could also hook into Kerbal Konstructs launchpad selection (?), although 
    ///     the monetary cost of the ship should be converted into required resources at the
    ///     colony site with a similar conversion rate from raw resources -> ships in EPL, and 
    ///     maybe also build time. Second cheapest part to build after the default empty base.
    /// Airstrip:
    ///     Like the Launchpad, but lauches vehicles horizontally (is this even an issue with EPL?).
    ///     Designed to be conected end-to-end in a straight line to provide as long a runway as 
    ///     is needed for the situation. Has good lighting to help with landing at night.
    /// Airlock/Commissary: 
    ///     The entrance/exit point for Kerbonauts into the colony. Acts as an active hatch that
    ///     EVA kerbals can board. This could allow for cool RP, such as having the airlock in 
    ///     the centre of a complex colony, necesitating a large vessel drop a transport rover 
    ///     to travel in close and transfer existing crew for new ones while the old ones get
    ///     some shore leave. Also provides access fo any manned craft in the vicinity (50-100m) 
    ///     to the colony's resources, which can be freely traded 1:1 through a single UI 
    ///     interface (could also allow for crew transfer because the EVA thing could be tedious).
    /// 
    /// DESIGN NOTE:
    ///     I have chosen to keep each colony running on their own separate resources, rather than 
    ///     hooking them into USI's planetary or local logistics. This means that there is an 
    ///     element of micromanagement for each colony on the surface, rather than the more broad,
    ///     abstract balancing that USI uses to keep distant vessel-based colonies connected and to 
    ///     reduce the overall micromanagement of hauling goods manually.
    ///     In RP terms I see this as the KSC company are setting up planetary supply chains (unseens 
    ///     pipes, unseen logistics rovers/probes/drop-pods, whatever) *as a company* and that colonies
    ///     are civilian outposts that must trade with KSP vessels for mined/processed goods, with the
    ///     promise of generating more kerbals far away from Kerbin as a payment. Or just a flat place 
    ///     to land and/or build your own vehicle-based colonies on with no extra mechanics. I'm not your mother.
    /// </summary>
    public enum HexTileType {
        Default,
        Habitation,
        Agriculture,
        Power,
        Nursery,
        Recreation,
        Education,
        CommRelay,
        Launchpad,
        Airstrip,
        Commissary
    }

    /// <summary>
    /// HexTile type information
    /// </summary>
    public class ProtoHexTile {
        public string Name { get; private set; }
        public string Desc { get; private set; }
        public int PreviewIndex { get; private set; }
        public int Time { get; private set; }
        public List<int> Cost { get; private set; }
        public List<string> BuildResources { get; private set; }
        public MultiLODModelDefinition ModelDefinition { get; private set; }
        public string CostString {
            get {
                string s = "";
                for (int i = 0; i < Cost.Count; i++) {
                    s += $"{Cost[i]} {BuildResources[i]}";
                    if (i < Cost.Count - 1)
                        s += ", ";
                }

                return s;
            }
        }

        public string TimeString {
            get { return Time.ToString(); }
        }


        public ProtoHexTile(ConfigNode node) {
            Name = node.GetValue("Name");
            Desc = node.GetValue("Desc");
            PreviewIndex = int.Parse(node.GetValue("PreviewIndex"));
            Time = int.Parse(node.GetValue("Time"));
            string[] entries = node.GetValue("Cost").Split(',');

            Cost = new List<int>();
            BuildResources = new List<string>();
            foreach (string entry in entries) {
                string[] cr = entry.Trim().Split(' ');
                Cost.Add(int.Parse(cr[0]));
                BuildResources.Add(cr[1]);
            }

            ModelDefinition = new MultiLODModelDefinition(node.GetNode("LODMODELS"));
        }
    }

    public class HexTile {
        public Colony Colony { get; private set; }
        public PQSCity2 AnchorCity { get; private set; }
        public MultiLODObject LODObject { get; private set; }
        public int HexID { get; private set; }
        public HexTileType TileType { get; private set; }

        private HexTile[] _connectedTiles;

        public static HexTile Create(Colony colony, int hexPosIndex, HexTileType partType) {
            Vector3 gridPosition = colony.HexGrid.Positions[hexPosIndex];
            MultiLODModelDefinition modelDefinition = ColonyManager.Instance.HexDefinitions[partType].ModelDefinition;

            MultiLODObject lodObject = PQSCity2Factory.AddMultiLODModelTo(colony.AnchorObject, modelDefinition, gridPosition, Vector3.zero, 0);

            return new HexTile(colony, partType, lodObject, hexPosIndex);
        }

        public HexTile(Colony colony, HexTileType partType, MultiLODObject tileObjects, int hexID) {
            TileType = partType;
            HexID = hexID;
            Colony = colony;
            AnchorCity = colony.AnchorObject;
            LODObject = tileObjects;
            _connectedTiles = new HexTile[6];
        }

        /// <summary>
        /// Sets the colour of the base part of the model.
        /// </summary>
        /// <param name="color"></param>
        public void SetBaseColor(Color color) {
            foreach (PQSCity2.LodObject lodObject in LODObject)
                lodObject.objects[0].GetComponentInChildren<MeshRenderer>().material.color = color;
        }

        public void Link(HexTile other, int onSide) {
            if (IsLinked(onSide)) {
                ModLogger.LogError($"Can't connect tiles {this} and {other}, another hex tile already has that link: {GetLinked(onSide)}");
                return;
            }

            _connectedTiles[onSide] = other;
            int otherSide = (onSide + 3)%6+1;
            other._connectedTiles[otherSide] = this;
        }

        public bool IsLinked(int onSide) {
            return _connectedTiles[onSide] != null;
        }

        public HexTile GetLinked(int onSide) {
            return _connectedTiles[onSide];
        }

        public void Unlink(HexTile other) {
            if (!_connectedTiles.Contains(other)) return;

            _connectedTiles[_connectedTiles.IndexOf(other)] = null;
            other._connectedTiles[other._connectedTiles.IndexOf(this)] = null;
        }

        public static HexTileType ParseType(string partString) {
            switch (partString) {
            case "Habitation": return HexTileType.Habitation;
            case "Agriculture": return HexTileType.Agriculture;
            case "Nursery": return HexTileType.Nursery;
            case "Power": return HexTileType.Power;
            case "Recreation": return HexTileType.Recreation;
            case "Education": return HexTileType.Education;
            case "CommRelay": return HexTileType.CommRelay;
            case "Launchpad": return HexTileType.Launchpad;
            case "Airstrip": return HexTileType.Airstrip;
            case "Commissary": return HexTileType.Commissary;
            //case "Empty":
            default: return HexTileType.Default;
            }
        }

        public ConfigNode ToNode() {
            ConfigNode toNode = new ConfigNode();

            toNode.AddValue("partType", TileType.ToString());
            toNode.AddValue("hexPosIndex", HexID);

            return toNode;
        }
    }
}