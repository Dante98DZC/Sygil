// Assets/PhysicsSystem/Core/MaterialDefinition.cs
using UnityEngine;

namespace PhysicsSystem.Core
{
    /// <summary>
    /// Define el comportamiento físico completo de un material en la simulación.
    ///
    /// ORGANIZACIÓN POR CAPA:
    ///   - El campo <see cref="layer"/> declara en qué capa vive este material.
    ///   - Las secciones de Inspector "Ground", "Liquid" y "Gas" contienen propiedades
    ///     específicas de cada capa. Solo configura la sección de tu capa.
    ///   - Las secciones "Transitions" y "Combustion" son transversales y aplican
    ///     a cualquier capa cuando corresponda.
    ///
    /// TRANSICIONES DE FASE:
    ///   - <see cref="heatingTransition"/>: lo que este material se convierte si supera
    ///     triggerTemperature (fusión, ebullición).
    ///   - <see cref="coolingTransition"/>: lo que se convierte si baja de triggerTemperature
    ///     (solidificación, condensación).
    ///   - Ambas usan <see cref="PhaseTransition"/>, struct simétrico con latentHeat incluido.
    ///
    /// EJEMPLOS RÁPIDOS:
    ///   ICE   → layer=Ground, heating=(30°→WATER, latent=+5), cooling=None
    ///   WATER → layer=Liquid, heating=(80°→STEAM, latent=+8), cooling=(30°→ICE, latent=-5)
    ///   STEAM → layer=Gas,    heating=None,                   cooling=(80°→WATER, latent=-8)
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsSystem/MaterialDefinition")]
    public class MaterialDefinition : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Tipo de material que define este asset.")]
        public MaterialType materialType;

        [Tooltip(
            "Capa de simulación a la que pertenece: Ground (sólidos), Liquid, Gas. " +
            "Determina qué campo de TileData usa este material.")]
        public MaterialLayer layer;

        // ── Física común ──────────────────────────────────────────────────────

        [Header("Physics — Common")]
        [Tooltip("Velocidad de transferencia de calor con tiles adyacentes. 0 = aislante, 1 = conductor.")]
        [Range(0f, 1f)]
        public float heatTransferCoeff;

        // ── Interacción con el jugador ─────────────────────────────────────────

        [Header("Interaction")]
        [Tooltip("Si true, las entidades no pueden atravesar este tile.")]
        public bool blocksMovement;

        [Tooltip("Si true, la visión no atraviesa este tile (oscurece FOV).")]
        public bool blocksVision;

        [Tooltip("Si true, las entidades se mueven más despacio en este tile.")]
        public bool slowsMovement;

        [Tooltip("Coste de movimiento relativo. 1 = normal, >1 = más lento.")]
        [Range(1f, 10f)]
        public float movementCost = 1f;

        // ── Transiciones de fase ──────────────────────────────────────────────

        [Header("Phase Transitions")]
        [Tooltip(
            "Qué ocurre cuando este material supera heatingTransition.triggerTemperature. " +
            "Ejemplos: STONE(s)→LAVA(l) | WATER(l)→STEAM(g). Dejar en None si no aplica.")]
        public PhaseTransition heatingTransition;

        [Tooltip(
            "Qué ocurre cuando este material baja de coolingTransition.triggerTemperature. " +
            "Ejemplos: STEAM(g)→WATER(l) | WATER(l)→ICE(s). Dejar en None si no aplica.")]
        public PhaseTransition coolingTransition;

        // ── Combustión ────────────────────────────────────────────────────────

        [Header("Combustion")]
        [Tooltip(
            "Comportamiento de combustión. Si combustion.ignitionTemperature = 0, " +
            "este material no arde. Aplica a sólidos (WOOD) y gases inflamables (ROCK_GAS).")]
        public CombustionData combustion;

        // ── Ground: propiedades de sólidos ────────────────────────────────────

        [Header("Ground Layer — Structural (solo para layer = Ground)")]
        [Tooltip("Propiedades estructurales: integridad base, colapso, conductividad eléctrica.")]
        public StructuralData structural;

        // ── Liquid: propiedades de fluidos ────────────────────────────────────

        [Header("Liquid Layer — Fluid (solo para layer = Liquid)")]
        [Tooltip("Propiedades de fluido: viscosidad, absorción por suelo poroso.")]
        public FluidData fluid;

        // ── Gas: propiedades atmosféricas ─────────────────────────────────────

        [Header("Gas Layer — Atmospheric (solo para layer = Gas)")]
        [Tooltip("Propiedades atmosféricas: disipación, permeabilidad, inflamabilidad.")]
        public AtmosphericData atmospheric;

        // ── Propiedades derivadas (no serializar) ─────────────────────────────

        /// <summary>Estado de materia derivado de la capa. No usar como campo serializado.</summary>
        public MatterState MatterState => layer switch
        {
            MaterialLayer.Ground => MatterState.Solid,
            MaterialLayer.Liquid => MatterState.Liquid,
            MaterialLayer.Gas    => MatterState.Gas,
            _                    => MatterState.Solid
        };

