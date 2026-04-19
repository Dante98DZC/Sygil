// Assets/PhysicsSystem/Tests/Editor/RuleTests.cs
// Unity Test Framework — Edit Mode
// Capa 1: tests unitarios de reglas, sin engine, sin tiempo, deterministas.
//
// SETUP EN UNITY:
//   1. Window → General → Test Runner
//   2. Asegúrate de tener un Assembly Definition en Tests/Editor/ con:
//        - "Unity.TestFramework" y "UnityEngine.TestRunner" como referencias
//        - "Editor" en platforms
//   3. Los tests aparecen bajo "EditMode" en el Test Runner

using NUnit.Framework;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;
using PhysicsSystem.Rules.Rules;
using UnityEngine;

namespace PhysicsSystem.Tests.Editor
{
    /// <summary>
    /// Helpers compartidos por todos los grupos de tests.
    /// </summary>
     internal static class TestHelpers
    {
        public static MaterialDefinition Wood() => new MaterialDefinition
        {
            materialType          = MaterialType.WOOD,
            flammabilityCoeff     = 0.8f,
            heatTransferCoeff     = 0.6f,
            electricTransferCoeff = 0.1f,
            gasPermeabilityCoeff  = 0.3f,
            integrityBase         = 60f,
            // Transiciones
            ignitionTemperature   = 70f,
            collapseInto          = MaterialType.EARTH,
            hasMeltingPoint       = false,
        };
 
        public static MaterialDefinition Water() => new MaterialDefinition
        {
            materialType          = MaterialType.WATER,
            flammabilityCoeff     = 0f,
            heatTransferCoeff     = 0.4f,
            electricTransferCoeff = 0.9f,
            gasPermeabilityCoeff  = 0.2f,
            integrityBase         = 20f,
            ignitionTemperature   = 0f,     // no arde
            collapseInto          = MaterialType.EMPTY,
            hasMeltingPoint       = false,
        };
 
        public static MaterialDefinition Metal() => new MaterialDefinition
        {
            materialType          = MaterialType.METAL,
            flammabilityCoeff     = 0f,
            heatTransferCoeff     = 0.8f,
            electricTransferCoeff = 0.95f,
            gasPermeabilityCoeff  = 0f,
            integrityBase         = 100f,
            ignitionTemperature   = 0f,     // no arde
            collapseInto          = MaterialType.EMPTY,
            hasMeltingPoint       = true,
            meltingTemperature    = 90f,
            meltInto              = MaterialType.EMPTY,
        };
 
        public static MaterialDefinition Stone() => new MaterialDefinition
        {
            materialType          = MaterialType.STONE,
            flammabilityCoeff     = 0f,
            heatTransferCoeff     = 0.3f,
            electricTransferCoeff = 0f,
            gasPermeabilityCoeff  = 0f,
            integrityBase         = 90f,
            ignitionTemperature   = 0f,
            collapseInto          = MaterialType.EMPTY,
            hasMeltingPoint       = false,
        };
 
        public static MaterialDefinition Empty() => new MaterialDefinition
        {
            materialType          = MaterialType.EMPTY,
            flammabilityCoeff     = 0f,
            heatTransferCoeff     = 0f,
            electricTransferCoeff = 0f,
            gasPermeabilityCoeff  = 0f,
            integrityBase         = 0f,
            ignitionTemperature   = 0f,
            collapseInto          = MaterialType.EMPTY,
            hasMeltingPoint       = false,
        };
 
        public static MaterialDefinition Gas() => new MaterialDefinition
        {
            materialType          = MaterialType.GAS,
            flammabilityCoeff     = 0.9f,
            heatTransferCoeff     = 0.2f,
            electricTransferCoeff = 0f,
            gasPermeabilityCoeff  = 1.0f,
            integrityBase         = 5f,
            ignitionTemperature   = 60f,
            collapseInto          = MaterialType.EMPTY,
            hasMeltingPoint       = false,
        };
 
        public static TileData[] NeutralNeighbors(int count = 4)
        {
            var n = new TileData[count];
            for (int i = 0; i < count; i++)
                n[i] = new TileData { material = MaterialType.EMPTY, structuralIntegrity = 50f };
            return n;
        }
 
