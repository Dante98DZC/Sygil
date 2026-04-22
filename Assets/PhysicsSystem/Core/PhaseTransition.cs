// Assets/PhysicsSystem/Core/PhaseTransition.cs
using System;
using UnityEngine;

namespace PhysicsSystem.Core
{
    /// <summary>
    /// Modela una transición de fase unidireccional entre materiales.
    /// Un mismo struct representa fusión, ebullición, condensación o solidificación
    /// dependiendo de su contexto (heatingTransition / coolingTransition).
    ///
    /// Uso: si triggerTemperature > 0 y resultMaterial != EMPTY, la transición está activa.
    /// </summary>
    [Serializable]
    public struct PhaseTransition
    {
        [Tooltip("Temperatura que dispara esta transición. 0 = deshabilitada.")]
        [Range(0f, 100f)]
        public float triggerTemperature;

        [Tooltip("Material resultante tras la transición.")]
        public MaterialType resultMaterial;

        [Tooltip(
            "Energía absorbida (+) o liberada (-) durante la transición, en unidades de temperatura. " +
            "Ejemplos: fusión absorbe calor (+), solidificación libera calor (-).")]
        public float latentHeat;

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>True si esta transición tiene datos válidos y está activa.</summary>
        public readonly bool IsEnabled =>
            triggerTemperature > 0f && resultMaterial != MaterialType.EMPTY;

        /// <summary>True si esta transición absorbe energía (endotérmica), como la fusión.</summary>
        public readonly bool IsEndothermic => latentHeat > 0f;

        /// <summary>True si esta transición libera energía (exotérmica), como la solidificación.</summary>
        public readonly bool IsExothermic => latentHeat < 0f;

        // ── Factory helpers ───────────────────────────────────────────────────

        /// <summary>Construye una transición de fusión o ebullición (endotérmica).</summary>
        public static PhaseTransition Heating(
            float triggerTemperature,
            MaterialType resultMaterial,
            float latentHeat = 0f)
            => new()
            {
                triggerTemperature = triggerTemperature,
                resultMaterial     = resultMaterial,
                latentHeat         = Mathf.Abs(latentHeat)  // siempre positivo — absorbe calor
            };

        /// <summary>Construye una transición de solidificación o condensación (exotérmica).</summary>
        public static PhaseTransition Cooling(
            float triggerTemperature,
            MaterialType resultMaterial,
            float latentHeat = 0f)
            => new()
            {
                triggerTemperature = triggerTemperature,
                resultMaterial     = resultMaterial,
                latentHeat         = -Mathf.Abs(latentHeat) // siempre negativo — libera calor
            };

        /// <summary>Transición deshabilitada (sin datos).</summary>
        public static readonly PhaseTransition None = new()
        {
            triggerTemperature = 0f,
            resultMaterial     = MaterialType.EMPTY,
            latentHeat         = 0f
        };
    }
}
