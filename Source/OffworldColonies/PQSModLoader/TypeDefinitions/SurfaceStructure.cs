using System;
using System.Collections.Generic;
using System.Linq;
using PQSModLoader.Factories;
using UnityEngine;

namespace PQSModLoader.TypeDefinitions {

    /// <summary>
    /// Basically a copy of PQSCity2 without the inheritance from PQSMod.
    /// This means it isn't picked up by PQS checking for and saving child
    /// mods in an unaccessable list thereby screwing up my abilty to remove
    /// structures from the persistent PSystem without leaving null references. 
    /// All SurfaceStructure instances are handled by the single *static* 
    /// BodyAnchor PQSMod (per body), added on PSystem Load.
    /// </summary>
    public class SurfaceStructure : MonoBehaviour {
        public BodyAnchor BodyAnchor;


        public PQSCity2.LodObject[] objects;
        public string objectName;
        public double lat;
        public double lon;
        public double alt;
        public double rotation;
        public bool snapToSurface;
        public Vector3 up;
        public double snapHeightOffset;

        void OnDestroy() {
            foreach (PQSCity2.LodObject o in objects) {
                int objCount = o.objects.Length; 
                while (--objCount >= 0) Destroy(o.objects[objCount]);
            }

            if (BodyAnchor.Contains(this))
                BodyAnchor.Remove(this);
        }

        public void Refresh() {
            OnSetup();
            OnSphereStart();
            Orientate();
            if (BodyAnchor.sphere.isActive)
                OnSphereActive();
        }

        public void Orientate() {
            Planetarium.CelestialFrame cf = new Planetarium.CelestialFrame();
            Planetarium.CelestialFrame.SetFrame(0.0, 0.0, 0.0, ref cf);
            Vector3d surfaceNvector = LatLon.GetSurfaceNVector(cf, lat, lon);
            if (snapToSurface && BodyAnchor.sphere.isStarted) {
                double snapAlt = BodyAnchor.sphere.GetSurfaceHeight(surfaceNvector) + snapHeightOffset;
                alt = snapAlt - BodyAnchor.sphere.radius;
                transform.localPosition = surfaceNvector * snapAlt;
            }
            else transform.localPosition = surfaceNvector * (BodyAnchor.sphere.radius + alt);

            transform.localRotation = Quaternion.FromToRotation(up, surfaceNvector) *
                                      Quaternion.AngleAxis((float)rotation, Vector3.up);
        }

        public void OnUpdateFinished() {
            if (BodyAnchor.sphere.target != null) {
                float distSq = Vector3.SqrMagnitude(BodyAnchor.sphere.target.transform.position - transform.position);
                int index = objects.Length;
                while (index-- > 0)
                    objects[index].SetActive(distSq < (double)objects[index].visibleRangeSqr);
            }
            else SetInactive();
        }


        public void SetInactive() {
            int i = objects.Length;
            while (i-- > 0)
                objects[i].SetActive(false);
        }

        public void Reset() { up = Vector3.up; }

        private void Start() { Orientate(); }
        
        public void OnSetup() {
            int length = objects.Length;
            while (length-- > 0) objects[length].Setup();

            SetInactive();
        }

        public bool OnSphereStart() {
            if (!BodyAnchor.sphere.isAlive) SetInactive();
            return false;
        }

        public void OnPostSetup() { Orientate(); }

        public void OnSphereReset() { SetInactive(); }

        public void OnSphereActive() { OnUpdateFinished(); }

        public void OnSphereInactive() { SetInactive(); }


        #region Factory Methods
        /// <summary>
        /// Creates a new PQSCity2 from a ConfigNode definition.
        /// Only guaranteed to hook into the correct PQS methods if created on 
        /// PSystemStartup Event, otherwise will have to hook into the 
        /// BodyAnchor system to use these.
        /// </summary>
        /// <param name="fromNode">A node of type 'OC_PQSLOADER_STATIC_DEFINITION'</param>
        /// <returns>The instance that was created</returns>
        public static SurfaceStructure Create(ConfigNode fromNode) {
            SurfaceStructureDefinition cityDefinition = new SurfaceStructureDefinition();
            cityDefinition.Load(fromNode);

            return Create(cityDefinition);
        }

