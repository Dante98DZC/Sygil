using UnityEngine;
using UnityEngine.Events;

namespace PhysicsSystem.Powers
{
    [System.Serializable]
    public struct PowerCastRequest
    {
        public CompiledPower power;
        public Vector2Int    origin;
        public Vector2       direction;  // normalizado — para Cone y Ray; ignorado en Area/Instant
    }

    /// <summary>
    /// Canal de eventos para activar poderes compilados desde cualquier sistema
    /// (input del jugador, AI, cutscenes, tests).
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsSystem/PowerChannel")]
    public class PowerChannel : ScriptableObject
    {
        public UnityEvent<PowerCastRequest> OnPowerRequested = new();

        public void Raise(PowerCastRequest request) => OnPowerRequested.Invoke(request);
    }
}
