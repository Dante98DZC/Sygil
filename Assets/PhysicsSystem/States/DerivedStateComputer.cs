using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.States;

namespace PhysicsSystem.States
{
    public class DerivedStateComputer
    {
        private readonly MaterialLibrary _lib;

        public DerivedStateComputer(MaterialLibrary lib)
        {
            _lib = lib;
        }

        public void Compute(PhysicsGrid grid)
        {
            foreach (var pos in grid.ActiveTiles)
            {
                ref var tile = ref grid.GetTile(pos);
                var def = _lib.Get(tile.material);

                StateFlags flags = StateFlags.NONE;

                if (tile.temperature > 70f && def != null && def.flammabilityCoeff > 0.5f)
                    flags |= StateFlags.ON_FIRE;

                if (tile.electricEnergy > 50f)
                    flags |= StateFlags.ELECTRIFIED;

                if (tile.gasDensity > 60f)
                    flags |= StateFlags.PRESSURIZED;

                if (tile.liquidVolume > 70f && tile.material != MaterialType.WATER)
                    flags |= StateFlags.FLOODED;

                if (tile.structuralIntegrity < 30f)
                    flags |= StateFlags.STRUCTURALLY_WEAK;

                if (tile.gasDensity > 60f && tile.temperature > 40f)
                    flags |= StateFlags.VOLATILE;

                if (tile.material == MaterialType.EMPTY && !tile.wasEmpty)
                    flags |= StateFlags.COLLAPSED;

                tile.derivedStates = flags;
            }
        }
    }
}