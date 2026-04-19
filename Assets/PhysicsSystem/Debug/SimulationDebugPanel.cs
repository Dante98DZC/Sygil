// Assets/PhysicsSystem/Debug/SimulationDebugPanel.cs
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using PhysicsSystem.Core;
using PhysicsSystem.Rules;
using PhysicsSystem.States;

namespace PhysicsSystem.DebugTools
{
    /// <summary>
    /// Panel unificado de debug para la simulación.
    /// Combina stats, logging de reglas e inspector de tiles.
    ///
    /// CONTROLES:
    ///   H — Mostrar/ocultar panel completo
    ///   L — Toggle section RuleLog
    ///   T — Toggle section TileInspector
    ///   C — Limpiar log de reglas
    ///   P — Pausar/reanudar simulación
    ///   F/S/W/I — Step tick (cuando pausado)
    ///   1-4 — Filtro de reglas
    ///   + / - — Aumentar/disminuir max entries
    ///
    /// Setup: adjuntar a cualquier GameObject con referencia al SimulationEngine.
    /// </summary>
    public class SimulationDebugPanel : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private SimulationEngine _engine;

        [Header("Panel")]
        [SerializeField] private bool _showPanel = true;
        [SerializeField] private Vector2 _position = new Vector2(10f, 10f);
        [SerializeField] private float _panelWidth = 340f;

        [Header("Secciones")]
        [SerializeField] private bool _showStats = true;
        [SerializeField] private bool _showRuleLog = true;
        [SerializeField] private bool _showTileInspector = true;

        [Header("RuleLog")]
        [SerializeField] private int _maxEntries = 50;
        [SerializeField] private bool _pauseOnIgnition = true;

        [Header("TileInspector")]
        [SerializeField] private bool _showNeighbors = true;

        // ── State ──────────────────────────────────────────────────────────
        private bool _panelVisible = true;
        private RuleFilter _currentFilter = RuleFilter.All;
        private Vector2Int? _pinnedTile;

        // ── Stats ─────────────────────────────────────────────────────────
        private float _elapsedTime;
        private int _rulesThisSecond;
        private int _rulesDisplay;
        private float _ruleSecondTimer;
        private int _framesThisSecond;
        private int _fpsDisplay;
        private float _fpsTimer;
        private long _lastMemory;

        private int _ticksFast, _ticksStandard, _ticksSlow, _ticksIntegrity;

        // ── Log ───────────────────────────────────────────────────────────
        private readonly Queue<RuleLogEntry> _logEntries = new();
        private int _totalFired;
        private readonly Dictionary<RuleID, int> _ruleCounts = new();

        // ── Styles ────────────────────────────────────────────────────────
        private GUIStyle _bgStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _dimStyle;

        // ── Colors ────────────────────────────────────────────────────────
        private static readonly Color _bgCol         = new(0.05f, 0.05f, 0.05f, 0.94f);
        private static readonly Color _borderCol     = new(0.2f,  0.2f,  0.2f);
        private static readonly Color _headerCol     = new(1f,    0.84f, 0f);
        private static readonly Color _textPrimary   = new(0.92f, 0.92f, 0.92f);
        private static readonly Color _textSecondary = new(0.55f, 0.55f, 0.55f);
        private static readonly Color _textMuted     = new(0.4f,  0.4f,  0.4f);
        private static readonly Color _runningCol    = new(0.13f, 0.77f, 0.37f);
        private static readonly Color _pausedCol     = new(0.98f, 0.45f, 0.06f);
        private static readonly Color _accentCol     = new(0.2f,  0.6f,  1f);
        private static readonly Color _warnCol       = new(0.98f, 0.75f, 0.15f);
        private static readonly Color _dangerCol     = new(0.98f, 0.25f, 0.25f);

