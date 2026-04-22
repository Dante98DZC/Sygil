// Assets/PhysicsSystem/Rules/Rules/R10_GasIgnition.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R10 — GasIgnition (STANDARD)
    ///
    /// Cuando hay alta densidad de gas INFLAMABLE y temperatura elevada, el gas se ignita:
    /// libera calor, consume gas y genera una onda de presión.
    ///
    /// Solo aplica a gases con isFlammableGas = true (GAS, ROCK_GAS).
    /// No aplica a SMOKE, STEAM, CO2, AIR.
    ///
    /// La onda de presión se modela subiendo gasDensity de vecinos (via difusión
    /// normal) en lugar del campo pressure eliminado en v4. El exceso de gasDensity
    /// en el tile fuente decaerá hacia el baseline en el siguiente DecaySystem tick.
    ///
    /// v4: elimina escritura en pressure (campo removido). El +10 de presurización
    /// pasa a gasDensity, que es el proxy de presión en v4.
    /// </summary>
    public class R10_GasIgnition : IInteractionRule
    {
        public RuleID   Id       => RuleID.R10_GAS_IGNITION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        private const float GasDensityThreshold = 60f;
        private const float TemperatureThreshold = 60f;

        private MaterialDefinition _gasDef;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.gasDensity <= GasDensityThreshold) return false;
            if (tile.temperature <= TemperatureThreshold) return false;
            if (tile.gasMaterial == MaterialType.EMPTY) return false;

            if (def == null || !def.isFlammableGas) return false;

            _gasDef = def;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float massConsumed = Mathf.Min(tile.gasDensity, 15f);
            float heatReleased = massConsumed * 0.4f;

            tile.temperature = Mathf.Clamp(tile.temperature + heatReleased, 0f, 100f);
            tile.gasDensity  = Mathf.Clamp(tile.gasDensity  - massConsumed, 0f, 100f);

            float pressureWave = massConsumed * 0.3f;
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i].gasDensity = Mathf.Clamp(
                    neighbors[i].gasDensity + pressureWave, 0f, 100f);
            }

            if (tile.gasDensity < 1f)
                tile.gasMaterial = MaterialType.EMPTY;
        }
    }
}