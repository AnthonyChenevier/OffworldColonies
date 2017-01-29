using System;
using System.Collections.Generic;
using System.Linq;
using Highlighting;
using ModUtilities;
using OffworldColonies.ColonyManagement;
using PQSModLoader.Factories;
using PQSModLoader.TypeDefinitions;
using UnityEngine;
using UnityEngine.Rendering;

namespace OffworldColonies.UI {
    public class GhostController: MonoBehaviour {
        private static ColonyManager CM => ColonyManager.Instance;
        private BuildUIManager BUIM => BuildUIManager.Instance;

        //Component/Child references
        private GameObject _ghostModel;
        private Transform _ghostModelTransform;
        private Vector3 _modelLocalOffset;
        private Renderer _renderer;
        private Highlighter _highlighter;

        //the attached builder vessel and celestial body
        private Vessel _builderVessel;
        private CelestialBody _body;

        //input control flags
        private bool _handleInput;
        private bool _trackElevation;
        private bool _trackRotation;
        private bool _trackPosition;

        //position, elevation and rotation of the base
        //used for initial placement of the colony
        private Vector3 _basePosition;

        private float _initialElevation;
        private bool _requireInitialElevation;
        private float _baseHeightOffset;

        private bool _requireInitialRotation;
        private float _initialRotation;
        private float _baseRotationOffset;

        //is the current position, elevation and rotation allowed?
        private bool _allowPlacement;

        /// <summary>
        /// Creates a ghostContrller instance.
        /// </summary>
        /// <returns>The new ghost instance</returns>
        public static GhostController Create() {
            return new GameObject("PlacementGhost").AddComponent<GhostController>();
        }
        
        //sets the ghost material to a variety of colors
        public void SetRed() {_renderer.material.color = new Color32(233, 55, 55, 255); }
        public void SetBlue() { _renderer.material.color = new Color32(47, 152, 227, 255); }
        public void SetGreen() {_renderer.material.color = new Color32(57, 234, 57, 255);}
        public void SetYellow() { _renderer.material.color = new Color32(246, 240, 60, 255); }

        /// <summary>
        /// Initialises the ghost's model, default variables and components
        /// </summary>
        private void Awake() {
            ModLogger.Log($"GhostController: Awake in {HighLogic.LoadedScene}");
            //set up our model
            _ghostModel = LoadModel(transform);
            //add a highlighting script to make it fancy
            _highlighter = _ghostModel.AddComponent<Highlighter>();
            _modelLocalOffset = _ghostModel.transform.localPosition;
            _ghostModelTransform = _ghostModel.transform;
            //set up our renderer for transparency
            //ghosts are transparent and don't cast shadows
            _renderer = _ghostModel.GetComponent<MeshRenderer>();
            _renderer.material.shader = Shader.Find("KSP/Alpha/Unlit Transparent");
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            //or collide with things
            _ghostModel.GetComponent<Collider>().enabled = false;
            //base model is on 'local scenery' layer, change to default 
            //as we use raycast on local scenery for position detection
            gameObject.SetLayerRecursive(LayerMask.NameToLayer("Default"));

            //Start us off with blue colouring
            SetBlue();
        }

        public void OnDestroy() {
            ModLogger.Log($"GhostController: OnDestroy in {HighLogic.LoadedScene}");
            Destroy(gameObject);
        }

        private static GameObject LoadModel(Transform parent) {
            ConfigNode hexData = GameDatabase.Instance.GetConfigNodes("OC_BASIC_HEX").Last();
            return StaticModelFactory.Create(new StaticModelDefinition(hexData), parent);
        }

