using System;
using Highlighting;
using OffworldColoniesPlugin.ColonyManagement;
using OffworldColoniesPlugin.Debug;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColoniesPlugin.Part
{
    /// <summary>
    ///     BaseBuilderPart is the part that allows a vessel to create new bases.
    ///     This class handles the positioning of a new base, including checks for distance to other existing bases.
    ///     It also handles adding new base pieces onto existing bases within range.
    ///     Checking for the position of a new base means showing a ghost version of the
    ///     base model and allowing the user to adjust its position. The initial piece takes
    ///     All subsequent base pieces in the same base
    ///     will use the inital piece as a reference and must be connected to one of the existing pieces.
    /// </summary>
    public class BaseBuilderPart : PartModule
    {
        private const float MaxMoveSpeed = 0.05f; //5cm/second
        private const float BaseTopRadius = 20f; //the radius of the top/ground level of the base piece

        private BaseController _myBaseController;
        private BaseEvent _baseEvent;
        private BaseEvent _deployEvent;
        private BodySurfacePosition _selectedCoords;
        private GameObject _placementGhost;

        public Vector3 BuildDirection => transform.right;

        private void Start()
        {
            _baseEvent = Events["CreateBaseHere"];
            _deployEvent = Events["DeployBuilder"];
            _baseEvent.active = false;
        }

        /// <summary>
        /// Begins the base creation process. 
        /// 
        /// Design goal and discussion: 
        /// Begins the base selection procedure. This process should:
        /// - bring up a hexBase ghost model centred under the cursor
        /// - color the placeholder for valid (blue) and invalid (red)
        ///   placement (over any vessel/object other than the ground, 
        ///   too far from builder, etc)
        /// - allow some control over placement (height & rotation) of
        ///   the initial hex. Other hexes will be built off this in a
        ///   fairly rigid structure (on one of the six sides, no changes
        ///   to altitude, only allow placement if there isn't something 
        ///   blocking it, etc)
        /// - Each subsequent hex is aligned to the initial piece. This
        ///   means that over large distances the furthest pieces will 
        ///   have a higher altitude relative to the sphere than the initial
        ///   piece, and that the angle of gravity will change as to the 
        ///   gravity well it seems like the euclidean flat structure is 
        ///   actually sloping upwards at an increasing angle. A stretch goal
        ///   to combat this is to allow each hex to align itself to the 
        ///   surface normal so that the base is always flat *relative to the 
        ///   sphere* rather than relative to the initial piece. This would
        ///   necessitate one or more pentagonal pieces, and may be impacted
        ///   by the radii of the different spheres (and even altitude on a 
        ///   single sphere) requiring different sized hexes to cover - hard maths :/
        ///   Instead the base size will be limited to a certain radius/number 
        ///   of hexes - testing on each body is required to see what the 
        ///   gravity differential is for different max-sizes - the smallest, densest
        ///   body vs the largest, rarest body are good test targets.
        /// 
        /// Testing/alpha functionality:
        /// 1. Show a ghost at mouse position on sphere
        /// </summary>
        private void CreateNewBase()
        {
            CelestialBody cBody = vessel.mainBody;
            //Get hex ghost instance
            if (_placementGhost == null)
                _placementGhost = ColonyManagerScenario.CurrentInstance.GetPlacementGhost();

            EnablePlanetOccluder(cBody, true);

            //Shader clearShader = Shader.Find("KSP/Alpha/Translucent");
            //Shader defaultShader = Shader.Find("KSP/Bumped");

            
            //link to mouse position on sphere
            Camera cam = FlightCamera.fetch.mainCamera;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            Physics.Raycast(cam.transform.position, ray.direction, out hit, 10000.0f, LayerMask.GetMask("Local Scenery"));
            Vector3 mPos = hit.point;
            //orient the model correctly
            Planetarium.CelestialFrame cf = new Planetarium.CelestialFrame();
            Planetarium.CelestialFrame.SetFrame(0.0, 0.0, 0.0, ref cf);
            Vector3d surfaceNvector = LatLon.GetSurfaceNVector(cf, cBody.GetLatitude(mPos), cBody.GetLongitude(mPos));

            _placementGhost.transform.position = Vector3.zero;
            _placementGhost.transform.rotation = Quaternion.identity;
            _placementGhost.transform.parent = cBody.pqsController.transform;
            _placementGhost.transform.localPosition = surfaceNvector * (cBody.Radius + cBody.GetAltitude(mPos)+5);
            _placementGhost.transform.localRotation = Quaternion.FromToRotation(_placementGhost.transform.up, (Vector3)surfaceNvector) * Quaternion.AngleAxis((float)_placementGhost.transform.rotation.eulerAngles.y, Vector3.up);





            //Vessel activeVessel = FlightGlobals.ActiveVessel;
            ////get the bounds of the vessel (the safe zone that the base part is not allowed to intersect)
            //Bounds vBounds = VesselUtils.GetVesselBounds(activeVessel);

            ////DEBUG: get a position ahead of the vessel.
            ////TODO: Change to player mouse position selection method (the rest 
            ////TODO: of this code will be replaced/moved to an eventHandler for the click event)
            ////allowed build position must be at least (1 vessel diameter + 1 base radius) away
            ////to ensure the base will not intefere with the vessel on placement/instantiation
            //Vector3 buildPos = vBounds.center + vessel.ReferenceTransform.up*(vBounds.size.magnitude + BaseTopRadius);

            ////get the altitude at the bottom of the craft
            //double craftBottomAltitude = activeVessel.altitude - activeVessel.heightFromTerrain;

            ////the base coordinates are the build position at the same height at the active vessel
            //_selectedCoords = new SurfaceBodyCoordinates(cBody.GetLatitude(buildPos), cBody.GetLongitude(buildPos),
            //    craftBottomAltitude, cBody);

            //VesselUtils.CreateHelperSphereAt("buildPosition", buildPos, XKCDColors.AppleGreen, .5f, transform);
            //VesselUtils.CreateHelperSphereAt("buildPositionGround", _selectedCoords.WorldPosition, XKCDColors.RedOrange, .5f,
            //    transform);


            //instead of build base, show a ghost and allow player to place it themselves
            //BuildBase(_selectedCoords);
        }

        //Add or remove a planetary occluder
        private void EnablePlanetOccluder(CelestialBody cBody, bool enable) {
            GameObject go = cBody.pqsController.gameObject;
            if (enable) {
                Highlighter occluder = go.GetComponent<Highlighter>() ??
                                       go.AddComponent<Highlighter>();
                occluder.OccluderOn();
                occluder.SeeThroughOff();
            }
            else {
                Highlighter occluder = go.GetComponent<Highlighter>();
                if (occluder == null) return;
                Destroy(occluder);
            }
        }

        /// <summary>
        /// Design goal:
        /// Add Base Part must be able to build parts over time, using a placement ghost as a
        /// placeholder and consuming resources (Ore or CRP) at a given rate. The placeholder 
        /// will have a collider as soon as building begins. This means that static base parts
        /// should be cheap and fast to build compared to active parts (with their own 
        /// behaviour/functionality) to prevent exploitation of unfinished base parts as a 
        /// cheap way of making flat landing areas.
        /// </summary>
        /// <param name="buildHexPosition"></param>
        /// <param name="partType"></param>
        /// <param name="baseController"></param>
        public void AddBasePart(BaseController baseController, int buildHexPosition, BaseType partType = BaseType.Default)
        {
            ColonyManagerScenario.CurrentInstance.AddBasePart(baseController, buildHexPosition, partType);
        }

        /// <summary>
        /// Creates a new baseController at the given position and begins the build process
        /// for the first base part at that position
        /// </summary>
        /// <param name="selectedCoords">The coordinates </param>
        /// <param name="initialPartType"></param>
        public void BuildBase(BodySurfacePosition selectedCoords, BaseType initialPartType = BaseType.Default)
        {
            _myBaseController = ColonyManagerScenario.CurrentInstance.CreateNewBase(selectedCoords);
            AddBasePart(_myBaseController, 0, initialPartType);
        }



        #region Buttons
        //TODO: halt/continue building buttons for resource management

        [KSPEvent(guiName = "Deploy", active = true, guiActive = true)]
        public void DepolyBuilder() {
            //do deploy animations

            //toggle the create base button
            _baseEvent.active = !_baseEvent.active;
            _baseEvent.guiActive = _baseEvent.active;
        }

        /// <summary>
        /// Builds a new base if there are no active bases in 
        /// range, adds new parts to a base if there is.
        /// </summary>
        [KSPEvent(guiName = "Create Base", active = false, guiActive = false)]
        public void CreateBaseHere() {
            Vessel activeVessel = FlightGlobals.ActiveVessel;

            if ((activeVessel.situation != Vessel.Situations.LANDED) ||
                (activeVessel.GetSrfVelocity().magnitude >= MaxMoveSpeed))
                return;

            if (_myBaseController == null) {
                CreateNewBase();
                //_baseEvent.active = false; //we cant build more bases until this one is complete
                //_baseEvent.guiName = "Build Base";
            }
            //else {
            //    int selectedBuildPostion = 1;
            //    BaseType selectedPart = BaseType.Default;
            //    AddBasePart(_myBaseController, selectedBuildPostion, selectedPart);
            //}
        }

        /// <summary>
        /// Run test code for this vessel/part
        /// </summary>
        //[KSPEvent(guiName = "Test Function", active = true, guiActive = true)]
        //public void TestingButton()
        //{
        //    //DebugTools.TestMaterialColour(vessel);
        //    //DebugTools.DumpHeirarchy();
        //    //DebugTools.TestMisc();
        //}
        #endregion
    }
}