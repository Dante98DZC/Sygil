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
        [SerializeField] private Tilemap           _groundTilemap;
        [SerializeField] private Tilemap           _liquidTilemap;
        [SerializeField] private Tilemap           _gasTilemap;
        [SerializeField] private TileVisualLibrary _visualLibrary;
        [SerializeField] private Vector3Int        _gridOrigin = Vector3Int.zero;

        [Header("Visual Settings")]
        [SerializeField] private float _liquidOpacityScale = 0.5f;
        [SerializeField] private float _gasOpacityScale = 0.35f;

        private EngineNotifier _notifier;

        private void Start()
        {
            if (_engine == null || _visualLibrary == null)
            {
                Debug.LogError("[SimulationRenderer] Faltan referencias en el Inspector.");
                enabled = false;
                return;
            }

            if (_groundTilemap == null || _liquidTilemap == null || _gasTilemap == null)
            {
                Debug.LogError("[SimulationRenderer] Faltan referencias a tilemaps (ground/liquid/gas).");
                enabled = false;
                return;
            }

            _visualLibrary.Initialize();

            _notifier = _engine.Notifier;
            _notifier.OnMaterialChanged += HandleMaterialChanged;

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

        public void Refresh() => DrawFullGrid();

        private void HandleMaterialChanged(Vector2Int pos, MaterialType prev, MaterialType next)
        {
            Debug.Log($"[Renderer] {pos}: {prev} → {next}");
            DrawFullGrid();
        }

        private void DrawFullGrid()
        {
            int w = _engine.Grid.Width;
            int h = _engine.Grid.Height;

            _groundTilemap.ClearAllTiles();
            _liquidTilemap.ClearAllTiles();
            _gasTilemap.ClearAllTiles();

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Vector3Int tilemapPos = SimToTilemap(pos);
                var tile = _engine.Grid.GetTile(pos);

                // Capa base: suelo siempre visible
                if (tile.groundMaterial != MaterialType.EMPTY)
                {
                    var groundTile = _visualLibrary.Get(tile.groundMaterial);
                    if (groundTile != null)
                    {
                        _groundTilemap.SetTile(tilemapPos, groundTile);
                    }
                }

                // Capa líquida: transparencia según volumen
                if (tile.liquidMaterial != MaterialType.EMPTY && tile.liquidVolume > 0f)
                {
                    var liquidTile = _visualLibrary.Get(tile.liquidMaterial);
                    if (liquidTile != null)
                    {
                        _liquidTilemap.SetTile(tilemapPos, liquidTile);
                        float opacity = Mathf.Clamp01(tile.liquidVolume / 200f) * _liquidOpacityScale;
                        _liquidTilemap.SetTileFlags(tilemapPos, TileFlags.None);
                        _liquidTilemap.SetColor(tilemapPos, new Color(1f, 1f, 1f, opacity));
                    }
                }

                // Capa de gas: transparencia según densidad
                if (tile.gasMaterial != MaterialType.EMPTY && tile.gasDensity > 0f)
                {
                    var gasTile = _visualLibrary.Get(tile.gasMaterial);
                    if (gasTile != null)
                    {
                        _gasTilemap.SetTile(tilemapPos, gasTile);
                        float opacity = Mathf.Clamp01(tile.gasDensity / 100f) * _gasOpacityScale;
                        _gasTilemap.SetTileFlags(tilemapPos, TileFlags.None);
                        _gasTilemap.SetColor(tilemapPos, new Color(1f, 1f, 1f, opacity));
                    }
                }
            }
        }

        private Vector3Int SimToTilemap(Vector2Int pos) =>
            new Vector3Int(pos.x + _gridOrigin.x, pos.y + _gridOrigin.y, 0);
    }
}