using System.Collections.Generic;
using ModUtils;
using UnityEngine;

namespace OffworldColoniesPlugin.Debug {
    public class VesselUtils {

        public static Bounds GetVesselBounds(Vessel vessel) {
            Bounds vesselBounds = new Bounds();
            bool unassigned = false;
            foreach (global::Part part in vessel.Parts) {
                foreach (Transform child in part.transform) {
                    Bounds partBounds = new Bounds();
                    if (MeshBounds(child, ref partBounds)) {
                        unassigned = SafeEncapsulate(partBounds, ref vesselBounds, unassigned);
                    }
                }
            }
            //ColonyManager.Log("Vessel Bounds: " + vesselBounds.ToString());
            return vesselBounds;
        }


        private static bool MeshBounds(Transform obj, ref Bounds bnd) {
            bool hasBounds = false;

            //find all part renderers
            bool rBoundCreated = false;
            MeshRenderer[] partRenderers = obj.GetComponents<MeshRenderer>();
            foreach (MeshRenderer pr in partRenderers) {
                rBoundCreated = SafeEncapsulate(pr.bounds, ref bnd, rBoundCreated);
                hasBounds = true;
            }

            //recurse for each child object in this part as well
            bool cBoundCreated = false;
            foreach (Transform c in obj) {
                Bounds cb = new Bounds();
                if (MeshBounds(c, ref cb)) {
                    cBoundCreated = SafeEncapsulate(cb, ref bnd, cBoundCreated);
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        private static bool SafeEncapsulate(Bounds inBounds, ref Bounds bounds, bool boundCreated) {
            if (boundCreated)
                bounds.Encapsulate(inBounds);
            else
                bounds = inBounds;

            return true; //either way the bounds are now initialized
        }

        #region Visualisation Helpers

        //remember our defined helpers so we don't create spam
        private static Dictionary<string, VisualHelper> _bHelpers;

        public static void CreateHelperCubeAt(string helperId, Transform trans, Vector3 size, Color color, Vector3 centre = default(Vector3)) {
            if (_bHelpers == null)
                _bHelpers = new Dictionary<string, VisualHelper>();
            if (!_bHelpers.ContainsKey(helperId) ||
                (_bHelpers.ContainsKey(helperId) && _bHelpers[helperId] == null)) {
                _bHelpers[helperId] = VisualHelper.CreateHelper(trans, size, color, centre);
            }
            if (_bHelpers.ContainsKey(helperId)) {
                _bHelpers[helperId].Change(trans, size, color, centre);
            }
        }

        public static void CreateHelperBoundsAt(string helperId, Transform trans, Bounds bounds, Color color) {
            if (_bHelpers == null)
                _bHelpers = new Dictionary<string, VisualHelper>();
            if (!_bHelpers.ContainsKey(helperId) ||
                (_bHelpers.ContainsKey(helperId) && _bHelpers[helperId] == null)) {
                _bHelpers[helperId] = VisualHelper.CreateHelper(trans, bounds, color);
            }
            if (_bHelpers.ContainsKey(helperId)) {
                _bHelpers[helperId].Change(color, bounds);
            }
        }

        public static void CreateHelperSphereAt(string helperId, Vector3 pos, Color color, float size, Transform parent = null) {
            if (_bHelpers == null)
                _bHelpers = new Dictionary<string, VisualHelper>();

            if (!_bHelpers.ContainsKey(helperId) || 
                (_bHelpers.ContainsKey(helperId) && _bHelpers[helperId] == null)) {
                _bHelpers[helperId] = VisualHelper.CreateHelper(color, parent);
            }
            if (_bHelpers.ContainsKey(helperId)) {
                _bHelpers[helperId].Change(pos, size);
            }
        }
        #endregion
    }
}
