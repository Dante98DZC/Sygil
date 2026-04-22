// Assets/PhysicsSystem/Core/MaterialDefinition.cs
using UnityEngine;

namespace PhysicsSystem.Core
{
    /// <summary>
    /// Define el comportamiento físico completo de un material en la simulación.
    ///
    /// ── ORGANIZACIÓN POR CAPA ────────────────────────────────────────────────
    ///   <see cref="layer"/> declara en qué capa vive este material.
    ///   Configura únicamente la sección de propiedades de tu capa:
    ///     · Ground → <see cref="structural"/>   (integridad, permeabilidad, absorción)
    ///     · Liquid → <see cref="fluid"/>         (viscosidad, coste de movimiento)
    ///     · Gas    → <see cref="atmospheric"/>   (dispersión, inflamabilidad)
    ///
    /// ── TRANSICIONES DE TEMPERATURA ─────────────────────────────────────────
    ///   Cada material tiene dos umbrales opcionales simétricos:
    ///
    ///   <see cref="coolingTransition"/>  →  temp &lt; triggerTemperature  →  cambio de fase (congelación)
    ///   <see cref="heatingTransition"/>  →  temp &gt; triggerTemperature  →  cambio de fase (fusión/ebullición)
    ///
    ///   Referencia rápida:
    ///   ┌──────────────┬────────────────────────────────┬─────────────────────────────────┐
    ///   │ Material     │ coolingTransition               │ heatingTransition                │
    ///   ├──────────────┼────────────────────────────────┼─────────────────────────────────┤
    ///   │ ICE          │ —                               │ 0°C → WATER,        latent=-15  │
    ///   │ WATER        │ 0°C → ICE,     latent=+15       │ 100°C → STEAM,      latent=+40  │
    ///   │ STEAM        │ 100°C → WATER, latent=-40       │ —                               │
    ///   │ STONE        │ —                               │ 1200°C → LAVA,      latent=+80  │
    ///   │ LAVA         │ 1200°C → STONE, latent=-80      │ —                               │
    ///   │ METAL        │ —                               │ 1500°C → MOLTEN_METAL, latent=+90│
    ///   │ MOLTEN_METAL │ 1500°C → METAL, latent=-90      │ —                               │
    ///   │ GLASS        │ —                               │ 800°C → MOLTEN_GLASS, latent=+60│
    ///   │ MOLTEN_GLASS │ 800°C → GLASS,  latent=-60      │ —                               │
    ///   └──────────────┴────────────────────────────────┴─────────────────────────────────┘
    ///
    /// ── BLOQUEO DE MOVIMIENTO ────────────────────────────────────────────────
    ///   <see cref="BlocksMovement"/> es una propiedad derivada, no configurable:
    ///   los tiles Ground siempre bloquean el movimiento; Liquid y Gas nunca.
    ///   Los líquidos penalizan el movimiento con <see cref="FluidData.movementCostMultiplier"/>.
    ///
    /// ── VISIÓN ──────────────────────────────────────────────────────────────
    ///   <see cref="visionObstructionCoeff"/> reemplaza el bool blocksVision.
    ///   Permite humo semitransparente, agua turbia, vapor leve.
    ///   0 = transparente · 1 = completamente opaco.
    ///
    /// ── PERMEABILIDAD AL GAS Y ABSORCIÓN DE LÍQUIDO ─────────────────────────
    ///   Ambas son propiedades del receptor (tile Ground), no del fluido.
    ///   Se definen en <see cref="structural"/>:
    ///     · gasPermeability        → ¿deja pasar gases? (0=Drywall, 1=Mesh)
    ///     · soilSaturationCapacity → ¿cuánto líquido absorbe? (0=piedra, 50=tierra)
    ///     · soilAbsorptionRate     → velocidad de absorción por tick
    ///   El nivel actual de saturación es estado de runtime del tile, no de este asset.
    ///
    /// ── COMBUSTIÓN ──────────────────────────────────────────────────────────
    ///   <see cref="combustion"/>.isFlammable activa la combustión.
    ///   <see cref="combustion"/>.flammabilityCoeff escala la velocidad de propagación.
    ///   <see cref="combustion"/>.smokeMaterial define el gas de humo generado.
    ///   Los subproductos (<see cref="CombustionData.subproducts"/>) definen qué materiales
    ///   se generan y en qué proporción másica al quemarse.
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsSystem/MaterialDefinition")]
    public class MaterialDefinition : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Tipo de material que identifica este asset en la simulación.")]
        public MaterialType materialType;

