// Assets/PhysicsSystem/Rules/Rules/R06_PressureRelease.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R06 — PressureRelease (INTEGRITY)
    ///
    /// Cuando gasConcentration supera el umbral de explosión y hay un vecino debilitado,
    /// la presión se libera hacia ese vecino dañando su integridad estructural.
    /// Complementa R05: R05 explota cuando no hay escape, R06 libera cuando lo hay.
    /// </summary>
    public class R06_PressureRelease : IInteractionRule
    {
        public RuleID   Id       => RuleID.R06_PRESSURE_RELEASE;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        private const float ReleaseThreshold = 80f;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.gasConcentration <= ReleaseThreshold) return false;
            foreach (var n in neighbors)
                if (n.structuralIntegrity < 60f) return true;
            return false;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            // Encontrar el vecino más débil como punto de escape
            int   weakestIdx      = 0;
            float lowestIntegrity = float.MaxValue;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i].structuralIntegrity < lowestIntegrity)
                {
                    lowestIntegrity = neighbors[i].structuralIntegrity;
                    weakestIdx      = i;
                }
            }

            float flow = tile.gasConcentration * 0.6f;
            neighbors[weakestIdx].gasConcentration = Mathf.Clamp(
                neighbors[weakestIdx].gasConcentration + flow, 0f, 100f);
            neighbors[weakestIdx].structuralIntegrity = Mathf.Clamp(
                neighbors[weakestIdx].structuralIntegrity - 20f, 0f, 100f);
            tile.gasConcentration = Mathf.Clamp(tile.gasConcentration - flow, 0f, 100f);
        }
    }
}