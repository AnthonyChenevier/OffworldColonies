using System.Collections.Generic;

namespace PQSModLoader {

    /// <summary>
    /// The PQS class has a private List of mods that it keeps and uses during its 
    /// internal upkeep. This leaves no access for modders to add their own PQSMods 
    /// at runtime that use the correct native calls for OnSphereStart(), OnSetup(), etc.
    /// 
    /// RuntimePQSModInjector holds its own list of mods and calls their respective methods 
    /// at the normal time that native mod methods are called by their PQS(controller). The 
    /// current implementation is dumb as a brick, and will just pass through the mod method
    /// calls for all mods at the right times.
    /// 
    /// *LIMITATIONS AND FUTURE PLANS DISCUSSION* 
    /// As any mods added via this method do not use the default (hidden) PQS mod list the order property
    /// only applies to the list of mods within this class, which runs with the highest order possible (last). 
    /// Possible expansion of this feature could have this class inject slave copies of itself at 
    /// different order levels, allowing adhoc mod injection at limited order levels during runtime,
    /// then having a mechanism to load mods created at runtime on game load, possibly though saving 
    /// config files when the mod is spawned in-game, then injecting them on PSystemManager.Instance.OnPSystemReady
    /// just like this class does. 
    /// 
    /// 
    /// I don't need much of this as all I want this for is to inject cities with 
    /// full method integration on both game load and at runtime.
    /// </summary>
    public sealed class RuntimePQSModInjector: PQSMod
    {
        private List<PQSMod> _mods;
        private int _modCount;
        private bool _setupComplete;
        private void Awake()
        {
            _setupComplete = false;
            order = int.MaxValue; //run last always
            _mods = new List<PQSMod>(); //create our mod list now
        }

        /// <summary>
        /// Adds a mod to the list of runtime mods 
        /// </summary>
        /// <param name="mod"></param>
        public void AddMod(PQSMod mod)
        {
            //don't add the mod if it's turned off (stock behaviour)
            if (!mod.modEnabled || !mod.gameObject.activeSelf)
                return;

            _mods.Add(mod);
            _modCount = _mods.Count;
            _mods.Sort((a, b) => a.order.CompareTo(b.order));
            this.requirements |= mod.requirements;
            //if we have already completed setup then run Setup on 
            //the new mod now (may break with some order-dependent PQSMods)
            if (_setupComplete)
            {
                mod.sphere = sphere;
                mod.OnSetup();

                //not sure when these are safe to use but inital investigation seems ok
                mod.OnSphereStart();
                mod.OnPostSetup(); //needed for PQSCity2 orientation at least, doesn't seem to be used by anthing else
                mod.OnSphereStarted();
            }
        }

        public bool RemoveMod(PQSMod mod)
        {
            if (mod != null && _mods.Remove(mod))
            {
                _modCount = _mods.Count;
                //mods.Sort((Comparison<PQSMod>)((a, b) => a.order.CompareTo(b.order)));
                return true;
            }
            return false;
        }

        public bool RemoveMod(int index)
        {
            if (index >= 0 && index < _modCount)
            {
                _mods.RemoveAt(index);
                _modCount = _mods.Count;
                //mods.Sort((Comparison<PQSMod>)((a, b) => a.order.CompareTo(b.order)));
                return true;
            }
            return false;
        }

        public PQSMod GetModAt(int index)
        {
            if (index < 0 || index >= _modCount)
                return null;
            return _mods[index];
        }

        public PQSMod GetMod(PQSMod mod)
        {
            return _mods.Find(p => p == mod);
        }

        public PQSMod GetMod(string modName)
        {
            return _mods.Find(p => p.name == modName);
        }

        public override void OnSetup() {
            base.OnSetup();
            if (_mods.Count > 0) {
                _mods.Sort((a, b) => a.order.CompareTo(b.order));

                int count = _mods.Count;
                while (count-- > 0) {
                    if (!_mods[count].modEnabled || !_mods[count].gameObject.activeSelf)
                        _mods.RemoveAt(count);
                }
            }
            this._modCount = this._mods.Count;
            for (int index = 0; index < this._modCount; ++index)
            {
                this._mods[index].sphere = sphere;
                this._mods[index].OnSetup();
            }

            this.requirements = PQS.ModiferRequirements.Default;
            foreach (PQSMod mod in this._mods)
                this.requirements |= mod.requirements;

            _setupComplete = true;
        }
        

        public override void OnPostSetup() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnPostSetup();
        }

        public override void OnSphereReset() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnSphereReset();
        }

        public override void OnSphereActive() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnSphereActive();
        }

        public override void OnSphereInactive() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnSphereInactive();
        }

        public override bool OnSphereStart()
        {
            bool failed = false;
            for (int index = 0; index < this._modCount; ++index)
                if (this._mods[index].OnSphereStart())
                    failed = true;
            return failed;
        }

        public override void OnSphereStarted() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnSphereStarted();
        }

        public override void OnSphereTransformUpdate() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnSphereTransformUpdate();
        }

        public override void OnPreUpdate() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnPreUpdate();
        }

        public override void OnUpdateFinished() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnUpdateFinished();
        }

        public override void OnVertexBuild(PQS.VertexBuildData data) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnVertexBuild(data);
        }

        public override void OnVertexBuildHeight(PQS.VertexBuildData data) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnVertexBuildHeight(data);
        }

        public override double GetVertexMaxHeight() {
            double num = 0.0;
            for (int index = 0; index < this._modCount; ++index)
                num += this._mods[index].GetVertexMaxHeight();
            return num;
        }

        public override double GetVertexMinHeight() {
            double num = 0.0;
            for (int index = 0; index < this._modCount; ++index)
                num += this._mods[index].GetVertexMinHeight();
            return num;
        }

        public override void OnMeshBuild() {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnMeshBuild();
        }

        public override void OnQuadCreate(PQ quad) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnQuadCreate(quad);
        }

        public override void OnQuadDestroy(PQ quad) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnQuadDestroy(quad);
        }

        public override void OnQuadPreBuild(PQ quad) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnQuadPreBuild(quad);
        }

        public override void OnQuadBuilt(PQ quad) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnQuadBuilt(quad);
        }

        public override void OnQuadUpdate(PQ quad) {
            base.OnQuadUpdate(quad);
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnQuadUpdate(quad);
        }

        public override void OnQuadUpdateNormals(PQ quad) {
            for (int index = 0; index < this._modCount; ++index)
                this._mods[index].OnQuadUpdateNormals(quad);
        }

    }
}
