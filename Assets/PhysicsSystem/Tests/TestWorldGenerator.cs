// Assets/PhysicsSystem/Tests/TestWorldGenerator.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Powers;
using PhysicsSystem.Renderer;

namespace PhysicsSystem.Tests
{
    // ── Parámetros configurables por zona ────────────────────────────────────

    [System.Serializable]
    public struct ZoneParams_Combustion
    {
        [Range(0f, 100f)] public float ignitionTemperature;
        [Range(0f, 100f)] public float woodRowTemperature;

        public static ZoneParams_Combustion Default => new ZoneParams_Combustion
        {
            ignitionTemperature = 85f,
            woodRowTemperature  = 0f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Evaporation
    {
        [Range(0f, 100f)] public float waterTemperature;

        public static ZoneParams_Evaporation Default => new ZoneParams_Evaporation
        {
            waterTemperature = 90f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Electric
    {
        [Range(0f, 100f)] public float metalCharge;

        public static ZoneParams_Electric Default => new ZoneParams_Electric
        {
            metalCharge = 90f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Pressure
    {
        [Range(0f, 100f)] public float centralPressure;

        public static ZoneParams_Pressure Default => new ZoneParams_Pressure
        {
            centralPressure = 95f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Structural
    {
        [Range(0f, 100f)] public float integrity;
        [Range(0f, 100f)] public float heatTriggerTemperature;

        public static ZoneParams_Structural Default => new ZoneParams_Structural
        {
            integrity             = 10f,
            heatTriggerTemperature = 80f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Humidity
    {
        [Range(0f, 100f)] public float hotWoodTemperature;
        [Range(0f, 100f)] public float waterHumidity;
        [Range(0f, 100f)] public float waterTemperature;

        public static ZoneParams_Humidity Default => new ZoneParams_Humidity
        {
            hotWoodTemperature = 85f,
            waterHumidity      = 90f,
            waterTemperature   = 75f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_GasIgnition
    {
        [Range(0f, 100f)] public float gasDensity;
        [Range(0f, 100f)] public float gasTemperature;

        public static ZoneParams_GasIgnition Default => new ZoneParams_GasIgnition
        {
            gasDensity      = 80f,
            gasTemperature  = 75f,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Gas
    {
        [Range(0f, 100f)] public float globalGasDensity;

        public static ZoneParams_Gas Default => new ZoneParams_Gas
        {
            globalGasDensity = 50f,
        };
    }

    // ── Escenarios predefinidos ───────────────────────────────────────────────

    [System.Serializable]
    public struct ScenarioPreset
    {
        public string name;
        public bool   enableCombustion;
        public bool   enableEvaporation;
        public bool   enableElectric;
        public bool   enablePressure;
        public bool   enableStructural;
        public bool   enableHumidity;
        public bool   enableGasIgnition;
        public bool   useHeightmap;

        public ZoneParams_Combustion combustion;
        public ZoneParams_Evaporation evaporation;
        public ZoneParams_Electric   electric;
        public ZoneParams_Pressure   pressure;
        public ZoneParams_Structural structural;
        public ZoneParams_Humidity   humidity;
        public ZoneParams_GasIgnition gasIgnition;
        public ZoneParams_Gas        gas;
    }

    // ── Generador principal ───────────────────────────────────────────────────

    /// <summary>
    /// Genera un mapa de prueba que expone las 10 reglas de simulación.
    /// Controles runtime:
    ///   R       — Reset al estado inicial
    ///   1–9     — Cargar escenario predefinido (si existe en _presets)
    ///   Space   — Lanzar testPower con PowerCaster
    /// </summary>
    public class TestWorldGenerator : MonoBehaviour
    {
        // ── Referencias ──────────────────────────────────────────────────────

        [Header("Refs")]
        [SerializeField] private SimulationEngine  _engine;
        [SerializeField] private SimulationRenderer _renderer;
        [SerializeField] private PowerCaster       _caster;
        [SerializeField] private CompiledPower     _testPower;

        // ── Toggles de zona ──────────────────────────────────────────────────

        [Header("Zonas activas")]
        [SerializeField] private bool _zone_combustion  = true;
        [SerializeField] private bool _zone_evaporation = true;
        [SerializeField] private bool _zone_electric    = true;
        [SerializeField] private bool _zone_pressure    = true;
        [SerializeField] private bool _zone_structural  = true;
        [SerializeField] private bool _zone_humidity    = true;
        [SerializeField] private bool _zone_gasIgnition = true;
        [SerializeField] private bool _useHeightmap     = true;

        // ── Parámetros por zona ───────────────────────────────────────────────

        [Header("Parámetros por zona")]
        [SerializeField] private ZoneParams_Combustion  _combustion  = ZoneParams_Combustion.Default;
        [SerializeField] private ZoneParams_Evaporation _evaporation = ZoneParams_Evaporation.Default;
        [SerializeField] private ZoneParams_Electric    _electric    = ZoneParams_Electric.Default;
        [SerializeField] private ZoneParams_Pressure    _pressure    = ZoneParams_Pressure.Default;
        [SerializeField] private ZoneParams_Structural  _structural  = ZoneParams_Structural.Default;
        [SerializeField] private ZoneParams_Humidity    _humidity    = ZoneParams_Humidity.Default;
        [SerializeField] private ZoneParams_GasIgnition _gasIgnition = ZoneParams_GasIgnition.Default;
        [SerializeField] private ZoneParams_Gas         _gas         = ZoneParams_Gas.Default;

        // ── Escenarios ───────────────────────────────────────────────────────

        [Header("Escenarios predefinidos (teclas 1–9)")]
        [SerializeField] private ScenarioPreset[] _presets = new ScenarioPreset[0];

        // ── Labels ───────────────────────────────────────────────────────────

        [Header("Labels")]
        [SerializeField] private bool _showZoneLabels = true;

        // Coordenadas de cada label (en tile-space, centro de la zona)
        private static readonly (string label, Vector2Int tilePos)[] ZoneLabels =
        {
            ("R01 Combustion",         new Vector2Int(16, 3)),
            ("R02 Evaporation",        new Vector2Int(3,  4)),
            ("R03+04 Electric",        new Vector2Int(9,  4)),
            ("R05+06 Pressure",        new Vector2Int(16, 4)),
            ("R07 Structural",         new Vector2Int(22, 3)),
            ("R08+09 Humidity",        new Vector2Int(26, 3)),
            ("R10 GasIgnition",        new Vector2Int(6,  7)),
        };

        private GUIStyle _labelStyle;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            if (_engine == null)
            {
                Debug.LogError("[TestWorldGenerator] Falta referencia al engine.");
                return;
            }

            GenerateWorld();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                Reset();

            if (Input.GetKeyDown(KeyCode.Space) && _caster != null && _testPower != null)
                _caster.Cast(_testPower, new Vector2Int(5, 5), Vector2.up);

            for (int i = 0; i < _presets.Length && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    LoadPreset(_presets[i]);
            }
        }

        private void OnGUI()
        {
            if (!_showZoneLabels) return;

            EnsureLabelStyle();

            Camera cam = Camera.main;
            if (cam == null) return;

            foreach (var (label, tilePos) in ZoneLabels)
            {
                // Centro del tile en world-space (asume cell size 1x1 con origen en 0,0)
                Vector3 worldPos    = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
                Vector3 screenPos   = cam.WorldToScreenPoint(worldPos);

                // Descartar tiles fuera de pantalla o detrás de la cámara
                if (screenPos.z < 0f) continue;

                // GUI usa Y invertida
                float guiY = Screen.height - screenPos.y;
                GUI.Label(new Rect(screenPos.x - 60f, guiY - 20f, 120f, 24f), label, _labelStyle);
            }
        }

        // ── API pública ──────────────────────────────────────────────────────

        [ContextMenu("Reset World")]
        public void Reset()
        {
            for (int x = 0; x < _engine.Grid.Width; x++)
            for (int y = 0; y < _engine.Grid.Height; y++)
                _engine.Grid.SetTile(new Vector2Int(x, y), default);

            GenerateWorld();
            Debug.Log("[TestWorldGenerator] Reset.");
        }

        public void LoadPreset(ScenarioPreset preset)
        {
            _zone_combustion  = preset.enableCombustion;
            _zone_evaporation = preset.enableEvaporation;
            _zone_electric    = preset.enableElectric;
            _zone_pressure    = preset.enablePressure;
            _zone_structural  = preset.enableStructural;
            _zone_humidity    = preset.enableHumidity;
            _zone_gasIgnition = preset.enableGasIgnition;
            _useHeightmap     = preset.useHeightmap;

            _combustion  = preset.combustion;
            _evaporation = preset.evaporation;
            _electric    = preset.electric;
            _pressure    = preset.pressure;
            _structural  = preset.structural;
            _humidity    = preset.humidity;
            _gasIgnition = preset.gasIgnition;
            _gas         = preset.gas;

            Reset();
            Debug.Log($"[TestWorldGenerator] Escenario cargado: '{preset.name}'");
        }

        // ── Generación ───────────────────────────────────────────────────────

        private void GenerateWorld()
        {
            if (_useHeightmap)
                ApplyHeightmap();

            FillGasLayer();
            FillRow(0, MaterialType.STONE, structuralIntegrity: 100f);

            if (_zone_combustion)  PlaceZone_Combustion();
            if (_zone_evaporation) PlaceZone_Evaporation();
            if (_zone_electric)    PlaceZone_Electric();
            if (_zone_pressure)    PlaceZone_Pressure();
            if (_zone_structural)  PlaceZone_Structural();
            if (_zone_humidity)    PlaceZone_Humidity();
            if (_zone_gasIgnition) PlaceZone_GasIgnition();

            Debug.Log($"[TestWorldGenerator] Mundo generado. ActiveTiles: {_engine.Grid.ActiveTiles.Count}");
            _renderer?.Refresh();
        }

        // ── Zonas ─────────────────────────────────────────────────────────────

        private void PlaceZone_Combustion()
        {
            FillRow(2, MaterialType.WOOD);
            Place(16, 2, MaterialType.WOOD, temperature: _combustion.ignitionTemperature);
        }

        private void PlaceZone_Evaporation()
        {
            Place(2, 4, MaterialType.WATER, temperature: _evaporation.waterTemperature);
            Place(3, 4, MaterialType.WATER, temperature: _evaporation.waterTemperature);
            Place(4, 4, MaterialType.WATER, temperature: _evaporation.waterTemperature);
        }

        private void PlaceZone_Electric()
        {
            Place(8,  4, MaterialType.METAL, electricEnergy: _electric.metalCharge);
            Place(9,  4, MaterialType.METAL);
            Place(10, 4, MaterialType.METAL);
            Place(11, 4, MaterialType.WATER);
        }

        private void PlaceZone_Pressure()
        {
            Place(16, 4, MaterialType.STONE, pressure: _pressure.centralPressure, liquidVolume: 0f);
            Place(15, 4, MaterialType.STONE);
            Place(17, 4, MaterialType.STONE);
            Place(16, 5, MaterialType.STONE);
        }

        private void PlaceZone_Structural()
        {
            Place(22, 2, MaterialType.WOOD, structuralIntegrity: _structural.integrity);
            Place(23, 2, MaterialType.WOOD, structuralIntegrity: _structural.integrity);
            Place(22, 3, MaterialType.WOOD, temperature: _structural.heatTriggerTemperature);
        }

        private void PlaceZone_Humidity()
        {
            Place(26, 2, MaterialType.WOOD,  temperature: _humidity.hotWoodTemperature, liquidVolume: _humidity.waterHumidity);
            Place(27, 2, MaterialType.WOOD,  temperature: _humidity.hotWoodTemperature);
            Place(26, 4, MaterialType.WATER, liquidVolume: _humidity.waterHumidity, temperature: _humidity.waterTemperature);
        }

        private void PlaceZone_GasIgnition()
        {
            Place(5, 7, MaterialType.GAS, gasDensity: _gasIgnition.gasDensity, temperature: _gasIgnition.gasTemperature);
            Place(6, 7, MaterialType.GAS, gasDensity: _gasIgnition.gasDensity, temperature: _gasIgnition.gasTemperature);
            Place(7, 7, MaterialType.GAS, gasDensity: _gasIgnition.gasDensity);
        }

        // ── Heightmap ────────────────────────────────────────────────────────

        private void ApplyHeightmap()
        {
            int w = _engine.Grid.Width;
            int h = _engine.Grid.Height;
            TileHeight[,] heightmap = HeightmapGenerator.GenerateStaticRoom(w, h);

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                var pos  = new Vector2Int(x, y);
                var tile = _engine.Grid.GetTile(pos);

                tile.height   = heightmap[x, y];
                tile.material = HeightToMaterial(heightmap[x, y]);
                tile.structuralIntegrity = HeightToIntegrity(heightmap[x, y]);

                _engine.Grid.SetTile(pos, tile);
            }
        }

        private static MaterialType HeightToMaterial(TileHeight height)
        {
            switch (height)
            {
                case TileHeight.Wall:
                case TileHeight.Tall:    return MaterialType.STONE;
                case TileHeight.Low:     return MaterialType.EARTH;
                case TileHeight.Shallow:
                case TileHeight.Deep:    return MaterialType.WATER;
                default:                 return MaterialType.EMPTY;
            }
        }

        private static float HeightToIntegrity(TileHeight height)
        {
            switch (height)
            {
                case TileHeight.Wall:
                case TileHeight.Tall: return 100f;
                case TileHeight.Low:  return 70f;
                default:              return 80f;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void FillGasLayer()
        {
            for (int x = 0; x < _engine.Grid.Width; x++)
            for (int y = 0; y < _engine.Grid.Height; y++)
            {
                var pos = new Vector2Int(x, y);
                ref var tile = ref _engine.Grid.GetTile(pos);
                tile.gasDensity = _gas.globalGasDensity;
            }
        }

        private void Place(int x, int y, MaterialType mat,
            float temperature        = 0f,
            float pressure          = 0f,
            float liquidVolume      = 0f,
            float electricEnergy    = 0f,
            float gasDensity        = 0f,
            float structuralIntegrity = 80f)
        {
            var pos  = new Vector2Int(x, y);
            var tile = _engine.Grid.GetTile(pos);

            tile.material           = mat;
            tile.temperature        = temperature;
            tile.gasDensity         = gasDensity + pressure;
            tile.liquidVolume       = liquidVolume;
            tile.electricEnergy     = electricEnergy;
            tile.structuralIntegrity = structuralIntegrity;

            _engine.Grid.SetTile(pos, tile);
        }

        private void FillRow(int y, MaterialType mat,
            float temperature        = 0f,
            float structuralIntegrity = 80f)
        {
            for (int x = 0; x < _engine.Grid.Width; x++)
                Place(x, y, mat, temperature: temperature, structuralIntegrity: structuralIntegrity);
        }

        private void EnsureLabelStyle()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 1f, 0.4f, 0.9f) }
            };
        }
    }
}