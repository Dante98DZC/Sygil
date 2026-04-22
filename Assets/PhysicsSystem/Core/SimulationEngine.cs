using UnityEngine;
using System.Collections.Generic;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;
using PhysicsSystem.Diffusion;
using PhysicsSystem.States;
using PhysicsSystem.Bridge;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]
namespace PhysicsSystem.Core
{
    public class SimulationEngine : MonoBehaviour
    {
        [SerializeField] private SimulationConfig config;
        [SerializeField] private MaterialLibrary library;
        [SerializeField] private int gridWidth  = 32;
        [SerializeField] private int gridHeight = 32;

        public PhysicsGrid   Grid     { get; private set; }
        public EngineNotifier Notifier => _notifier;

        public bool IsPaused { get; private set; }

        public void TogglePause() => IsPaused = !IsPaused;

        public void StepTick(TickType tickType)
        {
            bool wasPaused = IsPaused;
            IsPaused = false;
            RunTick(tickType);
            IsPaused = wasPaused;
        }

        internal void SetConfigForTest(SimulationConfig c) => config    = c;
        internal void SetLibraryForTest(MaterialLibrary l) => library   = l;
        internal void SetGridSizeForTest(int w, int h)     { gridWidth = w; gridHeight = h; }

        private RuleRegistry             _ruleRegistry;
        private float                    _timerFast, _timerStandard, _timerSlow, _timerIntegrity;
        private List<IDiffusionStrategy> _diffusers;
        private DerivedStateComputer     _derivedStateComputer;
        private EngineNotifier           _notifier;
        private DecaySystem              _decaySystem;

        private void Awake()
        {
            if (config == null || library == null) return;
            InitializeForTest();
        }

        internal void InitializeForTest()
        {
            library.Initialize();
            Grid = new PhysicsGrid(gridWidth, gridHeight, library);

            _ruleRegistry = new RuleRegistry(config);

            // ── Combustion & electricity ──────────────────────────────────────
            _ruleRegistry.AddRule(new Rules.Rules.R01_Combustion());
            _ruleRegistry.AddRule(new Rules.Rules.R03_ElectricPropagation());
            _ruleRegistry.AddRule(new Rules.Rules.R04_ElectricWater());
            _ruleRegistry.AddRule(new Rules.Rules.R09_HeatSuppression());
            _ruleRegistry.AddRule(new Rules.Rules.R10_GasIgnition());

            // ── Pressure & structure ──────────────────────────────────────────
            _ruleRegistry.AddRule(new Rules.Rules.R05_PressureExplosion());
            _ruleRegistry.AddRule(new Rules.Rules.R06_PressureRelease());
            _ruleRegistry.AddRule(new Rules.Rules.R07_StructuralCollapse());
            _ruleRegistry.AddRule(new Rules.Rules.R11_GasProduction(config.gasProductionRate, config.propertyCap));
            _ruleRegistry.AddRule(new Rules.Rules.R12_GasPressure(config.pressureFromGasCoeff, config.atmosphereConcentration));

            // ── Humidity ──────────────────────────────────────────────────────
            _ruleRegistry.AddRule(new Rules.Rules.R08_SlowEvaporation());

            // ── Filtration ─────────────────────────────────────────────────
            _ruleRegistry.AddRule(new Rules.Rules.R_Filtration(library));

            // ── Phase transitions: solid ↔ liquid ↔ gas ──────────────────────
            // Priority order within INTEGRITY tick: Melting(3) = Freezing(3) > Boiling(2) = Condensation(2)
            _ruleRegistry.AddRule(new Rules.Rules.R13_Melting(config.minTemperature, config.maxTemperature));
            _ruleRegistry.AddRule(new Rules.Rules.R14_Freezing(config.minTemperature, config.maxTemperature));
            _ruleRegistry.AddRule(new Rules.Rules.R15_Boiling(config.minTemperature, config.maxTemperature));
            _ruleRegistry.AddRule(new Rules.Rules.R16_Condensation(config.minTemperature, config.maxTemperature));

            _diffusers = new List<IDiffusionStrategy>
            {
                new Diffusion.GradientDiffusion(Diffusion.GradientProperty.Temperature),
                new Diffusion.GradientDiffusion(Diffusion.GradientProperty.ElectricEnergy),
                new Diffusion.GravityDiffusion(Diffusion.GravityProperty.Humidity),
                new Diffusion.GravityDiffusion(Diffusion.GravityProperty.GasConcentration),
                new Diffusion.PressureDiffusion()
            };

            _derivedStateComputer = new DerivedStateComputer(library);
            _notifier             = new EngineNotifier();
            _decaySystem          = new DecaySystem(config, library);
        }

