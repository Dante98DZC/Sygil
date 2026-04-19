// Assets/PhysicsSystem/Rules/Rules/R07_StructuralCollapse.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R07 — Colapso estructural. Dos triggers:
    ///
    ///   1. Integridad baja  — structuralIntegrity menor que 10
    ///      Resultado: collapseInto del MaterialDefinition (WOOD→EARTH, resto→EMPTY)
    ///
    ///   2. Fusión/calor extremo — hasMeltingPoint && temperature mayor que meltingTemperature
    ///      Resultado: meltInto del MaterialDefinition (normalmente EMPTY)
    ///
    /// Ambos triggers pressurizan vecinos y marcan dirty.
    /// </summary>
    public class R07_StructuralCollapse : IInteractionRule
    {
        public RuleID   Id       => RuleID.R07_STRUCTURAL_COLLAPSE;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 0;

        // Cacheado en CanApply para no recalcular en Apply
        private bool         _isMelting;
        private MaterialType _resultMaterial;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.material == MaterialType.EMPTY) return false;

            // Trigger 1: integridad baja
            if (tile.structuralIntegrity < 10f)
            {
                _isMelting      = false;
                _resultMaterial = def.collapseInto;
                return true;
            }

            // Trigger 2: fusión por temperatura
            if (def.hasMeltingPoint && tile.temperature > def.meltingTemperature)
            {
                _isMelting      = true;
                _resultMaterial = def.meltInto;
                return true;
            }

            return false;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.wasEmpty = (tile.material == MaterialType.EMPTY);

            // Presuriza vecinos — el colapso desplaza materia
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i].pressure = Mathf.Clamp(neighbors[i].pressure + 10f, 0f, 100f);
                neighbors[i].dirty    = true;
            }

            tile.material            = _resultMaterial;
            tile.structuralIntegrity = 0f;

            // Al fundirse también resetea temperatura para evitar loop infinito
            if (_isMelting)
                tile.temperature = 0f;
        }
    }
}