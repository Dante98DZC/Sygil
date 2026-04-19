// Assets/PhysicsSystem/Rules/Rules/R05_PressureExplosion.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R05 — PressureExplosion / Implosion (INTEGRITY)
    ///
    /// gasDensity como proxy de presión (baseline = 50 ≈ 1 atm):
    ///
    ///   gasDensity > 80  →  explosión: daña vecinos, sube temperatura y gasDensity
    ///   gasDensity < 20  →  implosión: colapsa materiales frágiles (vacío parcial)
    ///
    /// La implosión ocurre cuando gasDensity cae muy por debajo del baseline,
    /// generando presión diferencial negativa. Los materiales resistentes
    /// (METAL, STONE) aguantan; los frágiles (WOOD, GLASS) colapsan.
    /// </summary>
    public class R05_PressureExplosion : IInteractionRule
    {
        public RuleID   Id       => RuleID.R05_PRESSURE_EXPLOSION;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 1;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        private const float ExplosionThreshold = 80f;
        private const float ImplosionThreshold = 20f;
        private const float ImplosionDamage    = 40f;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (tile.gasDensity > ExplosionThreshold)
            {
                foreach (var n in neighbors)
                    if (n.structuralIntegrity <= 60f) return false;
                return true;
            }

            if (tile.gasDensity < ImplosionThreshold)
                return def != null && IsFragile(def.materialType);

            return false;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            if (tile.gasDensity > ExplosionThreshold)
                ApplyExplosion(ref tile, neighbors);
            else
                ApplyImplosion(ref tile);
        }

        private static void ApplyExplosion(ref TileData tile, TileData[] neighbors)
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i].structuralIntegrity = Mathf.Clamp(
                    neighbors[i].structuralIntegrity - tile.gasDensity * 0.4f, 0f, 100f);
                neighbors[i].temperature = Mathf.Clamp(
                    neighbors[i].temperature + tile.gasDensity * 0.2f, 0f, 100f);
                neighbors[i].gasDensity = Mathf.Clamp(
                    neighbors[i].gasDensity + tile.gasDensity * 0.3f, 0f, 100f);
            }
            tile.gasDensity = Mathf.Clamp(tile.gasDensity - 60f, 0f, 100f);
        }

        private static void ApplyImplosion(ref TileData tile)
        {
            tile.structuralIntegrity = Mathf.Clamp(
                tile.structuralIntegrity - ImplosionDamage, 0f, 100f);

            // El vacío parcial se relaja tras el colapso estructural
            tile.gasDensity = Mathf.Clamp(tile.gasDensity + 10f, 0f, 100f);
        }

        private static bool IsFragile(MaterialType mat) =>
            mat == MaterialType.WOOD  ||
            mat == MaterialType.GLASS ||
            mat == MaterialType.GAS   ||
            mat == MaterialType.WATER;
    }
}