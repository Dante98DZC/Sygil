// Assets/PhysicsSystem/Renderer/PropertyOverlayRenderer.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Bridge;

namespace PhysicsSystem.Renderer
{
    /// <summary>
    /// Render-2b — Overlay de propiedades numéricas sobre el grid.
    ///
    /// Mantiene una Texture2D (1 pixel = 1 tile) actualizada por evento.
    /// El modo activo determina qué propiedad se visualiza.
    ///
    /// CONTROLES EN RUNTIME:
    ///   0 → apagar overlay
    ///   1 → Temperatura        (rojo/amarillo)
    ///   2 → Presión            (azul claro)
    ///   3 → Humedad            (azul)
    ///   4 → Energía eléctrica  (cian)
    ///   5 → Densidad de gas    (verde)
    ///   6 → Daño estructural   (rojo — integridad invertida)
    ///   7 → Estados derivados  (colores por flag)
    ///   8 → Actividad          (debug — tiles activos)
    ///   9 → Combinado          (mezcla aditiva)
    ///
    /// SETUP EN ESCENA:
    ///   1. Crear GameObject con SpriteRenderer (Order in Layer > Tilemap).
    ///   2. Añadir este componente al mismo GameObject.
    ///   3. Asignar _engine y _overlayRenderer en Inspector.
    ///   4. Posicionar en (0, 0, 0).
    /// </summary>
    public class PropertyOverlayRenderer : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private SimulationEngine _engine;
        [SerializeField] private SpriteRenderer   _overlayRenderer;

        [Header("Modo inicial")]
        [SerializeField] private OverlayMode _activeMode = OverlayMode.Temperature;

        [Header("Debug")]
        [SerializeField] private bool _showModeLabel = true;

        // ── Internals ────────────────────────────────────────────────────────
        private Texture2D      _texture;
        private Color[]        _pixels;
        private bool           _textureDirty;
        private EngineNotifier _notifier;

        private int Width  => _engine.Grid.Width;
        private int Height => _engine.Grid.Height;

        private static readonly string[] _modeNames =
        {
            "OFF", "Temperatura", "Presión", "Humedad",
            "Electricidad", "Gas", "Daño estructural",
            "Estados derivados", "Actividad", "Combinado"
        };

        // ────────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (_engine == null || _overlayRenderer == null)
            {
                Debug.LogError("[PropertyOverlayRenderer] Faltan referencias en el Inspector.");
                enabled = false;
                return;
            }

            InitTexture();
            RefreshFullGrid();
            ApplyTexture();

            _notifier = _engine.Notifier;
            _notifier.OnPropertiesChanged += HandlePropertiesChanged;
        }

        private void OnDestroy()
        {
            if (_notifier != null)
                _notifier.OnPropertiesChanged -= HandlePropertiesChanged;

            if (_texture != null)
                Destroy(_texture);
        }

        // ── Input — teclas 0-9 ───────────────────────────────────────────────
        private void Update()
        {
            for (int i = 0; i <= 9; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha0 + i)) continue;

                var newMode = (OverlayMode)i;
                if (newMode == _activeMode) continue;

                _activeMode = newMode;
                OnModeChanged();
                break;
            }
        }

        private void OnModeChanged()
        {
            _overlayRenderer.enabled = (_activeMode != OverlayMode.None);

            if (_activeMode != OverlayMode.None)
            {
                RefreshFullGrid();
                ApplyTexture();
                _textureDirty = false;
            }
        }

        // ── LateUpdate aplica textura una vez por frame ──────────────────────
        private void LateUpdate()
        {
            if (!_textureDirty || _activeMode == OverlayMode.None) return;
            ApplyTexture();
            _textureDirty = false;
        }

        // ── Evento de propiedades cambiadas ──────────────────────────────────
        private void HandlePropertiesChanged(Vector2Int pos, TilePropertySnapshot snapshot)
        {
            if (_activeMode == OverlayMode.None) return;

            var tile = _engine.Grid.GetTile(pos);
            SetPixel(pos, tile, isActive: true);
            _textureDirty = true;
        }

        // ────────────────────────────────────────────────────────────────────
        private void InitTexture()
        {
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };
            _pixels = new Color[Width * Height];
        }

        private void RefreshFullGrid()
        {
            var activeTiles = _engine.Grid.ActiveTiles;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var pos     = new Vector2Int(x, y);
                    var tile    = _engine.Grid.GetTile(pos);
                    bool active = activeTiles.Contains(pos);
                    SetPixel(pos, tile, active);
                }
        }

        private void ApplyTexture()
        {
            _texture.SetPixels(_pixels);
            _texture.Apply(false);

            _overlayRenderer.sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, Width, Height),
                Vector2.zero,
                1f
            );
        }

        private void SetPixel(Vector2Int pos, TileData tile, bool isActive)
        {
            _pixels[pos.x + pos.y * Width] =
                OverlayColorizer.GetColor(tile, _activeMode, isActive);
        }

        // ── Label de debug ───────────────────────────────────────────────────
        private void OnGUI()
        {
            if (!_showModeLabel) return;

            int    idx   = (int)_activeMode;
            string label = idx < _modeNames.Length
                ? $"Overlay: {_modeNames[idx]}"
                : "Overlay: ?";

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(8, 8, 210, 26), Texture2D.whiteTexture);

            GUI.color = _activeMode == OverlayMode.None
                ? new Color(0.6f, 0.6f, 0.6f)
                : Color.white;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                padding   = new RectOffset(8, 0, 4, 0)
            };

            GUI.Label(new Rect(8, 8, 210, 26), label, style);
            GUI.color = Color.white;
        }
    }
}
