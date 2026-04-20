using UnityEngine;
using System.Collections.Generic;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;

namespace PhysicsSystem.Diffusion
{
    public class PressureDiffusion : IDiffusionStrategy
    {
        public TickType TickType => TickType.SLOW;

        public void Diffuse(PhysicsGrid grid, MaterialLibrary lib, SimulationConfig config)
        {
            var activeTiles = new List<Vector2Int>(grid.ActiveTiles);

            float atmDensity = config.atmosphereDensity;
            float atmDiffusionRate = config.atmosphereDiffusionRate;
            const float maxTransferPerTick = 5f;
            const float minThreshold = 1f;

            var snapshot = new Dictionary<Vector2Int, float>(activeTiles.Count);
            foreach (var pos in activeTiles)
                snapshot[pos] = grid.GetTile(pos).gasDensity;

            foreach (var pos in activeTiles)
            {
                ref var tile = ref grid.GetTile(pos);
                float sourceVal = snapshot[pos];
                if (sourceVal <= 0f) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var nDef = lib.Get(neighbor.groundMaterial);
                    if (nDef == null) continue;

                    float neighborVal = snapshot.TryGetValue(npos, out float sv) ? sv : neighbor.gasDensity;

                    float deltaP = sourceVal - neighborVal;
                    if (Mathf.Abs(deltaP) < minThreshold) continue;

                    float resistance = 1f - (neighbor.structuralIntegrity / 100f);
                    float transfer = deltaP * resistance * Mathf.Abs(deltaP) * 0.05f;
                    transfer = Mathf.Clamp(transfer, -maxTransferPerTick, maxTransferPerTick);

                    if (Mathf.Abs(transfer) < 0.01f) continue;

                    tile.gasDensity = Mathf.Clamp(tile.gasDensity - transfer, 0f, 100f);
                    neighbor.gasDensity = Mathf.Clamp(neighbor.gasDensity + transfer, 0f, 100f);
                    grid.MarkDirty(npos);
                }

                if (tile.isAtmosphereOpen)
                {
                    float atmDiff = sourceVal - atmDensity;
                    if (Mathf.Abs(atmDiff) > minThreshold)
                    {
                        float exchange = atmDiff * atmDiffusionRate * Mathf.Abs(atmDiff) * 0.05f;
                        exchange = Mathf.Clamp(exchange, -maxTransferPerTick, maxTransferPerTick);
                        if (Mathf.Abs(exchange) > 0.01f)
                        {
                            tile.gasDensity = Mathf.Clamp(tile.gasDensity - exchange, 0f, 100f);
                            if (tile.gasMaterial == MaterialType.EMPTY && sourceVal > atmDensity * 0.5f)
                                tile.gasMaterial = config.atmosphereGas;
                            grid.MarkDirty(pos);
                        }
                    }
                }
            }
        }
    }
}