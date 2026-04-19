using UnityEngine;
using System.Collections.Generic;
using PhysicsSystem.Config;

namespace PhysicsSystem.Core
{
    public class PhysicsGrid
    {
        private TileData[,] _grid;
        private MaterialLibrary _library;
        public int Width { get; }
        public int Height { get; }
        public HashSet<Vector2Int> ActiveTiles { get; } = new();

        private static readonly Vector2Int[] _neighborOffsets =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public PhysicsGrid(int width, int height, MaterialLibrary library)
        {
            Width = width;
            Height = height;
            _library = library;
            _grid = new TileData[width, height];
        }

        public ref TileData GetTile(Vector2Int pos) => ref _grid[pos.x, pos.y];
        public ref TileData GetTile(int x, int y) => ref _grid[x, y];

        public bool InBounds(Vector2Int pos) =>
            pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

        public Vector2Int[] GetNeighborPositions(Vector2Int pos)
        {
            var result = new List<Vector2Int>(4);
            foreach (var offset in _neighborOffsets)
            {
                var n = pos + offset;
                if (InBounds(n)) result.Add(n);
            }
            return result.ToArray();
        }

        public void WriteNeighbors(Vector2Int pos, TileData[] updated)
        {
            var positions = GetNeighborPositions(pos);
            for (int i = 0; i < positions.Length; i++)
            {
                _grid[positions[i].x, positions[i].y] = updated[i];
                // Solo marcar dirty si el vecino ya era activo o cambió de material
                if (ActiveTiles.Contains(positions[i]) || updated[i].material != MaterialType.EMPTY)
                    MarkDirty(positions[i]);
            }
        }

        public TileData[] GetNeighbors(Vector2Int pos)
        {
            var positions = GetNeighborPositions(pos);
            var result = new TileData[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                result[i] = _grid[positions[i].x, positions[i].y];
            return result;
        }

        public TileData[] GetNeighborsFromFrozen(Vector2Int pos, Dictionary<Vector2Int, TileData> frozen)
        {
            var positions = GetNeighborPositions(pos);
            var result = new TileData[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                frozen.TryGetValue(positions[i], out result[i]);
            return result;
        }

        public void SetTile(Vector2Int pos, TileData data)
        {
            _grid[pos.x, pos.y] = data;
            MarkDirty(pos);
        }

        public void MarkDirty(Vector2Int pos)
        {
            _grid[pos.x, pos.y].dirty = true;
            ActiveTiles.Add(pos);
        }

        public void ClearDirtyFlags()
        {
            foreach (var pos in ActiveTiles)
                _grid[pos.x, pos.y].dirty = false;
        }

        /// <summary>
        /// Elimina el tile de ActiveTiles sin condiciones propias.
        /// La decisión de estabilidad es responsabilidad exclusiva de DecaySystem,
        /// que tiene acceso a integrityBase por material y a la tolerancia configurada.
        /// </summary>
        public void TryDeactivateTile(Vector2Int pos)
        {
            ActiveTiles.Remove(pos);
        }

        public MaterialDefinition GetMaterialDef(Vector2Int pos, MaterialLayer layer)
        {
            var mat = layer switch
            {
                MaterialLayer.Ground => _grid[pos.x, pos.y].groundMaterial,
                MaterialLayer.Liquid => _grid[pos.x, pos.y].liquidMaterial,
                MaterialLayer.Gas => _grid[pos.x, pos.y].gasMaterial,
                _ => MaterialType.EMPTY
            };
            return _library.Get(mat);
        }

        public MaterialDefinition GetMaterialDef(Vector2Int pos) =>
            GetMaterialDef(pos, MaterialLayer.Ground);

        /// <summary>
        /// Recalcula isAtmosphereOpen para todos los tiles.
        /// Un tile es "atmósfera abierta" si no hay ningún tile sólido en su columna
        /// entre su posición y el tope del grid.
        /// Llamar al inicializar el grid y cuando un tile cambie a estado sólido o deje de serlo.
        /// </summary>
        public void RebuildAtmosphereFlags()
        {
            for (int x = 0; x < Width; x++)
            {
                bool solidFound = false;
                for (int y = Height - 1; y >= 0; y--)
                {
                    ref var tile = ref _grid[x, y];
                    if (tile.groundMaterial != MaterialType.EMPTY)
                    {
                        solidFound = true;
                    }
                    tile.isAtmosphereOpen = !solidFound;
                }
            }
        }
    }
}