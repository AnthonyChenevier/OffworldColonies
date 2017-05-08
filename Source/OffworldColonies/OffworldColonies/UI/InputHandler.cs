using System;
using System.Linq;
using UnityEngine;

namespace OffworldColonies.UI {
    public static class InputHandler {

        public static bool AcceptPlacementInputDown() { return Input.GetMouseButtonDown(0); }
        public static bool AcceptPlacementInputUp() { return Input.GetMouseButtonUp(0); }

        public static bool CancelPlacementInputDown() { return Input.GetMouseButtonDown(1); }
        public static bool CancelPlacementInputUp() { return Input.GetMouseButtonUp(1); }


        public static bool RotationInputUp() {
            return Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl);
        }

        public static bool RotationInputDown() {
            return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        }

        public static bool ElevationInputUp() {
            return Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift);
        }

        public static bool ElevationInputDown() {
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }


        /// <summary>
        /// Gets the planetary surface coordinate position of the mouse. 
        /// Can detect or ignore oceans as required. 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="ignoreOcean">Do we want to ignore the ocean (if one exists)?</param>
        /// <param name="worldHitPoint">output for the mouse position if one is found. If the 
        /// function returns false then this will be Vector3.zero</param>
        /// <returns>True if the mouse is over the ground and a position is 
        /// found, false otherwise.</returns>
        public static bool MouseSurfacePosition(CelestialBody body, bool ignoreOcean, out Vector3 worldHitPoint) {
            Camera cam = FlightCamera.fetch.mainCamera;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            //sea level detection could be handled multiple ways. I've elected to use a more intensive method that should
            //handle the edge case where there may be no local PQs to hit (an ocean world with no land PQS/a tiny physical
            //body with a massive ocean where the ray may end before hitting something) and very oblique camera angles where
            //the ocean is only skimmed
            Vector3 oceanHitPosition = new Vector3();
            bool hitOcean = false;
            if (!ignoreOcean && body.ocean) {
                Vector3d ssRayOrigin = ScaledSpace.LocalToScaledSpace(ray.origin);
                Vector3d ssRayDir = ScaledSpace.LocalToScaledSpace(ray.origin + ray.direction) - ssRayOrigin;
                //get the intersection of this ray and the body in scaled space
                RaycastHit hit;
                if (Physics.Raycast(new Ray(ssRayOrigin, ssRayDir), out hit, 10000f, LayerMask.GetMask("Scaled Scenery"))) {
                    oceanHitPosition = ScaledSpace.ScaledToLocalSpace(hit.point);
                    hitOcean = true;
                }
            }
            Vector3 groundHitPoint = new Vector3();
            bool hitGround = false;
            RaycastHit[] hits = Physics.RaycastAll(ray, 10000.0f, LayerMask.GetMask("Local Scenery"));
            Func<RaycastHit, bool> hasPQ = h => h.collider.gameObject.GetComponent<PQ>() != null;
            if (hits.Any(hasPQ)) {
                groundHitPoint = hits.First(hasPQ).point;
                hitGround = true;
            }

            if (hitGround && hitOcean) {
                //we have an intersection for both land and water, return whichever is highest altitude
                worldHitPoint = body.GetAltitude(groundHitPoint) > body.GetAltitude(oceanHitPosition) ? groundHitPoint : oceanHitPosition;
                return true;
            }
            if (hitOcean) {
                worldHitPoint = oceanHitPosition;
                return true;
            }
            if (hitGround) {
                worldHitPoint = groundHitPoint;
                return true;
            }
            worldHitPoint = Vector3.zero;
            return false;
        }



        public static float MouseToAxisOffset(Vector3 fromPoint, Vector3 projectionAxis) {
            Camera cam = FlightCamera.fetch.mainCamera;
            float dist = Vector3.Distance(fromPoint, cam.transform.position);
            //world to screen point acts weird, use ray instead
            Vector3 offsetPoint = cam.transform.position + cam.ScreenPointToRay(Input.mousePosition).direction * dist;

            //get the projection of the new point on our given vector
            return Vector3.Dot(projectionAxis.normalized, offsetPoint - fromPoint);
        }
    }
}