// Assets/PhysicsSystem/Renderer/SimulationRenderer.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using PhysicsSystem.Core;
using PhysicsSystem.Bridge;

namespace PhysicsSystem.Renderer
{
    public class SimulationRenderer : MonoBehaviour
    {
        [SerializeField] private SimulationEngine  _engine;
        [SerializeField] private Tilemap           _tilemap;
        [SerializeField] private TileVisualLibrary _visualLibrary;
        [SerializeField] private Vector3Int        _gridOrigin = Vector3Int.zero;

        private EngineNotifier _notifier;

        private void Start()
        {
            if (_engine == null || _tilemap == null || _visualLibrary == null)
            {
                Debug.LogError("[SimulationRenderer] Faltan referencias en el Inspector.");
                enabled = false;
                return;
            }

            _visualLibrary.Initialize();

            _notifier = _engine.Notifier;
            _notifier.OnMaterialChanged += HandleMaterialChanged;

            // Dos frames de delay: primero garantiza que todos los Awake/Start corrieron,
            // segundo garantiza que TestWorldGenerator.Start() escribió sus tiles.
            StartCoroutine(DrawAfterEngineReady());
        }

        private System.Collections.IEnumerator DrawAfterEngineReady()
        {
            yield return null;
            yield return null;
            DrawFullGrid();
        }

        private void OnDestroy()
        {
            if (_notifier != null)
                _notifier.OnMaterialChanged -= HandleMaterialChanged;
        }

        /// <summary>
        /// Fuerza un redibujado completo del grid.
        /// Llamar desde TestWorldGenerator tras Reset() o LoadPreset().
        /// </summary>
        public void Refresh() => DrawFullGrid();

        private void HandleMaterialChanged(Vector2Int pos, MaterialType prev, MaterialType next)
        {
            Debug.Log($"[Renderer] {pos}: {prev} → {next}");
            _tilemap.SetTile(SimToTilemap(pos), _visualLibrary.Get(next));
        }

        private void DrawFullGrid()
        {
            int w     = _engine.Grid.Width;
            int h     = _engine.Grid.Height;
            int count = w * h;

            var positions = new Vector3Int[count];
            var tiles     = new TileBase[count];

            int idx = 0;
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                positions[idx] = SimToTilemap(new Vector2Int(x, y));
                tiles[idx]     = _visualLibrary.Get(_engine.Grid.GetTile(x, y).material);
                idx++;
            }

            _tilemap.SetTiles(positions, tiles);
        }

        private Vector3Int SimToTilemap(Vector2Int pos) =>
            new Vector3Int(pos.x + _gridOrigin.x, pos.y + _gridOrigin.y, 0);
    }
}