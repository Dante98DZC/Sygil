// Assets/PhysicsSystem/Tests/TestWorldGenerator.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Powers;

namespace PhysicsSystem.Tests
{
    public enum WorldSize
    {
        Small8x8   = 8,
        Medium16x16 = 16,
        Large32x32  = 32,
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
            burnMaterial        = MaterialType.WOOD,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Liquid
    {
        [Range(0f, 100f)]  public float temperature;
        [Range(0f, 1000f)] public float volume;
        public MaterialType liquidType;

        public static ZoneParams_Liquid Default => new()
        {
            temperature = 90f,
            volume      = 200f,
            liquidType  = MaterialType.WATER,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Electric
    {
        [Range(0f, 100f)] public float charge;
        public MaterialType conductor;

        public static ZoneParams_Electric Default => new()
        {
            charge    = 90f,
            conductor = MaterialType.METAL,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Pressure
    {
        [Range(0f, 100f)] public float gasConcentration;
        public MaterialType gasType;

        public static ZoneParams_Pressure Default => new()
        {
            gasConcentration = 95f,
            gasType          = MaterialType.SMOKE,
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
            integrity   = 10f,
            heatTrigger = 80f,
            material    = MaterialType.WOOD,
        };
    }

    [System.Serializable]
    public struct ZoneParams_Phase
    {
        [Range(0f, 100f)]  public float temperature;
        [Range(0f, 1000f)] public float volume;
        public MaterialType fromMaterial;
        public bool isMelting;
        public bool isFreezing;
        public bool isBoiling;

        public static ZoneParams_Phase Melting => new()
        {
            temperature  = 95f,
            volume       = 100f,
            fromMaterial = MaterialType.STONE,
            isMelting    = true,
        };

        public static ZoneParams_Phase Freezing => new()
        {
            temperature  = 5f,
            volume       = 100f,
            fromMaterial = MaterialType.WATER,
            isFreezing   = true,
        };

        public static ZoneParams_Phase Boiling => new()
        {
            temperature  = 85f,
            volume       = 80f,
            fromMaterial = MaterialType.WATER,
            isBoiling    = true,
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
            density     = 50f,
            temperature = 20f,
            gasType     = MaterialType.STEAM,
        };
    }

    [System.Serializable]
    public class ScenarioPreset
    {
        public string        name;
        public ScenarioType  scenarioType;
        public WorldSize     worldSize;
        public bool          useHeightmap;

        public ZoneParams_Combustion combustion       = ZoneParams_Combustion.Default;
        public ZoneParams_Liquid     liquid           = ZoneParams_Liquid.Default;
        public ZoneParams_Electric   electric         = ZoneParams_Electric.Default;
        public ZoneParams_Pressure   pressure         = ZoneParams_Pressure.Default;
        public ZoneParams_Structural structural       = ZoneParams_Structural.Default;
        public ZoneParams_Phase      phaseTransition  = ZoneParams_Phase.Melting;
        public ZoneParams_Gas        gas              = ZoneParams_Gas.Default;
    }

    public class TestWorldGenerator : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SimulationEngine _engine;
        [SerializeField] private PowerCaster      _caster;
        [SerializeField] private CompiledPower    _testPower;

        [Header("Config")]
        [SerializeField] private WorldSize    _worldSize   = WorldSize.Small8x8;
        [SerializeField] private ScenarioType _scenario    = ScenarioType.Custom;
        [SerializeField] private bool         _useHeightmap = false;

        [Header("Zones")]
        [SerializeField] private bool _enableCombustion = true;
        [SerializeField] private bool _enableLiquid     = true;
        [SerializeField] private bool _enableElectric   = true;
        [SerializeField] private bool _enablePressure   = true;
        [SerializeField] private bool _enablePhase      = true;
        [SerializeField] private bool _enableFiltration = true;

        [Header("Params")]
        [SerializeField] private ZoneParams_Combustion _combustion = ZoneParams_Combustion.Default;
        [SerializeField] private ZoneParams_Liquid     _liquid     = ZoneParams_Liquid.Default;
        [SerializeField] private ZoneParams_Electric   _electric   = ZoneParams_Electric.Default;
        [SerializeField] private ZoneParams_Pressure   _pressure   = ZoneParams_Pressure.Default;
        [SerializeField] private ZoneParams_Structural _structural = ZoneParams_Structural.Default;
        [SerializeField] private ZoneParams_Phase      _phase      = ZoneParams_Phase.Melting;
        [SerializeField] private ZoneParams_Gas        _gas        = ZoneParams_Gas.Default;

        [Header("Presets")]
        [SerializeField] private ScenarioPreset[] _presets;

        [Header("UI")]
        [SerializeField] private bool _showLabels = true;

        private int _gridSize = 8;
        private (string label, Vector2Int pos)[] _labels;
        private GUIStyle _labelStyle;

        // ── Lifecycle ─────────────────────────────────────────────────────────

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
                Vector3 worldPos  = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                if (screenPos.z < 0f) continue;

                float guiY = Screen.height - screenPos.y;
                GUI.Label(new Rect(screenPos.x - 40f, guiY - 15f, 80f, 20f), label, _labelStyle);
            }

            GUI.Label(new Rect(10, 10, 280, 20),
                $"[{_gridSize}x{_gridSize}] R=reset  1-2=presets  Space=power",
                _labelStyle);
        }

        // ── Map generation ────────────────────────────────────────────────────

        public void GenerateMap()
        {
            if (_useHeightmap)
                MakeHeightmap();
            else
                MakeFlatGround();

            PlaceAllZones();

            // RebuildAtmosphereFlags infiere apertura por posición de borde,
            // pero seteamos isAtmosphereOpen explícitamente para garantizar
            // que el venting y la difusión atmosférica funcionen correctamente.
            OpenAtmosphere();
            _engine.Grid.RebuildAtmosphereFlags();
        }

        public void LoadPreset(ScenarioPreset preset)
        {
            _worldSize    = preset.worldSize;
            _gridSize     = (int)_worldSize;
            _useHeightmap = preset.useHeightmap;
            _scenario     = preset.scenarioType;

            _combustion = preset.combustion;
            _liquid     = preset.liquid;
            _electric   = preset.electric;
            _pressure   = preset.pressure;
            _structural = preset.structural;
            _phase      = preset.phaseTransition;
            _gas        = preset.gas;

            CreateLabels();
            GenerateMap();
            Debug.Log($"[TestWorldGenerator] Loaded: {preset.name}");
        }

        // ── Atmosphere ────────────────────────────────────────────────────────

        /// <summary>
        /// Marca como abiertos a la atmósfera:
        ///   - Toda la fila superior (escape de gas hacia exterior).
        ///   - Tiles interiores sin groundMaterial (aire libre, no pared).
        /// Sin esto el gas no ventea y la temperatura no se equilibra.
        /// </summary>
        private void OpenAtmosphere()
        {
            for (int x = 0; x < _gridSize; x++)
            for (int y = 0; y < _gridSize; y++)
            {
                var pos  = new Vector2Int(x, y);
                var tile = _engine.Grid.GetTile(pos);

                bool isTopRow      = y == _gridSize - 1;
                bool isOpenAirTile = tile.groundMaterial == MaterialType.EMPTY && y > 1;

                tile.isAtmosphereOpen = isTopRow || isOpenAirTile;
                _engine.Grid.SetTile(pos, tile);
            }
        }

        // ── Ground layouts ────────────────────────────────────────────────────

        private void MakeFlatGround()
        {
            for (int x = 0; x < _gridSize; x++)
            for (int y = 0; y < _gridSize; y++)
            {
                var pos  = new Vector2Int(x, y);
                var tile = _engine.Grid.GetTile(pos);

                if (y == 0)
                {
                    // Suelo base — STONE sólido, paredes laterales incluidas
                    tile.groundMaterial      = MaterialType.STONE;
                    tile.height              = TileHeight.Wall;
                    tile.structuralIntegrity = 100f;
                    tile.isAtmosphereOpen    = false;
                }
                else if (y == 1)
                {
                    // Suelo jugable — EARTH poroso para testear R_Filtration
                    tile.groundMaterial      = MaterialType.EARTH;
                    tile.height              = TileHeight.Ground;
                    tile.structuralIntegrity = 80f;
                    tile.isAtmosphereOpen    = false;
                }
                else
                {
                    // Aire interior — EMPTY para que gas y líquido operen libremente
                    tile.groundMaterial      = MaterialType.EMPTY;
                    tile.height              = TileHeight.Shallow;
                    tile.structuralIntegrity = 0f;
                    tile.gasConcentration    = 0f;
                    // isAtmosphereOpen se asigna en OpenAtmosphere()
                }

                _engine.Grid.SetTile(pos, tile);
            }
        }

        private void MakeHeightmap()
        {
            var hm = HeightmapGenerator.GenerateStaticRoom(_gridSize, _gridSize);
            for (int x = 0; x < _gridSize; x++)
            for (int y = 0; y < _gridSize; y++)
            {
                var pos  = new Vector2Int(x, y);
                var tile = _engine.Grid.GetTile(pos);

                tile.height              = hm[x, y];
                tile.groundMaterial      = HeightToMaterial(hm[x, y]);
                tile.structuralIntegrity = HeightToIntegrity(hm[x, y]);
                // isAtmosphereOpen se asigna en OpenAtmosphere()

                _engine.Grid.SetTile(pos, tile);
            }
        }

        // ── Zone placement ────────────────────────────────────────────────────

        private void PlaceAllZones()
        {
            if (_gridSize < 12)
            {
                PlaceSmallMapZones();
                return;
            }

            // Escalar offsets al tamaño del grid para que funcione en 16x16 y 32x32
            if (_enableCombustion) PlaceZoneCombustion();
            if (_enableLiquid)     PlaceZoneLiquid();
            if (_enableElectric)   PlaceZoneElectric();
            if (_enablePressure)   PlaceZonePressure();
            if (_enablePhase)      PlaceZonePhase();
            if (_enableFiltration) PlaceZoneFiltration();
        }

        private void PlaceSmallMapZones()
        {
            int m = _gridSize / 2;

            if (_enableCombustion)
            {
                SetSolid(m - 1, m, _combustion.burnMaterial, _combustion.ignitionTemperature);
                SetSolid(m,     m, _combustion.burnMaterial);
                SetSolid(m + 1, m, _combustion.burnMaterial);
            }

            if (_enableLiquid)
                SetLiquidZone(m - 2, m - 2);

            if (_enableFiltration)
            {
                // Tierra porosa debajo del líquido para testear absorción
                SetSolid(m - 2, m - 3, MaterialType.EARTH);
            }

            if (_enablePhase)
            {
                if (_phase.isMelting)  SetSolid(m + 2, m - 2, _phase.fromMaterial, _phase.temperature);
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
                SetGasZone(m,     m + 2);
            }
        }

        private void PlaceZoneCombustion()
        {
            int ox = Scale(2), oy = Scale(2);
            for (int x = ox; x < ox + 4; x++)
                SetSolid(x, oy, _combustion.burnMaterial);
            // Tile caliente para ignición inmediata
            SetSolid(ox + 2, oy, _combustion.burnMaterial, _combustion.ignitionTemperature + 5f);
        }

        private void PlaceZoneLiquid()
        {
            int ox = Scale(2), oy = Scale(3);
            SetLiquidZone(ox,     oy, _liquid.liquidType, _liquid.temperature, _liquid.volume);
            SetLiquidZone(ox + 1, oy, _liquid.liquidType, _liquid.temperature, _liquid.volume);
            SetLiquidZone(ox + 2, oy, _liquid.liquidType, _liquid.temperature, _liquid.volume);
        }

        private void PlaceZoneFiltration()
        {
            // Columna de EARTH porosa justo debajo de la zona de líquido
            // Permite ver en tiempo real cómo soilMoisture sube tick a tick
            int ox = Scale(2), oy = Scale(3) - 1;
            SetSolid(ox,     oy, MaterialType.EARTH);
            SetSolid(ox + 1, oy, MaterialType.EARTH);
            SetSolid(ox + 2, oy, MaterialType.EARTH);
        }

        private void PlaceZoneElectric()
        {
            int ox = Scale(5), oy = Scale(3);
            SetSolid(ox,     oy, _electric.conductor, 0f, _electric.charge);
            SetSolid(ox + 1, oy, _electric.conductor);
            SetSolid(ox + 2, oy, _electric.conductor);
            // Agua al final de la cadena — prueba R04_ElectricWater
            SetLiquidZone(ox + 3, oy, MaterialType.WATER, 20f, 100f);
        }

        private void PlaceZonePressure()
        {
            int ox = Scale(3), oy = Scale(4);
            SetGasZone(ox,     oy,     _pressure.gasType, _pressure.gasConcentration);
            SetGasZone(ox + 1, oy,     _pressure.gasType, _pressure.gasConcentration);
            SetGasZone(ox + 2, oy,     _pressure.gasType, _pressure.gasConcentration);
            SetGasZone(ox + 1, oy + 1, _pressure.gasType, _pressure.gasConcentration);
        }

        private void PlaceZonePhase()
        {
            int ox = Scale(7), oy = Scale(3);

            if (_phase.isMelting)
            {
                // Sólido caliente → debe fundir y convertirse en líquido
                SetSolid(ox,     oy, _phase.fromMaterial, _phase.temperature);
                SetSolid(ox + 1, oy, _phase.fromMaterial, _phase.temperature);
            }

            if (_phase.isFreezing)
            {
                // Líquido frío → debe congelar
                SetLiquidZone(ox + 3, oy, _phase.fromMaterial, _phase.temperature, _phase.volume);
                SetLiquidZone(ox + 4, oy, _phase.fromMaterial, _phase.temperature, _phase.volume);
            }

            if (_phase.isBoiling)
            {
                // Líquido caliente → debe hervir y generar gas
                SetLiquidZone(ox,     oy + 2, _phase.fromMaterial, _phase.temperature, _phase.volume);
                SetLiquidZone(ox + 1, oy + 2, _phase.fromMaterial, _phase.temperature, _phase.volume);
            }
        }

        // ── Tile helpers ──────────────────────────────────────────────────────

        private void SetSolid(int x, int y, MaterialType mat,
            float temp = 0f, float elec = 0f, float integ = 80f)
        {
            if (!InBounds(x, y)) return;
            var pos  = new Vector2Int(x, y);
            var tile = _engine.Grid.GetTile(pos);

            tile.groundMaterial      = mat;
            tile.temperature         = temp;
            tile.electricEnergy      = elec;
            tile.structuralIntegrity = integ;
            tile.isAtmosphereOpen    = false; // sólido nunca abierto

            _engine.Grid.SetTile(pos, tile);
        }

        private void SetLiquidZone(int x, int y,
            MaterialType mat = MaterialType.WATER,
            float temp = 30f, float vol = 100f)
        {
            if (!InBounds(x, y)) return;
            var pos  = new Vector2Int(x, y);
            var tile = _engine.Grid.GetTile(pos);

            // No sobreescribir groundMaterial si ya es sólido — el líquido va
            // encima, no reemplaza el suelo. Solo asignar EARTH si el tile está vacío.
            if (tile.groundMaterial == MaterialType.EMPTY)
                tile.groundMaterial = MaterialType.EARTH;

            tile.liquidMaterial = mat;
            tile.liquidVolume   = vol;
            tile.temperature    = temp;

            _engine.Grid.SetTile(pos, tile);
        }

        private void SetGasZone(int x, int y,
            MaterialType mat = MaterialType.STEAM,
            float dens = 50f, float temp = 20f)
        {
            if (!InBounds(x, y)) return;
            var pos  = new Vector2Int(x, y);
            var tile = _engine.Grid.GetTile(pos);

            tile.gasMaterial      = mat;
            tile.gasConcentration = dens;
            tile.temperature      = temp;

            _engine.Grid.SetTile(pos, tile);
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        /// <summary>Escala una coordenada base (diseñada para 8x8) al tamaño actual del grid.</summary>
        private int Scale(int baseCoord) =>
            Mathf.RoundToInt(baseCoord * (_gridSize / 8f));

        private bool InBounds(int x, int y) =>
            x >= 0 && x < _gridSize && y >= 0 && y < _gridSize;

        private void CreateLabels()
        {
            int m = _gridSize / 2;
            if (_gridSize < 12)
            {
                _labels = new (string, Vector2Int)[]
                {
                    ("FIRE",  new Vector2Int(m,     m)),
                    ("H2O",   new Vector2Int(m - 2, m - 2)),
                    ("PHASE", new Vector2Int(m + 2, m - 2)),
                };
            }
            else
            {
                _labels = new (string, Vector2Int)[]
                {
                    ("R01 Fire",  new Vector2Int(Scale(4), Scale(2))),
                    ("R17 Filter",new Vector2Int(Scale(3), Scale(3))),
                    ("R03 Elec",  new Vector2Int(Scale(6), Scale(3))),
                    ("R05 Press", new Vector2Int(Scale(4), Scale(4))),
                    ("R13 Phase", new Vector2Int(Scale(8), Scale(3))),
                };
            }
        }

        private void EnsureLabelStyle()
        {
            if (_labelStyle != null) return;
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.yellow }
            };
        }

        private static MaterialType HeightToMaterial(TileHeight h) => h switch
        {
            TileHeight.Wall    => MaterialType.STONE,
            TileHeight.Tall    => MaterialType.STONE,
            TileHeight.Ground  => MaterialType.EARTH,
            TileHeight.Low     => MaterialType.EARTH,
            TileHeight.Shallow => MaterialType.EARTH,
            TileHeight.Deep    => MaterialType.EARTH,
            _                  => MaterialType.EMPTY,
        };

        private static float HeightToIntegrity(TileHeight h) => h switch
        {
            TileHeight.Wall => 100f,
            TileHeight.Tall => 100f,
            TileHeight.Low  =>  70f,
            _               =>  80f,
        };
    }
}