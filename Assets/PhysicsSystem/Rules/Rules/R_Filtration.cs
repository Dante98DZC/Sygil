// Assets/PhysicsSystem/Rules/Rules/R_Filtration.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Config;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R17 — Filtración de líquido al suelo.
    /// Un material poroso (isPorous=true) absorbe líquido de la capa superior
    /// hasta alcanzar su capacidad de saturación (soilSaturationCapacity).
    /// La velocidad de absorción escala con la viscosidad del líquido.
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

            return tile.soilMoisture < groundDef.fluid.soilSaturationCapacity;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            var groundDef = _library.Get(tile.groundMaterial);
            var liquidDef = _library.Get(tile.liquidMaterial);

            float absorption = groundDef.fluid.soilAbsorptionRate * (liquidDef?.fluid.viscosity ?? 1f);
            float available = groundDef.fluid.soilSaturationCapacity - tile.soilMoisture;
            float actual = Mathf.Min(absorption, available, tile.liquidVolume);

            tile.liquidVolume -= actual;
            tile.soilMoisture = Mathf.Clamp(tile.soilMoisture + actual, 0f, groundDef.fluid.soilSaturationCapacity);

            if (tile.liquidVolume <= 0f)
                tile.liquidMaterial = MaterialType.EMPTY;
        }
    }
}