        private void Update()
        {
            if (IsPaused) return;

            _timerFast      += Time.deltaTime;
            _timerStandard  += Time.deltaTime;
            _timerSlow      += Time.deltaTime;
            _timerIntegrity += Time.deltaTime;

            if (_timerFast      >= config.tickFast)      { RunTick(TickType.FAST);      _timerFast      = 0f; }
            if (_timerStandard  >= config.tickStandard)  { RunTick(TickType.STANDARD);  _timerStandard  = 0f; }
            if (_timerSlow      >= config.tickSlow)      { RunTick(TickType.SLOW);      _timerSlow      = 0f; }
            if (_timerIntegrity >= config.tickIntegrity) { RunTick(TickType.INTEGRITY); _timerIntegrity = 0f; }
        }

        internal void RunTick(TickType tickType)
        {
            // Protección contra cuellos de botella - máximo 2000 tiles por tick
            if (Grid.ActiveTiles.Count > 2000)
            {
                Debug.LogWarning($"[RunTick] Too many active tiles: {Grid.ActiveTiles.Count} - truncating processing");
                return;
            }

#if UNITY_DEBUG
            Debug.Log($"[RunTick] {tickType} — ActiveTiles: {Grid.ActiveTiles.Count}");
#endif
            _notifier.Snapshot(Grid);

            var snapshot = new List<Vector2Int>(Grid.ActiveTiles);
            var frozen   = new Dictionary<Vector2Int, TileData>(snapshot.Count);
            foreach (var pos in snapshot)
                frozen[pos] = Grid.GetTile(pos);

            foreach (var pos in snapshot)
            {
                ref var tile     = ref Grid.GetTile(pos);
                var neighbors    = Grid.GetNeighborsFromFrozen(pos, frozen);

                // Siempre pasar def de Ground - GetRuleMaterialDef en RuleRegistry resolve la capa correcta
                var neighborDefs = GetNeighborDefs(pos, MaterialLayer.Ground);
                var def          = Grid.GetMaterialDef(pos, MaterialLayer.Ground);

                _ruleRegistry.Evaluate(ref tile, neighbors, neighborDefs, def, tickType, pos, Grid);
                Grid.WriteNeighbors(pos, neighbors);
            }

            foreach (var diffuser in _diffusers)
                if (diffuser.TickType == tickType)
                    diffuser.Diffuse(Grid, library, config);

            _derivedStateComputer.Compute(Grid);
            _notifier.Dispatch(Grid);

            if (tickType == TickType.SLOW)
            {
                _decaySystem.Apply(Grid);
                _decaySystem.ClearElectricSources();
            }

            if (tickType == TickType.INTEGRITY)
            {
                Grid.RebuildAtmosphereFlags();
            }

            Grid.ClearDirtyFlags();
        }

        private MaterialDefinition[] GetNeighborDefs(Vector2Int pos, MaterialLayer layer)
        {
            var positions = Grid.GetNeighborPositions(pos);
            var defs      = new MaterialDefinition[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                defs[i] = Grid.GetMaterialDef(positions[i], layer);
            return defs;
        }

        public void SetTile(Vector2Int pos, TileData data) => Grid.SetTile(pos, data);
        public void RegisterElectricSource(Vector2Int pos) => _decaySystem.RegisterElectricSource(pos);
    }
}