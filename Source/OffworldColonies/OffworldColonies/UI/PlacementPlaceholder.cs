using System;
using System.Collections.Generic;
using System.Linq;
using OffworldColonies.ColonyManagement;
using OffworldColonies.Part;
using OffworldColonies.Utilities;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.UI {
    public class PlacementPlaceholder: TilePlaceholder {
        private static ColonyManager CM { get; } = ColonyManager.Instance;

        private HextilePrinterModule _printerPart;
        private Renderer _modelRenderer;

        //input control flags
        private bool _trackingElevation;
        private bool _trackingRotation;

        private bool _requireInitialElevation;
        private bool _requireInitialRotation;
        private float _initialElevation;
        private float _initialRotation;

        private float _modelRotationOffset;

        private CelestialBody _parentBody;

        private Vector3 _placeholderPosition;

        private float _modelHeightOffset;
        private int _hexIDForPosition;

        public static PlacementPlaceholder Create() { return Create<PlacementPlaceholder>(); }

        /// <summary>
        /// Initialises the ghost's model, default variables and components
        /// </summary>
        public void Enable(SingleModelDefinition model, HextilePrinterModule printerPart) {
            if (IsEnabled) return;
            //set up our model
            base.Enable(model);

            _printerPart = printerPart;
            _parentBody = printerPart.vessel.mainBody;
            //get our first position from the mouse
            Vector3 mouseWorldPos;
            _placeholderPosition = InputHandler.MouseSurfacePosition(_parentBody, CM.IgnoreSeaLevelCollision,
                                                                     out mouseWorldPos) ? mouseWorldPos : Vector3.zero;

            _modelHeightOffset = 0f;
            _modelRotationOffset = 0f;

            transform.SetParent(_parentBody.pqsController.transform, false);

            ModelTransform.localRotation = /*_printerPart.CurrentColony ? _printerPart.CurrentColony.transform.localRotation :*/ Quaternion.identity;
            ModelTransform.localPosition = ModelLocalOffset;

            RefreshTransform(_parentBody, _placeholderPosition, _modelHeightOffset, _modelRotationOffset);
            
            _trackingRotation = false;
            _trackingElevation = false;

            //normal tile models are on the 'local scenery' layer, change to
            //'default' as we use raycast on local scenery for position detection
            gameObject.SetLayerRecursive(LayerMask.NameToLayer("Default"));
            //ghosts don't collide with things
            ModelObject.GetComponent<Collider>().enabled = false;
            _modelRenderer = ModelObject.GetComponent<MeshRenderer>();
            //set our initial color
            _modelRenderer.material.SetColor(PropertyIDs._RimColor, Color.red);
            _modelRenderer.material.SetFloat(PropertyIDs._RimFalloff, 4f);
            
            gameObject.SetActive(true);
            ModLogger.Log("PlacementPlaceholder Enabled");
        }

        public override void SetHighlight(bool highlightOn, Color color) {
            if (!IsEnabled) return;

            _modelRenderer.material.SetColor(PropertyIDs._RimColor, color);
            base.SetHighlight(highlightOn, color);
        }

        /// <summary>
        /// Handles input for the tile placement interface
        /// </summary>
        /// <remarks>
        /// - Called every frame (when enabled) so I'm *trying* to keep processing to a minimum
        /// </remarks>
        /// <remarks>
        /// - Controls are set up so that on inital input the relevent values of the 
        /// placeholder are stored, then as long as the modifier inputs are not released
        /// these values are updated from that inital value
        /// </remarks>
        /// <remarks>
        /// - Prioritises handling inputs in the order of Elevation, then Rotation, 
        /// then Position control handling
        /// </remarks>
        private void Update() {
            if (!IsEnabled) return;

            Colony printerColony = _printerPart.CurrentColony;
            bool printerHasColony = printerColony != null;

            //STEP 1: Check if we are tracking only position, or elevation and/or rotation
            UpdateTrackingInputState(!printerHasColony, true);

            //STEP 2: handle input tracking
            HandleTrackingInput(printerHasColony, printerColony);

            //STEP 3: check for placement obstacles
            bool canPlace = CheckPlacement();

            // STEP 4: update placeholder position and set highlight 
            UpdateModel(printerHasColony, canPlace);

            //STEP 5: Check for accept or cancel inputs
            HandleAcceptCancel(printerHasColony, canPlace);
        }

        private void UpdateTrackingInputState(bool trackElevation, bool trackRotation) {
            //pressing down the elevation modifier key enables moving
            //the mouse to change the tile placement elevation
            if (trackElevation && !_trackingElevation && InputHandler.ElevationInputDown()) {
                _trackingElevation = true;
                //on the first frame of input we need to get the current elevation
                _requireInitialElevation = true;
            }

            //similar deal for rotation controls
            if (trackRotation && !_trackingRotation && InputHandler.RotationInputDown()) {
                _trackingRotation = true;
                _requireInitialRotation = true;
            }

            //if the player stopped holding the modifier key then stop tracking,
            //even in the same frame or if tracking is disabled
            if (_trackingElevation && InputHandler.ElevationInputUp())
                _trackingElevation = false;
            if (_trackingRotation && InputHandler.RotationInputUp())
                _trackingRotation = false;
        }

        private void HandleTrackingInput(bool printerHasColony, Colony printerColony) {
            //only track position if neither elevation nor rotation are being tracked
            if (!(_trackingElevation || _trackingRotation)) {
                Vector3 mouseWorldPos;
                //get the mouse surface position. if we don't have a 
                //mouse hit then return our current position instead
                _placeholderPosition = InputHandler.MouseSurfacePosition(_parentBody, CM.IgnoreSeaLevelCollision, out mouseWorldPos)
                                        ? mouseWorldPos
                                        : _placeholderPosition;
                //snap to grid if the printer has an existing colony
                if (!printerHasColony) return;

                _hexIDForPosition = printerColony.HexGrid.CellIndexAtPosition(_placeholderPosition);
                printerColony.HexGrid.GetCell(_hexIDForPosition, out _placeholderPosition);

                return;
            }

            //separate if statements allow using the same input for elevation 
            //and rotation at the same time (both modifier keys held down)
            if (_trackingElevation) {
                if (_requireInitialElevation) {
                    _initialElevation = ModelTransform.localPosition.y - ModelLocalOffset.y;
                    _requireInitialElevation = false;
                }
                //get the projection of the mouse on our up vector and set 
                //(offset - initial elevation) as the height offset, clamped
                //to min/max values
                float elevationOffset = InputHandler.MouseToAxisOffset(transform.position, transform.up);
                _modelHeightOffset = Mathf.Clamp(elevationOffset + _initialElevation, CM.MinElevationOffset, CM.MaxElevationOffset);
            }

            if (_trackingRotation) {
                if (_requireInitialRotation) {
                    _initialRotation = ModelTransform.localRotation.eulerAngles.y;
                    _requireInitialRotation = false;
                }
                //get the projection of the mouse on our right vector and
                //set (offset + initial rotation) as the rotation offset, 
                //clamped between 0-359
                Vector3 projectionAxis = -FlightCamera.fetch.mainCamera.transform.right;
                float rotationOffset = InputHandler.MouseToAxisOffset(transform.position, projectionAxis);

                float rotationSpeed = (printerHasColony ? 2 : 1) * CM.RotationSpeed;
                _modelRotationOffset = (rotationOffset * rotationSpeed + _initialRotation) % 359;
            }
        }


        /// <summary>
        /// Check for any placement restrictions
        /// </summary>
        private bool CheckPlacement() {
            Vector3 buildBoundCentre = _printerPart.vessel.vesselTransform.position;
            float buildRange = CM.MaxBuildRange;

            //out-of-bounds, don't bother with expensive checks
            if (Vector3.Distance(buildBoundCentre, _placeholderPosition) > buildRange)
                return false;

            Bounds myBounds = ModelObject.MeshBounds();
            float myBoundRadius = myBounds.extents.magnitude;
            Vector3 myBoundCentre = ModelTransform.TransformPoint(myBounds.center);

            bool canPlace = false;
            if (_printerPart.CurrentColony == null) {
                IEnumerable<Collider> sceneryCloseBy = SceneryCloseBy(myBoundCentre, myBoundRadius);
                IEnumerable<Vessel> vesselsCloseBy = VesselsCloseBy(buildBoundCentre, buildRange, myBounds);

                canPlace = !sceneryCloseBy.Any() && !vesselsCloseBy.Any();
            }
            else {
                //we have an attached colony - ignore tiles when checking placement
                Collider[] sceneryCloseBy = SceneryCloseBy(myBoundCentre, myBoundRadius).ToArray();
                //tiles are (grand?)children of SurfaceStructures, so filter out any object who is a child of a SurfaceStructure
                sceneryCloseBy = Array.FindAll(sceneryCloseBy, collider => collider.GetComponentsInParent<SurfaceStructure>().Length == 0);

                IEnumerable<Vessel> vesselsCloseBy = VesselsCloseBy(buildBoundCentre, buildRange, myBounds);

                canPlace = !sceneryCloseBy.Any() && !vesselsCloseBy.Any();
            }

            return canPlace;
        }

        private void UpdateModel(bool stepRotation, bool canPlace) {
            //if the printer has an attached colony then tiles snap to 1/6th rotation increments
            float rotationOffset = stepRotation ? (int)(_modelRotationOffset / 60) * 60 : _modelRotationOffset;
            RefreshTransform(_parentBody, _placeholderPosition, _modelHeightOffset, rotationOffset);

            SetHighlight(true, canPlace ? Color.blue : Color.red);
        }

        private void HandleAcceptCancel(bool printerHasColony, bool canPlace) {
            if (InputHandler.AcceptPlacementInputDown()) {
                if (!canPlace) {
                    FlightUIController.Instance.DenyPlacement("Position not allowed");
                    return;
                }

                //we've clicked and placement is allowed. Try and begin a new build order
                if (printerHasColony) {
                    float rotationOffset = (int)(_modelRotationOffset / 60) * 60;
                    CM.AddTileBuildOrder(_printerPart, _hexIDForPosition, rotationOffset);
                }
                else {
                    BodySurfacePosition globePos = new BodySurfacePosition(_placeholderPosition, _parentBody);
                    CM.NewColonyBuildOrder(_printerPart, globePos, _modelHeightOffset, _modelRotationOffset);
                }
            }
            else if (InputHandler.CancelPlacementInputDown()) {
                FlightUIController.Instance.StopPlacementMode();
                FlightUIController.Instance.ClearSelected();
            }
        }

        /// <summary>
        /// Checks for Sphere overlap with any 
        /// Local-Scenery Layer colliders and filters
        /// out those with PQ components
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private IEnumerable<Collider> SceneryCloseBy(Vector3 origin, float radius) {
            Collider[] collidersInRange = Physics.OverlapSphere(origin, radius, LayerMask.GetMask("Local Scenery"));
            Collider[] sceneryCloseBy = Array.FindAll(collidersInRange, collider => collider.GetComponent<PQ>() == null);

            return sceneryCloseBy;
        }

        private IEnumerable<Vessel> VesselsCloseBy(Vector3 buildBoundsOrigin, float buildBoundsRange, Bounds myBounds) {
            Vector3 plBoundCentre = ModelTransform.TransformPoint(myBounds.center);
            float plBoundRadius = myBounds.extents.magnitude;

            List<Vessel> closeVessels = new List<Vessel>();
            foreach (Vessel vessel in FlightGlobals.VesselsLoaded) {
                Transform vTransform = vessel.vesselTransform;
                //only bother if the vessel is within max range
                if (Vector3.Distance(vTransform.position, buildBoundsOrigin) > buildBoundsRange) continue;

                //vessl is within range, if it's too close to the placeholder then add it to the list
                Bounds vBounds = vessel.GetVesselBounds();
                float vBoundsRadius = vBounds.extents.magnitude;
                Vector3 vBoundsCentre = vTransform.TransformPoint(vBounds.center);

                if (Vector3.Distance(vBoundsCentre, plBoundCentre) <= vBoundsRadius + plBoundRadius)
                    closeVessels.Add(vessel);
            }
            return closeVessels;
        }
    }
}