        public static MaterialDefinition[] NeutralNeighborDefs(int count = 4)
        {
            var d = new MaterialDefinition[count];
            for (int i = 0; i < count; i++) d[i] = Empty();
            return d;
        }
 
        public const float Epsilon = 0.01f;
    }
 
    // =========================================================================
    // Tests adicionales para las nuevas transiciones
    // =========================================================================
 
    [TestFixture]
    public class R07_TransitionTests
    {
        private R07_StructuralCollapse _rule;
 
        [SetUp] public void SetUp() => _rule = new R07_StructuralCollapse();
 
        [Test]
        public void Apply_WoodCollapse_LeavesEarth()
        {
            var tile = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f };
            var def  = TestHelpers.Wood(); // collapseInto = EARTH
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(MaterialType.EARTH, tile.material,
                "WOOD debe colapsar a EARTH, no a EMPTY");
        }
 
        [Test]
        public void Apply_MetalCollapse_LeavesEmpty_OnLowIntegrity()
        {
            var tile = new TileData { material = MaterialType.METAL, structuralIntegrity = 5f, temperature = 10f };
            var def  = TestHelpers.Metal(); // collapseInto = EMPTY
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(MaterialType.EMPTY, tile.material,
                "METAL con integridad baja debe colapsar a EMPTY");
        }
 
