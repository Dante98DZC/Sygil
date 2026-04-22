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

                MaterialDefinition def = null;
                if (tile.groundMaterial != MaterialType.EMPTY)
                    def = _lib.Get(tile.groundMaterial);
                else if (tile.liquidMaterial != MaterialType.EMPTY)
                    def = _lib.Get(tile.liquidMaterial);
                else if (tile.gasMaterial != MaterialType.EMPTY)
                    def = _lib.Get(tile.gasMaterial);

                StateFlags flags = StateFlags.NONE;

                if (tile.temperature > 70f && def != null && def.combustion.flammabilityCoeff > 0.5f)
                    flags |= StateFlags.ON_FIRE;

                if (tile.electricEnergy > 50f)
                    flags |= StateFlags.ELECTRIFIED;

                if (tile.gasConcentration > 60f)
                    flags |= StateFlags.PRESSURIZED;

                if (tile.liquidVolume > 70f && tile.liquidMaterial != MaterialType.WATER)
                    flags |= StateFlags.FLOODED;

                if (tile.structuralIntegrity < 30f)
                    flags |= StateFlags.STRUCTURALLY_WEAK;

                if (tile.gasConcentration > 60f && tile.temperature > 40f)
                    flags |= StateFlags.VOLATILE;

                bool allEmpty = tile.groundMaterial == MaterialType.EMPTY &&
                              tile.liquidMaterial == MaterialType.EMPTY &&
                              tile.gasMaterial == MaterialType.EMPTY;
                if (allEmpty && !tile.wasEmpty)
                    flags |= StateFlags.COLLAPSED;

                tile.derivedStates = flags;
            }
        }
    }
}