        /// <summary>
        /// Creates a new PQSCity2 from a SurfaceStructureDefinition.
        /// Only guaranteed to hook into the correct PQS methods if created on 
        /// PSystemStartup Event, otherwise will have to hook into the 
        /// BodyAnchor system to use these.
        /// </summary>
        /// <param name="cityDefinition">A complete definition for a structure (must have minimum definition)</param>
        /// <returns>The instance that was created</returns>
        public static SurfaceStructure Create(SurfaceStructureDefinition cityDefinition) {
            return Create(cityDefinition.LocationName, cityDefinition.Coordinates, cityDefinition.Rotation,
                          cityDefinition.LODDefines, cityDefinition.UpVector, cityDefinition.SurfaceSnap,
                          cityDefinition.SnapHeightOffset);
        }

        /// <summary>
        /// Creates a new PQSCity2 object at the given body surface surfacePosition. 
        /// 
        /// Only guaranteed to hook into the correct PQS methods if created on 
        /// PSystemStartup Event, otherwise will have to hook into the 
        /// BodyAnchor system to use these.
        /// </summary>
        /// <param name="cityName">The name of the structure</param>
        /// <param name="surfacePosition">The body-relative surfacePosition for the structure</param>
        /// <param name="rotation">The structure's rotation around it's up axis</param>
        /// <param name="lodModelDefinitions">An array of LODDefines containing models at 
        /// different LODs</param>
        /// <param name="upVector">The structure's local up vector</param>
        /// <param name="surfaceSnap">Flag for snapping to the body surface. If true then
        /// coordinate altitude is ignored</param>
        /// <param name="snapHeightOffset">How far from the surface to offset the structure in
        /// snap mode. This + surfaceSnap replace coordinate altitude in snap mode</param>
        /// <returns>The instance that was created</returns>
        public static SurfaceStructure Create(string cityName,
                                              BodySurfacePosition surfacePosition,
                                              double rotation = 0,
                                              List<MultiLODModelDefinition> lodModelDefinitions = null,
                                              Vector3 upVector = default(Vector3),
                                              bool surfaceSnap = false,
                                              double snapHeightOffset = 0) {

            SurfaceStructure newStructure = new GameObject(cityName).AddComponent<SurfaceStructure>();
            newStructure.BodyAnchor = BodyAnchorLoader.Instance.Anchors[surfacePosition.BodyName];
            newStructure.transform.SetParent(newStructure.BodyAnchor.transform, false);
            Debug.Log($"{Mod.Name}Created new GameObject instance for'{cityName}' and set parent to {surfacePosition.BodyName} anchor. Setting variables. ");
            newStructure.objectName = cityName;

            Debug.Log($"{Mod.Name}Coordinates: Lat:{surfacePosition.Latitude} Lon:{surfacePosition.Longitude} Alt:{surfacePosition.Altitude}");
            newStructure.lat = surfacePosition.Latitude;
            newStructure.lon = surfacePosition.Longitude;
            newStructure.alt = surfacePosition.Altitude;
        
            Debug.Log($"{Mod.Name}Rotation: {rotation}");
            newStructure.up = upVector == default(Vector3) ? Vector3.up : upVector;
            newStructure.rotation = rotation;
            newStructure.snapToSurface = surfaceSnap;
            newStructure.snapHeightOffset = snapHeightOffset;

            //PQSCity2s have an array of LodObjects. LodObjects contain a list of
            //GameObjects which are only visible below a given distance.
            Debug.Log($"{Mod.Name}{cityName}: Setting up LODs");

            List<PQSCity2.LodObject> lods = new List<PQSCity2.LodObject>();

            if (lodModelDefinitions != null) {
                foreach (MultiLODModelDefinition lod in lodModelDefinitions) {
                    foreach (LODModelDefinition define in lod.LODDefines) {
                        Debug.Log($"{Mod.Name}{cityName}: Setting up for LOD at {define.VisibleRange}m");
                        lods.Add(LODModelFactory.Create(define, newStructure.transform));
                    }
                }

                lods.Sort((p, q) => p.visibleRange.CompareTo(q.visibleRange));
            }
            else
                Debug.Log($"{Mod.Name}{cityName}: No LOD definitions found");
            newStructure.objects = lods.ToArray();

            //sets up layers to match native PQSCity2
            Debug.Log($"{Mod.Name}{cityName}: Setting layers.");
            newStructure.gameObject.SetLayerRecursive(LayerMask.NameToLayer("Local Scenery"));

            Debug.Log($"{Mod.Name}{cityName}: SurfaceStructure creation complete. ");
            return newStructure;
        }
        #endregion

