// Assets/PhysicsSystem/Tests/PlayMode/TickBehaviorTests.cs
// Unity Test Framework — Play Mode
// Capa 2: tests de comportamiento observable del tick.
// Verifican efectos del orden: reglas → diffusion → derivedStates → decay → clear
//
// SETUP EN UNITY:
//   1. Crear carpeta Assets/PhysicsSystem/Tests/PlayMode/
//   2. Window → General → Test Runner → PlayMode
//   3. Requiere un GameObject con SimulationEngine en escena, o se crea via SetUp

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.States;
using PhysicsSystem.Rules;

namespace PhysicsSystem.Tests.PlayMode
{
    /// <summary>
    /// Construye un SimulationEngine mínimo en memoria para cada test.
    /// No depende de escena ni de ScriptableObjects en disco —
    /// crea instancias via ScriptableObject.CreateInstance.
    /// </summary>
    public class TickBehaviorTests
    {
        private SimulationEngine _engine;
        private SimulationConfig _config;
        private MaterialLibrary _library;

        // ── Posiciones de test aisladas (lejos del borde para tener 4 vecinos) ──
        private static readonly Vector2Int POS = new(8, 8);
        private static readonly Vector2Int EAST = new(9, 8);

        [SetUp]
        public void SetUp()
        {
            // Config
            _config = ScriptableObject.CreateInstance<SimulationConfig>();
            _config.tickFast = 0.1f;
            _config.tickStandard = 0.3f;
            _config.tickSlow = 0.5f;
            _config.tickIntegrity = 1.0f;
            _config.maxRulesPerTile = 3;
            _config.decayTemperature = 2.0f;
            _config.decayPressure = 3.0f;
            _config.decayHumidity = 0.5f;
            _config.decayGasDensity = 1.0f;
            _config.deactivationTolerance = 2.0f;

            // MaterialLibrary con definiciones mínimas
            _library = ScriptableObject.CreateInstance<MaterialLibrary>();
            var wood = MakeDef(MaterialType.WOOD, flammability: 0.8f, integrity: 60f, heat: 0.6f, elec: 0.1f, gas: 0.3f,
                            ignitionTemp: 70f, collapseInto: MaterialType.EARTH);
            var water = MakeDef(MaterialType.WATER, flammability: 0.0f, integrity: 20f, heat: 0.4f, elec: 0.9f, gas: 0.2f);
            var metal = MakeDef(MaterialType.METAL, flammability: 0.0f, integrity: 100f, heat: 0.8f, elec: 0.95f, gas: 0.0f,
                            hasMeltingPoint: true, meltingTemp: 90f, meltInto: MaterialType.EMPTY);
            var stone = MakeDef(MaterialType.STONE, flammability: 0.0f, integrity: 90f, heat: 0.3f, elec: 0.0f, gas: 0.0f);
            var empty = MakeDef(MaterialType.EMPTY, flammability: 0.0f, integrity: 0f, heat: 0.0f, elec: 0.0f, gas: 0.0f);
            var gas = MakeDef(MaterialType.GAS, flammability: 0.9f, integrity: 5f, heat: 0.2f, elec: 0.0f, gas: 1.0f,
                            ignitionTemp: 60f, collapseInto: MaterialType.EMPTY);
            _library.SetDefinitionsForTest(new[] { wood, water, metal, stone, empty, gas });
            _library.Initialize();

            // Engine como MonoBehaviour en un GameObject temporal
            var go = new GameObject("TestEngine");
            _engine = go.AddComponent<SimulationEngine>();
            _engine.SetConfigForTest(_config);
            _engine.SetLibraryForTest(_library);
            _engine.SetGridSizeForTest(16, 16);
            _engine.InitializeForTest();

            // Grid neutral: STONE con integrity=90 en toda la grilla
            var neutral = new TileData
            {
                material = MaterialType.STONE,
                structuralIntegrity = 90f,
                gasDensity = _config.gasBaseline  // ← agregar esta línea
            };
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    _engine.Grid.SetTile(new Vector2Int(x, y), neutral);
        }

        [TearDown]
        public void TearDown()
        {
            if (_engine != null)
                Object.DestroyImmediate(_engine.gameObject);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_library);
        }

        // =========================================================================
        // T01 — DerivedStates se calculan DESPUÉS de las reglas en el mismo tick
        // =========================================================================
        [Test]
        public void T01_DerivedStates_ReflectPostRuleState_AfterStandardTick()
        {
_engine.SetTile(POS, new TileData
            {
                material = MaterialType.WOOD,
                temperature = 80f,
                structuralIntegrity = 80f,
                liquidVolume = 5f
            });

            _engine.RunTick(TickType.STANDARD);

            var tile = _engine.Grid.GetTile(POS);
            Assert.IsTrue((tile.derivedStates & StateFlags.ON_FIRE) != 0,
                "ON_FIRE debe estar activo después del tick — DerivedStates debe correr post-reglas");
        }

