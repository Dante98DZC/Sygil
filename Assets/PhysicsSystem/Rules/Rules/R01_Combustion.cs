// Assets/PhysicsSystem/Rules/Rules/R01_Combustion.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R01 — Combustion (STANDARD)
    ///
    /// Un material arde cuando su temperatura supera ignitionTemperature y su
    /// flammabilityCoeff es suficientemente alto.
    ///
    /// Efectos por tick de combustión:
    ///   - Sube temperatura (el fuego genera calor)
    ///   - Sube gasDensity (gases de combustión)
    ///   - Daña structuralIntegrity (el material se consume)
    ///
    /// La combustión completa (integridad → 0) activa R07 que escribe
    /// burnInto en groundMaterial y smokeForm en gasMaterial.
    ///
    /// v4: elimina escrituras de humidity y pressure (campos removidos).
    /// </summary>
    public class R01_Combustion : IInteractionRule
    {
        public RuleID   Id       => RuleID.R01_COMBUSTION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 3;

        private float _flammability;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def.ignitionTemperature <= 0f)               return false;
            if (tile.temperature <= def.ignitionTemperature)  return false;
            if (def.flammabilityCoeff   <= 0.5f)             return false;

            _flammability = def.flammabilityCoeff;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float f = _flammability;

            tile.temperature         = Mathf.Clamp(tile.temperature         + 5f * f, 0f, 100f);
            tile.gasDensity          = Mathf.Clamp(tile.gasDensity          + 3f * f, 0f, 100f);
            tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity - 2f * f, 0f, 100f);

            // clamp_all — propiedades no modificadas por esta regla
            tile.electricEnergy = Mathf.Clamp(tile.electricEnergy, 0f, 100f);
            tile.liquidVolume   = Mathf.Clamp(tile.liquidVolume,   0f, tile.LiquidCapacity);
        }
    }
}