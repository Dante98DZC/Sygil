using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;
using System.Collections.Generic;

namespace PhysicsSystem.Diffusion
{
    public enum GravityProperty { Humidity, GasDensity }

    /// <summary>
    /// GasDensity — isotropic spread: gas fills space equally, no directional bias.
    ///              Propagates gasMaterial alongside numeric density.
    /// Humidity   — height-based flow: fluid moves toward lower TileHeight neighbors.
    ///              Propagates liquidMaterial alongside numeric humidity.
    /// </summary>
    public class GravityDiffusion : IDiffusionStrategy
    {
        private readonly GravityProperty _property;

        public TickType TickType => TickType.SLOW;

        public GravityDiffusion(GravityProperty property)
        {
            _property = property;
        }

        public void Diffuse(PhysicsGrid grid, MaterialLibrary lib)
        {
            var activeTiles = new List<Vector2Int>(grid.ActiveTiles);

            // Snapshot numeric values — prevents double-transfer within same tick
            var snapshot = new Dictionary<Vector2Int, float>(activeTiles.Count);
            foreach (var pos in activeTiles)
                snapshot[pos] = GetValue(grid.GetTile(pos));

            foreach (var pos in activeTiles)
            {
                float sourceVal = snapshot[pos];

                if (_property == GravityProperty.GasDensity)
                {
                    if (Mathf.Abs(sourceVal - 50f) < 0.1f) continue;
                }
                else
                {
                    if (sourceVal <= 0f) continue;
                }

                ref var tile = ref grid.GetTile(pos);
                if (lib.Get(tile.material) == null) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var nDef = lib.Get(neighbor.material);
                    if (nDef == null) continue;

                    float neighborVal = snapshot.TryGetValue(npos, out float sv) ? sv : GetValue(neighbor);
                    float diff = sourceVal - neighborVal;
                    if (diff <= 0f) continue;

                    float bias     = ComputeBias(ref tile, ref neighbor);
                    float transfer = diff * nDef.gasPermeabilityCoeff * bias * 0.25f;
                    if (transfer <= 0f) continue;

                    AddValue(ref tile,     -transfer);
                    AddValue(ref neighbor,  transfer);
                    PropagateMaterial(ref tile, ref neighbor);
                    grid.MarkDirty(npos);
                }
            }
        }

        /// <summary>
        /// Gas: equal bias in all directions (top-down has no physical gravity axis).
        /// Fluid: flows toward lower TileHeight — higher delta = stronger flow.
        /// Scale 0.2f per height step: diff=1 → bias=0.7, diff=2 → bias=0.9, diff=-1 → bias=0.3.
        /// </summary>
        private float ComputeBias(ref TileData source, ref TileData neighbor)
        {
            if (_property == GravityProperty.GasDensity)
                return 0.25f;

            // TileHeight is int enum: Deep=-2 … Tall=3
            int heightDiff = (int)source.height - (int)neighbor.height;
            return Mathf.Clamp01(0.5f + heightDiff * 0.2f);
        }

        /// <summary>
        /// Copies gasMaterial/liquidMaterial from source to neighbor once enough
        /// numeric property has transferred. Clears source layer when depleted.
        /// </summary>
        private void PropagateMaterial(ref TileData source, ref TileData neighbor)
        {
            if (_property == GravityProperty.GasDensity)
            {
                if (source.gasMaterial != MaterialType.EMPTY && neighbor.gasMaterial == MaterialType.EMPTY)
                    if (neighbor.gasDensity >= 5f)
                        neighbor.gasMaterial = source.gasMaterial;

                if (source.gasDensity < 1f)
                    source.gasMaterial = MaterialType.EMPTY;
            }
            else
            {
                if (source.liquidMaterial != MaterialType.EMPTY && neighbor.liquidMaterial == MaterialType.EMPTY)
                    if (neighbor.liquidVolume >= 5f)
                        neighbor.liquidMaterial = source.liquidMaterial;

                if (source.liquidVolume < 1f)
                    source.liquidMaterial = MaterialType.EMPTY;
            }
        }

        private float GetValue(TileData t) =>
            _property == GravityProperty.Humidity ? t.liquidVolume : t.gasDensity;

        private void AddValue(ref TileData t, float delta)
        {
            if (_property == GravityProperty.Humidity)
                t.liquidVolume = Mathf.Clamp(t.liquidVolume + delta, 0f, 100f);
            else
                t.gasDensity = Mathf.Clamp(t.gasDensity + delta, 0f, 100f);
        }
    }
}