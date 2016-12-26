using System.Collections.Generic;
using System.Linq;
using ModUtils;
using PQSModLoader.Factories;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader {
    /// <summary>
    /// Loads a RuntimePQSModInjector for every compatible body 
    /// found so that other addons can add their own PQSMods at runtime
    /// by adding to the public ModInjectors Dictionary
    /// </summary>
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class RuntimePQSLoader : MonoBehaviour
    {
        public static RuntimePQSLoader Instance { get; private set; }

        public Dictionary<string, RuntimePQSModInjector> ModInjectors;

        public List<ConfigNode> StaticConfigs { get; private set; }
        public List<PQSMod> LoadedMods { get; private set; }


        void OnDestroy()
        {
            PSystemManager.Instance.OnPSystemReady.Remove(AddModsToPSystem);
        }

        /// <summary>
        /// This is the first code that runs in the entire mod (other than part loading). 
        /// If another part of the mod attempts to do things before this then it may 
        /// need to set up the logger instead.
        /// </summary>
        void Awake()
        {
            Instance = this;
            //don't die, we may need to do things in other scenes too
            DontDestroyOnLoad(this);

            //set up our logger with this mod's title
            ModLogger.Title = "PQSModLoader";
            StaticConfigs = new List<ConfigNode>(GameDatabase.Instance.GetConfigNodes("OC_PQSLOADER_STATIC_DEFINITION"));
            ModInjectors = new Dictionary<string, RuntimePQSModInjector>();

            //Add our mod once the system prefab has loaded (allows for Kopernicus compatibility?)
            ModLogger.Log("RuntimePQSLoader awake. Adding OnPSystemReady listener.");
            PSystemManager.Instance.OnPSystemReady.Add(AddModsToPSystem);
        }


        /// <summary>
        /// Event Listener for PSystemManager.Instance.OnPSystemReady. Adds our
        /// custom PQSMod to all PQSs found in PSystemManager once it has loaded and
        /// also loads any static mods from config files (bit of feature overlap with
        /// Kopernicus and KerbalKonstructs here).
        /// </summary>
        private void AddModsToPSystem() {
            ModLogger.Log("Adding Runtime Mod Injectors to bodies.");
            List<CelestialBody> systemCBodies = PSystemManager.Instance.localBodies;
            //Suns and Gas Giants don't have a PQS and cannot run mods
            foreach (CelestialBody cb in systemCBodies.Where(cb => cb.pqsController != null))
            {
                PQS pqsController = cb.pqsController;

                string modName = $"{cb.bodyName}_RuntimePQSModInjector";

                //dont go further if the mod has already been applied
                if (pqsController.transform.FindChild(modName))
                    break;

                //create a new gameObject as the child of the pqs
                //(like stock) and add our custom PQSMod component
                GameObject goMod = new GameObject(modName);
                goMod.transform.parent = pqsController.transform;

                //rikki don't lose that number, you don't wanna call nobody else
                ModInjectors[cb.bodyName] = goMod.AddComponent<RuntimePQSModInjector>();
                ModLogger.Log($"{modName} added to {cb.name}");
            }

            //Load any static mods defined in config files (can only handle PQSCity2s for now)
            LoadedMods = LoadModsFromConfig(StaticConfigs);

            //debug: check deets post-add. No scene so can't use DumpHeirarchy as that relies on a scene existing
            //DebugTools.DumpGameObjectDetails(cbKerbin.pqsController.transform.root.gameObject, -1);
        }


        /// <summary>
        /// Loads PQSMods from config nodes so that they can be used by the PSystem as if they were native.
        /// Each PQSMod type will require a factory that can create an instance with the correct links to 
        /// their PQSControllers and any other on-creation variables, but not call any of the Mod methods
        /// that are used by the engine in its update routine (OnSetup, OnSphereStart, etc) as they should
        /// be handled in the same way as native mods (i.e. not to be called by us fleshy meatbags in our code).
        /// </summary>
        /// <param name="staticConfigs">A list of PQSMod config nodes</param>
        /// <returns>The list of PQSMods that were successfully created</returns>
        private List<PQSMod> LoadModsFromConfig(List<ConfigNode> staticConfigs)
        {
            List<PQSMod> mods = new List<PQSMod>();
            foreach (ConfigNode staticConfig in staticConfigs.Where(s => s.HasData))
            {
                ModLogger.Log($"Found Config data: {staticConfig}");
                //We can only handle the one type for now
                if (staticConfig.HasNode("PQSCITY2"))
                    mods.Add(PQSCity2Factory.Create(staticConfig.GetNode("PQSCITY2")));

                //TODO: Handle other PQSMod Types
            }
            return mods;
        }
    }
}
