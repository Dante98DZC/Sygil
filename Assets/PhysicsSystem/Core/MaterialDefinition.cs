// Assets/PhysicsSystem/Core/MaterialDefinition.cs
using UnityEngine;

namespace PhysicsSystem.Core
{
    [CreateAssetMenu(menuName = "PhysicsSystem/MaterialDefinition")]
    public class MaterialDefinition : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────────────────────

        [Header("Identidad")]
        public MaterialType materialType;
        public MatterState  matterState;

        // ── Física ────────────────────────────────────────────────────────────

        [Header("Física")]
        [Range(0, 1)]   public float heatTransferCoeff;
        [Range(0, 1)]   public float electricTransferCoeff;
        [Range(0, 1)]   public float gasPermeabilityCoeff;
        [Range(0, 1)]   public float flammabilityCoeff;
        [Range(0, 100)] public float integrityBase;

        // ── Interacción ───────────────────────────────────────────────────────

        [Header("Interacción")]
        public bool           blocksMovement;
        public bool           blocksVision;
        public bool           slowsMovement;
        [Range(0, 10)] public float movementCost = 1f;

        // ── Transiciones de estado: calentamiento ─────────────────────────────
        // Sólido → Líquido → Gas

        [Header("Calentamiento")]

        /// <summary>
        /// Temperatura a la que este sólido se funde en liquidForm.
        /// 0 = no se funde (WOOD, ASH, EARTH).
        /// </summary>
        [Range(0, 100)] public float        meltingPoint  = 0f;

        /// <summary>
        /// Material líquido resultante de la fusión.
        /// Solo relevante si meltingPoint mayor que 0.
        /// </summary>
        public MaterialType liquidForm = MaterialType.EMPTY;

        /// <summary>
        /// Temperatura a la que este líquido hierve en gasForm.
        /// 0 = no hierve (LAVA, MUD).
        /// </summary>
        [Range(0, 100)] public float        boilingPoint  = 0f;

        /// <summary>
        /// Material gaseoso resultante de la ebullición.
        /// Solo relevante si boilingPoint mayor que 0.
        /// </summary>
        public MaterialType gasForm    = MaterialType.EMPTY;

        // ── Transiciones de estado: enfriamiento ──────────────────────────────
        // Gas → Líquido → Sólido

        [Header("Enfriamiento")]

        /// <summary>
        /// Temperatura a la que este gas condensa en condensedForm.
        /// Normalmente igual al boilingPoint del líquido correspondiente.
        /// 0 = no condensa (SMOKE, CO2).
        /// </summary>
        [Range(0, 100)] public float        condensationPoint = 0f;

        /// <summary>
        /// Material líquido resultante de la condensación.
        /// </summary>
        public MaterialType condensedForm = MaterialType.EMPTY;

        /// <summary>
        /// Temperatura a la que este líquido solidifica en solidForm.
        /// Normalmente igual al meltingPoint del sólido correspondiente.
        /// 0 = no solidifica (MUD, LAVA — solidifica via regla especial).
        /// </summary>
        [Range(0, 100)] public float        freezingPoint  = 0f;

        /// <summary>
        /// Material sólido resultante de la solidificación.
        /// </summary>
        public MaterialType solidForm   = MaterialType.EMPTY;

        // ── Combustión ────────────────────────────────────────────────────────

        [Header("Combustión")]

        /// <summary>
        /// Temperatura mínima para que R01 arda este material.
        /// 0 = no arde (STONE, METAL, WATER, ICE…).
        /// </summary>
        [Range(0, 100)] public float        ignitionTemperature = 0f;

        /// <summary>
        /// Material sólido que queda en groundMaterial tras la combustión completa.
        /// WOOD → ASH, resto → EMPTY.
        /// </summary>
        public MaterialType burnInto    = MaterialType.EMPTY;

        /// <summary>
        /// Gas producido durante la combustión que va a gasMaterial.
        /// WOOD → SMOKE, GAS material → CO2.
        /// </summary>
        public MaterialType smokeForm   = MaterialType.SMOKE;

