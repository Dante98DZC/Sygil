// Assets/PhysicsSystem/Core/PhysicsTypes.cs
using System;
using UnityEngine;

namespace PhysicsSystem.Core
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════════════════

    public enum MaterialType
    {
        EMPTY        = 0,
        // Sólidos
        WOOD         = 1,
        METAL        = 2,
        STONE        = 3,
        GLASS        = 7,
        ICE          = 8,
        ASH          = 9,
        SAND         = 10,
        EARTH        = 6,
        // Líquidos
        WATER        = 4,
        LAVA         = 11,
        MOLTEN_METAL = 12,
        MOLTEN_GLASS = 13,
        MUD          = 14,
        // Gases
        GAS          = 5,   // deprecated — usar tipos específicos
        STEAM        = 15,
        SMOKE        = 16,
        CO2          = 17,
        ROCK_GAS     = 18,
        AIR          = 19,
        NATURAL_GAS  = 20,
    }

    /// <summary>Capa de simulación del tile. Determina qué sección de TileData es activa.</summary>
    public enum MaterialLayer { Ground, Liquid, Gas }

    /// <summary>Estado de materia derivado — no serializar directamente, usar <see cref="MaterialLayer"/>.</summary>
    public enum MatterState { Solid, Liquid, Gas }


    // ═══════════════════════════════════════════════════════════════════════════
    //  PHASE TRANSITION
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Define la transformación que ocurre al cruzar un umbral de temperatura.
    ///
    /// CONVENCIÓN:
    ///   - <c>coolingTransition</c> en MaterialDefinition → se activa cuando <c>temp &lt; triggerTemperature</c>.
    ///   - <c>heatingTransition</c> en MaterialDefinition → se activa cuando <c>temp &gt; triggerTemperature</c>.
    ///
    /// EJEMPLOS:
    ///   WATER.coolingTransition → triggerTemperature=0°C,   resultMaterial=ICE,   latentHeat=+15
    ///   WATER.heatingTransition → triggerTemperature=100°C, resultMaterial=STEAM, latentHeat=+40
    ///   ICE.heatingTransition   → triggerTemperature=0°C,   resultMaterial=WATER, latentHeat=-15
    ///
    /// LATENT HEAT:
    ///   Amortiguación térmica durante la transición de fase; análogo al calor latente
    ///   pero expresado en °C para integrarse directamente con el sistema de temperatura.
    ///   Positivo  → el material absorbe calor sin subir de temp (frena el calentamiento).
    ///   Negativo  → el material libera calor sin bajar de temp  (frena el enfriamiento).
    /// </summary>
    [Serializable]
    public struct PhaseTransitionData
    {
        [Tooltip("Material resultante tras cruzar el umbral. EMPTY = sin transición (deshabilitada).")]
        public MaterialType resultMaterial;

        [Tooltip("Temperatura umbral en °C. Rango físico razonable para la simulación.")]
        [Range(-277, 5000)]
        public int triggerTemperature;

        [Tooltip(
            "Delta de temperatura absorbido o liberado durante la transición de fase.\n" +
            "Positivo  → absorbe calor (frena el calentamiento). Ej: fusión del hielo.\n" +
            "Negativo  → libera calor (frena el enfriamiento). Ej: congelación del agua.\n" +
            "Unidades: °C equivalentes de amortiguación.")]
        [Range(-500, 500)]
        public int latentHeat;

        /// <summary>True si esta transición está configurada (resultMaterial != EMPTY).</summary>
        public readonly bool IsEnabled => resultMaterial != MaterialType.EMPTY;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    //  COMBUSTION
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Subproducto generado por la combustión de un material.
    /// Modela que WOOD → ASH + CO2 + STEAM con proporciones específicas.
    /// </summary>
    [Serializable]
    public struct CombustionProduct
    {
        [Tooltip("Material generado como subproducto.")]
        public MaterialType material;

        [Tooltip(
            "Fracción másica producida: kg de subproducto por kg de material original consumido.\n" +
            "Ejemplo: 1 kg de WOOD → 0.1 kg ASH + 0.6 kg CO2 + 0.3 kg STEAM.")]
        [Range(0f, 2f)]
        public float massRatio;
    }

    /// <summary>
    /// Define el comportamiento de combustión de un material inflamable.
    /// Aplica tanto a sólidos (WOOD) como a gases inflamables (NATURAL_GAS).
    /// </summary>
    [Serializable]
    public struct CombustionData
    {
        [Tooltip("Si false, este material no puede arder. El resto de campos se ignoran.")]
        public bool isFlammable;

        [Tooltip(
            "Coeficiente de inflamabilidad [0..1]. Escala la velocidad de propagación del fuego.\n" +
            "0 = no se propaga · 1 = propagación máxima. Útil para combustión gradual.\n" +
            "Usado por R01_Combustion y DerivedStateComputer.")]
        [Range(0f, 1f)]
        public float flammabilityCoeff;

        [Tooltip("Temperatura mínima de ignición en °C. Rango: -277 (criogénico) a 5000.")]
        [Range(-277, 5000)]
        public int ignitionTemperature;

        [Tooltip("Calor liberado al entorno mientras el material arde (°C/s equivalentes).")]
        [Range(0, 5000)]
        public int heatOutput;

        [Tooltip("Velocidad de consumo del material en kg/s.")]
        [Range(0f, 100f)]
        public float burnRate;

        [Tooltip(
            "Material gaseoso generado como humo durante la combustión.\n" +
            "Normalmente SMOKE. EMPTY = sin humo.")]
        public MaterialType smokeMaterial;

        [Tooltip(
            "Subproductos generados durante la combustión (cenizas, gases, líquidos).\n" +
            "Ejemplos:\n" +
            "  WOOD        → ASH(0.1)  + CO2(0.5)  + STEAM(0.3)\n" +
            "  NATURAL_GAS → CO2(0.7)  + STEAM(0.5)\n" +
            "  COAL        → ASH(0.15) + CO2(0.8)")]
        public CombustionProduct[] subproducts;

        /// <summary>True si este material puede iniciar combustión.</summary>
        public readonly bool CanIgnite => isFlammable;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    //  STRUCTURAL DATA  (layer = Ground)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Propiedades físicas de materiales sólidos.
    ///
    /// PERMEABILIDAD AL GAS:
    ///   La permeabilidad es propiedad del tile sólido que lo contiene, no del gas.
    ///   Equivalente a la distinción Drywall (0) vs Mesh Tile (1) en Oxygen Not Included.
    ///
    /// ABSORCIÓN DE LÍQUIDO:
    ///   Es el suelo el que define cuánto líquido puede retener, no el líquido.
    ///   La saturación actual es estado de runtime del tile, no se define aquí.
    /// </summary>
    [Serializable]
    public struct StructuralData
    {
        [Tooltip("Integridad estructural base. 0 = colapsa inmediatamente al quedar sin soporte.")]
        [Range(0f, 1000f)]
        public float integrityBase;

        [Tooltip("Si true, este tile puede colapsar cuando pierde soporte estructural lateral/inferior.")]
        public bool canCollapse;

        [Tooltip(
            "Material al que se convierte este tile cuando colapsa estructuralmente.\n" +
            "Normalmente SAND, EARTH o EMPTY. EMPTY = desaparece.")]
        public MaterialType collapseInto;

        [Tooltip("Conductividad eléctrica relativa del material. 0 = aislante, 1 = conductor.")]
        [Range(0f, 1f)]
        public float electricTransferCoeff;

        [Tooltip(
            "Fracción de gas que puede atravesar este tile de sólido.\n" +
            "0.0 = completamente impermeable (STONE, DRYWALL).\n" +
            "1.0 = libre circulación de gas  (MESH, arena suelta).\n" +
            "Análogo a Mesh Tile vs Drywall en Oxygen Not Included.")]
        [Range(0f, 1f)]
        public float gasPermeability;

        [Tooltip(
            "Si true, este tile puede absorber líquido de la capa superior.\n" +
            "Derivado automáticamente de soilSaturationCapacity > 0.")]
        public bool isPorous;

        [Tooltip(
            "Volumen máximo de líquido que este tile puede absorber, en litros.\n" +
            "0 = no absorbente (STONE, CONCRETE).\n" +
            ">0 = suelo poroso (EARTH ~50 L, SAND ~30 L).\n" +
            "El nivel de saturación actual es estado de runtime del tile, no de este asset.")]
        [Range(0f, 1000f)]
        public float soilSaturationCapacity;

        [Tooltip(
            "Velocidad de absorción de líquido en litros por tick (SLOW).\n" +
            "Escala con la viscosidad del líquido presente.\n" +
            "EARTH ~5 L/tick · SAND ~3 L/tick · gravilla ~8 L/tick.")]
        [Range(0f, 100f)]
        public float soilAbsorptionRate;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    //  FLUID DATA  (layer = Liquid)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Propiedades hidrodinámicas de materiales líquidos.
    /// </summary>
    [Serializable]
    public struct FluidData
    {
        [Tooltip(
            "Resistencia interna al flujo.\n" +
            "0.0 = fluido ideal (WATER).\n" +
            "0.9 = casi sólido (LAVA fría, MUD espeso).")]
        [Range(0f, 1f)]
        public float viscosity;

        [Tooltip(
            "Multiplicador de coste de movimiento para entidades que se desplazan dentro de este líquido.\n" +
            "1.0 = sin penalización.\n" +
            ">1.0 = movimiento más lento (MUD = 3.0, LAVA = 8.0).")]
        [Range(1f, 10f)]
        public float movementCostMultiplier;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    //  ATMOSPHERIC DATA  (layer = Gas)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Propiedades atmosféricas de materiales gaseosos.
    /// </summary>
    [Serializable]
    public struct AtmosphericData
    {
        [Tooltip(
            "Velocidad a la que este gas se dispersa o disipa hacia tiles adyacentes.\n" +
            "0.0 = estático (no se mueve por sí solo).\n" +
            "1.0 = dispersión instantánea.")]
        [Range(0f, 1f)]
        public float dispersionRate;

        [Tooltip(
            "Densidad relativa del gas respecto al AIR baseline.\n" +
            "< 1.0 = gas ligero, sube  (STEAM = 0.6, SMOKE = 0.8).\n" +
            "> 1.0 = gas pesado, baja  (CO2 = 1.5, ROCK_GAS = 2.0).\n" +
            "Usado por GravityDiffusion para determinar dirección de movimiento.")]
        [Range(0f, 5f)]
        public float gasDensityRelative;

        [Tooltip(
            "Si true, este gas puede arder al alcanzar la ignitionTemperature configurada en CombustionData.\n" +
            "Debe coincidir con CombustionData.isFlammable para gases.")]
        public bool isFlammableGas;
    }
}