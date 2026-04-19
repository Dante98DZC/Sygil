// Assets/PhysicsSystem/Debug/RuleEventLog.cs
using UnityEngine;
using System.Collections.Generic;
using PhysicsSystem.Rules;

namespace PhysicsSystem.DebugTools
{
    /// <summary>
    /// Lista en pantalla de los últimos N disparos de reglas.
    /// Muestra: tick relativo, nombre de la regla, posición del tile.
    ///
    /// Tecla C — Limpiar el log.
    ///
    /// Setup: adjuntar a cualquier GameObject. No requiere referencia al engine.
    /// Se suscribe al evento estático RuleRegistry.OnRuleFired.
    /// </summary>
    public class RuleEventLog : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private int     _maxEntries    = 40;
        [SerializeField] private bool    _pauseOnIgnition = true;
        [SerializeField] private Vector2 _hudPosition   = new Vector2(10f, 200f);
        [SerializeField] private float   _panelWidth    = 280f;
        [SerializeField] private float   _panelHeight   = 260f;

        // Referencia opcional: si está asignada, puede pausar el engine al detectar ignición
        [SerializeField] private Core.SimulationEngine _engine;

        private readonly Queue<LogEntry> _entries = new();
        private int    _totalFired;
        private float  _elapsedTime;
        private Vector2 _scroll;

        private GUIStyle _bgStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _entryStyle;
        private GUIStyle _accentStyle;
        private GUIStyle _dimStyle;

        private static readonly Dictionary<RuleID, Color> RuleColors = new()
        {
            { RuleID.R01_COMBUSTION,          new Color(1f,   0.4f, 0.1f) },  // naranja — fuego
            { RuleID.R02_EVAPORATION,         new Color(0.4f, 0.7f, 1f)   },  // azul claro — agua→vapor
            { RuleID.R03_ELECTRIC_PROPAGATION,new Color(0.3f, 1f,   1f)   },  // cian — electricidad
            { RuleID.R04_ELECTRIC_WATER,      new Color(0.3f, 0.9f, 1f)   },  // cian tenue
            { RuleID.R05_PRESSURE_EXPLOSION,  new Color(1f,   0.2f, 0.2f) },  // rojo — explosión
            { RuleID.R06_PRESSURE_RELEASE,    new Color(1f,   0.6f, 0.6f) },  // rojo tenue
            { RuleID.R07_STRUCTURAL_COLLAPSE, new Color(0.8f, 0.5f, 0.2f) },  // marrón — colapso
            { RuleID.R08_HUMIDITY_VAPORIZATION,new Color(0.5f,0.8f, 1f)   },  // azul tenue
            { RuleID.R09_HEAT_SUPPRESSION,    new Color(0.4f, 0.9f, 0.5f) },  // verde — supresión
            { RuleID.R10_GAS_IGNITION,        new Color(1f,   0.8f, 0.1f) },  // amarillo — gas encendido
            { RuleID.R11_GAS_PRODUCTION,      new Color(0.5f, 0.9f, 0.5f) },  // verde tenue
            { RuleID.R12_GAS_PRESSURE,        new Color(0.7f, 0.7f, 0.3f) },  // amarillo tenue
        };

        private struct LogEntry
        {
            public RuleID     RuleId;
            public Vector2Int Pos;
            public float      Time;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()  => RuleRegistry.OnRuleFired += HandleRuleFired;
        private void OnDisable() => RuleRegistry.OnRuleFired -= HandleRuleFired;

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.C))
                ClearLog();
        }

        private void HandleRuleFired(RuleID ruleId, Vector2Int pos)
        {
            _totalFired++;

            _entries.Enqueue(new LogEntry
            {
                RuleId = ruleId,
                Pos    = pos,
                Time   = _elapsedTime,
            });

            while (_entries.Count > _maxEntries)
                _entries.Dequeue();

            if (_pauseOnIgnition && IsIgnitionRule(ruleId) && _engine != null && !_engine.IsPaused)
                _engine.TogglePause();
        }

        private void ClearLog()
        {
            _entries.Clear();
            _totalFired = 0;
        }

        // ── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            float x = _hudPosition.x;
            float y = _hudPosition.y;

            GUI.Box(new Rect(x, y, _panelWidth, _panelHeight), GUIContent.none, _bgStyle);

            x += 8f; y += 8f;
            GUI.Label(new Rect(x, y, _panelWidth - 16f, 18f),
                $"Log de reglas  [{_entries.Count}/{_maxEntries}]  total: {_totalFired}  [C] limpiar",
                _headerStyle);
            y += 22f;

            // Scroll view — muestra las entradas más recientes
            Rect scrollView   = new Rect(x, y, _panelWidth - 16f, _panelHeight - 34f);
            Rect scrollContent = new Rect(0f, 0f, _panelWidth - 32f, _entries.Count * 18f);

            _scroll = GUI.BeginScrollView(scrollView, _scroll, scrollContent);

            float ey = 0f;
            foreach (var entry in _entries)
            {
                Color col = RuleColors.TryGetValue(entry.RuleId, out var c) ? c : Color.white;
                _accentStyle.normal.textColor = col;

                // Tiempo relativo
                GUI.Label(new Rect(0f,   ey, 38f,  17f), $"{entry.Time:F1}s", _dimStyle);
                // Nombre de regla
                GUI.Label(new Rect(40f,  ey, 165f, 17f), entry.RuleId.ToString(), _accentStyle);
                // Posición
                GUI.Label(new Rect(208f, ey, 60f,  17f), $"({entry.Pos.x},{entry.Pos.y})", _dimStyle);

                ey += 18f;
            }

            GUI.EndScrollView();

            // Auto-scroll al último entry
            _scroll.y = Mathf.Max(0f, _entries.Count * 18f - (scrollView.height - 4f));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool IsIgnitionRule(RuleID id) =>
            id == RuleID.R01_COMBUSTION || id == RuleID.R10_GAS_IGNITION;

        private void EnsureStyles()
        {
            if (_bgStyle != null) return;

            var bgTex = MakeTex(1, 1, new Color(0.06f, 0.06f, 0.06f, 0.9f));

            _bgStyle = new GUIStyle { normal = { background = bgTex } };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            };

            _entryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = Color.white },
            };

            _accentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
            };

            _dimStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.5f, 0.5f, 0.5f) },
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