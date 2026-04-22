// Assets/PhysicsSystem/Rules/Rules/R_Filtration.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Config;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R17 — Filtración de líquido al suelo.
    /// Un material poroso (structural.isPorous=true) absorbe líquido de la capa superior
    /// hasta alcanzar su capacidad de saturación (structural.soilSaturationCapacity).
    /// La velocidad de absorción escala inversamente con la viscosidad del líquido:
    /// líquidos más viscosos (LAVA, MUD) se absorben más lento.
    /// </summary>
    public class R_Filtration : IInteractionRule
    {
        private readonly MaterialLibrary _library;

        public RuleID Id => RuleID.R17_FILTRATION;
        public TickType TickType => TickType.SLOW;
        public int Priority => 4;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        public R_Filtration(MaterialLibrary library)
        {
            _library = library;
        }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.liquidMaterial == MaterialType.EMPTY) return false;
            if (tile.liquidVolume <= 0f) return false;

            var groundDef = _library.Get(tile.groundMaterial);
            if (groundDef == null || !groundDef.structural.isPorous) return false;

            return tile.soilMoisture < groundDef.structural.soilSaturationCapacity;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            var groundDef = _library.Get(tile.groundMaterial);
            var liquidDef = _library.Get(tile.liquidMaterial);

            // La viscosidad del líquido reduce la velocidad de absorción:
            // viscosity=0 (agua) → factor=1.0 · viscosity=0.9 (lava) → factor=0.1
            float viscosityFactor = 1f - (liquidDef?.fluid.viscosity ?? 0f);
            float absorption = groundDef.structural.soilAbsorptionRate * viscosityFactor;
            float available  = groundDef.structural.soilSaturationCapacity - tile.soilMoisture;
            float actual     = Mathf.Min(absorption, available, tile.liquidVolume);

            tile.liquidVolume  -= actual;
            tile.soilMoisture   = Mathf.Clamp(
                tile.soilMoisture + actual,
                0f,
                groundDef.structural.soilSaturationCapacity);

            if (tile.liquidVolume <= 0f)
                tile.liquidMaterial = MaterialType.EMPTY;
        }
    }
}