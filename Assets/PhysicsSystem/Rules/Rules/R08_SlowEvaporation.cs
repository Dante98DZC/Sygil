// Assets/PhysicsSystem/Rules/Rules/R08_HumidityVaporization.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R08 — SlowEvaporation (STANDARD)
    ///
    /// Antes: "HumidityVaporization" operaba sobre el campo humidity (eliminado en v4).
    /// Ahora: evaporación lenta de líquido a temperatura moderada, por debajo del
    /// punto de ebullición. R15 Boiling maneja la ebullición completa.
    ///
    /// Condición: hay líquido presente + temperatura > 50 (calor moderado).
    /// Efecto: reduce liquidVolume gradualmente, aumenta gasDensity y agrega gasMaterial=STEAM
    /// si el slot de gas está vacío.
    /// </summary>
    public class R08_SlowEvaporation : IInteractionRule
    {
        public RuleID   Id       => RuleID.R08_HUMIDITY_VAPORIZATION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 5;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private const float TemperatureThreshold = 50f;
        private const float MinLiquidVolume      = 10f;   // mínimo para activar
        private const float EvaporationRate      = 5f;    // litros por tick
        private const float GasDensityGain       = 2f;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def) =>
            tile.liquidMaterial != MaterialType.EMPTY &&
            tile.liquidVolume   >= MinLiquidVolume    &&
            tile.temperature    >  TemperatureThreshold;

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            // Evaporar una fracción del líquido
            tile.liquidVolume = Mathf.Clamp(tile.liquidVolume - EvaporationRate, 0f, tile.LiquidCapacity);

            // Limpiar el slot si el volumen llegó a cero
            if (tile.liquidVolume <= 0f)
                tile.liquidMaterial = MaterialType.EMPTY;

            // El vapor aumenta la densidad atmosférica
            tile.gasDensity = Mathf.Clamp(tile.gasDensity + GasDensityGain, 0f, 100f);

            // Si el slot de gas está libre, producir vapor
            if (tile.gasMaterial == MaterialType.EMPTY)
                tile.gasMaterial = MaterialType.STEAM;

            // clamp_all — propiedades no modificadas por esta regla
            tile.temperature         = Mathf.Clamp(tile.temperature,         0f, 100f);
            tile.electricEnergy      = Mathf.Clamp(tile.electricEnergy,      0f, 100f);
            tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity, 0f, 100f);
        }
    }
}