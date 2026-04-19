using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Player
{
    public class PlayerActions : MonoBehaviour
    {
        [SerializeField] private PlayerActionChannel channel;
        [SerializeField] private SimulationEngine engine;

        [Header("Energy")]
        [SerializeField] private float maxEnergy  = 100f;
        [SerializeField] private float regenRate  = 5f;
        public float Energy { get; private set; }

        private void Awake()
        {
            Energy = maxEnergy;
            channel.OnActionRequested.AddListener(HandleAction);
        }

        private void OnDestroy() =>
            channel.OnActionRequested.RemoveListener(HandleAction);

        private void Update() =>
            Energy = Mathf.Clamp(Energy + regenRate * Time.deltaTime, 0f, maxEnergy);

        private void HandleAction(PlayerActionPayload payload)
        {
            float cost = GetCost(payload.actionType);
            if (Energy < cost) return;

            Energy -= cost;

            int radius = GetRadius(payload.actionType);
            for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
            {
                var pos = payload.origin + new Vector2Int(x, y);
                if (!engine.Grid.InBounds(pos)) continue;
                ApplyDelta(pos, payload.actionType);
            }
        }

        private void ApplyDelta(Vector2Int pos, ActionType action)
        {
            ref var tile = ref engine.Grid.GetTile(pos);

            switch (action)
            {
                case ActionType.THERMAL_PULSE:
                    tile.temperature    = Mathf.Clamp(tile.temperature    + 40f, 0f, 100f);
                    break;
                case ActionType.PRESSURE_INJECT:
                    tile.pressure       = Mathf.Clamp(tile.pressure       + 60f, 0f, 100f);
                    break;
                case ActionType.ELECTRIC_PULSE:
                    tile.electricEnergy = Mathf.Clamp(tile.electricEnergy + 80f, 0f, 100f);
                    break;
                case ActionType.HUMIDIFIER:
                    tile.humidity       = Mathf.Clamp(tile.humidity       + 50f, 0f, 100f);
                    break;
                case ActionType.SEAL:
                    tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity + 40f, 0f, 100f);
                    break;
                case ActionType.GAS_EXTRACT:
                    tile.gasDensity     = Mathf.Clamp(tile.gasDensity     - 80f, 0f, 100f);
                    break;
            }

            engine.Grid.MarkDirty(pos);
        }

        private static float GetCost(ActionType action) => action switch
        {
            ActionType.THERMAL_PULSE    => 30f,
            ActionType.PRESSURE_INJECT  => 40f,
            ActionType.ELECTRIC_PULSE   => 35f,
            ActionType.HUMIDIFIER       => 20f,
            ActionType.SEAL             => 25f,
            ActionType.GAS_EXTRACT      => 30f,
            _                           => 0f
        };

        private static int GetRadius(ActionType action) => action switch
        {
            ActionType.THERMAL_PULSE  => 1,
            ActionType.HUMIDIFIER     => 1,
            ActionType.GAS_EXTRACT    => 1,
            _                         => 0
        };

        // API para disparar acciones desde input externo
        public void RequestAction(ActionType type, Vector2Int origin) =>
            channel.Raise(new PlayerActionPayload
            {
                actionType = type,
                origin     = origin,
                radius     = GetRadius(type)
            });
    }
}