// Assets/PhysicsSystem/Core/TileData.cs
// Tipos de simulación en PhysicsTypes.cs

using PhysicsSystem.States;

namespace PhysicsSystem.Core
{
    // ── TileData v6 ───────────────────────────────────────────────────────────

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

        /// <summary>
        /// Volumen de líquido absorbido por el suelo (0..soilSaturationCapacity).
        /// Campo separado de structuralIntegrity para evitar colisión semántica.
        /// </summary>
        public float soilMoisture;

        // ══════════════════════════════════════════════════════════════════════
        // ATMOSPHERE — gas en el aire del tile
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gas predominante en el tile. STEAM, SMOKE, CO2, ROCK_GAS…
        /// EMPTY = aire limpio.
        /// </summary>
        public MaterialType gasMaterial;

        /// <summary>
        /// Concentración de gas en el tile [0..100%]. 0 = vacío, 100 = saturado.
        /// La presión es emergente — resultado de fuentes activas, no de baseline inyectada.
        /// Tiles abiertos al exterior difunden hacia atmosphereConcentration (normalmente 0%).
        /// </summary>
        public float gasConcentration;

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
            _                  =>    0f
        };

        public MaterialType GetActiveMaterial()
        {
            if (gasMaterial != MaterialType.EMPTY)    return gasMaterial;
            if (liquidMaterial != MaterialType.EMPTY) return liquidMaterial;
            return groundMaterial;
        }

        public static TileData Create(MaterialType ground, float temperature = 20f, float integrity = 100f)
            => new TileData { groundMaterial = ground, temperature = temperature, structuralIntegrity = integrity };
    }
}