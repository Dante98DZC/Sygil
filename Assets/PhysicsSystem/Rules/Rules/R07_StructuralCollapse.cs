// Assets/PhysicsSystem/Rules/Rules/R07_StructuralCollapse.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R07 — Colapso estructural. Trigger:
    ///
    ///   Integridad baja  — structuralIntegrity menor que threshold (10)
    ///   Resultado: collapseInto del MaterialDefinition (WOOD→EARTH, resto→EMPTY)
    ///
    /// PRESERVADO: La lógica de fusión fue movida a R13 (Melting).
    /// Este regla ahora maneja solo colapso estructural.
    /// </summary>
    public class R07_StructuralCollapse : IInteractionRule
    {
        public RuleID   Id       => RuleID.R07_STRUCTURAL_COLLAPSE;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 0;
        public MaterialLayer SourceLayer => MaterialLayer.Ground;

        private MaterialType _resultMaterial;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.groundMaterial == MaterialType.EMPTY) return false;

            if (tile.structuralIntegrity < 10f)
            {
                _resultMaterial = def.structural.collapseInto;
                return true;
            }

            return false;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.wasEmpty = (tile.groundMaterial == MaterialType.EMPTY);

            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i].gasDensity = Mathf.Clamp(neighbors[i].gasDensity + 10f, 0f, 100f);
                neighbors[i].dirty    = true;
            }

            tile.groundMaterial       = _resultMaterial;
            tile.structuralIntegrity = 0f;
        }
    }
}