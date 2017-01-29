using ModUtilities;

namespace PQSModLoader
{
    /// <summary>
    /// A PQSCity2 with debug log hooks for testing
    /// </summary>
    public class RuntimePQSCity2 : PQSCity2 {

        protected override void Start()
        {
            ModLogger.Log($"RuntimePQSCity2 '{name}' started");
            base.Start();
        }

        public override void Orientate() {
            ModLogger.Log($"{name}.Orientate() called. Current transform: P:{transform.position} R:{transform.rotation} S:{transform.localScale}");
            base.Orientate();
        }

        protected override void SetInactive() {
            ModLogger.Log($"{name}.SetInactive() called");
            base.SetInactive();
        }

        protected override void Reset() {
            ModLogger.Log($"{name}.Reset() called");
            base.Reset();
        }

        public override void OnSetup() {
            ModLogger.Log($"{name}.OnSetup() called");
            base.OnSetup();
        }

        public override bool OnSphereStart() {
            ModLogger.Log($"{name}.OnSphereStart() called");
            return base.OnSphereStart();
        }

        public override void OnSphereStarted() {
            ModLogger.Log($"{name}.OnSphereStarted() called");
            base.OnSphereStarted();
        }

        public override void OnPostSetup() {
            ModLogger.Log($"{name}.OnPostSetup() called");
            base.OnPostSetup();
        }

        public override void OnSphereReset() {
            ModLogger.Log($"{name}.OnSphereReset() called");
            base.OnSphereReset();
        }

        public override void OnSphereActive() {
            ModLogger.Log($"{name}.OnSphereActive() called");
            base.OnSphereActive();
        }

        public override void OnSphereInactive() {
            ModLogger.Log($"{name}.OnSphereInactive() called");
            base.OnSphereInactive();
        }

        public override void OnUpdateFinished() {
            //OCLogger.Log($"{name}.OnUpdateFinished() called"); //many logspam. much annoy. wow.
            base.OnUpdateFinished();
        }

        /// <summary>
        /// Refreshes the mod
        /// </summary>
        public void Refresh() {
            OnSetup();
            OnPostSetup();
            if (sphere.isAlive)
                OnSphereActive();
        }
    }
}