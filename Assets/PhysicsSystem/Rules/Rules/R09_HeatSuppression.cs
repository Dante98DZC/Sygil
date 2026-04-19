// Assets/PhysicsSystem/Rules/Rules/R09_HeatSuppression.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R09 — HeatSuppression (STANDARD)
    ///
    /// El líquido presente en un tile caliente absorbe temperatura.
    /// Modela el efecto de enfriamiento del agua sobre superficies calientes:
    /// chorros de agua apagando fuego, líquido refrigerando metales, etc.
    ///
    /// Condición: temperatura > 70 + hay suficiente líquido (> 50 litros).
    /// Efecto: reduce temperatura proporcional al volumen de líquido,
    /// consume parte del líquido (evaporado por el calor).
    /// </summary>
    public class R09_HeatSuppression : IInteractionRule
    {
        public RuleID   Id       => RuleID.R09_HEAT_SUPPRESSION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 4;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private const float TemperatureThreshold = 70f;
        private const float MinLiquidVolume      = 50f;   // mínimo efectivo
        private const float LiquidConsumedRate   = 10f;   // litros consumidos por tick
        private const float CoolingFactor        = 0.003f; // enfriamiento por litro

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def) =>
            tile.temperature    > TemperatureThreshold &&
            tile.liquidMaterial != MaterialType.EMPTY  &&
            tile.liquidVolume   > MinLiquidVolume;

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            // El enfriamiento escala con el volumen de líquido disponible
            float cooling = tile.liquidVolume * CoolingFactor;
            tile.temperature = Mathf.Clamp(tile.temperature - cooling, 0f, 100f);

            // El calor consume parte del líquido (evapora)
            tile.liquidVolume = Mathf.Clamp(tile.liquidVolume - LiquidConsumedRate, 0f, tile.LiquidCapacity);

            if (tile.liquidVolume <= 0f)
                tile.liquidMaterial = MaterialType.EMPTY;

            // clamp_all — propiedades no modificadas por esta regla
            tile.gasDensity          = Mathf.Clamp(tile.gasDensity,          0f, 100f);
            tile.electricEnergy      = Mathf.Clamp(tile.electricEnergy,      0f, 100f);
            tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity, 0f, 100f);
        }
    }
}