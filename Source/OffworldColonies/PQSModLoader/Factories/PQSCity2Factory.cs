using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModUtilities;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace PQSModLoader.Factories {
    public class PQSCity2Factory {
        /// <summary>
        /// Creates a new PQSCity2 from a ConfigNode definition.
        /// Only guaranteed to hook into the correct PQS methods if created on 
        /// PSystemStartup Event, otherwise will have to hook into the 
        /// RuntimePQSModInjector system to use these.
        /// </summary>
        /// <param name="fromNode">A node of type 'OC_PQSLOADER_STATIC_DEFINITION'</param>
        /// <returns>The instance that was created</returns>
        public static PQSCity2 Create(ConfigNode fromNode) {
            return Create(new PQSCity2Definition(fromNode));
        }

        /// <summary>
        /// Creates a new PQSCity2 from a PQSCity2Definition.
        /// Only guaranteed to hook into the correct PQS methods if created on 
        /// PSystemStartup Event, otherwise will have to hook into the 
        /// RuntimePQSModInjector system to use these.
        /// </summary>
        /// <param name="cityDefinition">A complete definition for a city (must have minimum definition)</param>
        /// <returns>The instance that was created</returns>
        public static PQSCity2 Create(PQSCity2Definition cityDefinition) {
            return Create(cityDefinition.LocationName, cityDefinition.Coordinates, cityDefinition.Rotation,
                cityDefinition.LODDefines.ToArray(), cityDefinition.UpVector, cityDefinition.SurfaceSnap,
                cityDefinition.SnapHeightOffset);
        }

        /// <summary>
        /// Creates a new PQSCity2 object at the given body surface surfacePosition. 
        /// 
        /// Only guaranteed to hook into the correct PQS methods if created on 
        /// PSystemStartup Event, otherwise will have to hook into the 
        /// RuntimePQSModInjector system to use these.
        /// </summary>
        /// <param name="cityName">The name of the city</param>
        /// <param name="surfacePosition">The body-relative surfacePosition for the city</param>
        /// <param name="rotation">The city's rotation around it's up axis</param>
        /// <param name="lodDefinitions">An array of LODDefines containing models at 
        /// different LODs</param>
        /// <param name="upVector">The city's local up vector</param>
        /// <param name="surfaceSnap">Flag for snapping to the body surface. If true then
        /// coordinate altitude is ignored</param>
        /// <param name="snapHeightOffset">How far from the surface to offset the city in
        /// snap mode. This + surfaceSnap replace coordinate altitude in snap mode</param>
        /// <returns>The instance that was created</returns>
        public static RuntimePQSCity2 Create(string cityName,
            BodySurfacePosition surfacePosition,
            double rotation = 0,
            LODDefinition[] lodDefinitions = null,
            Vector3 upVector = default(Vector3),
            bool surfaceSnap = false,
            double snapHeightOffset = 0) {
            //create a new GameObject to hold our PQSCity and set the PQS as our parent
            GameObject cityObject = new GameObject(cityName);
            cityObject.transform.parent = surfacePosition.Body.pqsController.transform;
            cityObject.transform.localPosition = Vector3.zero;
            ModLogger.Log($"Created new GameObject instance for'{cityName}' and set parent to PQSController '{cityObject.transform.parent.name}'");

            //and add our new city (actually use our derived type so we have some debugging messages as well)
            RuntimePQSCity2 newCity = cityObject.AddComponent<RuntimePQSCity2>();
            ModLogger.Log($"{cityName}: PQSCity2 Component added. Setting variables. ");
            ModLogger.Log($"Coordinates: Lat:{surfacePosition.Latitude} Lon:{surfacePosition.Longitude} Alt:{surfacePosition.Altitude}");
            newCity.objectName = cityName;
            newCity.sphere = surfacePosition.Body.pqsController;
            newCity.lat = surfacePosition.Latitude;
            newCity.lon = surfacePosition.Longitude;
            newCity.alt = surfacePosition.Altitude;

            ModLogger.Log($"Rotation: {rotation}");
            newCity.up = upVector == default(Vector3) ? Vector3.up : upVector;
            newCity.rotation = rotation;
            newCity.snapToSurface = surfaceSnap;
            newCity.snapHeightOffset = snapHeightOffset;
            newCity.modEnabled = true;

            //PQSCity2s have an array of LodObjects. LodObjects contain a list of
            //GameObjects which are only visible below a given distance.
            ModLogger.Log($"{cityName}: Setting up LODs");

            List<PQSCity2.LodObject> lods = new List<PQSCity2.LodObject>();

            if (lodDefinitions != null) {
                foreach (LODDefinition lod in lodDefinitions) {
                    ModLogger.Log($"{cityName}: Setting up for LOD at {lod.VisibleRange}m");
                    lods.Add(LODFactory.Create(lod, newCity.transform));
                }

                lods.Sort((p, q) => p.visibleRange.CompareTo(q.visibleRange));
            }
            else
                ModLogger.Log($"{cityName}: No LOD definitions found");
            newCity.objects = lods.ToArray();

            //sets up layers to match native PQSCity2
            ModLogger.Log($"{cityName}: Setting layers.");
            cityObject.SetLayerRecursive(LayerMask.NameToLayer("Local Scenery"));

            ModLogger.Log($"{cityName}: PQSCity2 creation complete. ");
            return newCity;
        }


        /// <summary>
        /// Sets the GameObject and all of its children's layer to the given value
        /// </summary>
        /// <param name="sGameObject">The GameObject to set the layer of</param>
        /// <param name="newLayerNumber">The layer to set the Gameobject to</param>
        //private static void SetObjectLayer(GameObject sGameObject, int newLayerNumber) {
        //    if (sGameObject.GetComponent<Collider>() == null)
        //        sGameObject.layer = newLayerNumber;
        //    else if (!sGameObject.GetComponent<Collider>().isTrigger)
        //        sGameObject.layer = newLayerNumber;

        //    foreach (Transform child in sGameObject.transform)
        //        SetObjectLayer(child.gameObject, newLayerNumber);
        //}

        /// <summary>
        /// Inserts a new model into the city.
        /// </summary>
        /// <param name="city">The city to add a model to</param>
        /// <param name="modelDefinition">The multi-LOD definition for the model</param>
        /// <param name="positionOffset">Local position offset from default value (config) for this model instance</param>
        /// <param name="rotationOffset">Local rotation offset from default value (config) for this model instance</param>
        /// <param name="scaleOffset">Local scale offset from default value (config) for this model instance</param>
        public static MultiLODObject AddMultiLODModelTo(PQSCity2 city, MultiLODModelDefinition modelDefinition,
            Vector3 positionOffset, Vector3 rotationOffset, float scaleOffset) {
            List<PQSCity2.LodObject> lods = new List<PQSCity2.LodObject>(city.objects);
            ModLogger.Log($"Adding Multi-LOD model to {city.name} from model definition");
            MultiLODObject multiLODModel = new MultiLODObject();

            //Add each LOD in the modelDefine
            foreach (LODDefinition modelLOD in modelDefinition.LODDefines) {
                //if we have found an existing LODObject that matches the one we are adding 
                //then merge model lists, otherwise just add the new LODObject wholesale
                int foundIndex = lods.FindIndex(l => l.visibleRange == modelLOD.VisibleRange);
                if (foundIndex != -1) {
                    ModLogger.Log($"-{city.name}: Adding model to LOD = {modelLOD.VisibleRange}m");
                    PQSCity2.LodObject matchingLOD = lods[foundIndex];
                    List<GameObject> existingModels = new List<GameObject>(matchingLOD.objects);

                    List<GameObject> newModels = (from m in modelLOD.Models
                                                  let pos = m.LocalPosition + positionOffset
                                                  let rot = m.LocalRotationEuler + rotationOffset
                                                  let scale = m.LocalScale + scaleOffset
                                                  select StaticModelFactory.Create(city.transform, m.ModelName, m.ModelPath, pos, rot, scale)).ToList();

                    //instantiate all of the model definitions in this LOD with the given 
                    //offsets and add them to the list of new models
                    multiLODModel.Add(new PQSCity2.LodObject { objects = newModels.ToArray(), visibleRange = matchingLOD.visibleRange });
                    //add the new model instances to the exisiting list
                    existingModels.AddRange(newModels);
                    matchingLOD.objects = existingModels.ToArray();
                }
                else {
                    ModLogger.Log($"-{city.name}: Adding new LODObject for LOD = {modelLOD.VisibleRange}m");
                    PQSCity2.LodObject lodObject = LODFactory.Create(modelLOD, city.transform, positionOffset, rotationOffset, scaleOffset);
                    multiLODModel.Add(new PQSCity2.LodObject { objects = lodObject.objects, visibleRange = lodObject.visibleRange });
                    lods.Add(lodObject);
                }
            }
            multiLODModel.Sort((p, q) => p.visibleRange.CompareTo(q.visibleRange));

            //sort the combined LODObject list and replace the existing lodObjects array in the PQSCity2
            lods.Sort((p, q) => p.visibleRange.CompareTo(q.visibleRange));
            city.objects = lods.ToArray();

            ModLogger.Log($"{city.name}: Setting layers.");
            city.gameObject.SetLayerRecursive(LayerMask.NameToLayer("Local Scenery"));

            return multiLODModel;
        }
    }

    public class MultiLODObject : List<PQSCity2.LodObject> {}
}