        [Tooltip(
            "Capa de simulación a la que pertenece este material.\n" +
            "Ground = sólidos   · Liquid = líquidos   · Gas = gases.\n" +
            "Determina qué sección de TileData es activa y qué propiedades son relevantes.")]
        public MaterialLayer layer;

        // ── Térmica ───────────────────────────────────────────────────────────

        [Header("Thermal")]
        [Tooltip(
            "Velocidad de transferencia de calor con tiles adyacentes.\n" +
            "0 = aislante perfecto (WOOD seco).\n" +
            "1 = conductor perfecto (STONE, LAVA).")]
        [Range(0f, 1f)]
        public float heatTransferCoeff;

        // ── Visión ────────────────────────────────────────────────────────────

        [Header("Visibility")]
        [Tooltip(
            "Fracción de visión bloqueada por este material.\n" +
            "0.0 = completamente transparente  (AIR, WATER limpia).\n" +
            "0.3 = semitransparente             (STEAM, niebla leve).\n" +
            "0.7 = semiopaco                    (SMOKE denso, MUD).\n" +
            "1.0 = completamente opaco          (STONE, EARTH, WOOD).\n\n" +
            "Nota: los tiles Ground no bloquean visión por ser sólidos automáticamente;\n" +
            "este valor se evalúa siempre para cualquier capa.")]
        [Range(0f, 1f)]
        public float visionObstructionCoeff;

        // ── Transiciones de temperatura ───────────────────────────────────────

        [Header("Temperature Transitions")]
        [Tooltip(
            "Transición activa cuando temp < triggerTemperature.\n" +
            "Ejemplos: WATER a 0°C → ICE | LAVA a 1200°C → STONE.\n" +
            "Dejar resultMaterial = EMPTY si no aplica.")]
        public PhaseTransitionData coolingTransition;

        [Tooltip(
            "Transición activa cuando temp > triggerTemperature.\n" +
            "Ejemplos: ICE a 0°C → WATER | WATER a 100°C → STEAM.\n" +
            "Dejar resultMaterial = EMPTY si no aplica.")]
        public PhaseTransitionData heatingTransition;

        // ── Combustión ────────────────────────────────────────────────────────

        [Header("Combustion")]
        [Tooltip(
            "Comportamiento de combustión.\n" +
            "Activar isFlammable y configurar ignitionTemperature, flammabilityCoeff,\n" +
            "smokeMaterial y subproducts.\n" +
            "Para gases inflamables, activar también atmospheric.isFlammableGas.")]
        public CombustionData combustion;

        // ── Ground ────────────────────────────────────────────────────────────

        [Header("Ground Layer — Structural")]
        [Tooltip(
            "Propiedades de materiales sólidos: integridad, conductividad eléctrica,\n" +
            "permeabilidad al gas y capacidad de absorción de líquidos.\n" +
            "Solo relevante cuando layer = Ground.")]
        public StructuralData structural;

        // ── Liquid ────────────────────────────────────────────────────────────

        [Header("Liquid Layer — Fluid")]
        [Tooltip(
            "Propiedades hidrodinámicas: viscosidad y penalización de movimiento.\n" +
            "Solo relevante cuando layer = Liquid.")]
        public FluidData fluid;

        // ── Gas ───────────────────────────────────────────────────────────────

        [Header("Gas Layer — Atmospheric")]
        [Tooltip(
            "Propiedades atmosféricas: dispersión, densidad relativa e inflamabilidad.\n" +
            "Solo relevante cuando layer = Gas.")]
        public AtmosphericData atmospheric;


        // ═════════════════════════════════════════════════════════════════════
        //  PROPIEDADES DERIVADAS
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Estado de materia derivado de la capa de simulación.</summary>
        public MatterState MatterState => layer switch
        {
            MaterialLayer.Ground => MatterState.Solid,
            MaterialLayer.Liquid => MatterState.Liquid,
            MaterialLayer.Gas    => MatterState.Gas,
            _                    => MatterState.Solid
        };

