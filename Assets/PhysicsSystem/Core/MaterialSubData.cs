// Assets/PhysicsSystem/Core/MaterialSubData.cs
// Sub-structs serializables agrupados por capa de simulación.
// Cada struct agrupa propiedades que solo tienen sentido para esa capa.
// MaterialDefinition los usa como campos para mantener el Inspector organizado.

using System;
using UnityEngine;

namespace PhysicsSystem.Core
{
    // ──────────────────────────────────────────────────────────────────────────
    // GROUND — propiedades de sólidos
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Propiedades físicas de materiales en la capa Ground (sólidos).
    /// Aplica a: STONE, WOOD, METAL, ICE, SAND, ASH, EARTH, GLASS…
    /// </summary>
    [Serializable]
    public struct StructuralData
    {
        [Tooltip("Integridad estructural inicial. 0 = el material colapsa inmediatamente.")]
        [Range(0f, 100f)]
        public float integrityBase;

        [Tooltip("Material que queda tras un colapso estructural (R07). EMPTY = desaparece.")]
        public MaterialType collapseInto;

        [Tooltip("Conductividad eléctrica. 0 = aislante, 1 = conductor perfecto.")]
        [Range(0f, 1f)]
        public float electricTransferCoeff;

        [Tooltip("Coeficiente de inflamabilidad del sólido. 0 = no arde, 1 = muy inflamable.")]
        [Range(0f, 1f)]
        public float flammabilityCoeff;

        // ── Defaults ─────────────────────────────────────────────────────────

        /// <summary>Configuración para sólidos indestructibles no conductores (STONE, METAL base).</summary>
        public static StructuralData Indestructible => new()
        {
            integrityBase       = 100f,
            collapseInto        = MaterialType.EMPTY,
            electricTransferCoeff = 0f,
            flammabilityCoeff   = 0f
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LIQUID — propiedades de fluidos
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Propiedades físicas de materiales en la capa Liquid.
    /// Aplica a: WATER, LAVA, MUD, MOLTEN_METAL, MOLTEN_GLASS…
    /// </summary>
    [Serializable]
    public struct FluidData
    {
        [Tooltip(
            "Coeficiente de flujo del líquido. 1.0 = agua (base). " +
            "Valores menores = más viscoso (lava, lodo). Afecta a GradientDiffusion.")]
        [Range(0.01f, 1f)]
        public float viscosity;

        [Tooltip(
            "Velocidad de absorción de este líquido por suelos porosos, en litros por tick. " +
            "Solo activo si el tile de suelo adyacente tiene isPorous = true.")]
        [Range(0f, 10f)]
        public float soilAbsorptionRate;

        [Tooltip("Volumen máximo de líquido que un suelo poroso puede retener, en litros.")]
        [Range(0f, 500f)]
        public float soilSaturationCapacity;

        // ── Defaults ─────────────────────────────────────────────────────────

        /// <summary>Configuración para agua estándar.</summary>
        public static FluidData Water => new()
        {
            viscosity              = 1f,
            soilAbsorptionRate     = 2f,
            soilSaturationCapacity = 50f
        };

        /// <summary>Configuración para fluidos muy viscosos (lava, metal fundido).</summary>
        public static FluidData Viscous => new()
        {
            viscosity              = 0.05f,
            soilAbsorptionRate     = 0f,
            soilSaturationCapacity = 0f
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GAS — propiedades de gases
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Propiedades físicas de materiales en la capa Gas.
    /// Aplica a: STEAM, SMOKE, CO2, ROCK_GAS, AIR…
    /// </summary>
    [Serializable]
    public struct AtmosphericData
    {
        [Tooltip(
            "Multiplicador de velocidad de disipación hacia la atmósfera exterior. " +
            "1.0 = base. >1 = se disipa más rápido. 0 = gas que no disipa (CO2 confinado).")]
        [Range(0f, 5f)]
        public float dissipationMultiplier;

        [Tooltip("Cuánto deja pasar este gas a través de materiales porosos. 0 = opaco, 1 = libre.")]
        [Range(0f, 1f)]
        public float gasPermeabilityCoeff;

        [Tooltip("Si es true, este gas puede arder bajo las condiciones de combustionData.")]
        public bool isFlammable;

        [Tooltip(
            "Temperatura de autoignición para gases inflamables. " +
            "0 = usar threshold global de R10 en SimulationConfig.")]
        [Range(0f, 100f)]
        public float ignitionTemperature;

        // ── Defaults ─────────────────────────────────────────────────────────

        /// <summary>Configuración para gases inertes (CO2, vapor).</summary>
        public static AtmosphericData Inert => new()
        {
            dissipationMultiplier  = 1f,
            gasPermeabilityCoeff   = 1f,
            isFlammable            = false,
            ignitionTemperature    = 0f
        };

        /// <summary>Configuración para gases inflamables (ROCK_GAS).</summary>
        public static AtmosphericData Flammable(float ignitionTemp) => new()
        {
            dissipationMultiplier  = 1f,
            gasPermeabilityCoeff   = 1f,
            isFlammable            = true,
            ignitionTemperature    = ignitionTemp
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // COMBUSTIÓN — compartido entre capas (sólidos y gases pueden arder)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Comportamiento de combustión del material.
    /// Aplica a sólidos inflamables (WOOD) y gases combustibles (ROCK_GAS).
    /// Los materiales no inflamables dejan este struct con ignitionTemperature = 0.
    /// </summary>
    [Serializable]
    public struct CombustionData
    {
        [Tooltip(
            "Temperatura mínima para que este material empiece a arder (R01). " +
            "0 = material no inflamable.")]
        [Range(0f, 100f)]
        public float ignitionTemperature;

        [Tooltip("Material sólido que queda en groundMaterial tras combustión completa. EMPTY = desaparece.")]
        public MaterialType ashMaterial;

        [Tooltip("Gas producido durante la combustión que va a gasMaterial (SMOKE, CO2…).")]
        public MaterialType smokeMaterial;

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>True si este material puede arder.</summary>
        public readonly bool CanIgnite => ignitionTemperature > 0f;

        // ── Defaults ─────────────────────────────────────────────────────────

        /// <summary>Material que no arde.</summary>
        public static CombustionData NonFlammable => new()
        {
            ignitionTemperature = 0f,
            ashMaterial         = MaterialType.EMPTY,
            smokeMaterial       = MaterialType.SMOKE
        };

        /// <summary>Madera: arde a temperatura media, deja ceniza, produce humo.</summary>
        public static CombustionData Wood => new()
        {
            ignitionTemperature = 45f,
            ashMaterial         = MaterialType.ASH,
            smokeMaterial       = MaterialType.SMOKE
        };
    }
}
