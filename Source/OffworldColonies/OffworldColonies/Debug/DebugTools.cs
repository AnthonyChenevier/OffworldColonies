using System;
using System.Collections.Generic;
using System.IO;
using ModUtilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OffworldColonies.Debug {
    /// <summary>
    ///     A toolkit full of
    /// </summary>
    public class DebugTools {
        #region Visualisation Helpers

        //remember our defined helpers so we don't create spam
        private static Dictionary<string, VisualHelper> _bHelpers;

        public static void CreateHelperCubeAt(string helperId, Transform parent, Vector3 size, Color color, Vector3 posOffset) {
            if (_bHelpers == null)
                _bHelpers = new Dictionary<string, VisualHelper>();
            if (!_bHelpers.ContainsKey(helperId) ||
                (_bHelpers.ContainsKey(helperId) && (_bHelpers[helperId] == null)))
                _bHelpers[helperId] = VisualHelper.CreateHelper(parent, size, color, posOffset);
            if (_bHelpers.ContainsKey(helperId))
                _bHelpers[helperId].Change(parent, size, color, posOffset);
        }

        public static void CreateHelperBoundsAt(string helperId, Transform parent, Bounds bounds, Color color) {
            if (_bHelpers == null)
                _bHelpers = new Dictionary<string, VisualHelper>();
            if (!_bHelpers.ContainsKey(helperId) ||
                (_bHelpers.ContainsKey(helperId) && (_bHelpers[helperId] == null)))
                _bHelpers[helperId] = VisualHelper.CreateHelper(parent, bounds, color);
            if (_bHelpers.ContainsKey(helperId))
                _bHelpers[helperId].Change(parent, bounds, color);
        }

        public static void CreateHelperSphereAt(string helperId, Transform parent, float radius, Color color, Vector3 posOffset) {
            if (_bHelpers == null)
                _bHelpers = new Dictionary<string, VisualHelper>();

            if (!_bHelpers.ContainsKey(helperId) ||
                (_bHelpers.ContainsKey(helperId) && (_bHelpers[helperId] == null)))
                _bHelpers[helperId] = VisualHelper.CreateHelper(parent, radius, color, posOffset);
            if (_bHelpers.ContainsKey(helperId))
                _bHelpers[helperId].Change(parent, radius, color, posOffset);
        }

        #endregion

        public static void DumpLayerMatrix() {
            ModLogger.Log("Getting Collision Matrix");
            string colTitles = ",";
            string[] matrix = new string[32];
            //32 Layers in Unity. Go through them all and get their collidability with every other layer
            for (int rowLayerID = 0; rowLayerID < 32; rowLayerID++) {
                string rowLayerName = LayerMask.LayerToName(rowLayerID);
                matrix[rowLayerID] = $"{rowLayerName},";
                for (int colLayerID = 31; colLayerID >= rowLayerID; colLayerID--) {
                    if (rowLayerID == 0) {
                        string colLayerName = LayerMask.LayerToName(colLayerID);
                        colTitles += $"{colLayerName},";
                    }

                    matrix[rowLayerID] += Physics.GetIgnoreLayerCollision(rowLayerID, colLayerID)? "O,": "X,";
                }
            }
            ModLogger.Log(colTitles);
            foreach (string row in matrix)
                ModLogger.Log(row);
            ModLogger.Log("Collision matrix dump complete");
        }


        /// <summary>
        ///     Dumps the given GameObject data, including name, active status, attached components and optionally all child object
        ///     as well
        /// </summary>
        /// <param name="go"></param>
        /// <param name="childDepth">Number of levels to recurse child GameObject dumping. -1 = infinite</param>
        /// <param name="depth"></param>
        public static void DumpGameObjectDetails(GameObject go, int childDepth, string depth = "") {
            ModLogger.Log($"{depth}GameObject(\"{go.name}\")({go.activeSelf}) layer:{LayerMask.LayerToName(go.layer)} {{");
            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components) {
                Type componentType = component.GetType();
                string componentHeader = $"{depth}-Component(\"{component.name}\"):{componentType}";

                //ADD ANY COMPONENT TYPES YOU WANT TO HANDLE SPECIALLY HERE
                if ((componentType == typeof(PQS)) || (componentType == typeof(PQSCity2)))
                    componentHeader += " Data {";

                ModLogger.Log(componentHeader);

                //PUT THE COMPONENT CHECKS HERE IN AN IF STATEMENT
                if (componentType == typeof(PQS)) {
                    PQS pqs = (PQS) component;
                    ModLogger.Log($"{depth}--\"PQS.surfaceMaterial = {pqs.surfaceMaterial}\"");
                }
                if (componentType == typeof(PQSCity2)) {
                    PQSCity2 pqsCity2 = (PQSCity2) component;
                    ModLogger.Log(
                        $"{depth}--\"PQSCity2.enabled ? {pqsCity2.enabled}. isActiveAndEnabled {pqsCity2.isActiveAndEnabled}. modEnabled {pqsCity2.modEnabled}.\"");
                }

                //ADD ANY COMPONENT TYPES YOU WANT TO HANDLE SPECIALLY HERE (AGAIN)
                if ((componentType == typeof(PQS)) || (componentType == typeof(PQSCity2)))
                    ModLogger.Log($"{depth}-}}");
            }

            if (go.transform.childCount == 0) {
                ModLogger.Log($"{depth}-Children: None");
                ModLogger.Log($"{depth}}}");
                return;
            }

            if ((childDepth > 0) || (childDepth == -1)) {
                ModLogger.Log($"{depth}-Children {{");
                Transform goTransform = go.transform;
                foreach (Transform tr in goTransform) {
                    int recursionsRemaining = childDepth > 0 ? childDepth - 1 : childDepth;
                    DumpGameObjectDetails(tr.gameObject, recursionsRemaining, depth + "--");
                }
                ModLogger.Log($"{depth}-}}");
            }
            else {
                ModLogger.Log($"{depth}-Children {{");
                Transform goTransform = go.transform;
                foreach (Transform tr in goTransform)
                    ModLogger.Log($"{depth}-GameObject(\"{tr.gameObject.name}\")({tr.gameObject.activeSelf});");
                ModLogger.Log($"{depth}-}}");
            }
            ModLogger.Log($"{depth}}}");
        }


        public static void DumpHeirarchy() {
            string oldTitle = ModLogger.Title;
            ModLogger.Title = oldTitle + "(SCENE DUMP)";
            ModLogger.Log("Dumping Scene Heirarchy");
            GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootGameObject in rootGameObjects)
                DumpGameObjectDetails(rootGameObject, -1);
            ModLogger.Log("Scene Dump Complete");
            ModLogger.Title = oldTitle;
        }

        public static void TestMaterialColour(Vessel vessel) {
            //Vector2 pos = Input.mousePosition; // Mouse position
            RaycastHit hit;
            //Camera cam = FlightCamera.fetch.mainCamera; // Camera to use for raycasting
            //Ray ray = cam.ScreenPointToRay(pos);
            Physics.Raycast(vessel.transform.position, -vessel.upAxis, out hit, 10000.0f,
                LayerMask.GetMask("Local Scenery"));
            if (hit.collider) {
                GameObject hitObject = hit.collider.gameObject;
                DumpGameObjectDetails(hitObject, 0);
                ModLogger.Log(
                    $"{hitObject.name} hit. Layer = {LayerMask.LayerToName(hitObject.layer)}({hitObject.layer})");

                Renderer renderer = hitObject.GetComponent<PQ>().meshRenderer;
                if (renderer == null)
                    return;

                // Get texture of object selected
                Material mat = renderer.materials[0];
                Color c = GetColorFromMaterialAt(mat, hit.textureCoord);
                ModLogger.Log($"Colour found at vessel position: {c}", c);
            }
        }

        private static Color GetColorFromMaterialAt(Material mat, Vector2 coord) {
            // Create a temporary RenderTexture of the same size as the unreadable texture
            RenderTexture tmp = RenderTexture.GetTemporary(mat.mainTexture.width, mat.mainTexture.height, 0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(mat.mainTexture, tmp, mat);
            // Backup the currently set RenderTexture (which is probably doing something - A)
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(tmp.width, tmp.height);
            // Copy the pixels from the (now active tmp -A) RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
            File.WriteAllBytes(Application.dataPath + "/../SavedScreen.png", myTexture2D.EncodeToPNG());
            // "myTexture2D" now has the same pixels from "texture" and it's readable.

            return myTexture2D.GetPixelBilinear(coord.x, coord.y);
        }

        public static void TestMisc() {
            //Vessel activeVessel = FlightGlobals.ActiveVessel;
            //CelestialBody cBody = activeVessel.mainBody;

            //double avLati = activeVessel.latitude;
            //double avLongi = activeVessel.longitude;
            //double avAlti = activeVessel.altitude;// - activeVessel.heightFromTerrain;

            //OCLogger.Log($"[P] activeVessel.GetWorldPos3D(): {activeVessel.GetWorldPos3D()}");
            //OCLogger.Log($"[P] (Vector3d)activeVessel.vesselTransform.position: {(Vector3d)activeVessel.vesselTransform.position}");
            //OCLogger.Log($"[P] activeVessel.mainBody: {cBody.bodyName}. Lat: {avLati}. Lon: {avLongi}. Alt: {avAlti}");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            //OCLogger.Log($"[P] CB_'{cBody.bodyName}'.GetRelSurfacePosition(activeVessel.GetWorldPos3D()): {cBody.GetRelSurfacePosition(activeVessel.GetWorldPos3D())}");
            //OCLogger.Log($"[P] CB_'{cBody.bodyName}'.GetRelSurfacePosition((Vector3d)activeVessel.vesselTransform.position): {cBody.GetRelSurfacePosition((Vector3d)activeVessel.vesselTransform.position)}");
            //OCLogger.Log($"[P] CB_'{cBody.bodyName}'.GetRelSurfacePosition(lati, longi, alti): {cBody.GetRelSurfacePosition(avLati, avLongi, avAlti)}");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            //OCLogger.Log($"[P] CB_'{cBody.bodyName}'.GetWorldSurfacePosition(lati, longi, alti): {cBody.GetWorldSurfacePosition(avLati, avLongi, avAlti)}");

            //OCLogger.Log($"[P] CB_'{cBody.bodyName}'.GetSurfaceNVector(lati, longi): {cBody.GetSurfaceNVector(avLati, avLongi)}");
            //OCLogger.Log($"[P] CB_'{cBody.bodyName}'.GetRelSurfaceNVector(lati, longi): {cBody.GetRelSurfaceNVector(avLati, avLongi)}");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");


            ////testing that changing different properties results in the same position
            //SurfaceBodyCoordinates bCoords1 = new SurfaceBodyCoordinates(activeVessel.vesselTransform.position, cBody);
            //OCLogger.Log($"[C0] Radial (PQSCity) Coordinates: {bCoords1.RadialPosition} ");
            //OCLogger.Log($"[C0] Body: {bCoords1.Body.bodyName}. Lat: { bCoords1.Latitude}. Lon: { bCoords1.Longitude }. Alt: { bCoords1.Altitude }");
            //OCLogger.Log($"[C0] World Position: {bCoords1.WorldPosition} ");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            //SurfaceBodyCoordinates bCoords = new SurfaceBodyCoordinates(avLati, avLongi, avAlti, cBody);
            //OCLogger.Log($"[C1] Radial (PQSCity) Coordinates: {bCoords.RadialPosition} ");
            //OCLogger.Log($"[C1] Body: {bCoords.Body.bodyName}. Lat: { bCoords.Latitude }. Lon: { bCoords.Longitude }. Alt: { bCoords.Altitude }");
            //OCLogger.Log($"[C1] World Position: {bCoords.WorldPosition} ");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            //SurfaceBodyCoordinates bCoords2 = new SurfaceBodyCoordinates(cBody.GetSurfaceNVector(avLati, avLongi), avAlti, cBody);
            //OCLogger.Log($"[C2] Radial (PQSCity) Coordinates: {bCoords2.RadialPosition} ");
            //OCLogger.Log($"[C2] Body: {bCoords2.Body.bodyName}. Lat: { bCoords2.Latitude }. Lon: { bCoords2.Longitude }. Alt: { bCoords2.Altitude }");
            //OCLogger.Log($"[C2] World Position: {bCoords2.WorldPosition} ");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

            //Bounds vBounds = activeVessel.GetVesselBounds();
            //OCLogger.Log($"[B] Vessel Bounds Centre: {vBounds.center} Min: {vBounds.min} Max: {vBounds.max} Size: {vBounds.size}");
            //OCLogger.Log($"++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            //OCLogger.Log($"Kerbin world position: {cBody.position}");
            //PQSSurfaceObject[] cities = cBody.GetComponentsInChildren<PQSSurfaceObject>();

            //foreach (PQSSurfaceObject pqsSurfaceObject in cities) {
            //    OCLogger.Log($"{ pqsSurfaceObject.GetType()}:{pqsSurfaceObject.SurfaceObjectName}. World Position: {pqsSurfaceObject.transform.position}. CB Local Position: {pqsSurfaceObject.PlanetRelativePosition}");
            //    if (pqsSurfaceObject.GetType() == typeof(PQSCity)) {
            //        PQSCity city = (PQSCity)pqsSurfaceObject;
            //        OCLogger.Log($"--repositionRadial: {city.repositionRadial}");
            //        OCLogger.Log($"--repositionRadiusOffset: {city.repositionRadiusOffset}");
            //    }
            //    //else if (pqsSurfaceObject.GetType() == typeof(PQSCity2)) {
            //    //    PQSCity2 city2 = (PQSCity2)pqsSurfaceObject;
            //    //    city2.
            //    //}
            //    //else
            //    //    Logger.Instance.Log($"--{pqsSurfaceObject.GetType()} is not of type PQSCity or PQSCity2");
            //}

            //TypeUtilities.CreateHelperBoundsAt("vesselBounds", activeVessel.transform, vBounds, XKCDColors.AlgaeGreen);
            //TypeUtilities.CreateHelperSphereAt("VesselExclusion", vBounds.center, new Color(255,0,0,10), vBounds.size.magnitude, activeVessel.transform);

            //TypeUtilities.CreateHelperSphereAt("PartUp",
            //    transform.position + (transform.up*4), XKCDColors.GrassyGreen, .5f, transform);
            //TypeUtilities.CreateHelperSphereAt("PartForward",
            //    transform.position + (transform.forward*4), XKCDColors.GreenApple, .5f, transform);
            //TypeUtilities.CreateHelperSphereAt("PartRight",
            //    transform.position + (transform.right*4), XKCDColors.GreenBlue, .5f, transform);


            //activeVessel.mainBody.GetLatitude;
            //activeVessel.mainBody.GetLongitude;
            //activeVessel.mainBody.GetRelSurfaceNVector;
            //activeVessel.mainBody.GetSurfaceNVector;
            //activeVessel.mainBody.Radius;
            //create a base at the correct position
        }

        public static void ColliderDump(Vector3 position, float radius) {
            Collider[] colliders = Physics.OverlapSphere(position, radius);
            foreach (Collider collider in colliders) {
                DumpGameObjectDetails(collider.gameObject, -1);
            }
        }
    }
}