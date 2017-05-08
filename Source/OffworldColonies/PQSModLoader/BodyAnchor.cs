using System;
using System.Collections.Generic;
using System.Linq;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader {

    /// <summary>
    /// The PQS class has a private List of mods that it keeps and uses during its 
    /// internal upkeep. This leaves no access for modders to add their own PQSMods 
    /// at runtime that use the correct native calls for OnSphereStart(), OnSetup(), etc.
    /// 
    /// This class's purpose is to provide a single entry point for adding and removing 
    /// multi-LOD static models ala Kerbal Konstructs, but on a save-by-save basis 
    /// (this class will handle cleaning it's children up on save/load)
    /// </summary>
    public sealed class BodyAnchor: PQSMod
    {
        private LinkedList<SurfaceStructure> _surfaceStructures;
        private bool _onSetupHasRun;

        private void Awake()
        {
            _onSetupHasRun = false;

            order = Int32.MaxValue; //run this mod last always
            _surfaceStructures = new LinkedList<SurfaceStructure>(); //create our surfaceStructure list now
        }


        public override void OnSetup() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnSetup();

            //remember that we have already run OnSetup so that any structures
            //added later know to set themselves up
            _onSetupHasRun = true;
        }

        public override bool OnSphereStart() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnSphereStart();

            return false;
        }
        

        public override void OnPostSetup() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnPostSetup();
        }

        public override void OnSphereReset() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnSphereReset();
        }

        public override void OnSphereActive() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnSphereActive();
        }

        public override void OnSphereInactive() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnSphereInactive();
        }

        public override void OnUpdateFinished() {
            foreach (SurfaceStructure surfaceStructure in _surfaceStructures)
                surfaceStructure.OnUpdateFinished();
        }



        #region SurfaceStructure list accessors
        /// <summary>
        /// Adds a surfaceStructure to the list of runtime mods 
        /// </summary>
        /// <param name="surfaceStructure"></param>
        public void Add(SurfaceStructure surfaceStructure) {
            //don't add the surfaceStructure if it's turned off
            if (!surfaceStructure.gameObject.activeSelf)
                return;

            if (_surfaceStructures.Count <= 0)
                _surfaceStructures.AddFirst(surfaceStructure);
            else
                _surfaceStructures.AddLast(surfaceStructure);
            

            //if we haven't already completed setup then we're done, 
            //otherwise run OnSetup on the new SurfaceStructure now
            if (!_onSetupHasRun)
                return;

            surfaceStructure.OnSetup();

            //not sure when these are safe to use any time but inital investigation seems ok
            surfaceStructure.OnSphereStart();
            surfaceStructure.OnPostSetup(); //needed for PQSCity2 orientation at least, doesn't seem to be used by anthing else
        }

        public bool Remove(SurfaceStructure surfaceStructure) {
            return surfaceStructure != null && _surfaceStructures.Remove(surfaceStructure);
        }

        public bool Contains(SurfaceStructure surfaceStructure) {
            return _surfaceStructures.Find(surfaceStructure) != null;
        }

        //public SurfaceStructure GetStructure(SurfaceStructure surfaceStructure) {
        //    return _surfaceStructures.Find(surfaceStructure)?.Value;
        //}

        //public List<T> GetStructures<T>() where T : SurfaceStructure {
        //    return _surfaceStructures.OfType<T>().ToList();
        //}

        //public SurfaceStructure GetStructure(string structureName) {
        //    return _surfaceStructures.First(p => p.objectName == structureName);
        //}

        #endregion



        #region Factory Methods
        public static BodyAnchor Create(ConfigNode node) {
            string bodyName = node.GetValue("BodyName");
            string anchorName = node.GetValue("ModName");
            CelestialBody body = PSystemManager.Instance.localBodies.Find(p => p.name == bodyName);
            return Create(body, anchorName);
        }

        public static BodyAnchor Create(CelestialBody cb, string anchorName) {
            PQS pqsController = cb.pqsController;

            //don't process if the anchor already exists
            Transform existingAnchorTransform = pqsController.transform.FindChild(anchorName);
            if (existingAnchorTransform != null)
                return existingAnchorTransform.GetComponent<BodyAnchor>();

            //create a new gameObject as the child of the pqs
            //(like stock) and add our Anchor component
            GameObject goAnchor = new GameObject(anchorName);
            goAnchor.transform.SetParent(pqsController.transform, false);
            return goAnchor.AddComponent<BodyAnchor>();
        }
        #endregion
    }
}
