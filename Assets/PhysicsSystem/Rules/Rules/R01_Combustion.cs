// Assets/PhysicsSystem/Rules/Rules/R01_Combustion.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R01 — Combustion (STANDARD)
    ///
    /// Un material arde cuando su temperatura supera ignitionTemperature y su
    /// flammabilityCoeff es suficientemente alto.
    ///
    /// Efectos por tick de combustión:
    ///   - Establece ON_FIRE en derivedStates (para respuesta visual)
    ///   - Sube temperatura (el fuego genera calor)
    ///   - Establece gasMaterial a smokeForm (gases de combustión)
    ///   - Daña structuralIntegrity (el material se consume)
    ///
    /// La combustión completa (integridad → 0) activa R07 que escribe
    /// collapseInto en groundMaterial.
    /// </summary>
    public class R01_Combustion : IInteractionRule
    {
        public RuleID   Id       => RuleID.R01_COMBUSTION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 3;
        public MaterialLayer SourceLayer => MaterialLayer.Ground;

        private float _flammability;
        private MaterialType _smokeForm;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null) return false;
            if (!def.IsFlammable) return false;
            if (tile.temperature <= def.combustion.ignitionTemperature) return false;
            if (def.combustion.flammabilityCoeff <= 0.5f) return false;

            _flammability = def.combustion.flammabilityCoeff;
            _smokeForm   = def.combustion.smokeMaterial;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float f = _flammability;
            if (f <= 0f)
            {
                f = 0.5f;
            }

            var smoke = _smokeForm;
            if (smoke == MaterialType.EMPTY)
            {
                smoke = MaterialType.SMOKE;
            }

            tile.temperature         = Mathf.Clamp(tile.temperature         + 8f * f, 0f, 100f);
            tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity - 3f * f, 0f, 100f);

            if (smoke != MaterialType.EMPTY)
            {
                tile.gasMaterial  = smoke;
                tile.gasConcentration = Mathf.Clamp(tile.gasConcentration + 5f * f, 0f, 100f);
            }

            tile.derivedStates |= StateFlags.ON_FIRE;

            if (tile.structuralIntegrity <= 0f)
            {
                tile.derivedStates &= ~StateFlags.ON_FIRE;
            }

            tile.electricEnergy = Mathf.Clamp(tile.electricEnergy, 0f, 100f);
            tile.liquidVolume = Mathf.Clamp(tile.liquidVolume, 0f, tile.LiquidCapacity);
        }
    }
}