        private static readonly Dictionary<RuleID, Color> RuleColors = new()
        {
            { RuleID.R01_COMBUSTION,            new Color(1f,    0.4f,  0.1f)  },
            { RuleID.R02_EVAPORATION,           new Color(0.4f,  0.7f,  1f)    },
            { RuleID.R03_ELECTRIC_PROPAGATION,  new Color(0.3f,  1f,    1f)    },
            { RuleID.R04_ELECTRIC_WATER,        new Color(0.3f,  0.9f,  1f)    },
            { RuleID.R05_PRESSURE_EXPLOSION,    new Color(1f,    0.2f,  0.2f)  },
            { RuleID.R06_PRESSURE_RELEASE,      new Color(1f,    0.6f,  0.6f)  },
            { RuleID.R07_STRUCTURAL_COLLAPSE,   new Color(0.8f,  0.5f,  0.2f)  },
            { RuleID.R08_HUMIDITY_VAPORIZATION, new Color(0.5f,  0.8f,  1f)    },
            { RuleID.R09_HEAT_SUPPRESSION,      new Color(0.4f,  0.9f,  0.5f)  },
            { RuleID.R10_GAS_IGNITION,          new Color(1f,    0.8f,  0.1f)  },
            { RuleID.R11_GAS_PRODUCTION,        new Color(0.5f,  0.9f,  0.5f)  },
            { RuleID.R12_GAS_PRESSURE,          new Color(0.7f,  0.7f,  0.3f)  },
            { RuleID.R13_MELTING,               new Color(1f,    0.5f,  0.3f)  },
            { RuleID.R14_FREEZING,              new Color(0.5f,  0.85f, 1f)    },
            { RuleID.R15_BOILING,               new Color(0.9f,  0.95f, 1f)    },
            { RuleID.R16_CONDENSATION,          new Color(0.55f, 0.8f,  0.95f) },
        };

        private struct RuleLogEntry
        {
            public RuleID RuleId;
            public Vector2Int Pos;
            public float Time;
        }

        private enum RuleFilter { All, Fire, Phase, Pressure }

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void OnEnable()  => RuleRegistry.OnRuleFired += HandleRuleFired;
        private void OnDisable() => RuleRegistry.OnRuleFired -= HandleRuleFired;

