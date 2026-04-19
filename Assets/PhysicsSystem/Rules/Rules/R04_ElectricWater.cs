using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    public class R04_ElectricWater : IInteractionRule
    {
        public RuleID Id         => RuleID.R04_ELECTRIC_WATER;
        public TickType TickType => TickType.FAST;
        public int Priority      => 4;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def) =>
            tile.electricEnergy > 40f && tile.material == MaterialType.WATER;

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.temperature    = Mathf.Clamp(tile.temperature    + tile.electricEnergy * 0.3f, 0f, 100f);
            tile.electricEnergy = Mathf.Clamp(tile.electricEnergy - 20f,                        0f, 100f);
        }
    }
}