        /// <summary>
        /// Los tiles Ground siempre bloquean el movimiento de entidades.
        /// Propiedad derivada — no configurable en el Inspector (evita errores).
        /// Los líquidos no bloquean, pero penalizan con <see cref="FluidData.movementCostMultiplier"/>.
        /// </summary>
        public bool BlocksMovement => layer == MaterialLayer.Ground;

        /// <summary>
        /// Coste de movimiento efectivo para una entidad que ocupa este tile.
        /// Ground = infinito (bloqueado), Liquid = viscosidad aplicada, Gas = paso libre.
        /// </summary>
        public float EffectiveMovementCost => layer switch
        {
            MaterialLayer.Ground => float.MaxValue,
            MaterialLayer.Liquid => fluid.movementCostMultiplier,
            MaterialLayer.Gas    => 1f,
            _                    => 1f
        };

        /// <summary>
        /// Permeabilidad efectiva al paso de gases a través de este tile.
        ///   Ground → definida por <see cref="StructuralData.gasPermeability"/> (0=Drywall, 1=Mesh).
        ///   Liquid → 0 (los líquidos bloquean gases).
        ///   Gas    → 1 (los gases no se bloquean entre sí por defecto).
        /// </summary>
        public float GasPermeability => layer switch
        {
            MaterialLayer.Ground => structural.gasPermeability,
            MaterialLayer.Liquid => 0f,
            MaterialLayer.Gas    => 1f,
            _                    => 0f
        };

        /// <summary>True si este tile impide completamente el paso de gas.</summary>
        public bool BlocksGas => GasPermeability < 0.01f;

        /// <summary>True si este material puede iniciar combustión.</summary>
        public bool IsFlammable => combustion.CanIgnite;

        /// <summary>True si es un gas inflamable (requiere oxidante para arder).</summary>
        public bool IsFlammableGas => layer == MaterialLayer.Gas && atmospheric.isFlammableGas;

        /// <summary>True si tiene transición de fase por enfriamiento configurada.</summary>
        public bool HasCoolingTransition => coolingTransition.IsEnabled;

        /// <summary>True si tiene transición de fase por calentamiento configurada.</summary>
        public bool HasHeatingTransition => heatingTransition.IsEnabled;


        // ═════════════════════════════════════════════════════════════════════
        //  HELPERS ESTÁTICOS
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Estado de materia por defecto para un MaterialType dado.
        /// Útil durante la inicialización antes de que se carguen los assets.
        /// </summary>
        public static MatterState GetDefaultState(MaterialType type) => type switch
        {
            MaterialType.WATER        => MatterState.Liquid,
            MaterialType.LAVA         => MatterState.Liquid,
            MaterialType.MUD          => MatterState.Liquid,
            MaterialType.MOLTEN_METAL => MatterState.Liquid,
            MaterialType.MOLTEN_GLASS => MatterState.Liquid,
            MaterialType.STEAM        => MatterState.Gas,
            MaterialType.SMOKE        => MatterState.Gas,
            MaterialType.CO2          => MatterState.Gas,
            MaterialType.NATURAL_GAS  => MatterState.Gas,
            MaterialType.ROCK_GAS     => MatterState.Gas,
            MaterialType.AIR          => MatterState.Gas,
            MaterialType.GAS          => MatterState.Gas,
            _                         => MatterState.Solid
        };


        // ═════════════════════════════════════════════════════════════════════
        //  VALIDACIÓN EN EDITOR
        // ═════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-derivar isPorous desde soilSaturationCapacity para evitar desincronización
            structural.isPorous = structural.soilSaturationCapacity > 0f;