        private void HandleRuleFired(RuleID ruleId, Vector2Int pos)
        {
            _rulesThisSecond++;
            _totalFired++;

            if (!_ruleCounts.ContainsKey(ruleId))
                _ruleCounts[ruleId] = 0;
            _ruleCounts[ruleId]++;

            _logEntries.Enqueue(new RuleLogEntry { RuleId = ruleId, Pos = pos, Time = _elapsedTime });
            while (_logEntries.Count > _maxEntries)
                _logEntries.Dequeue();

            if (_pauseOnIgnition && IsIgnitionRule(ruleId) && _engine != null && !_engine.IsPaused)
                _engine.TogglePause();
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            UpdateTimers();
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.H)) _panelVisible = !_panelVisible;
            if (!_panelVisible) return;

            if (Input.GetKeyDown(KeyCode.C)) ClearLog();
            if (Input.GetKeyDown(KeyCode.P)) _engine?.TogglePause();

            if (_engine?.IsPaused == true)
            {
                if (Input.GetKeyDown(KeyCode.F)) { _engine.StepTick(TickType.FAST);      _ticksFast++; }
                if (Input.GetKeyDown(KeyCode.S)) { _engine.StepTick(TickType.STANDARD);  _ticksStandard++; }
                if (Input.GetKeyDown(KeyCode.W)) { _engine.StepTick(TickType.SLOW);      _ticksSlow++; }
                if (Input.GetKeyDown(KeyCode.I)) { _engine.StepTick(TickType.INTEGRITY); _ticksIntegrity++; }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) _currentFilter = RuleFilter.All;
            if (Input.GetKeyDown(KeyCode.Alpha2)) _currentFilter = RuleFilter.Fire;
            if (Input.GetKeyDown(KeyCode.Alpha3)) _currentFilter = RuleFilter.Phase;
            if (Input.GetKeyDown(KeyCode.Alpha4)) _currentFilter = RuleFilter.Pressure;

            if (Input.GetKeyDown(KeyCode.Equals)  || Input.GetKeyDown(KeyCode.KeypadPlus))
                _maxEntries = Mathf.Min(200, _maxEntries + 10);
            if (Input.GetKeyDown(KeyCode.Minus)   || Input.GetKeyDown(KeyCode.KeypadMinus))
                _maxEntries = Mathf.Max(10, _maxEntries - 10);

            if (Input.GetMouseButtonDown(0) && _showTileInspector)
                TogglePin();
        }

        private void UpdateTimers()
        {
            _ruleSecondTimer += Time.deltaTime;
            if (_ruleSecondTimer >= 1f)
            {
                _rulesDisplay    = _rulesThisSecond;
                _rulesThisSecond = 0;
                _ruleSecondTimer = 0f;
            }

            _fpsTimer += Time.deltaTime;
            _framesThisSecond++;
            if (_fpsTimer >= 1f)
            {
                _fpsDisplay       = _framesThisSecond;
                _framesThisSecond = 0;
                _fpsTimer         = 0f;
            }

            _lastMemory = System.GC.GetTotalMemory(false) >> 20;
        }

        private void ClearLog()
        {
            _logEntries.Clear();
            _totalFired = 0;
            _ruleCounts.Clear();
        }

        private void TogglePin()
        {
            if (_engine == null) return;
            Vector2Int hovered = ScreenToTile(Input.mousePosition);
            if (!_engine.Grid.InBounds(hovered)) return;

            _pinnedTile = (_pinnedTile.HasValue && _pinnedTile.Value == hovered) ? null : hovered;
        }

        // ── GUI ───────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_panelVisible || _engine == null) return;

            EnsureStyles();

            float x      = _position.x;
            float startY = _position.y;

            // First pass: measure actual height by doing a dry run with GUI.enabled = false
            float totalHeight = MeasurePanelHeight();

            GUI.Box(new Rect(x, startY, _panelWidth, totalHeight), GUIContent.none, _bgStyle);

            float y = startY + 6f;
            x += 8f;

            DrawHeader(x, ref y);
            if (_showStats)         DrawStats(x, ref y);
            if (_showRuleLog)       DrawRuleLog(x, ref y);
            if (_showTileInspector) DrawTileInspector(x, ref y);
        }

        private float MeasurePanelHeight()
        {
            float h = 38f; // header

            if (_showStats)
            {
                h += 18f;       // section title
                h += 5 * 18f;   // 5 stat rows
                h += 4f + 8f;   // padding + divider
            }

            if (_showRuleLog)
            {
                h += 18f;       // section title
                int shown = 0;
                foreach (var entry in _logEntries)
                {
                    if (!MatchesFilter(entry.RuleId)) continue;
                    if (shown >= 6) break;
                    shown++;
                }
                h += Mathf.Max(1, shown) * 18f;
                h += 6f + 8f;   // padding + divider
            }

            if (_showTileInspector)
            {
                h += 18f;       // section title
                h += 18f;       // position label

                Vector2Int tilePos = _pinnedTile ?? ScreenToTile(Input.mousePosition);
                if (!_engine.Grid.InBounds(tilePos))
                {
                    h += 18f;
                }
                else
                {
                    h += 18f;   // materials row
                    h += 18f;   // temp + gas
                    h += 18f;   // liquid + electric
                    h += 18f;   // integrity
                    if (_showNeighbors) h += 18f;
                }
                h += 8f;        // divider
            }

            return h + 6f;      // bottom padding
        }

        private void DrawHeader(float x, ref float y)
        {
            bool   paused    = _engine.IsPaused;
            string status    = paused ? "|| PAUSED" : "▶ RUNNING";
            float  barWidth  = _panelWidth - 20f;

            _headerStyle.fontSize = 12;
            _headerStyle.normal.textColor = paused ? _pausedCol : _runningCol;
            GUI.Label(new Rect(x, y, 120f, 22f), status, _headerStyle);

            _dimStyle.fontSize = 11;
            _dimStyle.normal.textColor = _textSecondary;
            GUI.Label(new Rect(x + 130f, y + 3f, 80f, 18f), FormatTime(_elapsedTime), _dimStyle);

            _dimStyle.fontSize = 9;
            _dimStyle.normal.textColor = _textMuted;
            GUI.Label(new Rect(x + barWidth - 30f, y + 4f, 40f, 18f), "[H]", _dimStyle);

            GUI.color = _borderCol;
            GUI.DrawTexture(new Rect(x, y + 24f, barWidth, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            y += 32f;
        }

        private void DrawStats(float x, ref float y)
        {
            _headerStyle.fontSize = 11;
            _headerStyle.normal.textColor = _headerCol;
            GUI.Label(new Rect(x, y, 60f, 16f), "STATS", _headerStyle);
            y += 18f;

            _dimStyle.fontSize  = 10;
            _valueStyle.fontSize = 11;

            DrawStatRow(x, ref y, "Tiles:",  _engine.Grid.ActiveTiles.Count.ToString(), _textPrimary);
            DrawStatRow(x, ref y, "Rules:",  _rulesDisplay.ToString(),                   _runningCol);
            DrawStatRow(x, ref y, "FPS:",    _fpsDisplay.ToString(),                     _textPrimary);
            DrawStatRow(x, ref y, "Mem:",    _lastMemory + "MB",                         _warnCol);
            DrawStatRow(x, ref y, "Total:",  _totalFired.ToString(),                     _textSecondary);

            y += 4f;
            DrawDivider(x, ref y);
        }

        private void DrawStatRow(float x, ref float y, string label, string value, Color valueColor)
        {
            _dimStyle.normal.textColor = _textSecondary;
            GUI.Label(new Rect(x, y, 50f, 18f), label, _dimStyle);

            _valueStyle.normal.textColor = valueColor;
            GUI.Label(new Rect(x + 50f, y, 60f, 18f), value, _valueStyle);

            y += 18f;
        }

        private void DrawRuleLog(float x, ref float y)
        {
            _headerStyle.fontSize = 11;
            _headerStyle.normal.textColor = _headerCol;
            GUI.Label(new Rect(x, y, 60f, 16f), "RULES", _headerStyle);

            _dimStyle.fontSize = 9;
            _dimStyle.normal.textColor = _textMuted;
            GUI.Label(new Rect(x + _panelWidth - 50f, y + 2f, 35f, 18f), "[C]cls", _dimStyle);
            y += 18f;

            _dimStyle.fontSize = 10;
            int   displayCount = 0;
            const int maxDisplay = 6;

            foreach (var entry in _logEntries)
            {
                if (!MatchesFilter(entry.RuleId)) continue;
                if (displayCount >= maxDisplay)   break;

                Color ruleCol = RuleColors.TryGetValue(entry.RuleId, out var c) ? c : _textSecondary;
                _dimStyle.normal.textColor = ruleCol;

                GUI.Label(new Rect(x,        y, 30f, 18f), entry.Time.ToString("F1") + "s", _dimStyle);

                string ruleName = entry.RuleId.ToString();
                if (ruleName.Length > 10) ruleName = ruleName.Substring(0, 10);
                GUI.Label(new Rect(x + 32f,  y, 80f, 18f), ruleName, _dimStyle);

                _dimStyle.normal.textColor = _textMuted;
                GUI.Label(new Rect(x + 115f, y, 40f, 18f), "(" + entry.Pos.x + "," + entry.Pos.y + ")", _dimStyle);

                y += 18f;
                displayCount++;
            }

            if (displayCount == 0)
            {
                _dimStyle.normal.textColor = _textMuted;
                GUI.Label(new Rect(x, y, _panelWidth - 40f, 18f), "(no rules fired)", _dimStyle);
                y += 18f;
            }

            y += 6f;
            DrawDivider(x, ref y);
        }

        private void DrawTileInspector(float x, ref float y)
        {
            _headerStyle.fontSize = 11;
            _headerStyle.normal.textColor = _headerCol;
            GUI.Label(new Rect(x, y, 40f, 16f), "TILE", _headerStyle);

            if (_pinnedTile.HasValue)
            {
                _dimStyle.fontSize = 9;
                _dimStyle.normal.textColor = _warnCol;
                GUI.Label(new Rect(x + 45f, y + 2f, 30f, 18f), "[PIN]", _dimStyle);
            }

            y += 18f;

            Vector2Int tilePos = _pinnedTile ?? ScreenToTile(Input.mousePosition);
            if (!_engine.Grid.InBounds(tilePos))
            {
                _dimStyle.normal.textColor = _textMuted;
                GUI.Label(new Rect(x, y, _panelWidth - 40f, 18f), "(out of bounds)", _dimStyle);
                y += 16f;
                return;
            }

            TileData tile = _engine.Grid.GetTile(tilePos);

            _valueStyle.fontSize = 12;
            _valueStyle.normal.textColor = _textPrimary;
            GUI.Label(new Rect(x, y, 60f, 16f), "(" + tilePos.x + "," + tilePos.y + ")", _valueStyle);
            y += 18f;

            // Material row: Ground / Liquid / Gas
            _dimStyle.fontSize  = 10;
            _valueStyle.fontSize = 10;

            DrawInlineMaterial(x,        y, "G:", tile.groundMaterial);
            DrawInlineMaterial(x + 85f,  y, "L:", tile.liquidMaterial);
            DrawInlineMaterial(x + 165f, y, "G:", tile.gasMaterial);
            y += 18f;

            // Properties row 1
            DrawInlineProperty(x,       y, "Tmp:", tile.temperature.ToString("F1"), _textPrimary);
            DrawInlineProperty(x + 80f, y, "Gas:", tile.gasDensity.ToString("F1"),  _textPrimary);
            y += 18f;

            // Properties row 2
            DrawInlineProperty(x,       y, "Liq:", tile.liquidVolume.ToString("F1"),   _textPrimary);
            DrawInlineProperty(x + 80f, y, "Ele:", tile.electricEnergy.ToString("F1"), _accentCol);
            y += 18f;

            // Integrity
            Color intColor = tile.structuralIntegrity > 60 ? _runningCol
                           : tile.structuralIntegrity > 30 ? _warnCol
                           : _dangerCol;
            DrawInlineProperty(x, y, "Int:", tile.structuralIntegrity.ToString("F0") + "%", intColor);
            y += 18f;

            if (_showNeighbors)
                DrawNeighbors(x, ref y, tilePos);

            DrawDivider(x, ref y);
        }

        private void DrawInlineMaterial(float x, float y, string label, MaterialType mat)
        {
            _dimStyle.normal.textColor = _textSecondary;
            GUI.Label(new Rect(x, y, 20f, 18f), label, _dimStyle);

            _valueStyle.normal.textColor = GetMaterialColor(mat);
            GUI.Label(new Rect(x + 20f, y, 60f, 18f), mat.ToString(), _valueStyle);
        }

        private void DrawInlineProperty(float x, float y, string label, string value, Color valueColor)
        {
            _dimStyle.normal.textColor = _textSecondary;
            GUI.Label(new Rect(x, y, 30f, 18f), label, _dimStyle);

            _valueStyle.normal.textColor = valueColor;
            GUI.Label(new Rect(x + 30f, y, 45f, 18f), value, _valueStyle);
        }

        private void DrawNeighbors(float x, ref float y, Vector2Int pos)
        {
            var      grid  = _engine.Grid;
            var      dirs  = new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
            string[] names = { "N", "E", "S", "W" };
            float    colW  = (_panelWidth - 40f) / 4f;

            for (int i = 0; i < 4; i++)
            {
                Vector2Int neighbor = pos + dirs[i];
                string     matName  = "—";
                Color      matColor = _textMuted;

                if (grid.InBounds(neighbor))
                {
                    TileData t = grid.GetTile(neighbor);
                    if (t.groundMaterial != MaterialType.EMPTY)
                    {
                        matName  = t.groundMaterial.ToString();
                        if (matName.Length > 4) matName = matName.Substring(0, 4);
                        matColor = GetMaterialColor(t.groundMaterial);
                    }
                }

                _dimStyle.fontSize = 8;
                _dimStyle.normal.textColor = _textSecondary;
                GUI.Label(new Rect(x + i * colW,        y, 20f, 18f), names[i], _dimStyle);

                _valueStyle.fontSize = 9;
                _valueStyle.normal.textColor = matColor;
                GUI.Label(new Rect(x + i * colW + 18f, y, 40f, 18f), matName, _valueStyle);
            }

            y += 18f;
        }

        private void DrawDivider(float x, ref float y)
        {
            GUI.color = _borderCol;
            GUI.DrawTexture(new Rect(x, y, _panelWidth - 30f, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            y += 8f;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static bool IsIgnitionRule(RuleID id) =>
            id == RuleID.R01_COMBUSTION || id == RuleID.R10_GAS_IGNITION;

        private bool MatchesFilter(RuleID id)
        {
            switch (_currentFilter)
            {
                case RuleFilter.Fire:
                    return id == RuleID.R01_COMBUSTION || id == RuleID.R10_GAS_IGNITION;
                case RuleFilter.Phase:
                    return id >= RuleID.R13_MELTING && id <= RuleID.R16_CONDENSATION;
                case RuleFilter.Pressure:
                    return id == RuleID.R05_PRESSURE_EXPLOSION
                        || id == RuleID.R06_PRESSURE_RELEASE
                        || id == RuleID.R12_GAS_PRESSURE;
                default:
                    return true;
            }
        }

        private static string FormatTime(float seconds)
        {
            int   mins = Mathf.FloorToInt(seconds / 60f);
            float secs = seconds % 60f;
            return $"{mins:D2}:{secs:F2}";
        }

        private static Color GetMaterialColor(MaterialType mat)
        {
            switch (mat)
            {
                case MaterialType.WATER:        return new Color(0.2f,  0.5f,  1f);
                case MaterialType.LAVA:         return new Color(1f,    0.35f, 0.1f);
                case MaterialType.STEAM:        return new Color(0.9f,  0.9f,  1f);
                case MaterialType.SMOKE:        return new Color(0.3f,  0.25f, 0.2f);
                case MaterialType.CO2:          return new Color(0.3f,  0.8f,  0.6f);
                case MaterialType.STONE:        return new Color(0.5f,  0.5f,  0.5f);
                case MaterialType.METAL:        return new Color(0.7f,  0.7f,  0.75f);
                case MaterialType.WOOD:         return new Color(0.6f,  0.4f,  0.2f);
                case MaterialType.EARTH:        return new Color(0.45f, 0.3f,  0.2f);
                case MaterialType.GLASS:        return new Color(0.6f,  0.9f,  0.9f);
                case MaterialType.ICE:          return new Color(0.7f,  0.9f,  1f);
                case MaterialType.MUD:          return new Color(0.35f, 0.25f, 0.15f);
                case MaterialType.MOLTEN_METAL: return new Color(0.9f,  0.7f,  0.5f);
                case MaterialType.MOLTEN_GLASS: return new Color(0.5f,  0.8f,  0.8f);
                case MaterialType.ROCK_GAS:     return new Color(0.7f,  0.3f,  0.8f);
                default:                        return Color.gray;
            }
        }

        private static Vector2Int ScreenToTile(Vector3 screenPos)
        {
            if (Camera.main == null) return Vector2Int.zero;
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
            return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        }

        // ── Styles ────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_bgStyle != null) return;

            _bgStyle = new GUIStyle { normal = { background = MakeTex(1, 1, _bgCol) } };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = _headerCol }
            };

            _rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = _textPrimary }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = _textPrimary }
            };

            _dimStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = _textSecondary }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}