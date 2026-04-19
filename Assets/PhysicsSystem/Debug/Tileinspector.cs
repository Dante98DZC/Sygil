// Assets/PhysicsSystem/Debug/TileInspector.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.DebugTools
{
    /// <summary>
    /// Muestra los valores exactos de un tile al pasar el cursor.
    /// Click izquierdo fija/desfija el tile inspeccionado.
    /// No modifica el engine ni el grid — solo lectura.
    ///
    /// Setup: adjuntar a cualquier GameObject con referencia al SimulationEngine.
    /// </summary>
    public class TileInspector : MonoBehaviour
    {
        [SerializeField] private SimulationEngine _engine;
        [SerializeField] private bool             _enabled = true;

        private Vector2Int? _pinned;
        private GUIStyle    _panelStyle;
        private GUIStyle    _headerStyle;
        private GUIStyle    _rowStyle;
        private GUIStyle    _labelStyle;
        private GUIStyle    _valueStyle;
        private GUIStyle    _pinnedBadge;

        private const float PanelWidth  = 210f;
        private const float PanelHeight = 350f;
        private const float PanelOffset = 14f;

        private void Update()
        {
            if (!_enabled) return;
            if (Input.GetMouseButtonDown(0))
                TogglePin();
        }

        private void OnGUI()
        {
            if (!_enabled || _engine == null) return;

            EnsureStyles();

            Vector2Int tilePos = _pinned ?? ScreenToTile(Input.mousePosition);
            if (!_engine.Grid.InBounds(tilePos)) return;

            TileData tile = _engine.Grid.GetTile(tilePos);

            Rect panelRect = ComputePanelRect(Input.mousePosition);
            DrawPanel(panelRect, tilePos, tile);
        }

        // ── Pin ──────────────────────────────────────────────────────────────

        private void TogglePin()
        {
            Vector2Int hovered = ScreenToTile(Input.mousePosition);
            if (!_engine.Grid.InBounds(hovered)) return;

            _pinned = (_pinned.HasValue && _pinned.Value == hovered) ? null : hovered;
        }

        // ── Drawing ──────────────────────────────────────────────────────────

        private void DrawPanel(Rect rect, Vector2Int pos, TileData tile)
        {
            GUI.Box(rect, GUIContent.none, _panelStyle);

            float x = rect.x + 10f;
            float y = rect.y + 10f;

            // Header
            string pinIndicator = _pinned.HasValue ? " [FIJADO]" : "";
            GUI.Label(new Rect(x, y, PanelWidth - 20f, 20f),
                $"Tile ({pos.x}, {pos.y}){pinIndicator}", _headerStyle);
            y += 22f;

            // Material + height
            DrawRow(x, ref y, "Ground",    tile.groundMaterial.ToString());
            DrawRow(x, ref y, "Liquid",   tile.liquidMaterial.ToString());
            DrawRow(x, ref y, "Gas",      tile.gasMaterial.ToString());
            DrawRow(x, ref y, "Altura",    tile.height.ToString());
            y += 4f;

            // Propiedades numéricas
            DrawRow(x, ref y, "Temperatura",    $"{tile.temperature:F1}");
            DrawRow(x, ref y, "Densidad Gas",  $"{tile.gasDensity:F1}");
            DrawRow(x, ref y, "Volumen Líq",  $"{tile.liquidVolume:F1}");
            DrawRow(x, ref y, "Electricidad",   $"{tile.electricEnergy:F1}");
            DrawRow(x, ref y, "Integridad",    $"{tile.structuralIntegrity:F1}");
            y += 4f;

            // Estados derivados
            DrawRow(x, ref y, "En llamas",     FlagStr(tile.derivedStates.HasFlag(StateFlags.ON_FIRE)));
            DrawRow(x, ref y, "Electrificado", FlagStr(tile.derivedStates.HasFlag(StateFlags.ELECTRIFIED)));
            DrawRow(x, ref y, "Presurizado",   FlagStr(tile.derivedStates.HasFlag(StateFlags.PRESSURIZED)));
            DrawRow(x, ref y, "Inundado",      FlagStr(tile.derivedStates.HasFlag(StateFlags.FLOODED)));
            DrawRow(x, ref y, "Volátil",       FlagStr(tile.derivedStates.HasFlag(StateFlags.VOLATILE)));
            DrawRow(x, ref y, "Débil",         FlagStr(tile.derivedStates.HasFlag(StateFlags.STRUCTURALLY_WEAK)));
            DrawRow(x, ref y, "Colapsado",     FlagStr(tile.derivedStates.HasFlag(StateFlags.COLLAPSED)));
            DrawRow(x, ref y, "Activo",        FlagStr(_engine.Grid.ActiveTiles.Contains(pos)));
            y += 4f;

            // Hint
            string hint = _pinned.HasValue ? "Click para desfijar" : "Click para fijar";
            GUI.Label(new Rect(x, y, PanelWidth - 20f, 16f), hint, _labelStyle);
        }

        private void DrawRow(float x, ref float y, string label, string value)
        {
            GUI.Label(new Rect(x,          y, 100f, 18f), label, _labelStyle);
            GUI.Label(new Rect(x + 105f,   y, 90f,  18f), value, _valueStyle);
            y += 18f;
        }

        private static string FlagStr(bool value) => value ? "SÍ" : "–";

        // ── Coordinate helpers ────────────────────────────────────────────────

        private static Vector2Int ScreenToTile(Vector3 screenPos)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
            return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        }

        private static Rect ComputePanelRect(Vector3 mouseScreen)
        {
            float guiY  = Screen.height - mouseScreen.y;
            float left  = mouseScreen.x + PanelOffset;
            float top   = guiY          + PanelOffset;

            // Clamp dentro de la pantalla
            if (left + PanelWidth  > Screen.width)  left = mouseScreen.x - PanelWidth  - PanelOffset;
            if (top  + PanelHeight > Screen.height)  top  = guiY          - PanelHeight - PanelOffset;

            return new Rect(left, top, PanelWidth, PanelHeight);
        }

        // ── Style init ────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;

            var panelTex = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.08f, 0.88f));

            _panelStyle = new GUIStyle
            {
                normal    = { background = panelTex },
                border    = new RectOffset(4, 4, 4, 4),
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1f, 0.85f, 0.3f) },
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = new Color(0.7f, 0.7f, 0.7f) },
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize   = 11,
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.UpperRight,
                normal     = { textColor = Color.white },
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