        // ── Colapso estructural ───────────────────────────────────────────────

        [Header("Colapso estructural")]

        /// <summary>
        /// Material resultante cuando R07 colapsa por integridad baja.
        /// Compatibilidad con v2 — en materiales nuevos usar burnInto o solidForm.
        /// </summary>
        public MaterialType collapseInto = MaterialType.EMPTY;

        // ── Calor Latente ─────────────────────────────────────────────────────
        // Energía absorbida/liberada durante transiciones de fase.
        // El mismo valor aplica en ambas direcciones (fusión/solidificación, ebullición/condensación).

        [Header("Calor Latente")]

        /// <summary>
        /// Energía absorbida durante fusión (sólido → líquido) en unidades de temperatura.
        /// Mismo valor se libera durante solidificación (líquido → sólido).
        /// </summary>
        public float latentHeatOfFusion = 0f;

        /// <summary>
        /// Energía absorbida durante ebullición (líquido → gas) en unidades de temperatura.
        /// Mismo valor se libera durante condensación (gas → líquido).
        /// </summary>
        public float latentHeatOfVaporization = 0f;

        // ── Filtración ─────────────────────────────────────────────────────

        [Header("Filtración")]

        /// <summary>
        /// Velocidad de absorción de líquido por tick (litros/tick).
        /// Solo activo si isPorous = true.
        /// </summary>
        [Range(0f, 10f)] public float soilAbsorptionRate = 0f;

        /// <summary>
        /// Volumen máximo de líquido que este material puede retener.
        /// </summary>
        [Range(0f, 500f)] public float soilSaturationCapacity = 0f;

        /// <summary>
        /// Si true, este material absorbe líquidos de la capa superior.
        /// </summary>
        public bool isPorous = false;

        // ── Disipación ─────────────────────────────────────────────────────

        [Header("Disipación")]

        /// <summary>
        /// Multiplicador de velocidad de disipación en atmósfera abierta.
        /// 1.0 = base, >1 = más rápido, <1 = más lento. Mínimo 0 para gases
        /// que no disipen (ej. bolsa de CO2 confinada).
        /// </summary>
        [Range(0f, 5f)] public float dissipationMultiplier = 1f;

        // ── Viscosidad ─────────────────────────────────────────────────────

        [Header("Viscosidad")]

        /// <summary>
        /// Coeficiente de flujo para líquidos. 1.0 = agua (base), valores menores
        /// = más viscoso. No aplica a sólidos ni gases.
        /// </summary>
        [Range(0.01f, 1f)] public float viscosity = 1f;

        // ── Campos v2 obsoletos ───────────────────────────────────────────────
        // Mantenidos para que los ScriptableObjects existentes no pierdan datos.
        // No usar en código nuevo — usar meltingPoint / liquidForm en su lugar.

        [Header("Obsoleto — no usar en código nuevo")]
        [System.Obsolete("Usar meltingPoint > 0 en su lugar")]
        public bool         hasMeltingPoint     = false;
        [System.Obsolete("Usar meltingPoint en su lugar")]
        [Range(0, 100)] public float meltingTemperature = 100f;
        [System.Obsolete("Usar liquidForm en su lugar")]
        public MaterialType meltInto = MaterialType.EMPTY;

        // ── Helper estático ───────────────────────────────────────────────────

        /// <summary>
        /// Estado de materia por defecto de cada MaterialType.
        /// Usado por TileData.material setter para compatibilidad con reglas antiguas.
        /// </summary>
        public static MatterState GetDefaultState(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.WATER:
                case MaterialType.LAVA:
                case MaterialType.MOLTEN_METAL:
                case MaterialType.MOLTEN_GLASS:
                case MaterialType.MUD:
                    return MatterState.Liquid;

                case MaterialType.STEAM:
                case MaterialType.SMOKE:
                case MaterialType.CO2:
                case MaterialType.ROCK_GAS:
                    return MatterState.Gas;

                default:
                    return MatterState.Solid;
            }
        }
    }
}