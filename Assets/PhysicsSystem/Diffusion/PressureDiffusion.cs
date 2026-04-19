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

        public void Diffuse(PhysicsGrid grid, MaterialLibrary lib)
        {
            foreach (var pos in new List<Vector2Int>(grid.ActiveTiles))
            {
                ref var tile = ref grid.GetTile(pos);
                if (tile.pressure <= 0f) continue;

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var nDef = lib.Get(neighbor.material);
                    if (nDef == null) continue;

                    float resistance = 1f - (neighbor.structuralIntegrity / 100f);
                    float transfer = (tile.pressure - neighbor.pressure) * resistance * 0.25f;
                    if (transfer <= 0f) continue;

                    tile.pressure     = Mathf.Clamp(tile.pressure     - transfer, 0f, 100f);
                    neighbor.pressure = Mathf.Clamp(neighbor.pressure + transfer, 0f, 100f);
                    grid.MarkDirty(npos);
                }
            }
        }
    }
}