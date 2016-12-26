using System.Collections.Generic;
using Highlighting;
using ModUtils;
using OffworldColoniesPlugin.Debug;
using PQSModLoader;
using PQSModLoader.Factories;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColoniesPlugin.ColonyManagement {

    public enum BaseType {
        Default
    }

    /// <summary>
    /// The offworld colonies main plugin handles saving and 
    /// loading config data and persistent save data, managing persistent
    /// base progression, resource usage and needs for all developed colonies
    /// and providing UI hooks for certain functions
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION)]
    public class ColonyManagerScenario : ScenarioModule {
        //NOT A SINGLETON INSTANCE. This property refers to the currently instantiated 
        //scenario if the scene actually has one, otherwise will return NULL, so make 
        //sure to check for that if using this.
        public static ColonyManagerScenario CurrentInstance { get; private set; }

        private ConfigNode _settingsNode;
        private const string ModName = "OWColonies";
        private List<BaseController> _baseControllers = new List<BaseController>();
        private StaticModelDefinition _basicHexModel;
        private GameObject _ghostInstance;

        public ColonyManagerScenario()
        {
            ModLogger.Log("ColonyManager ScenarioModule instance created"); 
            if (CurrentInstance != null)
                ModLogger.Log("ERROR: ColonyManagerScenario is attempting to overwrite an existing instance!", XKCDColors.RedOrange);
            CurrentInstance = this;
            ModLogger.Title = "OC ColonyManager";
        }

        public override void OnAwake() {
            ModLogger.Log($"ColonyManagerScenario.OnAwake in {HighLogic.LoadedScene}");
            base.OnAwake();

            switch (HighLogic.LoadedScene) {
            case GameScenes.SPACECENTER:
                ModLogger.Log("SpaceCenter");
                //adds a default base in a stupid, but visible location just offshore from KSC.
                LoadTestCity();
                break;
            case GameScenes.TRACKSTATION:
                ModLogger.Log("TrackingStation");
                break;
            case GameScenes.FLIGHT:
                ModLogger.Log("Flight");
                break;
            case GameScenes.EDITOR:
                ModLogger.Log("Editor");
                break;
            }
        }

        public void OnDestroy() {
            ModLogger.Log($"ColonyManagerScenario.OnDestroy in {HighLogic.LoadedScene}");
            CurrentInstance = null;
        }

        public override void OnLoad(ConfigNode gameNode) {
            base.OnLoad(gameNode);
            if (gameNode.HasNode("OFFWORLD_COLONY_SETTINGS")) {
                _settingsNode = gameNode.GetNode("OFFWORLD_COLONY_SETTINGS");
            }
            else {
                ConfigNode[] settingsNodes = GameDatabase.Instance.GetConfigNodes("OFFWORLD_COLONY_SETTINGS");
                if (settingsNodes.Length > 1) {
                    ModLogger.Log("ERROR multiple OFFWORLD_COLONY_SETTINGS nodes found", XKCDColors.RedOrange);
                    return;
                }
                _settingsNode = settingsNodes[0];
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
                ConfigNode hexNode = GameDatabase.Instance.GetConfigNodes("OC_BASIC_HEX")[0];
                StaticModelDefinition modDef = new StaticModelDefinition();
                modDef.Load(hexNode.GetNode("MODEL"));
                _basicHexModel = modDef;
                _basicHexModel.ModelName = "BasicHex";
            }
            ModLogger.Log("ColonyManager ScenarioModule loaded");
        }

        public override void OnSave(ConfigNode gameNode) {
            base.OnSave(gameNode);
            if (gameNode.HasNode("OFFWORLD_COLONY_SETTINGS")) {
                _settingsNode = gameNode.GetNode("OFFWORLD_COLONY_SETTINGS");
            }
            else {
                _settingsNode = gameNode.AddNode("OFFWORLD_COLONY_SETTINGS");
            }
            ModLogger.Log("ColonyManager ScenarioModule saved");
        }

        /// <summary>
        /// Creates a new base. 
        /// -Creates a new PQSCity2 at the given coordinates with the given models.
        /// -Creates a new BaseController object for the new base and adds it to the PQSCity2's GameObject. 
        /// </summary>
        /// <param name="bodyCoordinates">The body and location to place the base</param>
        /// <param name="initialBaseType"></param>
        /// <param name="baseName">Optional name for the base</param>
        /// <returns>The newly created base controller</returns>
        public BaseController CreateNewBase(BodySurfacePosition bodyCoordinates, BaseType initialBaseType = BaseType.Default, string baseName = null) {
            //use a basic unique identifier if no name given
            if (baseName == null)
                baseName = "ColonyBase" + bodyCoordinates.Body.name;// + _baseControllers.Count;

            MultiLODModelDefinition baseTypeData;
            HexTile.GetModelDataForBaseType(initialBaseType, out baseTypeData);


            //create the initial PQSCity2. This will be the anchor point for the entire base 
            //from which new base sections can be added (as child models/lodObjects)
            PQSCity2 newCity = PQSCity2Factory.Create(baseName, bodyCoordinates);
            //PQSCity2Factory.AddMultiLODModelTo(newCity, baseTypeData, Vector3.zero, Vector3.zero, 0);
            RuntimePQSLoader.Instance.ModInjectors[bodyCoordinates.Body.bodyName].AddMod(newCity);

            BaseController hb = newCity.gameObject.AddComponent<BaseController>();
            hb.Init(newCity);

            ScreenMessages.PostScreenMessage($"New Base '{baseName}': Construction Complete", 10f, ScreenMessageStyle.UPPER_CENTER);

            return hb;
        }

        public void AddBasePart(BaseController baseController, int hexBasePosition, BaseType partType) {
            MultiLODModelDefinition baseTypeData;
            HexTile.GetModelDataForBaseType(partType, out baseTypeData);

            Vector3 initialPosition = baseController.AnchorObject.transform.position;
            Vector3 newPosOffset = -baseController.AnchorObject.transform.forward * 20;
            List<Vector3> nextPosition = FlatLandGenerator.GenerateBasePositions(initialPosition, 1, newPosOffset);

            PQSCity2Factory.AddMultiLODModelTo(baseController.AnchorObject, baseTypeData, nextPosition[0], Vector3.zero, 0);

            //baseController.
            //baseController.AddTile();
        }

        public GameObject GetPlacementGhost() {
            //we already have one. just return the instance
            if (_ghostInstance != null)
                return _ghostInstance;

            //otherwise we have to generate one.
            _ghostInstance = StaticModelFactory.Create(_basicHexModel, null);
            //ghosts don't collide
            _ghostInstance.GetComponentInChildren<Collider>().enabled = false;

            //add a highlighting script to make it all fancypants
            Highlighter highlight = _ghostInstance.AddComponent<Highlighter>();
            highlight.ConstantOn(XKCDColors.BrightRed);
            highlight.SeeThroughOff();
            //and give it a colour befitting its status
            MaterialPropertyBlock clrMPB = new MaterialPropertyBlock();
            clrMPB.SetFloat(PropertyIDs._RimFalloff, 1.35f);
            clrMPB.SetColor(PropertyIDs._RimColor, new Color32(255,101,101,255));
            //clrMPB.SetColor("_TemperatureColor", new Color(28 / 256f, 0, 0, 0));
            clrMPB.SetColor("_BurnColor", new Color32(141,0,0, 255));


            Renderer renderer = _ghostInstance.GetComponentInChildren<MeshRenderer>();
            renderer.material.shader = Shader.Find("KSP/Alpha/Translucent");
            renderer.SetPropertyBlock(clrMPB);
            //now turn it on
            _ghostInstance.SetActive(true);

            return _ghostInstance;
        }

        /// <summary>
        /// Simple test for creating new City on load
        /// </summary>
        private void LoadTestCity() {
            CelestialBody cbKerbin = PSystemManager.Instance.localBodies.Find(p => p.bodyName == "Kerbin");
            BodySurfacePosition coordinates = new BodySurfacePosition(0, -74.395f, 65, cbKerbin);
            ConfigNode cityNode = RuntimePQSLoader.Instance.StaticConfigs[0].GetNode("PQSCITY2");
            //because we are doing this at load time the mod will hook into the native PQS
            //mod system without using our custom mod injector
            PQSCity2 pqsCity = PQSCity2Factory.Create(cityNode);
            //when adding them at any other time then use this
            RuntimePQSLoader.Instance.ModInjectors[pqsCity.sphere.name].AddMod(pqsCity);
            Vector3 initialPosition = coordinates.WorldPosition;
            Vector3 newPosOffset = pqsCity.transform.right * 40;
            List<Vector3> nextPosition = FlatLandGenerator.GenerateBasePositions(initialPosition, 2, newPosOffset);
            MultiLODModelDefinition baseData;
            HexTile.GetModelDataForBaseType(BaseType.Default, out baseData);
            PQSCity2Factory.AddMultiLODModelTo(pqsCity, baseData, nextPosition[1], Vector3.zero, 0);


            pqsCity.objects[0].objects[0].GetComponentInChildren<MeshRenderer>().material.color = XKCDColors.BrightOlive;
        }
    }
}
