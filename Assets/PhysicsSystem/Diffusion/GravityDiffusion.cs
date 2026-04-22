using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;
using System.Collections.Generic;

#if UNITY_DEBUG
using System.Diagnostics;
#endif

namespace PhysicsSystem.Diffusion
{
    public enum GravityProperty { Humidity, GasConcentration }

    /// <summary>
    /// GasConcentration — isotropic spread with atmosphere exchange.
    /// Humidity        — height-based flow.
    ///
    /// El modelo de gas concentration:
    ///   0% = vacío absoluto
    ///   100% = tile saturado
    ///   La atmósfera exterior tiene atmosphereConcentration (normalmente 0%).
    ///   Tiles abiertos al exterior difunden hacia 0%, no hacia un baseline artificial.
    /// </summary>
    public class GravityDiffusion : IDiffusionStrategy
    {
        private readonly GravityProperty _property;
        private static readonly Dictionary<Vector2Int, float> _snapshotCache = new(128);

        public TickType TickType => TickType.SLOW;

        public GravityDiffusion(GravityProperty property)
        {
            _property = property;
        }

        public void Diffuse(PhysicsGrid grid, MaterialLibrary lib, SimulationConfig config)
        {
            if (grid.ActiveTiles.Count == 0) return;

            var activeTiles = new List<Vector2Int>(grid.ActiveTiles);
            float atmConcentration = config.atmosphereConcentration;
            float atmDiffusionRate = config.atmosphereDiffusionRate;
            float ventThreshold    = config.ventThreshold;
            float ventRate        = config.ventRate;
            const float FIXED_DELTA_TIME = 0.016f;
            int maxTiles = config.maxDiffusionTilesPerTick;

            _snapshotCache.Clear();
            foreach (var pos in activeTiles)
                _snapshotCache[pos] = GetValue(grid.GetTile(pos));

            if (activeTiles.Count > maxTiles)
                activeTiles.RemoveRange(maxTiles, activeTiles.Count - maxTiles);

            foreach (var pos in activeTiles)
            {
                ref var tile = ref grid.GetTile(pos);
                float sourceVal = _snapshotCache[pos];

                if (sourceVal <= 0.5f) continue;

                if (_property == GravityProperty.GasConcentration)
                {
                    if (!IsInteractiveGas(ref tile, lib) && sourceVal <= 0f) continue;
                }

                var tileMat = _property == GravityProperty.GasConcentration ? tile.gasMaterial : tile.liquidMaterial;
                var tileDef = lib.Get(tileMat);
                if (tileDef == null) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var neighborMat = _property == GravityProperty.GasConcentration ? neighbor.gasMaterial : neighbor.liquidMaterial;

                    float neighborVal = _snapshotCache.TryGetValue(npos, out float sv) ? sv : GetValue(neighbor);
                    float diff = sourceVal - neighborVal;
                    float absDiff = Mathf.Abs(diff);
                    if (absDiff < 0.5f) continue;

                    if (_property == GravityProperty.Humidity)
                    {
                        float neighborCapacity = neighbor.LiquidCapacity;
                        if (neighborCapacity > 0f && neighborVal >= neighborCapacity - 0.1f)
                            continue;
                    }

                    Vector2Int direction = npos - pos;
                    float bias = ComputeBias(ref tile, ref neighbor, direction);

                    float coeff = tileDef.GasPermeability;
                    if (neighborMat != MaterialType.EMPTY)
                    {
                        var nDef = lib.Get(neighborMat);
                        if (nDef != null)
                            coeff = Mathf.Min(coeff, nDef.GasPermeability);
                    }

                    float transfer = diff * coeff * bias * absDiff * 0.1f;
                    transfer = Mathf.Clamp(transfer, -10f, 10f);

                    AddValue(ref tile, -transfer);
                    AddValue(ref neighbor, transfer);
                    PropagateMaterial(ref tile, ref neighbor);
                    grid.MarkDirty(npos);
                }

                if (_property == GravityProperty.GasConcentration && tile.isAtmosphereOpen)
                {
                    float atmDiff = sourceVal - atmConcentration;
                    float absAtmDiff = Mathf.Abs(atmDiff);

                    if (absAtmDiff > 0.5f)
                    {
                        float exchange = atmDiff * atmDiffusionRate * absAtmDiff * 0.1f;
                        exchange = Mathf.Clamp(exchange, -10f, 10f);
                        AddValue(ref tile, -exchange);

                        if (sourceVal > ventThreshold)
                        {
                            float excess     = sourceVal - atmConcentration;
                            float ventAmount = Mathf.Min(excess, ventRate * FIXED_DELTA_TIME);
                            AddValue(ref tile, -ventAmount);
                            grid.MarkDirty(pos);

                            if (tile.gasConcentration < 1f)
                                tile.gasMaterial = MaterialType.EMPTY;
                        }
                        else if (tile.gasMaterial == MaterialType.EMPTY && sourceVal > atmConcentration + 5f)
                        {
                            tile.gasMaterial = config.atmosphereGas;
                        }
                    }
                }
            }

