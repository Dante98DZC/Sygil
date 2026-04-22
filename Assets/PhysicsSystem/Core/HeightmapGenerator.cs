// Assets/PhysicsSystem/Core/HeightmapGenerator.cs
namespace PhysicsSystem.Core
{
    /// <summary>
    /// Genera un heightmap discreto estático para el mundo de prueba.
    /// No depende de MonoBehaviour — puede usarse desde tests y desde escena.
    /// </summary>
    public static class HeightmapGenerator
    {
        /// <summary>
        /// Genera un heightmap + materials por defecto para un cuarto cerrado.
        /// </summary>
        public static (TileHeight[,] height, TileData[,] tiles) GenerateTestWorld(int width, int height)
        {
            var heights = GenerateStaticRoom(width, height);
            var tiles = InitializeMaterials(heights);
            return (heights, tiles);
        }

        /// <summary>
        /// Inicializa materials por defecto para cada tile según su altura.
        /// </summary>
        public static TileData[,] InitializeMaterials(TileHeight[,] heights)
        {
            int w = heights.GetLength(0);
            int h = heights.GetLength(1);
            var tiles = new TileData[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var tile = new TileData
                    {
                        height             = heights[x, y],
                        groundMaterial    = GetDefaultGround(heights[x, y]),
                        liquidMaterial   = MaterialType.EMPTY,
                        liquidVolume     = 0f,
                        gasMaterial      = MaterialType.EMPTY,
                        gasConcentration  = 0f,
                        temperature    = 20f,
                        structuralIntegrity = 100f,
                        electricEnergy = 0f,
                        dirty          = false,
                        wasEmpty       = false
                    };
                    tiles[x, y] = tile;
                }
            }
            return tiles;
        }

        private static MaterialType GetDefaultGround(TileHeight height) => height switch
        {
            TileHeight.Wall   => MaterialType.STONE,
            TileHeight.Tall   => MaterialType.STONE,
            TileHeight.Low   => MaterialType.STONE,
            TileHeight.Ground => MaterialType.EARTH,
            TileHeight.Shallow => MaterialType.EMPTY,
            TileHeight.Deep  => MaterialType.EMPTY,
            _              => MaterialType.STONE
        };

        /// <summary>
        /// Genera un mapa con un cuarto cerrado, zona baja, media y alta.
        /// </summary>
        public static TileHeight[,] GenerateStaticRoom(int width, int height)
        {
            var map = new TileHeight[width, height];

            // Default: todo Ground
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    map[x, y] = TileHeight.Ground;

            // ── Borde exterior — Wall ────────────────────────────────────────
            for (int x = 0; x < width; x++)
            {
                map[x, 0]          = TileHeight.Wall;
                map[x, height - 1] = TileHeight.Wall;
            }
            for (int y = 0; y < height; y++)
            {
                map[0, y]         = TileHeight.Wall;
                map[width - 1, y] = TileHeight.Wall;
            }

            // ── Cuarto interior — paredes internas ───────────────────────────
            // Habitación centrada, ocupa ~mitad del mapa
            int roomX1 = width  / 4;
            int roomX2 = width  * 3 / 4;
            int roomY1 = height / 4;
            int roomY2 = height * 3 / 4;

            for (int x = roomX1; x <= roomX2; x++)
            {
                map[x, roomY1] = TileHeight.Wall;
                map[x, roomY2] = TileHeight.Wall;
            }
            for (int y = roomY1; y <= roomY2; y++)
            {
                map[roomX1, y] = TileHeight.Wall;
                map[roomX2, y] = TileHeight.Wall;
            }

            // Puerta en la pared sur del cuarto (3 tiles de ancho)
            int doorX = (roomX1 + roomX2) / 2;
            map[doorX - 1, roomY1] = TileHeight.Ground;
            map[doorX,     roomY1] = TileHeight.Ground;
            map[doorX + 1, roomY1] = TileHeight.Ground;

            // ── Zona alta — plataforma elevada (Low) dentro del cuarto ───────
            int highX1 = roomX1 + 2;
            int highX2 = roomX1 + (roomX2 - roomX1) / 2 - 1;
            int highY1 = roomY1 + 2;
            int highY2 = roomY2 - 2;

            for (int x = highX1; x <= highX2; x++)
                for (int y = highY1; y <= highY2; y++)
                    map[x, y] = TileHeight.Low;

            // Columna central en zona alta
            map[(highX1 + highX2) / 2, (highY1 + highY2) / 2] = TileHeight.Tall;

            // ── Zona baja — depresión (Shallow) fuera del cuarto, esquina SW ─
            int pitX1 = 2;
            int pitX2 = roomX1 - 2;
            int pitY1 = 2;
            int pitY2 = roomY1 - 2;

            for (int x = pitX1; x <= pitX2; x++)
                for (int y = pitY1; y <= pitY2; y++)
                    map[x, y] = TileHeight.Shallow;

            // Foso profundo en el centro de la zona baja
            int deepX = (pitX1 + pitX2) / 2;
            int deepY = (pitY1 + pitY2) / 2;
            map[deepX,     deepY]     = TileHeight.Deep;
            map[deepX + 1, deepY]     = TileHeight.Deep;
            map[deepX,     deepY + 1] = TileHeight.Deep;

            return map;
        }
    }
}