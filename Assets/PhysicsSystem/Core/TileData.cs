// Assets/PhysicsSystem/Core/TileData.cs
using PhysicsSystem.States;

namespace PhysicsSystem.Core
{
    // ── Estado de materia ─────────────────────────────────────────────────────

    public enum MatterState { Solid, Liquid, Gas }

    // ── Tipos de material ─────────────────────────────────────────────────────
    // Orden: mantener valores existentes en las primeras 8 posiciones
    // para que los ScriptableObjects serializados en Unity no se invaliden.

    public enum MaterialType
    {
        // ── Existentes (no reordenar) ────────────────────────────────────────
        EMPTY        = 0,
        WOOD         = 1,
        METAL        = 2,
        STONE        = 3,
        WATER        = 4,
        GAS          = 5,   // deprecated — mantener por compatibilidad de assets
        EARTH        = 6,
        GLASS        = 7,

        // ── Sólidos nuevos ───────────────────────────────────────────────────
        ICE          = 8,   // H2O sólido  → funde en WATER
        ASH          = 9,   // residuo de combustión, no arde ni funde
        SAND         = 10,  // STONE disgregado → funde en MOLTEN_GLASS

        // ── Líquidos ─────────────────────────────────────────────────────────
        LAVA         = 11,  // STONE fundido  → solidifica en STONE, hierve en ROCK_GAS
        MOLTEN_METAL = 12,  // METAL fundido  → solidifica en METAL
        MOLTEN_GLASS = 13,  // GLASS/SAND fundido → solidifica en GLASS
        MUD          = 14,  // EARTH + WATER  → seca en EARTH

        // ── Gases ─────────────────────────────────────────────────────────────
        STEAM        = 15,  // WATER hervida  → condensa en WATER
        SMOKE        = 16,  // subproducto de combustión, no condensa
        CO2          = 17,  // gas inerte denso
        ROCK_GAS     = 18,  // LAVA vaporizada, muy rara
    }

    // ── TileData v4 ───────────────────────────────────────────────────────────

    public struct TileData
    {
        // ══════════════════════════════════════════════════════════════════════
        // TERRAIN — el suelo base del tile
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Material sólido del suelo. STONE, EARTH, WOOD, ICE, SAND, ASH…
        /// EMPTY = suelo vacío (foso, vacío).
        /// </summary>
        public MaterialType groundMaterial;

        /// <summary>
        /// Altura del tile en el heightmap discreto.
        /// Determina la capacidad de líquido del tile (ver LiquidCapacity).
        /// </summary>
        public TileHeight height;

        /// <summary>Resistencia estructural del terreno [0..100].</summary>
        public float structuralIntegrity;

        /// <summary>Energía eléctrica almacenada en el tile [0..100].</summary>
        public float electricEnergy;

        // ══════════════════════════════════════════════════════════════════════
        // LIQUID — líquido sobre el suelo
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tipo de líquido presente. WATER, LAVA, MUD, MOLTEN_METAL, MOLTEN_GLASS…
        /// EMPTY = sin líquido.
        /// </summary>
        public MaterialType liquidMaterial;

        /// <summary>
        /// Volumen de líquido en litros [0 .. LiquidCapacity].
        /// 0 = seco aunque liquidMaterial != EMPTY (estado transitorio inválido).
        /// El flujo hacia tiles vecinos de menor altura se implementa en LiquidDiffusion (futuro).
        /// </summary>
        public float liquidVolume;

        // ══════════════════════════════════════════════════════════════════════
        // ATMOSPHERE — gas en el aire del tile
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gas predominante en el tile. STEAM, SMOKE, CO2, ROCK_GAS…
        /// EMPTY = aire limpio.
        /// </summary>
        public MaterialType gasMaterial;

        /// <summary>
        /// Densidad / masa de gas [0..100]. 50 = 1 atm (baseline).
        /// La presión diferencial se deriva: gasDensity - SimulationConfig.gasBaseline.
        /// Reemplaza el campo pressure eliminado en v4.
        /// </summary>
        public float gasDensity;

        // ══════════════════════════════════════════════════════════════════════
        // SHARED — propiedad que atraviesa todas las capas
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Temperatura compartida entre todas las capas del tile [0..100].</summary>
        public float temperature;

        // ══════════════════════════════════════════════════════════════════════
        // ENTITY — objetos sobre el tile
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ID de la entidad principal (exclusiva) del tile.
        /// 0 = sin entidad exclusiva.
        /// Categorías exclusivas: Structure, Creature, Player.
        /// Dos entidades exclusivas nunca comparten tile.
        /// </summary>
        public int primaryEntityID;

        /// <summary>
        /// IDs de entidades secundarias (no exclusivas) presentes en el tile.
        /// Categorías no exclusivas: Item, Effect.
        /// Puede ser null (sin entidades secundarias).
        /// </summary>
        public int[] secondaryEntityIDs;

        // ══════════════════════════════════════════════════════════════════════
        // GEOMETRY — derivada
        // ══════════════════════════════════════════════════════════════════════

        public StateFlags derivedStates;

        // ══════════════════════════════════════════════════════════════════════
        // INTERNO
        // ══════════════════════════════════════════════════════════════════════

        public bool dirty;
        public bool wasEmpty;

        // ══════════════════════════════════════════════════════════════════════
        // ATMOSPHERE FLAG — derivado de la geometría
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// true si no hay ningún tile sólido en la columna entre este tile y el tope del grid.
        /// Se calcula en PhysicsGrid.RebuildAtmosphereFlags() — no asignar directamente.
        /// </summary>
        public bool isAtmosphereOpen;

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Capacidad máxima de líquido en litros según la altura del tile.
        /// Tiles sólidos (Low y superiores) no aceptan líquido libre.
        /// 1 tile = 1m², la profundidad disponible escala con TileHeight.
        /// </summary>
        public float LiquidCapacity => height switch
        {
            TileHeight.Deep    => 1000f,
            TileHeight.Shallow =>  500f,
            TileHeight.Ground  =>  200f,
            _                  =>    0f   // Low=1, Wall=2, Tall=3 → sin líquido libre
        };

        // ── Compatibilidad v3 ─────────────────────────────────────────────────
        // Mantenida temporalmente para que las reglas legacy (R01–R12) compilen
        // sin cambios. Marcar usos como obsoletos y migrar regla por regla.

        /// <summary>
        /// Propiedad calculada de compatibilidad con v3.
        /// GET: liquidMaterial > groundMaterial > gasMaterial > EMPTY.
        /// SET: asigna al slot correcto según MatterState.
        /// OBSOLETO — usar groundMaterial / liquidMaterial / gasMaterial directamente.
        /// </summary>
        [System.Obsolete("v3 compat — usar groundMaterial / liquidMaterial / gasMaterial")]
        public MaterialType material
        {
            get
            {
                if (liquidMaterial  != MaterialType.EMPTY) return liquidMaterial;
                if (groundMaterial  != MaterialType.EMPTY) return groundMaterial;
                if (gasMaterial     != MaterialType.EMPTY) return gasMaterial;
                return MaterialType.EMPTY;
            }
            set
            {
                var state = MaterialDefinition.GetDefaultState(value);
                switch (state)
                {
                    case MatterState.Liquid: liquidMaterial  = value; break;
                    case MatterState.Gas:    gasMaterial     = value; break;
                    default:                 groundMaterial  = value; break;
                }
            }
        }
    }
}