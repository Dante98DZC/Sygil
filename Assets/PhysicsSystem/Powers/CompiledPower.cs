using UnityEngine;

namespace PhysicsSystem.Powers
{
    // ── Enums ────────────────────────────────────────────────────────────────

    public enum DeliveryMode
    {
        Instant,      // aplicado en el tile del jugador inmediatamente
        Area,         // área centrada en el origen
        Projectile,   // viaja hasta impacto (posición resuelta antes de compilar el tick)
        Cone,         // sector angular desde el origen
        Ray           // línea recta hasta alcance máximo
    }

    public enum PropertyTarget
    {
        Temperature, Pressure, Humidity,
        ElectricEnergy, GasDensity, StructuralIntegrity
    }

    public enum FalloffCurve { Flat, Linear, Quadratic }

    public enum TimingMode { Instant, Duration, Pulse }

    // ── Data structs ─────────────────────────────────────────────────────────

    [System.Serializable]
    public struct ShapeData
    {
        public int   radius;     // tiles — para Area, Projectile
        public float angle;      // grados — para Cone
        public int   length;     // tiles — para Ray
        public Vector2Int offset; // desplazamiento desde el origen del caster
    }

    [System.Serializable]
    public struct ConditionData
    {
        public bool          active;           // false = sin condición
        public Core.MaterialType requiredMaterial;  // ignorado si checkMaterial=false
        public bool          checkMaterial;
        public PropertyTarget checkedProperty;
        public float         threshold;        // la propiedad debe superar este valor
        public bool          checkProperty;
    }

    [System.Serializable]
    public struct EffectData
    {
        public PropertyTarget target;
        public float          baseValue;    // delta base (puede ser negativo)
        public FalloffCurve   falloff;      // cómo decae por distancia al origen
        public ConditionData  condition;    // condición opcional de aplicación
    }

    [System.Serializable]
    public struct TimingData
    {
        public TimingMode mode;
        public float      duration;   // segundos — para Duration y Pulse
        public float      interval;   // segundos entre pulsos — para Pulse
        public int        pulseCount; // 0 = infinito hasta expirar
    }

    // ── ScriptableObject ─────────────────────────────────────────────────────

    /// <summary>
    /// Output serializado del PowerCompiler (editor).
    /// En runtime es read-only — nunca se modifica durante la partida.
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsSystem/CompiledPower")]
    public class CompiledPower : ScriptableObject
    {
        [Header("Identidad")]
        public string powerId;
        public float  energyCost;

        [Header("Entrega")]
        public DeliveryMode delivery;
        public ShapeData    shape;

        [Header("Efectos")]
        public EffectData[] effects;

        [Header("Temporización")]
        public TimingData timing;
    }
}
