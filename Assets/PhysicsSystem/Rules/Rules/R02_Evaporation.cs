// Assets/PhysicsSystem/Rules/Rules/R02_Evaporation.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R02 — Evaporation (INTEGRITY)
    ///
    /// WATER en liquidMaterial a temperatura muy alta se evapora por completo.
    /// Actúa como fallback para WATER sin MaterialDefinition configurada con
    /// boilingPoint, o para evaporación instantánea a temperatura extrema (> 80).
    ///
    /// R15 Boiling es la ruta preferida cuando existe un MaterialDefinition con
    /// boilingPoint configurado — este es el caso normal de WATER→STEAM.
    /// R08 SlowEvaporation maneja la evaporación gradual a temperatura moderada.
    ///
    /// TD-11: candidata a eliminar cuando todos los MaterialDef de líquidos
    /// tengan boilingPoint configurado y R15 cubra todos los casos.
    ///
    /// v4: usa liquidMaterial / liquidVolume. Elimina humidity y pressure
    /// (campos removidos). Presuriza vecinos via gasDensity.
    /// </summary>
    public class R02_Evaporation : IInteractionRule
    {
        public RuleID   Id       => RuleID.R02_EVAPORATION;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 6;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private const float EvaporationTemperature = 80f;
        private const float NeighborDensityGain    = 15f;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def) =>
            tile.temperature    > EvaporationTemperature &&
            tile.liquidMaterial == MaterialType.WATER    &&
            tile.liquidVolume   > 0f;

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.temperature = Mathf.Clamp(tile.temperature - 10f, 0f, 100f);

            // El agua se evapora completamente
            tile.liquidMaterial = MaterialType.EMPTY;
            tile.liquidVolume   = 0f;

            // Producir vapor en el slot de gas si está libre
            if (tile.gasMaterial == MaterialType.EMPTY)
                tile.gasMaterial = MaterialType.STEAM;

            tile.gasDensity = Mathf.Clamp(tile.gasDensity + 10f, 0f, 100f);

            // El vapor presuriza los vecinos via gasDensity
            for (int i = 0; i < neighbors.Length; i++)
            {
                float htc = neighborDefs[i] != null ? neighborDefs[i].heatTransferCoeff : 0f;
                neighbors[i].gasDensity = Mathf.Clamp(
                    neighbors[i].gasDensity + NeighborDensityGain * htc, 0f, 100f);
            }
        }
    }
}