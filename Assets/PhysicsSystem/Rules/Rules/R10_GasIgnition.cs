// Assets/PhysicsSystem/Rules/Rules/R10_GasIgnition.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R10 — GasIgnition (STANDARD)
    ///
    /// Cuando hay alta concentración de gas INFLAMABLE y temperatura elevada, el gas se ignita:
    /// libera calor, consume gas y genera una onda de presión.
    ///
    /// Solo aplica a gases con IsFlammableGas = true (ROCK_GAS).
    /// No aplica a SMOKE, STEAM, CO2, AIR.
    ///
    /// La onda de presión se modela subiendo gasConcentration de vecinos.
    /// Umbral configurable desde SimulationConfig.gasIgnitionThreshold.
    /// </summary>
    public class R10_GasIgnition : IInteractionRule
    {
        public RuleID   Id       => RuleID.R10_GAS_IGNITION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        private const float TemperatureThreshold = 60f;

        private readonly float _concentrationThreshold;
        private MaterialDefinition _gasDef;

        public R10_GasIgnition(float concentrationThreshold = 10f)
        {
            _concentrationThreshold = concentrationThreshold;
        }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.gasConcentration < _concentrationThreshold) return false;
            if (tile.temperature <= TemperatureThreshold) return false;
            if (tile.gasMaterial == MaterialType.EMPTY) return false;

            if (def == null || !def.IsFlammableGas) return false;

            _gasDef = def;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float massConsumed = Mathf.Min(tile.gasConcentration, 15f);
            float heatReleased = massConsumed * 0.4f;

            tile.temperature = Mathf.Clamp(tile.temperature + heatReleased, 0f, 100f);
            tile.gasConcentration = Mathf.Clamp(tile.gasConcentration - massConsumed, 0f, 100f);

            float pressureWave = massConsumed * 0.3f;
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i].gasConcentration = Mathf.Clamp(
                    neighbors[i].gasConcentration + pressureWave, 0f, 100f);
            }

            if (tile.gasConcentration < 1f)
                tile.gasMaterial = MaterialType.EMPTY;
        }
    }
}