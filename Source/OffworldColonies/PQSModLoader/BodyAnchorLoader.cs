using System.Collections.Generic;
using System.Linq;
using PQSModLoader.Factories;
using UnityEngine;

namespace PQSModLoader {
    public static class Mod { public const string Name = "[OC BodyAnchorLoader]: "; }

    /// <summary>
    /// Loads a BodyAnchor for every compatible body 
    /// found so that other addons can add their own StaticModels at runtime
    /// by adding to the public Anchors Dictionary.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class BodyAnchorLoader : MonoBehaviour
    {
        public static BodyAnchorLoader Instance { get; private set; }

        public Dictionary<string, BodyAnchor> Anchors;

        public List<ConfigNode> StaticConfigs { get; private set; }
        //public List<BodyAnchor> LoadedAnchors { get; private set; }


        private void OnDestroy()
        {
            PSystemManager.Instance.OnPSystemReady.Remove(AddModsToPSystem);
        }

        /// <summary>
        /// This is the first code that runs in the entire mod (other than part loading). 
        /// If another part of the mod attempts to do things before this then it may 
        /// need to set up the logger instead.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            //don't die, we may need to do things in other scenes too
            DontDestroyOnLoad(this);

            //set up our logger with this mod's title
            StaticConfigs = new List<ConfigNode>(GameDatabase.Instance.GetConfigNodes("SURFACESTRUCTURE_BODYANCHOR"));
            Anchors = new Dictionary<string, BodyAnchor>();

            //Add our mod once the system prefab has loaded (allows for Kopernicus compatibility?)
            Debug.Log($"{Mod.Name}BodyAnchorLoader awake. Adding OnPSystemReady listener.");
            PSystemManager.Instance.OnPSystemReady.Add(AddModsToPSystem);
        }


        /// <summary>
        /// Event Listener for PSystemManager.Instance.OnPSystemReady. Adds our
        /// BodyAnchor to all PQSs found in PSystemManager once it has loaded and
        /// also loads any static mods from config files.
        /// </summary>
        private void AddModsToPSystem() {
            Debug.Log($"{Mod.Name}Adding SurfaceStructure BodyAnchors to bodies.");
            List<CelestialBody> systemCBodies = PSystemManager.Instance.localBodies;
            //Suns and Gas Giants don't have a PQS and cannot run mods
            foreach (CelestialBody cb in systemCBodies.Where(cb => cb.pqsController != null)) {
                string anchorName = $"{cb.bodyName}_BodyAnchor";
                //rikki don't lose that number, you don't wanna call nobody else
                Anchors.Add(cb.bodyName, BodyAnchor.Create(cb, anchorName));
                Debug.Log($"{Mod.Name}{anchorName} added to {cb.bodyName}");
            }

            //Load any anchors defined in config files (global for all saves, may be useful for multiple layers or something)
            //LoadAnchorsFromConfig(StaticConfigs);

            //debug: check deets post-add. No scene so can't use DumpHeirarchy as that relies on a scene existing
            //DebugTools.DumpGameObjectDetails(cbKerbin.pqsController.transform.root.gameObject, -1);
        }


        private void LoadAnchorsFromConfig(IEnumerable<ConfigNode> staticConfigs)
        {
            foreach (ConfigNode staticConfig in staticConfigs.Where(s => s.HasData)) {
                Debug.Log($"{Mod.Name}Found Config data: {staticConfig}");
                Anchors.Add(staticConfig.GetValue("BodyName"), BodyAnchor.Create(staticConfig));
            }
        }
    }
}