        [Test]
        public void Apply_MetalMelts_WhenTemperatureExceedsLimit()
        {
            // Metal con integridad alta pero temperatura sobre el punto de fusión
            var tile = new TileData { material = MaterialType.METAL, structuralIntegrity = 100f, temperature = 95f };
            var def  = TestHelpers.Metal(); // hasMeltingPoint=true, meltingTemperature=90
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def),
                "R07 debe activarse cuando temperatura supera meltingTemperature");
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(MaterialType.EMPTY, tile.material,
                "METAL fundido debe resultar en EMPTY");
            Assert.AreEqual(0f, tile.temperature, TestHelpers.Epsilon,
                "Temperatura debe resetearse al fundirse para evitar loop");
        }
 
        [Test]
        public void CanApply_ReturnsFalse_MetalBelowMeltingPoint()
        {
            var tile = new TileData { material = MaterialType.METAL, structuralIntegrity = 100f, temperature = 80f };
            var def  = TestHelpers.Metal(); // meltingTemperature = 90
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def),
                "METAL por debajo del punto de fusión no debe colapsar");
        }
 
        [Test]
        public void CanApply_ReturnsFalse_WoodAboveIgnitionButHighIntegrity()
        {
            // R07 no depende de temperatura para WOOD (hasMeltingPoint=false)
            // Solo debe activarse cuando integridad < 10
            var tile = new TileData { material = MaterialType.WOOD, structuralIntegrity = 50f, temperature = 95f };
            var def  = TestHelpers.Wood();
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def),
                "WOOD con integridad alta no debe colapsar aunque tenga temperatura alta");
        }
    }
 
    [TestFixture]
    public class R01_IgnitionTemperatureTests
    {
        private R01_Combustion _rule;
 
        [SetUp] public void SetUp() => _rule = new R01_Combustion();
 
        [Test]
        public void CanApply_ReturnsFalse_WhenTempBelowIgnitionTemperature()
        {
            // WOOD ignitionTemperature = 70 — con 69 no debe arder
            var tile = new TileData { temperature = 69f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }
 
        [Test]
        public void CanApply_ReturnsTrue_WhenTempAboveIgnitionTemperature()
        {
            var tile = new TileData { temperature = 71f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }
 
        [Test]
        public void CanApply_ReturnsFalse_WhenIgnitionTemperatureIsZero()
        {
            // STONE, METAL, WATER — ignitionTemperature = 0 — nunca arden
            var tile = new TileData { temperature = 99f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Stone()),
                "STONE no debe arder — ignitionTemperature = 0");
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Metal()),
                "METAL no debe arder — ignitionTemperature = 0");
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Water()),
                "WATER no debe arder — ignitionTemperature = 0");
        }
 
        [Test]
        public void CanApply_ReturnsTrue_GasAtIgnitionTemperature()
        {
            // GAS ignitionTemperature = 60, flammabilityCoeff = 0.9
            var tile = new TileData { temperature = 61f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Gas()));
        }
    }

    // =========================================================================
    // R01 — Combustion
    // =========================================================================
    [TestFixture]
    public class R01_CombustionTests
    {
        private R01_Combustion _rule;

        [SetUp] public void SetUp() => _rule = new R01_Combustion();

        [Test]
        public void CanApply_ReturnsFalse_WhenTempBelow70()
        {
            var tile = new TileData { temperature = 69f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenFlammabilityTooLow()
        {
            var tile = new TileData { temperature = 80f };
            var def  = TestHelpers.Water(); // flammability = 0
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenConditionsMet()
        {
            var tile = new TileData { temperature = 80f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void Apply_IncreasesTemperature()
        {
            var tile = new TileData
            {
                temperature        = 80f,
                gasDensity         = 0f,
                humidity           = 20f,
                structuralIntegrity= 80f
            };
            var def = TestHelpers.Wood();
            float before = tile.temperature;
            // CanApply debe llamarse primero — cachea _flammability que Apply usa
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.temperature, before);
        }

        [Test]
        public void Apply_IncreasesGasDensity()
        {
            var tile = new TileData { temperature = 80f, gasDensity = 0f, humidity = 20f, structuralIntegrity = 80f };
            var def  = TestHelpers.Wood();
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.gasDensity, 0f);
        }

        [Test]
        public void Apply_DecreasesHumidity()
        {
            var tile = new TileData { temperature = 80f, humidity = 20f, structuralIntegrity = 80f };
            var def  = TestHelpers.Wood();
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.humidity, 20f);
        }

        [Test]
        public void Apply_DecreasesIntegrity()
        {
            var tile = new TileData { temperature = 80f, humidity = 20f, structuralIntegrity = 80f };
            var def  = TestHelpers.Wood();
            float before = tile.structuralIntegrity;
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), def));
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.structuralIntegrity, before);
        }

        [Test]
        public void Apply_NeverExceedsCap()
        {
            // Partimos de valores al límite
            var tile = new TileData { temperature = 99f, gasDensity = 98f, humidity = 1f, structuralIntegrity = 1f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.LessOrEqual(tile.temperature,         100f);
            Assert.LessOrEqual(tile.gasDensity,          100f);
            Assert.GreaterOrEqual(tile.humidity,           0f);
            Assert.GreaterOrEqual(tile.structuralIntegrity,0f);
        }
    }

    // =========================================================================
    // R02 — Evaporation
    // =========================================================================
    [TestFixture]
    public class R02_EvaporationTests
    {
        private R02_Evaporation _rule;

        [SetUp] public void SetUp() => _rule = new R02_Evaporation();

        [Test]
        public void CanApply_ReturnsFalse_WhenNotWater()
        {
            var tile = new TileData { material = MaterialType.WOOD, temperature = 90f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenTempBelow80()
        {
            var tile = new TileData { material = MaterialType.WATER, temperature = 79f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Water()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenWaterAndHot()
        {
            var tile = new TileData { material = MaterialType.WATER, temperature = 85f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Water()));
        }

        [Test]
        public void Apply_SetsMaterialToEmpty()
        {
            var tile = new TileData { material = MaterialType.WATER, temperature = 85f, humidity = 50f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(MaterialType.EMPTY, tile.material);
        }

        [Test]
        public void Apply_DecreasesHumidity()
        {
            var tile = new TileData { material = MaterialType.WATER, temperature = 85f, humidity = 50f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.humidity, 50f);
        }

        [Test]
        public void Apply_IncreasesNeighborPressure()
        {
            var tile      = new TileData { material = MaterialType.WATER, temperature = 85f, humidity = 50f };
            var neighbors = TestHelpers.NeutralNeighbors();
            var defs      = new MaterialDefinition[] { TestHelpers.Stone(), TestHelpers.Stone(),
                                                       TestHelpers.Stone(), TestHelpers.Stone() };
            // Guardamos presión inicial (0)
            _rule.Apply(ref tile, neighbors, defs);
            foreach (var n in neighbors)
                Assert.Greater(n.pressure, 0f, "Neighbor pressure should increase after evaporation");
        }
    }

    // =========================================================================
    // R03 — Electric Propagation
    // =========================================================================
    [TestFixture]
    public class R03_ElectricPropagationTests
    {
        private R03_ElectricPropagation _rule;

        [SetUp] public void SetUp() => _rule = new R03_ElectricPropagation();

        [Test]
        public void CanApply_ReturnsFalse_WhenNoEnergy()
        {
            var tile = new TileData { electricEnergy = 0f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Metal()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenHasEnergy()
        {
            var tile = new TileData { electricEnergy = 50f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Metal()));
        }

        [Test]
        public void Apply_HalvesTileEnergy()
        {
            var tile = new TileData { material = MaterialType.METAL, electricEnergy = 80f };
            float before = tile.electricEnergy;
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(before * 0.5f, tile.electricEnergy, TestHelpers.Epsilon);
        }

        [Test]
        public void Apply_TransfersEnergyToNeighbors()
        {
            var tile      = new TileData { material = MaterialType.METAL, electricEnergy = 80f };
            var neighbors = TestHelpers.NeutralNeighbors();
            var defs      = new MaterialDefinition[] { TestHelpers.Metal(), TestHelpers.Metal(),
                                                       TestHelpers.Metal(), TestHelpers.Metal() };
            _rule.Apply(ref tile, neighbors, defs);
            foreach (var n in neighbors)
                Assert.Greater(n.electricEnergy, 0f, "Neighbor should receive electric energy");
        }

        [Test]
        public void Apply_MarkNeighborsDirty()
        {
            var tile      = new TileData { material = MaterialType.METAL, electricEnergy = 80f };
            var neighbors = TestHelpers.NeutralNeighbors();
            var defs      = new MaterialDefinition[] { TestHelpers.Metal(), TestHelpers.Metal(),
                                                       TestHelpers.Metal(), TestHelpers.Metal() };
            _rule.Apply(ref tile, neighbors, defs);
            foreach (var n in neighbors)
                Assert.IsTrue(n.dirty, "Neighbor should be marked dirty after electric propagation");
        }

        [Test]
        public void Apply_ZeroTransferCoeff_NoEnergyLeaks()
        {
            var tile      = new TileData { material = MaterialType.METAL, electricEnergy = 80f };
            var neighbors = TestHelpers.NeutralNeighbors();
            var defs      = new MaterialDefinition[] { TestHelpers.Empty(), TestHelpers.Empty(),
                                                       TestHelpers.Empty(), TestHelpers.Empty() };
            _rule.Apply(ref tile, neighbors, defs);
            foreach (var n in neighbors)
                Assert.AreEqual(0f, n.electricEnergy, TestHelpers.Epsilon,
                    "Zero transferCoeff should not propagate energy");
        }
    }

    // =========================================================================
    // R04 — Electric Water
    // =========================================================================
    [TestFixture]
    public class R04_ElectricWaterTests
    {
        private R04_ElectricWater _rule;

        [SetUp] public void SetUp() => _rule = new R04_ElectricWater();

        [Test]
        public void CanApply_ReturnsFalse_WhenNotWater()
        {
            var tile = new TileData { material = MaterialType.METAL, electricEnergy = 50f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Metal()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenEnergyBelow40()
        {
            var tile = new TileData { material = MaterialType.WATER, electricEnergy = 39f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Water()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenWaterAndHighEnergy()
        {
            var tile = new TileData { material = MaterialType.WATER, electricEnergy = 50f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Water()));
        }

        [Test]
        public void Apply_IncreasesTemperature()
        {
            var tile = new TileData { material = MaterialType.WATER, electricEnergy = 60f, temperature = 10f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.temperature, 10f);
        }

        [Test]
        public void Apply_DecreasesElectricEnergy()
        {
            var tile = new TileData { material = MaterialType.WATER, electricEnergy = 60f, temperature = 10f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.electricEnergy, 60f);
        }

        [Test]
        public void Apply_TemperatureDelta_MatchesFormula()
        {
            float energy = 60f;
            var tile = new TileData { material = MaterialType.WATER, electricEnergy = energy, temperature = 10f };
            float expected = Mathf.Clamp(10f + energy * 0.3f, 0f, 100f);
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(expected, tile.temperature, TestHelpers.Epsilon);
        }
    }

    // =========================================================================
    // R05 — Pressure Explosion
    // =========================================================================
    [TestFixture]
    public class R05_PressureExplosionTests
    {
        private R05_PressureExplosion _rule;

        [SetUp] public void SetUp() => _rule = new R05_PressureExplosion();

        [Test]
        public void CanApply_ReturnsFalse_WhenPressureBelow80()
        {
            var tile      = new TileData { pressure = 79f };
            var neighbors = BuildHighIntegrityNeighbors();
            Assert.IsFalse(_rule.CanApply(tile, neighbors, TestHelpers.Stone()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenAnyNeighborIntegrityBelow60()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildHighIntegrityNeighbors();
            neighbors[0].structuralIntegrity = 59f; // uno débil
            Assert.IsFalse(_rule.CanApply(tile, neighbors, TestHelpers.Stone()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenHighPressureAndStrongNeighbors()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildHighIntegrityNeighbors();
            Assert.IsTrue(_rule.CanApply(tile, neighbors, TestHelpers.Stone()));
        }

        [Test]
        public void Apply_ReducesTilePressure()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildHighIntegrityNeighbors();
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.pressure, 90f);
        }

        [Test]
        public void Apply_DamagesNeighborIntegrity()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildHighIntegrityNeighbors();
            float before  = neighbors[0].structuralIntegrity;
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            Assert.Less(neighbors[0].structuralIntegrity, before);
        }

        [Test]
        public void Apply_IncreasesNeighborTemperature()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildHighIntegrityNeighbors();
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            foreach (var n in neighbors)
                Assert.Greater(n.temperature, 0f);
        }

        private TileData[] BuildHighIntegrityNeighbors()
        {
            var n = new TileData[4];
            for (int i = 0; i < 4; i++)
                n[i] = new TileData { material = MaterialType.STONE, structuralIntegrity = 80f };
            return n;
        }
    }

    // =========================================================================
    // R06 — Pressure Release
    // =========================================================================
    [TestFixture]
    public class R06_PressureReleaseTests
    {
        private R06_PressureRelease _rule;

        [SetUp] public void SetUp() => _rule = new R06_PressureRelease();

        [Test]
        public void CanApply_ReturnsFalse_WhenPressureBelow80()
        {
            var tile      = new TileData { pressure = 79f };
            var neighbors = BuildWeakNeighbors();
            Assert.IsFalse(_rule.CanApply(tile, neighbors, TestHelpers.Stone()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenAllNeighborsStrong()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = TestHelpers.NeutralNeighbors(); // integrity = 50, que es < 60
            // Forzamos todos con integrity >= 60
            for (int i = 0; i < neighbors.Length; i++)
                neighbors[i].structuralIntegrity = 60f;
            Assert.IsFalse(_rule.CanApply(tile, neighbors, TestHelpers.Stone()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenHighPressureAndWeakNeighbor()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildWeakNeighbors();
            Assert.IsTrue(_rule.CanApply(tile, neighbors, TestHelpers.Stone()));
        }

        [Test]
        public void Apply_ReducesTilePressure()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildWeakNeighbors();
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.pressure, 90f);
        }

        [Test]
        public void Apply_TransfersPressureToWeakestNeighbor()
        {
            var tile      = new TileData { pressure = 90f };
            var neighbors = BuildWeakNeighbors();
            // El más débil tiene integrity=20 (índice 0)
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            Assert.Greater(neighbors[0].pressure, 0f, "Weakest neighbor should receive pressure");
        }

        private TileData[] BuildWeakNeighbors()
        {
            var n = new TileData[4];
            n[0] = new TileData { structuralIntegrity = 20f }; // el más débil
            n[1] = new TileData { structuralIntegrity = 40f };
            n[2] = new TileData { structuralIntegrity = 70f };
            n[3] = new TileData { structuralIntegrity = 80f };
            return n;
        }
    }

    // =========================================================================
    // R07 — Structural Collapse
    // =========================================================================
    [TestFixture]
    public class R07_StructuralCollapseTests
    {
        private R07_StructuralCollapse _rule;

        [SetUp] public void SetUp() => _rule = new R07_StructuralCollapse();

        [Test]
        public void CanApply_ReturnsFalse_WhenIntegrityAbove10()
        {
            var tile = new TileData { structuralIntegrity = 11f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenIntegrityBelow10()
        {
            var tile = new TileData { structuralIntegrity = 5f, material = MaterialType.WOOD };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void Apply_SetsMaterialToEmpty()
        {
            var tile = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(MaterialType.EMPTY, tile.material);
        }

        [Test]
        public void Apply_SetsIntegrityToZero()
        {
            var tile = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(0f, tile.structuralIntegrity, TestHelpers.Epsilon);
        }

        [Test]
        public void Apply_IncreasesNeighborPressure()
        {
            var tile      = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f };
            var neighbors = TestHelpers.NeutralNeighbors();
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            foreach (var n in neighbors)
                Assert.Greater(n.pressure, 0f, "Collapse should pressurize neighbors");
        }

        [Test]
        public void Apply_SetsWasEmpty_WhenMaterialWasNotEmpty()
        {
            var tile = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            // wasEmpty debe ser false porque el material era WOOD (no estaba vacío antes)
            Assert.IsFalse(tile.wasEmpty);
        }

        [Test]
        public void Apply_MarkNeighborsDirty()
        {
            var tile      = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f };
            var neighbors = TestHelpers.NeutralNeighbors();
            _rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            foreach (var n in neighbors)
                Assert.IsTrue(n.dirty);
        }
    }

    // =========================================================================
    // R08 — Humidity Vaporization
    // =========================================================================
    [TestFixture]
    public class R08_HumidityVaporizationTests
    {
        private R08_HumidityVaporization _rule;

        [SetUp] public void SetUp() => _rule = new R08_HumidityVaporization();

        [Test]
        public void CanApply_ReturnsFalse_WhenHumidityBelow60()
        {
            var tile = new TileData { humidity = 59f, temperature = 60f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenTempBelow50()
        {
            var tile = new TileData { humidity = 70f, temperature = 49f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenBothConditionsMet()
        {
            var tile = new TileData { humidity = 70f, temperature = 60f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void Apply_DecreasesHumidity()
        {
            var tile = new TileData { humidity = 70f, temperature = 60f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.humidity, 70f);
        }

        [Test]
        public void Apply_IncreasesPressure()
        {
            var tile = new TileData { humidity = 70f, temperature = 60f, pressure = 0f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.pressure, 0f);
        }

        [Test]
        public void Apply_IncreasesGasDensity()
        {
            var tile = new TileData { humidity = 70f, temperature = 60f, gasDensity = 0f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.gasDensity, 0f);
        }
    }

    // =========================================================================
    // R09 — Heat Suppression
    // =========================================================================
    [TestFixture]
    public class R09_HeatSuppressionTests
    {
        private R09_HeatSuppression _rule;

        [SetUp] public void SetUp() => _rule = new R09_HeatSuppression();

        [Test]
        public void CanApply_ReturnsFalse_WhenTempBelow70()
        {
            var tile = new TileData { temperature = 69f, humidity = 60f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenHumidityBelow50()
        {
            var tile = new TileData { temperature = 80f, humidity = 49f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenHotAndHumid()
        {
            var tile = new TileData { temperature = 80f, humidity = 60f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Wood()));
        }

        [Test]
        public void Apply_DecreasesTemperature()
        {
            var tile = new TileData { temperature = 80f, humidity = 60f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.temperature, 80f);
        }

        [Test]
        public void Apply_DecreasesHumidity()
        {
            var tile = new TileData { temperature = 80f, humidity = 60f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.humidity, 60f);
        }

        [Test]
        public void Apply_TempReduction_ScalesWithHumidity()
        {
            float humidity = 60f;
            float temp     = 80f;
            float expected = Mathf.Clamp(temp - humidity * 0.3f, 0f, 100f);

            var tile = new TileData { temperature = temp, humidity = humidity };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(expected, tile.temperature, TestHelpers.Epsilon);
        }
    }

    // =========================================================================
    // R10 — Gas Ignition
    // =========================================================================
    [TestFixture]
    public class R10_GasIgnitionTests
    {
        private R10_GasIgnition _rule;

        [SetUp] public void SetUp() => _rule = new R10_GasIgnition();

        [Test]
        public void CanApply_ReturnsFalse_WhenGasBelow60()
        {
            var tile = new TileData { gasDensity = 59f, temperature = 70f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Gas()));
        }

        [Test]
        public void CanApply_ReturnsFalse_WhenTempBelow60()
        {
            var tile = new TileData { gasDensity = 70f, temperature = 59f };
            Assert.IsFalse(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Gas()));
        }

        [Test]
        public void CanApply_ReturnsTrue_WhenDenseAndHot()
        {
            var tile = new TileData { gasDensity = 70f, temperature = 65f };
            Assert.IsTrue(_rule.CanApply(tile, TestHelpers.NeutralNeighbors(), TestHelpers.Gas()));
        }

        [Test]
        public void Apply_IncreasesTemperature()
        {
            var tile = new TileData { gasDensity = 70f, temperature = 65f, pressure = 0f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.temperature, 65f);
        }

        [Test]
        public void Apply_DecreasesGasDensity()
        {
            var tile = new TileData { gasDensity = 70f, temperature = 65f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Less(tile.gasDensity, 70f);
        }

        [Test]
        public void Apply_IncreasesPressure()
        {
            var tile = new TileData { gasDensity = 70f, temperature = 65f, pressure = 0f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.Greater(tile.pressure, 0f);
        }

        [Test]
        public void Apply_TempDelta_MatchesFormula()
        {
            float gas  = 70f;
            float temp = 65f;
            float expected = Mathf.Clamp(temp + gas * 0.4f, 0f, 100f);

            var tile = new TileData { gasDensity = gas, temperature = temp, pressure = 0f };
            _rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            Assert.AreEqual(expected, tile.temperature, TestHelpers.Epsilon);
        }
    }

    // =========================================================================
    // Invariant: ninguna regla lee derivedStates (verificación en runtime)
    // =========================================================================
    [TestFixture]
    public class InvariantI1_NoDerivedStatesRead
    {
        /// <summary>
        /// Verifica que pasar derivedStates con flags activos no cambia el
        /// resultado de CanApply ni Apply respecto a tenerlos en NONE.
        /// Si alguna regla leyera derivedStates, el resultado sería distinto.
        /// </summary>
        [Test]
        public void R01_IgnoresDerivedStates()
        {
            var rule  = new R01_Combustion();
            var def   = TestHelpers.Wood();
            var n     = TestHelpers.NeutralNeighbors();

            var tileClean = new TileData { temperature = 80f, humidity = 20f, structuralIntegrity = 80f, derivedStates = States.StateFlags.NONE };
            var tileDirty = new TileData { temperature = 80f, humidity = 20f, structuralIntegrity = 80f, derivedStates = States.StateFlags.ON_FIRE | States.StateFlags.VOLATILE };

            Assert.AreEqual(rule.CanApply(tileClean, n, def), rule.CanApply(tileDirty, n, def),
                "R01 CanApply must not depend on derivedStates (I1 violation)");

            var ndClean = TestHelpers.NeutralNeighborDefs();
            var ndDirty = TestHelpers.NeutralNeighborDefs();
            rule.Apply(ref tileClean, n, ndClean);
            rule.Apply(ref tileDirty, n, ndDirty);

            Assert.AreEqual(tileClean.temperature,         tileDirty.temperature,         TestHelpers.Epsilon, "R01 Apply must not depend on derivedStates (I1)");
            Assert.AreEqual(tileClean.gasDensity,          tileDirty.gasDensity,           TestHelpers.Epsilon);
            Assert.AreEqual(tileClean.structuralIntegrity, tileDirty.structuralIntegrity,  TestHelpers.Epsilon);
        }

        [Test]
        public void R07_IgnoresDerivedStates()
        {
            var rule = new R07_StructuralCollapse();
            var def  = TestHelpers.Wood();
            var n    = TestHelpers.NeutralNeighbors();

            var tileClean = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f, derivedStates = States.StateFlags.NONE };
            var tileDirty = new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f, derivedStates = States.StateFlags.STRUCTURALLY_WEAK | States.StateFlags.COLLAPSED };

            Assert.AreEqual(rule.CanApply(tileClean, n, def), rule.CanApply(tileDirty, n, def),
                "R07 CanApply must not depend on derivedStates (I1 violation)");
        }
    }

    // =========================================================================
    // Invariant I4: todos los writes clampeados [0..100]
    // =========================================================================
    [TestFixture]
    public class InvariantI4_ClampTests
    {
        private const float OVER  = 200f;
        private const float UNDER = -100f;

        [Test]
        public void R01_NeverExceedsBounds_WhenStartingAtExtremes()
        {
            var rule = new R01_Combustion();
            var tile = new TileData { temperature = OVER, gasDensity = OVER, humidity = UNDER, structuralIntegrity = OVER };
            rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            AssertClamped(tile, "R01");
        }

        [Test]
        public void R08_NeverExceedsBounds_WhenStartingAtExtremes()
        {
            var rule = new R08_HumidityVaporization();
            var tile = new TileData { humidity = OVER, temperature = OVER, pressure = OVER, gasDensity = OVER };
            rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            AssertClamped(tile, "R08");
        }

        [Test]
        public void R10_NeverExceedsBounds_WhenStartingAtExtremes()
        {
            var rule = new R10_GasIgnition();
            var tile = new TileData { gasDensity = OVER, temperature = OVER, pressure = OVER };
            rule.Apply(ref tile, TestHelpers.NeutralNeighbors(), TestHelpers.NeutralNeighborDefs());
            AssertClamped(tile, "R10");
        }

        [Test]
        public void R05_NeighborProperties_NeverExceedBounds()
        {
            var rule      = new R05_PressureExplosion();
            var tile      = new TileData { pressure = OVER };
            var neighbors = new TileData[4];
            for (int i = 0; i < 4; i++)
                neighbors[i] = new TileData { structuralIntegrity = 80f, temperature = 0f, gasDensity = 0f };
            rule.Apply(ref tile, neighbors, TestHelpers.NeutralNeighborDefs());
            foreach (var n in neighbors)
                AssertClamped(n, "R05 neighbor");
        }

        private void AssertClamped(TileData t, string label)
        {
            Assert.LessOrEqual(t.temperature,          100f, $"{label}: temperature > 100");
            Assert.GreaterOrEqual(t.temperature,         0f, $"{label}: temperature < 0");
            Assert.LessOrEqual(t.pressure,             100f, $"{label}: pressure > 100");
            Assert.GreaterOrEqual(t.pressure,            0f, $"{label}: pressure < 0");
            Assert.LessOrEqual(t.humidity,             100f, $"{label}: humidity > 100");
            Assert.GreaterOrEqual(t.humidity,            0f, $"{label}: humidity < 0");
            Assert.LessOrEqual(t.electricEnergy,       100f, $"{label}: electricEnergy > 100");
            Assert.GreaterOrEqual(t.electricEnergy,      0f, $"{label}: electricEnergy < 0");
            Assert.LessOrEqual(t.gasDensity,           100f, $"{label}: gasDensity > 100");
            Assert.GreaterOrEqual(t.gasDensity,          0f, $"{label}: gasDensity < 0");
            Assert.LessOrEqual(t.structuralIntegrity,  100f, $"{label}: structuralIntegrity > 100");
            Assert.GreaterOrEqual(t.structuralIntegrity, 0f, $"{label}: structuralIntegrity < 0");
        }
    }
}