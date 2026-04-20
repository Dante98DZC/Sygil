// Assets/PhysicsSystem/Tests/TestWorldGenerator.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Powers;
using PhysicsSystem.Renderer;

namespace PhysicsSystem.Tests
{
    public enum WorldSize
    {
        Small8x8 = 8,
        Medium16x16 = 16,
        Large32x32 = 32,
    }

    public enum ScenarioType
    {
        AllRules,
        FireOnly,
        WaterOnly,
        PhaseTransitions,
        PressureTest,
        ElectricTest,
        Custom,
    }

    [System.Serializable]
    public struct ZoneParams_Combustion
    {
        [Range(0f, 100f)] public float ignitionTemperature;
        public MaterialType burnMaterial;

        public static ZoneParams_Combustion Default => new()
        {
            ignitionTemperature = 70f,
            burnMaterial = MaterialType.WOOD,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Liquid
    {
        [Range(0f, 100f)] public float temperature;
        [Range(0f, 1000f)] public float volume;
        public MaterialType liquidType;

        public static ZoneParams_Liquid Default => new()
        {
            temperature = 90f,
            volume = 200f,
            liquidType = MaterialType.WATER,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Electric
    {
        [Range(0f, 100f)] public float charge;
        public MaterialType conductor;

        public static ZoneParams_Electric Default => new()
        {
            charge = 90f,
            conductor = MaterialType.METAL,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Pressure
    {
        [Range(0f, 100f)] public float gasDensity;
        public MaterialType gasType;

        public static ZoneParams_Pressure Default => new()
        {
            gasDensity = 95f,
            gasType = MaterialType.SMOKE,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Structural
    {
        [Range(0f, 100f)] public float integrity;
        [Range(0f, 100f)] public float heatTrigger;
        public MaterialType material;

        public static ZoneParams_Structural Default => new()
        {
            integrity = 10f,
            heatTrigger = 80f,
            material = MaterialType.WOOD,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Phase
    {
        [Range(0f, 100f)] public float temperature;
        [Range(0f, 1000f)] public float volume;
        public MaterialType fromMaterial;
        public bool isMelting;
        public bool isFreezing;
        public bool isBoiling;

        public static ZoneParams_Phase Melting => new()
        {
            temperature = 95f,
            volume = 100f,
            fromMaterial = MaterialType.STONE,
            isMelting = true,
        };

        public static ZoneParams_Phase Freezing => new()
        {
            temperature = 5f,
            volume = 100f,
            fromMaterial = MaterialType.WATER,
            isFreezing = true,
        };

        public static ZoneParams_Phase Boiling => new()
        {
            temperature = 85f,
            volume = 80f,
            fromMaterial = MaterialType.WATER,
            isBoiling = true,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Gas
    {
        [Range(0f, 100f)] public float density;
        [Range(0f, 100f)] public float temperature;
        public MaterialType gasType;

        public static ZoneParams_Gas Default => new()
        {
            density = 50f,
            temperature = 20f,
            gasType = MaterialType.STEAM,
        };
    }

    [System.Serializable]
    public class ScenarioPreset
    {
        public string name;
        public ScenarioType scenarioType;
        public WorldSize worldSize;
        public bool useHeightmap;

        public ZoneParams_Combustion combustion = ZoneParams_Combustion.Default;
        public ZoneParams_Liquid liquid = ZoneParams_Liquid.Default;
        public ZoneParams_Electric electric = ZoneParams_Electric.Default;
        public ZoneParams_Pressure pressure = ZoneParams_Pressure.Default;
        public ZoneParams_Structural structural = ZoneParams_Structural.Default;
        public ZoneParams_Phase phaseTransition = ZoneParams_Phase.Melting;
        public ZoneParams_Gas gas = ZoneParams_Gas.Default;
    }

    public class TestWorldGenerator : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SimulationEngine _engine;
        [SerializeField] private SimulationRenderer _renderer;
        [SerializeField] private PowerCaster _caster;
        [SerializeField] private CompiledPower _testPower;

        [Header("Config")]
        [SerializeField] private WorldSize _worldSize = WorldSize.Small8x8;
        [SerializeField] private ScenarioType _scenario = ScenarioType.Custom;
        [SerializeField] private bool _useHeightmap = false;

        [Header("Zones")]
        [SerializeField] private bool _enableCombustion = true;
        [SerializeField] private bool _enableLiquid = true;
        [SerializeField] private bool _enableElectric = true;
        [SerializeField] private bool _enablePressure = true;
        [SerializeField] private bool _enablePhase = true;

        [Header("Params")]
        [SerializeField] private ZoneParams_Combustion _combustion = ZoneParams_Combustion.Default;
        [SerializeField] private ZoneParams_Liquid _liquid = ZoneParams_Liquid.Default;
        [SerializeField] private ZoneParams_Electric _electric = ZoneParams_Electric.Default;
        [SerializeField] private ZoneParams_Pressure _pressure = ZoneParams_Pressure.Default;
        [SerializeField] private ZoneParams_Structural _structural = ZoneParams_Structural.Default;
        [SerializeField] private ZoneParams_Phase _phase = ZoneParams_Phase.Melting;
        [SerializeField] private ZoneParams_Gas _gas = ZoneParams_Gas.Default;

        [Header("Presets")]
        [SerializeField] private ScenarioPreset[] _presets;

        [Header("UI")]
        [SerializeField] private bool _showLabels = true;

        private int _gridSize = 8;
        private (string label, Vector2Int pos)[] _labels;

        private GUIStyle _labelStyle;

        private void Start()
        {
            if (_engine == null)
            {
                Debug.LogError("[TestWorldGenerator] Engine missing.");
                return;
            }

            _gridSize = (int)_worldSize;
            CreateLabels();
            GenerateMap();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                GenerateMap();

            if (Input.GetKeyDown(KeyCode.Alpha1) && _presets?.Length > 0)
                LoadPreset(_presets[0]);
            if (Input.GetKeyDown(KeyCode.Alpha2) && _presets?.Length > 1)
                LoadPreset(_presets[1]);

            if (Input.GetKeyDown(KeyCode.Space) && _caster != null && _testPower != null)
                _caster.Cast(_testPower, new Vector2Int(_gridSize / 2, _gridSize / 2), Vector2.up);
        }

        private void OnGUI()
        {
            if (!_showLabels || Camera.main == null) return;

            EnsureLabelStyle();
            foreach (var (label, pos) in _labels)
            {
                Vector3 worldPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                if (screenPos.z < 0f) continue;

                float guiY = Screen.height - screenPos.y;
                GUI.Label(new Rect(screenPos.x - 40f, guiY - 15f, 80f, 20f), label, _labelStyle);
            }

            GUI.Label(new Rect(10, 10, 250, 20), $"[{_gridSize}x{_gridSize}] 1-2 presets, R reset", _labelStyle);
        }

        public void GenerateMap()
        {
            if (_useHeightmap)
                MakeHeightmap();
            else
                MakeFlatGround();

            PlaceAllZones();

            _engine.Grid.RebuildAtmosphereFlags();

            Debug.Log($"[TestWorldGenerator] Generated {_gridSize}x{_gridSize} - {_engine.Grid.ActiveTiles.Count} active");
            _renderer?.Refresh();
        }

        public void LoadPreset(ScenarioPreset preset)
        {
            _worldSize = preset.worldSize;
            _gridSize = (int)_worldSize;
            _useHeightmap = preset.useHeightmap;
            _scenario = preset.scenarioType;

            _combustion = preset.combustion;
            _liquid = preset.liquid;
            _electric = preset.electric;
            _pressure = preset.pressure;
            _structural = preset.structural;
            _phase = preset.phaseTransition;
            _gas = preset.gas;

            CreateLabels();
            GenerateMap();
            Debug.Log($"[TestWorldGenerator] Loaded: {preset.name}");
        }

        private void PlaceAllZones()
        {
            if (_gridSize < 12)
            {
                PlaceSmallMapZones();
                return;
            }

            if (_enableCombustion) PlaceZoneCombustion();
            if (_enableLiquid) PlaceZoneLiquid();
            if (_enableElectric) PlaceZoneElectric();
            if (_enablePressure) PlaceZonePressure();
            if (_enablePhase) PlaceZonePhase();
        }

        private void PlaceSmallMapZones()
        {
            int m = _gridSize / 2;

            if (_enableCombustion)
            {
                SetSolid(m - 1, m, _combustion.burnMaterial, _combustion.ignitionTemperature);
                SetSolid(m, m, _combustion.burnMaterial);
                SetSolid(m + 1, m, _combustion.burnMaterial);
            }

            if (_enableLiquid)
                SetLiquidZone(m - 2, m - 2);

            if (_enablePhase)
            {
                if (_phase.isMelting) SetSolid(m + 2, m - 2, _phase.fromMaterial, _phase.temperature);
                if (_phase.isFreezing) SetLiquidZone(m + 2, m - 2, _phase.fromMaterial, _phase.temperature, _phase.volume);
            }

            if (_enableElectric)
            {
                SetSolid(m + 1, m - 2, _electric.conductor, 0f, _electric.charge);
                SetLiquidZone(m + 2, m - 2);
            }

            if (_enablePressure)
            {
                SetGasZone(m - 1, m + 2);
                SetGasZone(m, m + 2);
            }
        }

        private void PlaceZoneCombustion()
        {
            for (int x = 2; x < 6; x++)
                SetSolid(x, 2, _combustion.burnMaterial);
            SetSolid(4, 2, _combustion.burnMaterial, _combustion.ignitionTemperature);
        }

        private void PlaceZoneLiquid()
        {
            SetLiquidZone(2, 3);
            SetLiquidZone(3, 3);
            SetLiquidZone(4, 3);
        }

        private void PlaceZoneElectric()
        {
            SetSolid(8, 3, _electric.conductor, 0f, _electric.charge);
            SetSolid(9, 3, _electric.conductor);
            SetSolid(10, 3, _electric.conductor);
            SetLiquidZone(11, 3);
        }

        private void PlaceZonePressure()
        {
            SetGasZone(4, 4, _pressure.gasType, _pressure.gasDensity);
            SetGasZone(5, 4, _pressure.gasType, _pressure.gasDensity);
            SetGasZone(6, 4, _pressure.gasType, _pressure.gasDensity);
            SetGasZone(5, 5, _pressure.gasType, _pressure.gasDensity);
        }

        private void PlaceZonePhase()
        {
            if (_phase.isMelting)
            {
                SetSolid(12, 3, _phase.fromMaterial, _phase.temperature);
                SetSolid(13, 3, MaterialType.METAL, _phase.temperature);
                SetLiquidZone(12, 4, _phase.fromMaterial, _phase.temperature, _phase.volume);
            }
            if (_phase.isFreezing)
            {
                SetLiquidZone(16, 3, _phase.fromMaterial, _phase.temperature, _phase.volume);
                SetLiquidZone(17, 3, _phase.fromMaterial, _phase.temperature);
            }
            if (_phase.isBoiling)
            {
                SetLiquidZone(12, 5, _phase.fromMaterial, _phase.temperature, _phase.volume);
                SetLiquidZone(13, 5, _phase.fromMaterial, _phase.temperature, _phase.volume);
            }
        }

        private void MakeHeightmap()
        {
            var hm = HeightmapGenerator.GenerateStaticRoom(_gridSize, _gridSize);
            for (int x = 0; x < _gridSize; x++)
            for (int y = 0; y < _gridSize; y++)
            {
                var tile = _engine.Grid.GetTile(new Vector2Int(x, y));
                tile.height = hm[x, y];
                tile.groundMaterial = HeightToMaterial(hm[x, y]);
                tile.structuralIntegrity = HeightToIntegrity(hm[x, y]);
                _engine.Grid.SetTile(new Vector2Int(x, y), tile);
            }
        }

        private void MakeFlatGround()
        {
            for (int x = 0; x < _gridSize; x++)
            for (int y = 0; y < _gridSize; y++)
            {
                var tile = _engine.Grid.GetTile(new Vector2Int(x, y));

                if (y == 0)
                {
                    tile.groundMaterial = MaterialType.STONE;
                    tile.height = TileHeight.Wall;
                }
                else if (y == 1)
                {
                    tile.groundMaterial = MaterialType.STONE;
                    tile.height = TileHeight.Ground;
                }
                else
                {
                    // Tierra porosa que absorbe agua
                    tile.groundMaterial = MaterialType.EARTH;
                    tile.height = TileHeight.Shallow;
                    tile.gasDensity = 0f;
                }

                tile.structuralIntegrity = 80f;
                _engine.Grid.SetTile(new Vector2Int(x, y), tile);
            }
        }

        private void SetSolid(int x, int y, MaterialType mat, float temp = 0f, float elec = 0f, float integ = 80f)
        {
            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize) return;
            var tile = _engine.Grid.GetTile(new Vector2Int(x, y));
            tile.groundMaterial = mat;
            tile.temperature = temp;
            tile.electricEnergy = elec;
            tile.structuralIntegrity = integ;
            _engine.Grid.SetTile(new Vector2Int(x, y), tile);
        }

        private void SetLiquidZone(int x, int y, MaterialType mat = MaterialType.WATER, float temp = 30f, float vol = 100f)
        {
            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize) return;
            var tile = _engine.Grid.GetTile(new Vector2Int(x, y));

            // Opción A: tierra mojada (ground absorbe, líquido superficial)
            // El suelo debe ser un material poroso que pueda absorber agua
            if (tile.groundMaterial == MaterialType.EMPTY || tile.groundMaterial == MaterialType.STONE)
                tile.groundMaterial = MaterialType.EARTH;

            tile.liquidMaterial = mat == MaterialType.WATER ? _liquid.liquidType : mat;
            tile.liquidVolume = vol;
            tile.temperature = temp;

            _engine.Grid.SetTile(new Vector2Int(x, y), tile);
        }

        private void SetGasZone(int x, int y, MaterialType mat = MaterialType.STEAM, float dens = 50f, float temp = 20f)
        {
            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize) return;
            var tile = _engine.Grid.GetTile(new Vector2Int(x, y));
            tile.gasMaterial = mat;
            tile.gasDensity = dens;
            tile.temperature = temp;
            _engine.Grid.SetTile(new Vector2Int(x, y), tile);
        }

        private void CreateLabels()
        {
            int m = _gridSize / 2;
            if (_gridSize < 12)
            {
                _labels = new (string, Vector2Int)[]
                {
                    ("FIRE", new Vector2Int(m, m)),
                    ("H2O", new Vector2Int(m - 2, m - 2)),
                    ("PHASE", new Vector2Int(m + 2, m - 2)),
                };
            }
            else
            {
                _labels = new (string, Vector2Int)[]
                {
                    ("R01 Fire", new Vector2Int(4, 2)),
                    ("R02 Water", new Vector2Int(3, 3)),
                    ("R03 Elec", new Vector2Int(9, 3)),
                    ("R05 Press", new Vector2Int(5, 4)),
                    ("R13 Phase", new Vector2Int(14, 3)),
                };
            }
        }

        private void EnsureLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow }
                };
            }
        }

        private static MaterialType HeightToMaterial(TileHeight h) => h switch
        {
            TileHeight.Wall => MaterialType.STONE,
            TileHeight.Tall => MaterialType.STONE,
            TileHeight.Low => MaterialType.EARTH,
            TileHeight.Shallow => MaterialType.EARTH,  // Tierra que puede absorber agua
            TileHeight.Deep => MaterialType.EARTH,
            _ => MaterialType.EMPTY,
        };

        private static float HeightToIntegrity(TileHeight h) => h switch
        {
            TileHeight.Wall => 100f,
            TileHeight.Tall => 100f,
            TileHeight.Low => 70f,
            _ => 80f,
        };
    }
}