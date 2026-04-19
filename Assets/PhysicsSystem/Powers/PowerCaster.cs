using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Powers
{
    /// <summary>
    /// MonoBehaviour que conecta PowerChannel → SimulationModifierRegistry → PhysicsGrid.
    /// Tick del registry en Update. Escucha PowerChannel para instanciar CompiledPowerModifiers.
    /// </summary>
    public class PowerCaster : MonoBehaviour
    {
        [SerializeField] private SimulationEngine engine;
        [SerializeField] private PowerChannel     channel;

        private readonly SimulationModifierRegistry _registry = new();

        private void Awake()   => channel.OnPowerRequested.AddListener(HandlePower);
        private void OnDestroy() => channel.OnPowerRequested.RemoveListener(HandlePower);

        private void Update() => _registry.Tick(Time.deltaTime, engine.Grid);

        private void HandlePower(PowerCastRequest request)
        {
            if (request.power == null) return;

            var modifier = new CompiledPowerModifier(
                request.power,
                request.origin,
                request.direction
            );

            _registry.Add(modifier);
        }

        /// <summary>API para disparar poderes desde código (input, tests, cutscenes).</summary>
        public void Cast(CompiledPower power, Vector2Int origin, Vector2 direction) =>
            channel.Raise(new PowerCastRequest
            {
                power     = power,
                origin    = origin,
                direction = direction
            });
    }
}
