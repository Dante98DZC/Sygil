// Assets/PhysicsSystem/Debug/SimulationDebugHUD.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Rules;

namespace PhysicsSystem.DebugTools
{
    /// <summary>
    /// HUD de control de la simulación.
    ///
    /// Controles:
    ///   P            — Pausar / Reanudar
    ///   F            — Step FAST tick (solo en pausa)
    ///   S            — Step STANDARD tick (solo en pausa)
    ///   L            — Step SLOW tick (solo en pausa)
    ///   I            — Step INTEGRITY tick (solo en pausa)
    ///
    /// Setup: adjuntar a cualquier GameObject con referencia al SimulationEngine.
    /// </summary>
    public class SimulationDebugHUD : MonoBehaviour
    {
        [SerializeField] private SimulationEngine _engine;

        [Header("Posición del HUD")]
        [SerializeField] private Vector2 _hudPosition = new Vector2(10f, 10f);

        private int  _ticksFast, _ticksStandard, _ticksSlow, _ticksIntegrity;
        private int  _rulesThisSecond;
        private int  _rulesDisplay;
        private float _ruleSecondTimer;

        private GUIStyle _bgStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _pausedStyle;
        private GUIStyle _keyStyle;

        private const float PanelWidth = 220f;

        private void OnEnable()
        {
            RuleRegistry.OnRuleFired += HandleRuleFired;
        }

        private void OnDisable()
        {
            RuleRegistry.OnRuleFired -= HandleRuleFired;
        }

        private void HandleRuleFired(RuleID id, Vector2Int pos)
        {
            _rulesThisSecond++;
        }

        private void Update()
        {
            HandleInput();
            UpdateRuleCounter();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.P))
                _engine.TogglePause();

            if (!_engine.IsPaused) return;

            if (Input.GetKeyDown(KeyCode.F)) { _engine.StepTick(TickType.FAST);      _ticksFast++;      }
            if (Input.GetKeyDown(KeyCode.S)) { _engine.StepTick(TickType.STANDARD);  _ticksStandard++;  }
            if (Input.GetKeyDown(KeyCode.L)) { _engine.StepTick(TickType.SLOW);       _ticksSlow++;      }
            if (Input.GetKeyDown(KeyCode.I)) { _engine.StepTick(TickType.INTEGRITY);  _ticksIntegrity++; }
        }

        private void UpdateRuleCounter()
        {
            _ruleSecondTimer += Time.deltaTime;
            if (_ruleSecondTimer >= 1f)
            {
                _rulesDisplay     = _rulesThisSecond;
                _rulesThisSecond  = 0;
                _ruleSecondTimer  = 0f;
            }
        }

        private void OnGUI()
        {
            if (_engine == null) return;
            EnsureStyles();

            float x = _hudPosition.x;
            float y = _hudPosition.y;

            bool paused = _engine.IsPaused;

            // Fondo
            GUI.Box(new Rect(x, y, PanelWidth, paused ? 190f : 160f), GUIContent.none, _bgStyle);

            x += 10f; y += 10f;

            // Estado
            string statusLabel = paused ? "⏸ PAUSADO" : "▶ CORRIENDO";
            GUI.Label(new Rect(x, y, PanelWidth - 20f, 20f), statusLabel,
                paused ? _pausedStyle : _headerStyle);
            y += 24f;

            // Tiles activos
            DrawRow(x, ref y, "Tiles activos", _engine.Grid.ActiveTiles.Count.ToString());
            DrawRow(x, ref y, "Reglas/seg",    _rulesDisplay.ToString());
            y += 4f;

            // Contadores de tick
            DrawRow(x, ref y, "Ticks FAST",      _ticksFast.ToString());
            DrawRow(x, ref y, "Ticks STANDARD",  _ticksStandard.ToString());
            DrawRow(x, ref y, "Ticks SLOW",      _ticksSlow.ToString());
            DrawRow(x, ref y, "Ticks INTEGRITY", _ticksIntegrity.ToString());

            if (paused)
            {
                y += 6f;
                GUI.Label(new Rect(x, y, PanelWidth - 20f, 16f), "[F] Fast  [S] Std  [L] Slow  [I] Int", _keyStyle);
            }
        }

        private void DrawRow(float x, ref float y, string label, string value)
        {
            GUI.Label(new Rect(x,         y, 130f, 18f), label, _rowStyle);
            GUI.Label(new Rect(x + 135f,  y, 65f,  18f), value, _rowStyle);
            y += 18f;
        }

        private void EnsureStyles()
        {
            if (_bgStyle != null) return;

            var bgTex = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.08f, 0.88f));

            _bgStyle = new GUIStyle { normal = { background = bgTex } };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.4f, 1f, 0.4f) },
            };

            _pausedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 0.6f, 0.2f) },
            };

            _rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = Color.white },
            };

            _keyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.6f, 0.6f, 0.6f) },
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