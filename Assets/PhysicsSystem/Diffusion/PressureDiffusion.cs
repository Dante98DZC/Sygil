using UnityEngine;
using System.Collections.Generic;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;

#if UNITY_DEBUG
using System.Diagnostics;
#endif

namespace PhysicsSystem.Diffusion
{
    public class PressureDiffusion : IDiffusionStrategy
    {
        private static readonly Dictionary<Vector2Int, float> _snapshotCache = new(128);

        public TickType TickType => TickType.SLOW;

        public void Diffuse(PhysicsGrid grid, MaterialLibrary lib, SimulationConfig config)
        {
            if (grid.ActiveTiles.Count == 0) return;

            var activeTiles = new List<Vector2Int>(grid.ActiveTiles);
            const float FIXED_DELTA_TIME = 0.016f;
            int maxTiles = config.maxDiffusionTilesPerTick;

            if (activeTiles.Count > maxTiles)
                activeTiles.RemoveRange(maxTiles, activeTiles.Count - maxTiles);

            float atmDensity = config.atmosphereConcentration;
            float atmDiffusionRate = config.atmosphereDiffusionRate;
            const float maxTransferPerTick = 5f;
            const float minThreshold = 1f;

            _snapshotCache.Clear();
            foreach (var pos in activeTiles)
                _snapshotCache[pos] = grid.GetTile(pos).gasConcentration;

            foreach (var pos in activeTiles)
            {
                ref var tile = ref grid.GetTile(pos);
                float sourceVal = _snapshotCache[pos];
                if (sourceVal <= 0f) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var nDef = lib.Get(neighbor.groundMaterial);
                    if (nDef == null) continue;

                    float neighborVal = _snapshotCache.TryGetValue(npos, out float sv) ? sv : neighbor.gasConcentration;

                    float deltaP = sourceVal - neighborVal;
                    float absDeltaP = Mathf.Abs(deltaP);
                    if (absDeltaP < minThreshold) continue;

                    float resistance = 1f - (neighbor.structuralIntegrity / 100f);
                    float transfer = deltaP * resistance * absDeltaP * 0.05f;
                    transfer = Mathf.Clamp(transfer, -maxTransferPerTick, maxTransferPerTick);

                    tile.gasConcentration = Mathf.Clamp(tile.gasConcentration - transfer, 0f, 100f);
                    neighbor.gasConcentration = Mathf.Clamp(neighbor.gasConcentration + transfer, 0f, 100f);
                    grid.MarkDirty(npos);
                }

                if (tile.isAtmosphereOpen)
                {
                    float atmDiff = sourceVal - atmDensity;
                    float absAtmDiff = Mathf.Abs(atmDiff);
                    if (absAtmDiff > minThreshold)
                    {
                        float exchange = atmDiff * atmDiffusionRate * absAtmDiff * 0.05f;
                        exchange = Mathf.Clamp(exchange, -maxTransferPerTick, maxTransferPerTick);
                        if (Mathf.Abs(exchange) > 0.01f)
                        {
                            tile.gasConcentration = Mathf.Clamp(tile.gasConcentration - exchange, 0f, 100f);
                            if (tile.gasMaterial == MaterialType.EMPTY && sourceVal > atmDensity + 5f)
                                tile.gasMaterial = config.atmosphereGas;
                            grid.MarkDirty(pos);
                        }
                    }
                }
            }
        }

#if UNITY_DEBUG
        [Conditional("DEBUG")]
        private void LogTransfer(Vector2Int pos, Vector2Int npos, float amount)
        {
            Debug.Log($"[Diffusion] Pressure: {pos} → {npos}: {amount:F2}");
        }
#endif
    }
}