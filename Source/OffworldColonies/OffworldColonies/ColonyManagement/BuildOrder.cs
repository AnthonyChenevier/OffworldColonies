using System;
using System.Collections.Generic;
using System.Linq;
using OffworldColonies.HexTiles;
using OffworldColonies.Part;
using OffworldColonies.UI;
using OffworldColonies.Utilities;
using PQSModLoader.TypeDefinitions;
using Object = UnityEngine.Object;

namespace OffworldColonies.ColonyManagement {
    /// <summary>
    /// BuildOrder encapsulates a timed build order for a colony tile.
    /// </summary>
    public class BuildOrder {
        private readonly ISharedResourceProvider _resourceProvider;
        private readonly HexTileDefinition _tileDef;
        private readonly int _hexPositionIndex;
        private readonly float _tileRotation;
        private readonly double _totalRequiredResources;

        private readonly Dictionary<int, double> _buildResourceRequest;
        
        private readonly Callback<HexTileDefinition, int, float> _onBuildComplete;
        private readonly Callback<bool, float> _onBuildPaused;
        private readonly Callback<float> _onBuildCanceled;
        private readonly Callback<int, double, double> _onBuildLackResource;

        private bool _processing;
        private double _lastUpdateTime;

        public bool IsPaused => !_processing;
        public float CompletionProgress { get; private set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resourceProvider">The printer part of the constructing vessel</param>
        /// <param name="tileDef"></param>
        /// <param name="hexPositionIndex"></param>
        /// <param name="tilePosition"></param>
        /// <param name="tileRotation"></param>
        /// <param name="builtCallback"></param>
        /// <param name="canceledCallback"></param>
        /// <param name="resourceLackCallback"></param>
        /// <param name="pausedCallback"></param>
        public BuildOrder(ISharedResourceProvider resourceProvider, HexTileDefinition tileDef, int hexPositionIndex, BodySurfacePosition tilePosition, float tileRotation, 
            Callback<HexTileDefinition, int, float> builtCallback, 
            Callback<float> canceledCallback,
            Callback<int, double, double> resourceLackCallback, 
            Callback<bool, float> pausedCallback) 
        {
            _onBuildComplete = builtCallback;
            _onBuildCanceled = canceledCallback;
            _onBuildLackResource = resourceLackCallback;
            _onBuildPaused = pausedCallback;

            _resourceProvider = resourceProvider;
            _tileDef = tileDef;
            _tileRotation = tileRotation;
            _hexPositionIndex = hexPositionIndex;
            _buildPosition = tilePosition;
            CompletionProgress = 0.0f;

            _buildResourceRequest = new Dictionary<int, double>();
            _totalRequiredResources = 0;
            foreach (KeyValuePair<int, RecipeResource> res in _tileDef.Recipe.Resources) {
                _buildResourceRequest.Add(res.Key, res.Value.UnitsRequired);
                _totalRequiredResources += res.Value.UnitsRequired;
            }
            _totalRequiredResources /= _tileDef.Recipe.Resources.Count;
        }



        private double GetDeltaTime() {
            double t = Planetarium.GetUniversalTime();
            double dt = Math.Max(_lastUpdateTime - t, TimeWarp.fixedDeltaTime);

            _lastUpdateTime = t;
            return dt;
        }



        public bool Start() {
            _lastUpdateTime = Planetarium.GetUniversalTime();
            _processing = true;

            EnableBuildPlaceholder(_tileDef.ModelDefinition.LODDefines[0].Models[0], _buildPosition, _tileRotation, 0f);
            string time = KSPUtil.PrintDateCompact(_lastUpdateTime, true, true);
            //Dictionary to pretty string LINQ :) learning is fun
            string resources = _buildResourceRequest.Select(r => $"{r.Value} {_tileDef.Recipe.Resources[r.Key].Name}").Aggregate((w, n) => $"{w}, {n}");
            ModLogger.Log($"Build order Started at {time}. Total resources required: {resources}");
            return true;
        }



        public bool Process() {
            if (!_processing) {
                SetBuildProgress(CompletionProgress);
                return false;
            }

            List<RecipeResource> buildResources = _tileDef.Recipe.Resources.Values.ToList();
            double deltaTime = GetDeltaTime();

            //process each resource for this frame
            foreach (RecipeResource res in buildResources) {
                int resID = res.ID;
                double resRequired = res.FlowSpeed * deltaTime;
                double resAvailable = _resourceProvider.RequestResource(resID, resRequired);

                if (resAvailable < resRequired*0.99) {
                    _resourceProvider.RequestResource(resID, -resAvailable);
                    _onBuildLackResource(resID, resRequired, resAvailable);
                    SetBuildProgress(CompletionProgress);
                    return false;
                }
                double resRemaining = _buildResourceRequest[resID] - resAvailable;

                _buildResourceRequest[resID] = resRemaining < 0 ? 0 : resRemaining;
                //negative resource remaining, add overflow back in as a negative request
                if (resRemaining < 0) _resourceProvider.RequestResource(resID, resRemaining);
            }

            CompletionProgress = 1-(float)(1 / _totalRequiredResources * _buildResourceRequest.Average(r => r.Value));
            //ModLogger.Log($"Processing build order ({_completion * 100}%). dt={deltaTime}");

            if (CompletionProgress < 1f) {
                SetBuildProgress(CompletionProgress);
                return true;
            }
            
            ModLogger.Log($"Build order Completed at {KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true, true)}");
            _processing = false;
            CompletionProgress = 1f;
            SetBuildProgress(CompletionProgress);
            DisableBuildPlaceholder();
            _onBuildComplete(_tileDef, _hexPositionIndex, _tileRotation);
            return true;
        }



        public void Pause(bool doPause) {
            double t = Planetarium.GetUniversalTime();
            ModLogger.Log($"Build order {(doPause ? "Paused" : "Unpaused")} at {KSPUtil.PrintDateCompact(t, true, true)}");
            _processing = !doPause;
            if (!doPause) _lastUpdateTime = t;
            _onBuildPaused(doPause, CompletionProgress);
        }



        public void Cancel() {
            ModLogger.Log($"Build order Canceled at {KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true, true)}");
            _processing = false;
            DisableBuildPlaceholder();
            _onBuildCanceled(CompletionProgress);
        }

        private BuildPlaceholder _buildPlaceholder;
        private BodySurfacePosition _buildPosition;

        public void EnableBuildPlaceholder(SingleModelDefinition modelDefinition, BodySurfacePosition bodyPos, float rotationOffset, float heightOffset) {
            if (_buildPlaceholder == null)
                _buildPlaceholder = BuildPlaceholder.Create();

            _buildPlaceholder.Enable(modelDefinition, bodyPos, rotationOffset, heightOffset);
        }

        public void SetBuildProgress(float progress) {
            if (_buildPlaceholder != null && _buildPlaceholder.IsEnabled)
                _buildPlaceholder.SetBuildProgress(0.1f + progress * 0.9f);
        }

        public void DisableBuildPlaceholder() {
            if (_buildPlaceholder == null) return;

            _buildPlaceholder.Disable();
            Object.Destroy(_buildPlaceholder);
        }
    }
}