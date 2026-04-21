using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using PhysicsSystem.Core;
using PhysicsSystem.Bridge;
using PhysicsSystem.States;
using System.Collections.Generic;

namespace PhysicsSystem.Renderer
{
    public class SimulationRenderer : MonoBehaviour
    {
        [Header("Engine")]
        [SerializeField] private SimulationEngine engine;

        [Header("Tilemaps")]
        [SerializeField] private Tilemap backgroundMap;
        [SerializeField] private Tilemap groundMap;
        [SerializeField] private Tilemap liquidMap;
        [SerializeField] private Tilemap gasMap;
        [SerializeField] private Tilemap overlayMap;
        [SerializeField] private Tilemap ambientMap;
        [SerializeField] private TileVisualLibrary visualLib;
        [SerializeField] private TileBase whiteTile;

        [Header("Config")]
        [SerializeField] private Vector3Int origin = Vector3Int.zero;
        [SerializeField] private float liquidOpacity = 0.5f;
        [SerializeField] private float gasOpacityScale = 0.35f;
        [SerializeField] private float fireOpacityScale = 0.6f;
        [SerializeField] private Color fireColor = new Color(1f, 0.4f, 0f);

        [Header("Overlay")]
        [SerializeField] private bool useTemperatureOverlay = false;
        [SerializeField] private bool useGasDensityOverlay = false;

        [Header("UI")]
        [SerializeField] private Text infoText;

        private EngineNotifier notifier;
        private bool ready;
        private float tickTimer;
        private float ambientTimer;

        private Dictionary<Vector2Int, float> tempValues = new();
        private Dictionary<Vector2Int, float> gasValues = new();

        private HashSet<Vector2Int> dirtySet = new();
        private bool fullRebuildPending = false;

        private void Start()
        {
            if (!CheckRefs()) { enabled = false; return; }

            visualLib.Initialize();
            notifier = engine.Notifier;
            notifier.OnMaterialChanged += OnChange;

            StartCoroutine(ReadyAfter(2));
        }

        private bool CheckRefs() =>
            engine != null && visualLib != null && groundMap != null &&
            liquidMap != null && gasMap != null && overlayMap != null &&
            ambientMap != null && backgroundMap != null && whiteTile != null;

        private System.Collections.IEnumerator ReadyAfter(int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
            ready = true;
            RebuildAll();
        }

        private void OnDestroy()
        {
            if (notifier != null)
            {
                notifier.OnMaterialChanged -= OnChange;
            }
        }

        private void Update()
        {
            if (!ready) return;

            tickTimer += Time.deltaTime;
            if (tickTimer >= 1.0f)
            {
                tickTimer = 0f;
                CollectStats();
                UpdateUI();
                if (useTemperatureOverlay || useGasDensityOverlay)
                    UpdateOverlay();
            }

            ambientTimer += Time.deltaTime;
            if (ambientTimer >= 0.5f)
            {
                ambientTimer = 0f;
                UpdateAmbientTint();
            }
        }

        private void LateUpdate()
        {
            if (!ready) return;

            if (fullRebuildPending)
            {
                RebuildAll();
                fullRebuildPending = false;
                dirtySet.Clear();
                return;
            }

            foreach (var pos in dirtySet)
                DrawCell(pos);

            dirtySet.Clear();
        }

        private void OnChange(Vector2Int p, MaterialType a, MaterialType b)
        {
            if (ready) dirtySet.Add(p);
        }

        private void CollectStats()
        {
            tempValues.Clear();
            gasValues.Clear();

            var grid = engine.Grid;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var tile = grid.GetTile(pos);
                    tempValues[pos] = tile.temperature;
                    gasValues[pos] = tile.gasDensity;
                }
        }

        private void UpdateUI()
        {
            if (infoText == null) return;

            float avgTemp = GetAverage(tempValues, 20f);
            float avgGas = GetAverage(gasValues, 50f);
            float pressure = avgGas - 50f;

            string tempLabel = avgTemp < 20 ? "COLD" : avgTemp > 60 ? "HOT" : "TEMP";

            infoText.text = $"{tempLabel}: {avgTemp:F1}C  GAS: {avgGas:F1}%  P: {pressure:+0.0;-0.0}";
        }

        private float GetAverage(Dictionary<Vector2Int, float> dict, float fallback)
        {
            if (dict.Count == 0) return fallback;
            float sum = 0;
            foreach (var v in dict.Values) sum += v;
            return sum / dict.Count;
        }