        /// <summary>
        /// Called every frame so I'm trying to keep processing to a minimum
        /// </summary>
        private void Update() {
            //only keep going if we want to handle input
            if (!_handleInput) return;

            //pressing down shift enables moving the mouse up and down to change the placement elevation
            if (!_trackElevation && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))) {
                _trackElevation = true;
                _requireInitialElevation = true;
            }
            //if the player stopped holding the modifier key then stop elevating 
            if (_trackElevation && (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)))
                _trackElevation = false;

            //ditto for rotation controls
            if (!_trackRotation && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))) {
                _trackRotation = true;
                _requireInitialRotation = true;
            }
            if (_trackRotation && (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl)))
                _trackRotation = false;
            
            //prioritise elevation, then rotation, then position control handling
            if (_trackElevation) CalculateElevation();
            else if (_trackRotation) CalculateRotation();
            else if (_trackPosition) CalculatePosition();

            UpdateTransform();

            //check placement restrictions for the object ghost
            CheckPlacement();

            //Set the ghost colour blue if allowed, red if not
            if (_allowPlacement) SetBlue();
            else SetRed();

            if (Input.GetMouseButtonDown(0)) {
                if (_allowPlacement) {
                    BUIM.BuildNewColony(new BodySurfacePosition(_basePosition, _body), _baseHeightOffset, _baseRotationOffset);
                    BUIM.CancelPlacementMode();
                }
                else
                    BUIM.DenyPlacement();
            }
            else if (Input.GetMouseButtonDown(1)) {
                BUIM.CancelPlacementMode();
            }
        }

        /// <summary>
        /// Calculate the base position on the planet's surface
        /// </summary>
        private void CalculatePosition() {
            Vector3 mouseWorldPos;
            //only proceed if we are turned on and we have a world position
            if (!MouseSurfacePosition(_body, CM.IgnoreSeaLevelCollision, out mouseWorldPos)) return;

            //set our base position
            _basePosition = mouseWorldPos;
        }

        private void CalculateElevation() {
            if (_requireInitialElevation) {
                _initialElevation = _ghostModelTransform.localPosition.y - _modelLocalOffset.y;
                _requireInitialElevation = false;
            }

            //get the projection of the mouse on our up vector and set 
            //(offset - initial elevation) as the height offset, clamped
            //to min/max values
            _baseHeightOffset = Mathf.Clamp(MouseToAxisOffset(transform.position, transform.up) + _initialElevation,
                    CM.MinElevationOffset, CM.MaxElevationOffset);
        }

        private void CalculateRotation() {
            if (_requireInitialRotation) {
                _initialRotation = _ghostModelTransform.localRotation.eulerAngles.y;
                _requireInitialRotation = false;
            }
            //get the projection of the mouse on our right vector and
            //set (offset + initial rotation) as the rotation offset, 
            //clamped between 0-359
            _baseRotationOffset = (MouseToAxisOffset(transform.position, transform.right) *
                                   CM.RotationSpeed + _initialRotation) % 359;
        }


        private void UpdateTransform() {
            //orient the model with the body's surface normal
            //transform.rotation = Quaternion.FromToRotation(Vector3.up,
            //    _body.GetSurfaceNVector(_body.GetLatitude(_basePosition), _body.GetLongitude(_basePosition)));
            Planetarium.CelestialFrame cf = new Planetarium.CelestialFrame();
            Planetarium.CelestialFrame.SetFrame(0.0, 0.0, 0.0, ref cf);
            Vector3d surfaceNvector = LatLon.GetSurfaceNVector(cf, _body.GetLatitude(_basePosition), _body.GetLongitude(_basePosition));
            transform.localRotation = Quaternion.FromToRotation(Vector3.up, (Vector3)surfaceNvector) * Quaternion.AngleAxis(0f, Vector3.up);
            //move the ghost to the position
            transform.position = _basePosition;

            //move our ghost model up by the offset amount
            _ghostModelTransform.localPosition = Vector3.up*(_baseHeightOffset + _modelLocalOffset.y);

            //rotate our ghost model by the offset amount
            _ghostModelTransform.localRotation = Quaternion.AngleAxis(_baseRotationOffset, Vector3.up);
        }

        private void CheckPlacement() {
            Vector3 buildOrigin = _builderVessel.vesselTransform.position;
            _allowPlacement = true;

            if (Vector3.Distance(buildOrigin, _basePosition) < CM.MaxBuildRange) {
                Bounds gBounds = _ghostModel.MeshBounds();
                float gRadius = gBounds.extents.magnitude;
                Vector3 gPosition = _ghostModelTransform.TransformPoint(gBounds.center);

                List<Collider> hitColliders =
                    new List<Collider>(Physics.OverlapSphere(gPosition, gRadius, LayerMask.GetMask("Local Scenery")))
                        .FindAll(h => h.GetComponent<PQ>() == null && h.gameObject != _ghostModel);

                //only bother checking vessels if we haven't already hit something
                if (hitColliders.Count == 0) {
                    List<Vessel> vesselsLoaded = FlightGlobals.VesselsLoaded;
                    foreach (Vessel vessel in vesselsLoaded) {
                        Transform vTransform = vessel.vesselTransform;
                        //if it's out of max range of the origin ignore it
                        if (Vector3.Distance(vTransform.position, buildOrigin) > CM.MaxBuildRange)
                            continue;
                        //the vessel is in range of the builder vessel, get it's min range 
                        Bounds vBounds = vessel.GetVesselBounds();
                        float vRadius = vBounds.extents.magnitude;
                        Vector3 vPosition = vTransform.TransformPoint(vBounds.center);

                        float minRange = vRadius + gRadius;
                        //it's out of min range ignore it
                        if (Vector3.Distance(vPosition, gPosition) > minRange)
                            continue;

                        //TODO: we should have another closer-grained check here
                        
                        //within min range, we've collided
                        _allowPlacement = false;
                        break;
                    }
                }
                else _allowPlacement = false;
            }
            else _allowPlacement = false;
        }

        /// <summary>
        /// Enable the ghost instance for the builder vessel.
        /// </summary>
        /// <param name="builderVessel">The vessel we are attached to</param>
        public void Enable(Vessel builderVessel) {
            _builderVessel = builderVessel;
            _body = builderVessel.mainBody;
            transform.parent = _body.pqsController.transform;

            //Start handling our custom input
            _handleInput = true;
            _trackPosition = true;
            _trackRotation = false;
            _trackElevation = false;

            _basePosition = Vector3.zero;
            _baseHeightOffset = 0f;
            _baseRotationOffset = 0f;
            _ghostModelTransform.localRotation = Quaternion.Euler(Vector3.zero);
            _ghostModelTransform.localPosition = _modelLocalOffset;

            UpdateTransform();
            ModLogger.Log($"Build Ghost Enabled. Current Transform: P:{transform.position}, R:{transform.rotation} S: {transform.localScale}");
            //_highlighter.ConstantOn(XKCDColors.BrightRed);
            //_highlighter.SeeThroughOff();
            //enable the planet occluder so the base highlighting looks less like ass.
            //if (_body != null) {
            //    Highlighter occluder = _body.pqsController.gameObject.AddOrGetComponent<Highlighter>();
            //    occluder.OccluderOn();
            //    occluder.SeeThroughOff();
            //}

            gameObject.SetActive(true);
        }

        public void Disable() {
            gameObject.SetActive(false);
            _body = null;
            _builderVessel = null;

            //stop handling our custom input
            _handleInput = false;
            _trackPosition = false;
            _trackRotation = false;
            _trackElevation = false;

            //_highlighter.ConstantOff();
            //_highlighter.SeeThroughOn();
            //disable the planet occluder now that we're done
            //Highlighter occluder = _body.pqsController.gameObject.GetComponent<Highlighter>();
            //if (occluder != null)
            //    Destroy(occluder);
        }

        /// <summary>
        /// Gets the planetary surface coordinate position of the mouse. 
        /// Can detect or ignore sea level as required. 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="ignoreSeaLevel">Do we want to ignore the ocean (if one exists)?</param>
        /// <param name="worldHitPoint">output for the mouse position if one is found. If the 
        /// function returns false then this will be a 0 vector</param>
        /// <returns>True if the mouse is over the ground and a position is 
        /// found, false otherwise.</returns>
        public static bool MouseSurfacePosition(CelestialBody body, bool ignoreSeaLevel, out Vector3 worldHitPoint) {
            Camera cam = FlightCamera.fetch.mainCamera;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            //sea level detection could be handled multiple ways. I've elected to use a more intensive method that should
            //handle the edge case where there may be no local PQs to hit (an ocean world with no land PQS/a tiny physical
            //body with a massive ocean where the ray may end before hitting something) and very oblique camera angles where
            //the ocean is only skimmed
            Vector3 oceanHitPosition = new Vector3();
            bool hitOcean = false;
            if (!ignoreSeaLevel && body.ocean) {
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
