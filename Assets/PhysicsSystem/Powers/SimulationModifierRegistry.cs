using System.Collections.Generic;
using PhysicsSystem.Core;

namespace PhysicsSystem.Powers
{
    /// <summary>
    /// Administra el ciclo de vida de los ISimulationModifier activos.
    /// Por tick: ApplyProperties → Tick (avanza TTL) → purga expirados.
    ///
    /// El orden Apply→Tick garantiza que el efecto de un modifier se aplica
    /// en el mismo tick en que expira, sin necesidad de guards adicionales.
    /// Llamado desde PowerCaster.Update().
    /// </summary>
    public class SimulationModifierRegistry
    {
        private readonly List<ISimulationModifier> _active  = new();
        private readonly List<ISimulationModifier> _expired = new();

        public IReadOnlyList<ISimulationModifier> Active => _active;

        public void Add(ISimulationModifier modifier) => _active.Add(modifier);

        public void Tick(float deltaTime, PhysicsGrid grid)
        {
            foreach (var mod in _active)
            {
                mod.ApplyProperties(grid);
                mod.Tick(deltaTime);

                if (mod.IsExpired)
                    _expired.Add(mod);
            }

            foreach (var mod in _expired)
                _active.Remove(mod);

            _expired.Clear();
        }

        public void Clear() => _active.Clear();
    }
}