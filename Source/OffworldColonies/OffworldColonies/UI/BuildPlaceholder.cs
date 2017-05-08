using OffworldColonies.Utilities;
using PQSModLoader.TypeDefinitions;
using UnityEngine;

namespace OffworldColonies.UI {
    public class BuildPlaceholder: TilePlaceholder {
        public static BuildPlaceholder Create() { return Create<BuildPlaceholder>(); }

        /// <summary>
        /// Enable the ghost for build mode
        /// </summary>
        /// <param name="model"></param>
        /// <param name="bodyPos"></param>
        /// <param name="rotationOffset"></param>
        /// <param name="heightOffset"></param>
        public void Enable(SingleModelDefinition model, BodySurfacePosition bodyPos, float rotationOffset, float heightOffset) {
            if (IsEnabled) return;

            base.Enable(model);

            transform.SetParent(bodyPos.Body.pqsController.transform, false);

            ModelTransform.localRotation = Quaternion.Euler(Vector3.zero);
            ModelTransform.localPosition = ModelLocalOffset;

            RefreshTransform(bodyPos.Body, bodyPos.WorldPosition, heightOffset, rotationOffset);

            SetBuildProgress(0.1f);

            gameObject.SetActive(true);
            ModLogger.Log("BuildPlaceholder Enabled");
        }

        public void SetBuildProgress(float percentComplete) {
            if (!IsEnabled) return;
            ModelTransform.localScale = new Vector3(1,0,1) * Mathf.Clamp01(percentComplete) + Vector3.up;
        }
    }
}