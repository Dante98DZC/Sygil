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
    public enum GravityProperty { Humidity, GasDensity }

    /// <summary>
    /// GasDensity — isotropic spread with atmosphere exchange.
    /// Humidity   — height-based flow.
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
            int maxTiles = 500;
            if (activeTiles.Count > maxTiles)
            {
                activeTiles.RemoveRange(maxTiles, activeTiles.Count - maxTiles);
            }

            float atmDensity = config.atmosphereDensity;
            float atmDiffusionRate = config.atmosphereDiffusionRate;

            _snapshotCache.Clear();
            foreach (var pos in activeTiles)
                _snapshotCache[pos] = GetValue(grid.GetTile(pos));

            foreach (var pos in activeTiles)
            {
                ref var tile = ref grid.GetTile(pos);
                float sourceVal = _snapshotCache[pos];

                // Condición de parada: volumen muy bajo = proceso innecesario
                if (sourceVal <= 0.5f) continue;

                if (_property == GravityProperty.GasDensity)
                {
                    if (!IsInteractiveGas(ref tile, lib) && sourceVal <= 0f) continue;
                }
                else if (sourceVal <= 0f) continue;

                var tileMat = _property == GravityProperty.GasDensity ? tile.gasMaterial : tile.liquidMaterial;
                var tileDef = lib.Get(tileMat);
                if (tileDef == null) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var neighborMat = _property == GravityProperty.GasDensity ? neighbor.gasMaterial : neighbor.liquidMaterial;

                    float neighborVal = _snapshotCache.TryGetValue(npos, out float sv) ? sv : GetValue(neighbor);
                    float diff = sourceVal - neighborVal;
                    float absDiff = Mathf.Abs(diff);
                    if (absDiff < 0.5f) continue;

                    // Para líquidos: no transferir si vecino ya está lleno
                    if (_property == GravityProperty.Humidity)
                    {
                        float neighborCapacity = neighbor.LiquidCapacity;
                        if (neighborCapacity > 0f && neighborVal >= neighborCapacity - 0.1f)
                            continue;
                    }

                    Vector2Int direction = npos - pos;
                    float bias = ComputeBias(ref tile, ref neighbor, direction);

                    float coeff = tileDef.gasPermeabilityCoeff;
                    if (neighborMat != MaterialType.EMPTY)
                    {
                        var nDef = lib.Get(neighborMat);
                        if (nDef != null)
                            coeff = Mathf.Min(coeff, nDef.gasPermeabilityCoeff);
                    }

                    float transfer = diff * coeff * bias * absDiff * 0.1f;
                    transfer = Mathf.Clamp(transfer, -10f, 10f);

                    AddValue(ref tile, -transfer);
                    AddValue(ref neighbor, transfer);
                    PropagateMaterial(ref tile, ref neighbor);
                    grid.MarkDirty(npos);
                }

                if (_property == GravityProperty.GasDensity && tile.isAtmosphereOpen)
                {
                    float atmDiff = sourceVal - atmDensity;
                    float absAtmDiff = Mathf.Abs(atmDiff);

                    if (absAtmDiff > 0.5f)
                    {
                        float exchange = atmDiff * atmDiffusionRate * absAtmDiff * 0.1f;
                        exchange = Mathf.Clamp(exchange, -10f, 10f);
                        AddValue(ref tile, -exchange);

                        if (sourceVal > config.atmosphereVentThreshold)
                        {
                            float excess = sourceVal - atmDensity;
                            float ventAmount = Mathf.Min(excess, config.atmosphereVentRate * Time.deltaTime);
                            AddValue(ref tile, -ventAmount);
                            grid.MarkDirty(pos);

                            if (tile.gasDensity < 1f)
                                tile.gasMaterial = MaterialType.EMPTY;
                        }
                        else if (tile.gasMaterial == MaterialType.EMPTY && sourceVal > atmDensity * 0.5f)
                        {
                            tile.gasMaterial = config.atmosphereGas;
                        }
                    }
                }
            }

            if (_property == GravityProperty.GasDensity)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var topPos = new Vector2Int(x, grid.Height - 1);
                    ref var topTile = ref grid.GetTile(topPos);
                    if (!topTile.isAtmosphereOpen) continue;

                    float density = topTile.gasDensity;
                    if (density > config.atmosphereVentThreshold)
                    {
                        float excess = density - atmDensity;
                        float ventAmount = Mathf.Min(excess, config.atmosphereVentRate * Time.deltaTime);
                        topTile.gasDensity = Mathf.Max(atmDensity, density - ventAmount);
                        if (topTile.gasDensity < 1f)
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
            return def != null && def.matterState == MatterState.Gas;
        }

        private float ComputeBias(ref TileData source, ref TileData neighbor, Vector2Int direction)
        {
            if (_property == GravityProperty.GasDensity)
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

#if UNITY_DEBUG
        [Conditional("DEBUG")]
        private void LogTransfer(Vector2Int pos, Vector2Int npos, float amount)
        {
            string propName = _property == GravityProperty.Humidity ? "Humidity" : "Gas";
            Debug.Log($"[Diffusion] {propName}: {pos} → {npos}: {amount:F2}");
        }
#endif
    }
}