        /// <summary>True si este material puede iniciar combustión.</summary>
        public bool IsFlammable => combustion.CanIgnite;

        /// <summary>True si este gas puede arder como gas (R10).</summary>
        public bool IsFlammableGas => layer == MaterialLayer.Gas && atmospheric.isFlammable;

        /// <summary>True si tiene una transición de calentamiento configurada.</summary>
        public bool HasHeatingTransition => heatingTransition.IsEnabled;

        /// <summary>True si tiene una transición de enfriamiento configurada.</summary>
        public bool HasCoolingTransition => coolingTransition.IsEnabled;

        /// <summary>True si este material bloquea el movimiento de gases (no poroso).</summary>
        public bool BlocksGas => GasPermeability < 0.01f;

        // ── Helpers de acceso unificado ───────────────────────────────────────

        /// <summary>
        /// Devuelve la permeabilidad efectiva al gas, teniendo en cuenta la capa.
        /// Los sólidos no porosos tienen permeabilidad 0; gases y líquidos usan atmospheric.
        /// </summary>
        public float GasPermeability => layer switch
        {
            MaterialLayer.Gas    => atmospheric.gasPermeabilityCoeff,
            MaterialLayer.Liquid => 0f,   // los líquidos bloquean gases por defecto
            MaterialLayer.Ground => 0f,   // los sólidos bloquean gases salvo regla especial
            _                    => 0f
        };

        // ── Helpers estáticos ───────────────────────────────────────────────

        /// <summary>
        /// Estado de materia derivado del tipo de material.
        /// </summary>
        public static MatterState GetDefaultState(MaterialType type) => type switch
        {
            MaterialType.WATER        => MatterState.Liquid,
            MaterialType.LAVA         => MatterState.Liquid,
            MaterialType.MOLTEN_METAL => MatterState.Liquid,
            MaterialType.MOLTEN_GLASS => MatterState.Liquid,
            MaterialType.MUD          => MatterState.Liquid,
            MaterialType.STEAM        => MatterState.Gas,
            MaterialType.SMOKE        => MatterState.Gas,
            MaterialType.CO2          => MatterState.Gas,
            MaterialType.ROCK_GAS     => MatterState.Gas,
            MaterialType.AIR          => MatterState.Gas,
            _                         => MatterState.Solid
        };

        // ── Validación en Editor ──────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateLayer();
            ValidateTransitions();
            ValidateCombustion();
        }

        private void ValidateLayer()
        {
            // Detecta mismatches obvios entre layer y materialType
            var expectedLayer = MaterialTypeToLayer(materialType);
            if (expectedLayer.HasValue && expectedLayer.Value != layer)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': materialType={materialType} " +
                    $"sugiere layer={expectedLayer.Value}, pero layer={layer}. " +
                    "Verifica la configuración del asset.", this);
            }
        }

        private void ValidateTransitions()
        {
            if (heatingTransition.IsEnabled)
            {
                var expectedTargetLayer = MaterialTypeToLayer(heatingTransition.resultMaterial);
                // Calentamiento: Ground→Liquid, Liquid→Gas (o Ground→Gas directo como sublimación)
                if (layer == MaterialLayer.Gas)
                {
                    Debug.LogWarning(
                        $"[MaterialDefinition] '{name}': un Gas no debería tener heatingTransition " +
                        "(los gases no se 'calientan' a otra fase en esta simulación).", this);
                }
            }

            if (coolingTransition.IsEnabled)
            {
                if (layer == MaterialLayer.Ground)
                {
                    Debug.LogWarning(
                        $"[MaterialDefinition] '{name}': un sólido Ground no debería tener coolingTransition " +
                        "(ya es el estado más frío). ¿Querías heatingTransition?", this);
                }
            }
        }

        private void ValidateCombustion()
        {
            if (combustion.CanIgnite && layer == MaterialLayer.Gas && !atmospheric.isFlammable)
            {
                Debug.LogWarning(
                    $"[MaterialDefinition] '{name}': tiene ignitionTemperature > 0 " +
                    "pero atmospheric.isFlammable = false. Para gases inflamables activa isFlammable.", this);
            }
        }

        /// <summary>Inferencia de capa esperada por tipo de material para validación.</summary>
        private static MaterialLayer? MaterialTypeToLayer(MaterialType type) => type switch
        {
            MaterialType.WATER        => MaterialLayer.Liquid,
            MaterialType.LAVA         => MaterialLayer.Liquid,
            MaterialType.MOLTEN_METAL => MaterialLayer.Liquid,
            MaterialType.MOLTEN_GLASS => MaterialLayer.Liquid,
            MaterialType.MUD          => MaterialLayer.Liquid,
            MaterialType.STEAM        => MaterialLayer.Gas,
            MaterialType.SMOKE        => MaterialLayer.Gas,
            MaterialType.CO2          => MaterialLayer.Gas,
            MaterialType.ROCK_GAS     => MaterialLayer.Gas,
            MaterialType.AIR          => MaterialLayer.Gas,
            MaterialType.EMPTY        => null,              // EMPTY no tiene capa
            _                         => MaterialLayer.Ground
        };
#endif
    }
}