        // =========================================================================
        // T02 — Decay SOLO corre en SLOW tick
        // =========================================================================
        [Test]
        public void T02_Decay_OnlyRunsOnSlowTick()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.STONE,
                gasDensity = 50f,
                structuralIntegrity = 90f
            });

            float pressureBefore = _engine.Grid.GetTile(POS).gasDensity;

            _engine.RunTick(TickType.FAST);
            Assert.AreEqual(pressureBefore, _engine.Grid.GetTile(POS).gasDensity, 0.01f,
                "Decay no debe correr en FAST tick");

            _engine.RunTick(TickType.STANDARD);
            Assert.AreEqual(pressureBefore, _engine.Grid.GetTile(POS).gasDensity, 0.01f,
                "Decay no debe correr en STANDARD tick");

            _engine.RunTick(TickType.INTEGRITY);
            Assert.AreEqual(pressureBefore, _engine.Grid.GetTile(POS).gasDensity, 0.01f,
                "Decay no debe correr en INTEGRITY tick");

            _engine.RunTick(TickType.SLOW);
            Assert.Less(_engine.Grid.GetTile(POS).gasDensity, pressureBefore,
                "Decay DEBE correr en SLOW tick");
        }

        // =========================================================================
        // T03 — Decay corre DESPUÉS de diffusion (I8)
        // =========================================================================
        [Test]
        public void T03_Decay_RunsAfterDiffusion_I8()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.STONE,
                gasDensity = 80f,
                structuralIntegrity = 90f
            });
            _engine.SetTile(EAST, new TileData
            {
                material = MaterialType.EMPTY,
                gasDensity = 0f,
                structuralIntegrity = 0f
            });

            _engine.RunTick(TickType.SLOW);

            float neighborPressure = _engine.Grid.GetTile(EAST).gasDensity;
            float sourcePressure = _engine.Grid.GetTile(POS).gasDensity;

            Assert.Greater(neighborPressure, 0f,
                "Diffusion debe haber propagado presión al vecino antes de que decay la reduzca");
            Assert.Less(sourcePressure, 80f,
                "Decay debe haber reducido la presión del tile origen");
        }

        // =========================================================================
        // T04 — ClearDirtyFlags corre al final del tick
        // =========================================================================
        [Test]
        public void T04_DirtyFlags_ClearedAfterTick()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.WOOD,
                temperature = 80f,
                structuralIntegrity = 80f,
                liquidVolume = 5f
            });

            _engine.RunTick(TickType.STANDARD);

            foreach (var pos in _engine.Grid.ActiveTiles)
            {
                var tile = _engine.Grid.GetTile(pos);
                Assert.IsFalse(tile.dirty,
                    $"Tile en {pos} debe tener dirty=false después del tick");
            }
        }

        // =========================================================================
        // T05 — Tile entra en ActiveTiles cuando una propiedad cambia
        // =========================================================================
        [Test]
        public void T05_ActiveTiles_TileEnters_WhenPropertyChanges()
        {
            _engine.SetTile(POS, new TileData { material = MaterialType.METAL, temperature = 50f });
            Assert.IsTrue(_engine.Grid.ActiveTiles.Contains(POS),
                "Tile debe entrar en ActiveTiles al ser modificado");
        }

        // =========================================================================
        // T06 — Tile sale de ActiveTiles cuando se estabiliza (via SLOW tick)
        // =========================================================================
        [Test]
        public void T06_ActiveTiles_TileExits_WhenStabilized()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.STONE,
                gasDensity = _config.gasBaseline,
                structuralIntegrity = 90f
            });

            Assert.IsTrue(_engine.Grid.ActiveTiles.Contains(POS),
                "Tile debe estar activo antes del tick");

            _engine.RunTick(TickType.SLOW);

            Assert.IsFalse(_engine.Grid.ActiveTiles.Contains(POS),
                "Tile debe salir de ActiveTiles después de estabilizarse");
        }

        // =========================================================================
        // T07 — Reglas FAST no corren en tick STANDARD
        // =========================================================================
        [Test]
        public void T07_FastRules_DontRunOnStandardTick()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.METAL,
                electricEnergy = 80f,
                structuralIntegrity = 100f
            });
            _engine.SetTile(EAST, new TileData
            {
                material = MaterialType.METAL,
                electricEnergy = 0f,
                structuralIntegrity = 100f
            });

            float energyBefore = _engine.Grid.GetTile(POS).electricEnergy;

            _engine.RunTick(TickType.STANDARD);

            Assert.AreEqual(energyBefore, _engine.Grid.GetTile(POS).electricEnergy, 0.01f,
                "R03 (FAST) no debe ejecutarse en tick STANDARD");
            Assert.AreEqual(0f, _engine.Grid.GetTile(EAST).electricEnergy, 0.01f,
                "Vecino no debe recibir energía si R03 no corrió");
        }

        // =========================================================================
        // T08 — Reglas INTEGRITY no corren en tick FAST
        // =========================================================================
        [Test]
        public void T08_IntegrityRules_DontRunOnFastTick()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.WOOD,
                structuralIntegrity = 5f
            });

            _engine.RunTick(TickType.FAST);

            var tile = _engine.Grid.GetTile(POS);
            Assert.AreEqual(MaterialType.WOOD, tile.material,
                "R07 (INTEGRITY) no debe ejecutarse en tick FAST — tile no debe colapsar");
        }

        // =========================================================================
        // T09 — Reglas INTEGRITY sí corren en tick INTEGRITY
        // =========================================================================
        [Test]
        public void T09_IntegrityRules_RunOnIntegrityTick()
        {
            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.WOOD,
                structuralIntegrity = 5f
            });
            _engine.Grid.ActiveTiles.Clear();
            _engine.Grid.MarkDirty(POS);

            _engine.RunTick(TickType.INTEGRITY);

            var tile = _engine.Grid.GetTile(POS);
            Assert.AreEqual(MaterialType.EARTH, tile.material,
                "R07 debe ejecutarse en tick INTEGRITY y colapsar WOOD a EARTH (collapseInto)");
        }

        // =========================================================================
        // T10 — DecaySystem respeta ON_FIRE: temperatura no decae si está en llamas
        // =========================================================================
        /// <summary>
        /// DerivedStateComputer corre ANTES de DecaySystem.
        /// Si el orden fuera al revés, decay reduciría temp antes de que ON_FIRE
        /// se calcule, y la protección de temperatura no funcionaría.
        ///
        /// El tile se aísla térmicamente rodeándolo de EMPTY para que GradientDiffusion
        /// no drene calor hacia los vecinos STONE del grid neutral durante el tick.
        /// </summary>
        [Test]
        public void T10_Decay_TemperatureProtected_WhenOnFire()
        {
            // Aislar POS: rodear con EMPTY para bloquear difusión térmica
            IsolateTile(POS);

            _engine.SetTile(POS, new TileData
            {
                material = MaterialType.WOOD,
                temperature = 80f,
                structuralIntegrity = 80f,
                liquidVolume = 5f
            });

            _engine.Grid.ActiveTiles.Clear();
            _engine.Grid.MarkDirty(POS);

            float tempBefore = _engine.Grid.GetTile(POS).temperature;

            _engine.RunTick(TickType.SLOW);

            float tempAfter = _engine.Grid.GetTile(POS).temperature;

            Assert.GreaterOrEqual(tempAfter, tempBefore,
                "Temperatura no debe decaer cuando ON_FIRE está activo — DerivedStates debe correr antes de Decay");
        }

        // =========================================================================
        // Helpers
        // =========================================================================

        /// <summary>
        /// Rodea <paramref name="pos"/> con tiles EMPTY en los 4 vecinos cardinales.
        /// Evita que GradientDiffusion drene propiedades hacia el grid neutral (STONE)
        /// en tests que necesitan aislar el tile bajo análisis.
        /// </summary>
        private void IsolateTile(Vector2Int pos)
        {
            var empty = new TileData { material = MaterialType.EMPTY };
            _engine.Grid.SetTile(pos + Vector2Int.up, empty);
            _engine.Grid.SetTile(pos + Vector2Int.down, empty);
            _engine.Grid.SetTile(pos + Vector2Int.left, empty);
            _engine.Grid.SetTile(pos + Vector2Int.right, empty);
        }

        private MaterialDefinition MakeDef(
            MaterialType type,
            float flammability, float integrity, float heat, float elec, float gas,
            float ignitionTemp = 0f,
            MaterialType collapseInto = MaterialType.EMPTY,
            bool hasMeltingPoint = false,
            float meltingTemp = 100f,
            MaterialType meltInto = MaterialType.EMPTY)
        {
            var def = ScriptableObject.CreateInstance<MaterialDefinition>();
            def.materialType = type;
            def.flammabilityCoeff = flammability;
            def.integrityBase = integrity;
            def.heatTransferCoeff = heat;
            def.electricTransferCoeff = elec;
            def.gasPermeabilityCoeff = gas;
            def.ignitionTemperature = ignitionTemp;
            def.collapseInto = collapseInto;
            def.hasMeltingPoint = hasMeltingPoint;
            def.meltingTemperature = meltingTemp;
            def.meltInto = meltInto;
            return def;
        }
    }
}