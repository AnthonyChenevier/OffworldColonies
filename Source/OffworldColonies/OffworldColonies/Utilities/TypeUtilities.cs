using System.Collections.Generic;
using UnityEngine;

namespace OffworldColonies.Utilities {
    /// <summary>
    /// Provides add-on functions for exisiting types
    /// </summary>
    public static class TypeUtilities {

        /// <summary>
        /// Extension Method for bounds. There is no function in Bounds to 
        /// get its corners (other than min/max which only gives us 2 of the
        /// 8 values needed). This remedies that.
        /// </summary>
        /// <remarks>Data may not be in the best order, should follow mesh conventions 
        /// so a mesh can be directly constructed from the box data if required</remarks>
        /// <param name="bounds">The bounds to get the corners of</param>
        /// <returns>An 8-element List of Vector3s for each corner. The first 4 
        /// elements are the positive Z square in clockwise order, and the last 4
        /// are the same for the negative Z square</returns>
        public static List<Vector3> Corners(this Bounds bounds) {
            Vector3 c = bounds.center;
            float x = bounds.extents.x;
            float y = bounds.extents.y;
            float z = bounds.extents.z;
            return new List<Vector3> {
                c + new Vector3( x,  y,  z),
                c + new Vector3( x,  y, -z),
                c + new Vector3(-x,  y,  z),
                c + new Vector3(-x,  y, -z),
                c + new Vector3( x, -y,  z),
                c + new Vector3( x, -y, -z),
                c + new Vector3(-x, -y,  z),
                c + new Vector3(-x, -y, -z)
            };
        }

        /// <summary>
        /// Extension method for Vessel. Returns a volume that encapsulates
        /// the entire vessel mesh in the vessel's local coordinates.
        /// </summary>
        /// <param name="vessel">The vessel to get the bounds of</param>
        /// <returns>An AABB Bounds in the vessel's local space</returns>
        public static Bounds GetVesselBounds(this Vessel vessel) {
            Bounds vesselBounds = new Bounds();
            foreach (global::Part part in vessel.Parts) {
                GameObject partObject = part.gameObject;
                Bounds partBounds = partObject.MeshBounds();

                //gets part bound corners (in part local space), convert each
                //point into vessel local space (p.s.>w.s.>v.s.) and encapsulate it
                foreach (Vector3 corner in partBounds.Corners())
                    vesselBounds.Encapsulate(vessel.vesselTransform.InverseTransformPoint(partObject.transform.TransformPoint(corner)));
            }

            return vesselBounds;
        }

        /// <summary>
        /// Extension method for GameObject. Gets a bounding volume 
        /// for the game object and all of its children in the object's
        /// local space. Only checks objects with a MeshFilter Component
        /// so children rendered with a different method will be ignored.
        /// </summary>
        /// <param name="o">The object to get the bounds of</param>
        /// <returns>An AABB Bounds in the object's local space</returns>
        public static Bounds MeshBounds(this GameObject o) {
            Bounds bounds = new Bounds();
            Transform oTransform = o.transform;
            //get a flattened list of all children (strip heirarchy). 
            //Use GetComponentsInChildren instead of go.transform to 
            //include self as well
            Transform[] transforms = o.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms) {
                MeshFilter meshFilter = t.GetComponent<MeshFilter>();
                //Don't process if we don't have a mesh filter
                if (meshFilter == null)
                    continue;

                //gets child bound corners (in child local space), convert each
                //point into parent local space (c.s.>w.s.>p.s.) and encapsulate it
                foreach (Vector3 corner in meshFilter.mesh.bounds.Corners())
                    bounds.Encapsulate(oTransform.InverseTransformPoint(t.TransformPoint(corner)));
            }
            return bounds;
        }
    }
}