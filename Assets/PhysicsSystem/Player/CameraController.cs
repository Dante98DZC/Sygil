// Assets/PhysicsSystem/Player/CameraController.cs
using UnityEngine;

namespace PhysicsSystem.Player
{
    /// <summary>
    /// Control de cámara 2D top-down para inspeccionar el grid.
    ///
    /// CONTROLES:
    ///   Click medio (rueda) + arrastrar  → pan
    ///   Click derecho + arrastrar        → pan (alternativo)
    ///   Rueda del ratón                  → zoom
    ///
    /// SETUP:
    ///   Añadir este componente a Main Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed    = 2f;
        [SerializeField] private float _zoomMin      = 2f;
        [SerializeField] private float _zoomMax      = 30f;
        [SerializeField] private float _zoomSmoothing = 8f;

        // ── Internals ────────────────────────────────────────────────────────
        private Camera  _cam;
        private Vector3 _dragOriginWorld;
        private bool    _isPanning;
        private float   _targetOrthoSize;

        // ────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _cam             = GetComponent<Camera>();
            _targetOrthoSize = _cam.orthographicSize;
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
            SmoothZoom();
        }

        // ── Pan ──────────────────────────────────────────────────────────────
        private void HandlePan()
        {
            bool panButton = Input.GetMouseButton(2) || Input.GetMouseButton(1);

            if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
            {
                _dragOriginWorld = MouseToWorld();
                _isPanning       = true;
            }

            if (_isPanning && panButton)
            {
                Vector3 delta     = _dragOriginWorld - MouseToWorld();
                transform.position += delta;
            }

            if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
                _isPanning = false;
        }

        // ── Zoom ─────────────────────────────────────────────────────────────
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            Vector3 pivotBefore  = MouseToWorld();

            _targetOrthoSize = Mathf.Clamp(
                _targetOrthoSize - scroll * _zoomSpeed,
                _zoomMin,
                _zoomMax
            );

            _cam.orthographicSize = _targetOrthoSize;

            Vector3 pivotAfter   = MouseToWorld();
            transform.position  += pivotBefore - pivotAfter;
        }

        private void SmoothZoom()
        {
            if (Mathf.Approximately(_cam.orthographicSize, _targetOrthoSize)) return;

            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize,
                _targetOrthoSize,
                Time.deltaTime * _zoomSmoothing
            );
        }

        // ── Helper ───────────────────────────────────────────────────────────
        private Vector3 MouseToWorld()
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z       = -transform.position.z;
            return _cam.ScreenToWorldPoint(mouse);
        }
    }
}