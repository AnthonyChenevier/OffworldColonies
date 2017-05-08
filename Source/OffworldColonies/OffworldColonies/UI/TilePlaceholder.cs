using Highlighting;
using OffworldColonies.Utilities;
using PQSModLoader.Factories;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.UI {
    public class TilePlaceholder: MonoBehaviour {
        //Component/Child references
        protected GameObject ModelObject;
        protected Transform ModelTransform;
        protected Vector3 ModelLocalOffset;

        private Highlighter _modelHighlighter;
        private Highlighter _planetOccluder;

        public bool IsEnabled { get; protected set; } = false;

        /// <summary>
        /// Creates a TilePlaceholder instance.
        /// </summary>
        /// <returns>The new placeholder instance</returns>
        protected static T Create<T>() where T : TilePlaceholder {
            return new GameObject(typeof(T).Name).AddComponent<T>();
        }

        /// <summary>
        /// Initialises the placeholder's model, default variables and components
        /// </summary>
        protected virtual void Enable(SingleModelDefinition modelDefinition) {
            if (IsEnabled) return;

            ModelObject = SingleModelFactory.Create(modelDefinition, transform);
            
            _modelHighlighter = ModelObject.AddComponent<Highlighter>();

            ModelTransform = ModelObject.transform;
            ModelLocalOffset = ModelObject.transform.localPosition;
            IsEnabled = true;
        }

        public void OnDestroy() {
           Disable();
        }

        /// <summary>
        /// Destroys self and the model instance
        /// </summary>
        public virtual void Disable() {
            if (!IsEnabled) return;
            ModLogger.Log("Disabling placeholder");
            if (ModelObject != null) Destroy(ModelObject); //destroy our model instance
            ModelObject = null;
            ModelTransform = null;
            ModelLocalOffset = Vector3.zero;

            _modelHighlighter = null;
            _planetOccluder = null;

            IsEnabled = false;
        }

        //sets the ghost highlight mode and color
        public virtual void SetHighlight(bool highlightOn, Color color) {
            if (!IsEnabled) return;

            if (highlightOn) {
                _modelHighlighter.ConstantOn(color);
                _modelHighlighter.SeeThroughOff();

                if (transform.parent == null) return;

                //enable a planet occluder so the base highlighting looks less like ass.
                _planetOccluder = transform.parent.gameObject.AddOrGetComponent<Highlighter>();
                _planetOccluder.OccluderOn();
                _planetOccluder.SeeThroughOff();
            }
            else {
                _modelHighlighter.ConstantOff();
                _modelHighlighter.SeeThroughOn();

                //destroy the planet occluder if there is one
                if (_planetOccluder != null)
                    Destroy(_planetOccluder);
            }
        }

        public virtual void RefreshTransform(CelestialBody body, Vector3 position, float heightOffset, float rotationOffset) {
            if (!IsEnabled) return;
            //move us to the position
            transform.position = position;

            //orient us with the body's surface normal
            Planetarium.CelestialFrame cf = new Planetarium.CelestialFrame();
            Planetarium.CelestialFrame.SetFrame(0.0, 0.0, 0.0, ref cf);
            Vector3d surfaceNvector = LatLon.GetSurfaceNVector(cf, body.GetLatitude(position), body.GetLongitude(position));
            transform.localRotation = Quaternion.FromToRotation(Vector3.up, surfaceNvector) * Quaternion.AngleAxis(0f, Vector3.up);

            //move and rotate our placeholder model by its offsets
            ModelTransform.localPosition = ModelLocalOffset + Vector3.up * heightOffset;
            ModelTransform.localRotation = Quaternion.AngleAxis(rotationOffset, Vector3.up);
        }
    }
}
