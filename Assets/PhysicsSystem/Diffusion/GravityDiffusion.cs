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

        [SerializeField] private float _dissipationThreshold = 5f;

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
                var tileMat = _property == GravityProperty.GasDensity ? tile.gasMaterial : tile.liquidMaterial;
                if (lib.Get(tileMat) == null) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var neighborMat = _property == GravityProperty.GasDensity ? neighbor.gasMaterial : neighbor.liquidMaterial;
                    var nDef = lib.Get(neighborMat);
                    if (nDef == null) continue;

                    float neighborVal = snapshot.TryGetValue(npos, out float sv) ? sv : GetValue(neighbor);
                    float diff = sourceVal - neighborVal;
                    if (diff <= 0f) continue;

                    Vector2Int direction = npos - pos;
                    float bias     = ComputeBias(ref tile, ref neighbor, direction);
                    float neighborViscosity = nDef?.viscosity ?? 1f;
                    float transfer = diff * nDef.gasPermeabilityCoeff * neighborViscosity * bias * 0.25f;
                    if (transfer <= 0f) continue;

                    AddValue(ref tile,     -transfer);
                    AddValue(ref neighbor,  transfer);
                    PropagateMaterial(ref tile, ref neighbor);
                    grid.MarkDirty(npos);
                }
            }

            // Disipación post-difusión para gas en atmósfera abierta
            if (_property == GravityProperty.GasDensity)
            {
                ApplyDissipation(grid, lib);
            }
        }

        /// <summary>
        /// Gas: directional bias — gas rises more easily (direction.y < 0 = up gets bias 0.5).
        /// Fluid: flows toward lower TileHeight — higher delta = stronger flow.
        /// Scale 0.2f per height step: diff=1 → bias=0.7, diff=2 → bias=0.9, diff=-1 → bias=0.3.
        /// </summary>
        private float ComputeBias(ref TileData source, ref TileData neighbor, Vector2Int direction)
        {
            if (_property == GravityProperty.GasDensity)
            {
                if (direction.y < 0) return 0.50f;  // Arriba: sube por flotabilidad
                if (direction.y > 0) return 0.15f; // Abajo: resistido por gravedad
                return 0.40f;                     // Lateral: expansión horizontal
            }

            // TileHeight is int enum: Deep=-2 … Tall=3
            int heightDiff = (int)source.height - (int)neighbor.height;
            return Mathf.Clamp01(0.5f + heightDiff * 0.2f);
        }

        /// <summary>
        /// Elimina gas con densidad baja en atmósfera abierta.
        /// La energía se considera perdida a la atmósfera.
        /// Aplica dissipationMultiplier por tipo de gas.
        /// </summary>
        private void ApplyDissipation(PhysicsGrid grid, MaterialLibrary lib)
        {
            foreach (var pos in grid.ActiveTiles)
            {
                ref var tile = ref grid.GetTile(pos);
                if (!tile.isAtmosphereOpen || tile.gasDensity < _dissipationThreshold)
                    continue;

                var matDef = lib.Get(tile.gasMaterial);
                float mult = matDef?.dissipationMultiplier ?? 1f;
                float amount = _dissipationThreshold * mult;

                tile.gasDensity -= amount;

                if (tile.gasDensity <= 0f)
                {
                    tile.gasDensity = 0f;
                    tile.gasMaterial = MaterialType.EMPTY;
                }
            }
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