            if (_property == GravityProperty.GasConcentration)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var topPos = new Vector2Int(x, grid.Height - 1);
                    ref var topTile = ref grid.GetTile(topPos);
                    if (!topTile.isAtmosphereOpen) continue;

                    float concentration = topTile.gasConcentration;
                    if (concentration > ventThreshold)
                    {
                        float excess     = concentration - atmConcentration;
                        float ventAmount = Mathf.Min(excess, ventRate * FIXED_DELTA_TIME);
                        topTile.gasConcentration = Mathf.Max(atmConcentration, concentration - ventAmount);
                        if (topTile.gasConcentration < 1f)
                            topTile.gasMaterial = MaterialType.EMPTY;
                        grid.MarkDirty(topPos);
                    }
                }
            }
        }

        private bool IsInteractiveGas(ref TileData tile, MaterialLibrary lib)
        {
            if (tile.gasMaterial == MaterialType.EMPTY) return false;
            var def = lib.Get(tile.gasMaterial);
            return def != null && def.MatterState == MatterState.Gas;
        }

        private float ComputeBias(ref TileData source, ref TileData neighbor, Vector2Int direction)
        {
            if (_property == GravityProperty.GasConcentration)
            {
                if (direction.y < 0) return 0.50f;
                if (direction.y > 0) return 0.15f;
                return 0.40f;
            }

            int heightDiff = (int)source.height - (int)neighbor.height;
            return Mathf.Clamp01(0.5f + heightDiff * 0.2f);
        }

        private void PropagateMaterial(ref TileData source, ref TileData neighbor)
        {
            if (_property == GravityProperty.GasConcentration)
            {
                if (source.gasMaterial != MaterialType.EMPTY && neighbor.gasMaterial == MaterialType.EMPTY)
                    if (neighbor.gasConcentration >= 5f)
                        neighbor.gasMaterial = source.gasMaterial;

                if (source.gasConcentration < 1f)
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
            _property == GravityProperty.Humidity ? t.liquidVolume : t.gasConcentration;

        private void AddValue(ref TileData t, float delta)
        {
            if (_property == GravityProperty.Humidity)
                t.liquidVolume = Mathf.Clamp(t.liquidVolume + delta, 0f, 100f);
            else
                t.gasConcentration = Mathf.Clamp(t.gasConcentration + delta, 0f, 100f);
        }

#if UNITY_DEBUG
        [Conditional("DEBUG")]
        private void LogTransfer(Vector2Int pos, Vector2Int npos, float amount)
        {
            string propName = _property == GravityProperty.Humidity ? "Humidity" : "Gas";
            Debug.Log($"[Diffusion] {propName}: {pos} �� {npos}: {amount:F2}");
        }
#endif
    }
}