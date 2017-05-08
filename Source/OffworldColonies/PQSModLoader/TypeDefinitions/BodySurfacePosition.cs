namespace PQSModLoader.TypeDefinitions {

    /// <summary>
    /// Represents a set of coordinates on a given Celestial Body for easy conversion
    /// between ksp world (Unity), radial position (PQSCity) and latitude, longitude, 
    /// altitude (CelestialBody/Vessel) coordinates.
    /// </summary>

    public class BodySurfacePosition: IConfigNode {
        private double _latitude;
        private double _longitude;
        private double _altitude;
        private string _bodyName;
        private CelestialBody _body;
        //Store coordinates as the body, latitude, longitude and altitude
        //as these seem to provide the largest number of conversion options
        //between coordinate systems
        public CelestialBody Body {
            get { return _body; }
            set {
                _bodyName = value.bodyName;
                _body = value;
            }
        }

        public string BodyName
        {
            get { return _bodyName; }
            set {
                _bodyName = value;
                _body = PSystemManager.Instance.localBodies.Find(p => p.bodyName == value);
            }
        }

        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }

        public double Altitude
        {
            get { return _altitude; }
            set { _altitude = value; }
        }

        /// <summary>
        /// The coordinates in world space
        /// </summary>
        public Vector3d WorldPosition {
            get { return Body.GetWorldSurfacePosition(Latitude, Longitude, Altitude); }
            set { Body.GetLatLonAlt(value, out _latitude, out _longitude, out _altitude); }
        }

        ///<summary>
        /// Radial Position is the normal unit vector from the current planet's 
        /// center out through the latitude/longitude of the coordinate.
        /// this gives a spherical position in the body's (non-rotating) reference frame.
        /// </summary>
        public Vector3d RadialPosition => Body.GetSurfaceNVector(Latitude, Longitude);

        public BodySurfacePosition()
        {
            Body = FlightGlobals.Bodies[0]; //the sun/centre body
            WorldPosition = Vector3d.zero;
        }

        public BodySurfacePosition(double latitude, double longitude, double altitude, CelestialBody body) {
            Body = body;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        public BodySurfacePosition(Vector3d worldPosition, CelestialBody body) {
            Body = body;
            WorldPosition = worldPosition;
        }

        public BodySurfacePosition(Vector3d radialPosition, double altitude, CelestialBody body) {
            Body = body;
            Vector3d worldPosition = body.position + radialPosition * (body.Radius + altitude);
            WorldPosition = worldPosition;
        }

        public BodySurfacePosition(ConfigNode node) {
            Load(node);
        }

        public BodySurfacePosition(BodySurfacePosition bodyCoordinates) {
            Body = bodyCoordinates.Body;
            WorldPosition = bodyCoordinates.WorldPosition;
        }

        public void Save(ConfigNode node) {
            node.AddValue("BodyName", BodyName);
            node.AddValue("Latitude", Latitude);
            node.AddValue("Longitude", Longitude);
            node.AddValue("Altitude", Altitude);
        }

        public void Load(ConfigNode node) {
            BodySurfacePosition tmp = ResourceUtilities.LoadNodeProperties<BodySurfacePosition>(node);
            Body = tmp.Body;
            WorldPosition = tmp.WorldPosition;
        }
    }
}
