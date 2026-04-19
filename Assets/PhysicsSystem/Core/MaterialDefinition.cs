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