        private void UpdateOverlay()
        {
            overlayMap.ClearAllTiles();
            var grid = engine.Grid;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var tile = grid.GetTile(pos);
                    Vector3Int cell = ToCell(pos);

                    Color col = Color.white;

                    if (useTemperatureOverlay)
                        col = TempToColor(tile.temperature);
                    else if (useGasDensityOverlay)
                        col = GasToColor(tile.gasDensity);

                    overlayMap.SetTile(cell, whiteTile);
                    overlayMap.SetTileFlags(cell, TileFlags.None);
                    overlayMap.SetColor(cell, col);
                }
        }

        private void UpdateAmbientTint()
        {
            var grid = engine.Grid;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var tile = grid.GetTile(pos);
                    Vector3Int cell = ToCell(pos);

                    Color ambient = TempToColor(tile.temperature);
                    ambient.a *= 0.15f;

                    ambientMap.SetTile(cell, whiteTile);
                    ambientMap.SetTileFlags(cell, TileFlags.None);
                    ambientMap.SetColor(cell, ambient);
                }
        }

        private Color TempToColor(float temp)
        {
            if (temp < 10f) return new Color(0f, 0f, 1f, 0.3f);
            if (temp < 20f) return new Color(0f, 0.5f, 1f, 0.3f);
            if (temp < 40f) return new Color(0f, 1f, 0f, 0.3f);
            if (temp < 60f) return new Color(1f, 1f, 0f, 0.3f);
            if (temp < 80f) return new Color(1f, 0.5f, 0f, 0.3f);
            return new Color(1f, 0f, 0f, 0.4f);
        }

        private Color GasToColor(float density)
        {
            float t = density / 100f;
            return Color.Lerp(new Color(0, 0, 0, 0), new Color(0.5f, 0.5f, 0.5f, 0.5f), t);
        }

        private void DrawCell(Vector2Int pos)
        {
            Vector3Int cell = ToCell(pos);
            var tile = engine.Grid.GetTile(pos);

            // Ground
            groundMap.SetTile(cell, tile.groundMaterial != MaterialType.EMPTY
                ? visualLib.Get(tile.groundMaterial) : null);

            // Liquid
            liquidMap.SetTile(cell, null);
            if (tile.liquidMaterial != MaterialType.EMPTY && tile.liquidVolume > 0f)
            {
                liquidMap.SetTile(cell, visualLib.Get(tile.liquidMaterial));
                liquidMap.SetTileFlags(cell, TileFlags.None);
                float o = Mathf.Clamp01(tile.liquidVolume / 200f) * liquidOpacity;
                liquidMap.SetColor(cell, Color.white);
            }

            // Gas / Fire
            gasMap.SetTile(cell, null);
            bool onFire = (tile.derivedStates & StateFlags.ON_FIRE) != 0;

            if (tile.gasMaterial != MaterialType.EMPTY || onFire)
            {
                if (tile.gasMaterial != MaterialType.EMPTY)
                    gasMap.SetTile(cell, visualLib.Get(tile.gasMaterial));

                float gasA = Mathf.Clamp01(tile.gasDensity / 100f) * gasOpacityScale;
                float fireA = onFire ? fireOpacityScale * (tile.temperature / 100f) : 0f;
                Color c = onFire ? fireColor : Color.white;
                c.a = Mathf.Clamp01(gasA + fireA);
                gasMap.SetColor(cell, c);
            }
        }

        public void RebuildAll()
        {
            var grid = engine.Grid;
            int w = grid.Width, h = grid.Height;

            backgroundMap.ClearAllTiles();
            groundMap.ClearAllTiles();
            liquidMap.ClearAllTiles();
            gasMap.ClearAllTiles();
            overlayMap.ClearAllTiles();
            ambientMap.ClearAllTiles();

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = ToCell(pos);
                    backgroundMap.SetTile(cell, whiteTile);
                }

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    DrawCell(new Vector2Int(x, y));

            ambientTimer = 0.5f;
        }

        public void ToggleTempOverlay()
        {
            useTemperatureOverlay = !useTemperatureOverlay;
            useGasDensityOverlay = false;
            if (ready) RebuildAll();
        }

        public void ToggleGasOverlay()
        {
            useGasDensityOverlay = !useGasDensityOverlay;
            useTemperatureOverlay = false;
            if (ready) RebuildAll();
        }

        public void ClearOverlay()
        {
            useTemperatureOverlay = false;
            useGasDensityOverlay = false;
            if (ready) overlayMap.ClearAllTiles();
        }

        public void RequestFullRebuild()
        {
            fullRebuildPending = true;
        }

        private Vector3Int ToCell(Vector2Int p) => new Vector3Int(p.x + origin.x, p.y + origin.y, 0);
    }
}