using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    public class R03_ElectricPropagation : IInteractionRule
    {
        public RuleID Id         => RuleID.R03_ELECTRIC_PROPAGATION;
        public TickType TickType => TickType.FAST;
        public int Priority      => 7;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def) =>
            tile.electricEnergy > 0f;

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                float etc = neighborDefs[i] != null ? neighborDefs[i].electricTransferCoeff : 0f;
                float transfer = tile.electricEnergy * etc;
                neighbors[i].electricEnergy = Mathf.Clamp(neighbors[i].electricEnergy + transfer, 0f, 100f);
                neighbors[i].dirty = true;
            }
            tile.electricEnergy *= 0.5f;
        }
    }
}