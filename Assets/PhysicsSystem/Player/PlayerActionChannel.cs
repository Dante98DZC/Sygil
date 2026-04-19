using UnityEngine;
using UnityEngine.Events;

namespace PhysicsSystem.Player
{
    public enum ActionType
    {
        THERMAL_PULSE, PRESSURE_INJECT, ELECTRIC_PULSE,
        HUMIDIFIER, SEAL, GAS_EXTRACT
    }

    public struct PlayerActionPayload
    {
        public ActionType actionType;
        public Vector2Int origin;
        public int radius;
    }

    [CreateAssetMenu(menuName = "PhysicsSystem/PlayerActionChannel")]
    public class PlayerActionChannel : ScriptableObject
    {
        public UnityEvent<PlayerActionPayload> OnActionRequested = new();

        public void Raise(PlayerActionPayload payload) =>
            OnActionRequested.Invoke(payload);
    }
}