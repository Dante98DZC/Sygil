// Assets/PhysicsSystem/Rules/Rules/R10_GasIgnition.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R10 — GasIgnition (STANDARD)
    ///
    /// Cuando hay alta densidad de gas y temperatura elevada, el gas se ignita:
    /// libera calor, consume gas y genera una onda de presión.
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

        private const float GasDensityThreshold = 60f;
        private const float TemperatureThreshold = 60f;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def) =>
            tile.gasDensity > GasDensityThreshold && tile.temperature > TemperatureThreshold;

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            // El gas ignita: libera calor y genera onda de presión (gasDensity)
            tile.temperature = Mathf.Clamp(tile.temperature + tile.gasDensity * 0.4f, 0f, 100f);
            tile.gasDensity  = Mathf.Clamp(tile.gasDensity  - 15f + 10f,             0f, 100f);
            // -15 consumido + +10 de onda expansiva = neto -5 en el tile fuente

            // clamp_all
            tile.electricEnergy      = Mathf.Clamp(tile.electricEnergy,      0f, 100f);
            tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity, 0f, 100f);
            tile.liquidVolume        = Mathf.Clamp(tile.liquidVolume,        0f, tile.LiquidCapacity);
        }
    }
}