            ValidateLayerConsistency();
            ValidateTemperatureTransitions();
            ValidateCombustion();
            ValidateStructural();
        }

        private void ValidateLayerConsistency()
        {
            var expected = InferLayerFromType(materialType);
            if (expected.HasValue && expected.Value != layer)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': materialType={materialType} sugiere " +
                    $"layer={expected.Value}, pero está configurado como layer={layer}. " +
                    "Verifica el asset.", this);
            }
        }

        private void ValidateTemperatureTransitions()
        {
            if (heatingTransition.IsEnabled && layer == MaterialLayer.Gas)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': Gas con heatingTransition es inusual. " +
                    "Los gases no suben a otra fase en esta simulación.", this);
            }

            if (coolingTransition.IsEnabled && layer == MaterialLayer.Ground)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': Ground con coolingTransition es inusual. " +
                    "Los sólidos son el estado más frío; ¿quisiste heatingTransition?", this);
            }

            if (coolingTransition.IsEnabled && heatingTransition.IsEnabled
                && coolingTransition.triggerTemperature >= heatingTransition.triggerTemperature)
            {
                Debug.LogError(
                    $"[MaterialDefinition] '{name}': coolingTransition.triggerTemperature " +
                    $"({coolingTransition.triggerTemperature}°C) debe ser estrictamente menor que " +
                    $"heatingTransition.triggerTemperature ({heatingTransition.triggerTemperature}°C).", this);
            }
        }

        private void ValidateCombustion()
        {
            if (combustion.isFlammable && layer == MaterialLayer.Gas && !atmospheric.isFlammableGas)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': combustion.isFlammable=true pero " +
                    "atmospheric.isFlammableGas=false. Para gases inflamables activa ambos.", this);
            }

            if (combustion.isFlammable && (combustion.subproducts == null || combustion.subproducts.Length == 0))
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': isFlammable=true pero no tiene subproducts " +
                    "configurados. ¿Olvidaste definir ceniza, CO2 u otros residuos?", this);
            }

            if (combustion.isFlammable && combustion.smokeMaterial == MaterialType.EMPTY)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': isFlammable=true pero smokeMaterial=EMPTY. " +
                    "Considera asignar SMOKE si este material debe producir humo visible.", this);
            }

            if (combustion.subproducts != null)
            {
                float totalRatio = 0f;
                foreach (var product in combustion.subproducts)
                    totalRatio += product.massRatio;

                if (totalRatio > 1.5f)
                {
                    Debug.LogWarning(
                        $"[MaterialDefinition] '{name}': la suma de massRatios de subproductos " +
                        $"es {totalRatio:F2}, lo que implica que la combustión crea más masa de la " +
                        "que consume. Revisa los valores.", this);
                }
            }
        }

        private void ValidateStructural()
        {
            if (layer != MaterialLayer.Ground) return;

            if (structural.soilSaturationCapacity > 0f && structural.soilAbsorptionRate <= 0f)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': soilSaturationCapacity > 0 pero " +
                    "soilAbsorptionRate = 0. El suelo nunca absorberá líquido. " +
                    "Asigna un valor > 0 a soilAbsorptionRate.", this);
            }

            if (structural.canCollapse && structural.collapseInto == materialType)
            {
                Debug.LogError(
                    $"[MaterialDefinition] '{name}': collapseInto apunta al mismo material. " +
                    "Esto causaría un bucle infinito de colapso.", this);
            }
        }

        /// <summary>Infiere la capa esperada a partir del MaterialType para detección de errores en Editor.</summary>
        private static MaterialLayer? InferLayerFromType(MaterialType type) => type switch
        {
            MaterialType.WATER        => MaterialLayer.Liquid,
            MaterialType.LAVA         => MaterialLayer.Liquid,
            MaterialType.MUD          => MaterialLayer.Liquid,
            MaterialType.MOLTEN_METAL => MaterialLayer.Liquid,
            MaterialType.MOLTEN_GLASS => MaterialLayer.Liquid,
            MaterialType.STEAM        => MaterialLayer.Gas,
            MaterialType.SMOKE        => MaterialLayer.Gas,
            MaterialType.CO2          => MaterialLayer.Gas,
            MaterialType.NATURAL_GAS  => MaterialLayer.Gas,
            MaterialType.ROCK_GAS     => MaterialLayer.Gas,
            MaterialType.AIR          => MaterialLayer.Gas,
            MaterialType.GAS          => MaterialLayer.Gas,
            MaterialType.STONE        => MaterialLayer.Ground,
            MaterialType.EARTH        => MaterialLayer.Ground,
            MaterialType.SAND         => MaterialLayer.Ground,
            MaterialType.WOOD         => MaterialLayer.Ground,
            MaterialType.ASH          => MaterialLayer.Ground,
            MaterialType.METAL        => MaterialLayer.Ground,
            MaterialType.GLASS        => MaterialLayer.Ground,
            MaterialType.ICE          => MaterialLayer.Ground,
            MaterialType.EMPTY        => null,
            _                         => null
        };
#endif
    }
}