        /// <summary>
        /// Inserts a new model into the structure.
        /// </summary>
        /// <param name="modelDefinition">The multi-LOD definition for the model</param>
        /// <param name="positionOffset">Local position offset from default value (config) for this model instance</param>
        /// <param name="rotationOffset">Local rotation offset from default value (config) for this model instance</param>
        /// <param name="scaleOffset">Local scale offset from default value (config) for this model instance</param>
        /// <returns>The Multi-LOD model (a list of LODModels)</returns>
        public MultiLODModel AddModelTo(MultiLODModelDefinition modelDefinition, Vector3 positionOffset, Vector3 rotationOffset, float scaleOffset) {
            Debug.Log($"{Mod.Name}Adding Multi-LOD model to {objectName} from model definition");

            MultiLODModel multiLODModel = new MultiLODModel();

            //use a list to get access to find index
            List<PQSCity2.LodObject> lodObjects = objects.ToList();

            //Add each LOD in the modelDefinition
            foreach (LODModelDefinition modelLOD in modelDefinition.LODDefines) {
                //if we have found an existing LODObject that matches the one we are adding 
                //then merge model lists, otherwise just add the new LODObject wholesale
                //int foundIndex = lodObjects.FindIndex(l => l.visibleRange == modelLOD.VisibleRange);
                //if (foundIndex >= 0) {
                //    Debug.Log($"{Mod.Name}{objectName}: Adding model to existing LODObject for LOD = {modelLOD.VisibleRange}m");
                //    PQSCity2.LodObject exisitingLodObject = lodObjects[foundIndex];
                //    //get all of the exisiting models
                //    List<GameObject> existingModels = new List<GameObject>(exisitingLodObject.objects);

                //    //instantiate all of the new models and add them to the exisiting model list
                //    PQSCity2.LodObject newLodObject = LODModelFactory.Create(modelLOD, transform, positionOffset, rotationOffset, scaleOffset);
                //    existingModels.AddRange(newLodObject.objects);

                //    //overwrite with the merged list
                //    exisitingLodObject.objects = existingModels.ToArray();
                    
                //    multiLODModel.Add(newLodObject);
                //}
                //else {
                    Debug.Log($"{Mod.Name}{objectName}: Adding new LODObject and models for LOD = {modelLOD.VisibleRange}m");
                    PQSCity2.LodObject lodObject = LODModelFactory.Create(modelLOD, transform, positionOffset, rotationOffset, scaleOffset);
                    lodObjects.Add(lodObject);

                    multiLODModel.Add(lodObject);
                //}
            }
            multiLODModel.Sort((p, q) => p.visibleRange.CompareTo(q.visibleRange));

            //sort the combined LODObject list and replace the existing lodObjects array in the PQSCity2
            lodObjects.Sort((p, q) => p.visibleRange.CompareTo(q.visibleRange));
            objects = lodObjects.ToArray();

            Debug.Log($"{Mod.Name}{name}: Setting layers.");
            gameObject.SetLayerRecursive(LayerMask.NameToLayer("Local Scenery"));

            return multiLODModel;
        }
    }
    /// <summary>
    /// Encapsulates a list of LodObjects that represents a single, multi-LOD model
    /// </summary>
    public class MultiLODModel : List<PQSCity2.LodObject> { }
}