// Assets/PhysicsSystem/Bridge/EngineNotifier.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Bridge
{
    /// <summary>
    /// Compara snapshot pre-tick vs post-tick y emite eventos de cambio.
    ///
    /// Eventos:
    ///   OnMaterialChanged    — el material dominante del tile cambió
    ///   OnTileStatesChanged  — los derivedStates del tile cambiaron
    ///   OnPropertiesChanged  — al menos una propiedad numérica cambió
    /// </summary>
    public class EngineNotifier
    {
        // ── Eventos ───────────────────────────────────────────────────────────

        public event Action<Vector2Int, StateFlags, StateFlags>     OnTileStatesChanged;
        public event Action<Vector2Int, MaterialType, MaterialType> OnMaterialChanged;

        /// <summary>
        /// Dispara cuando cambia cualquier propiedad numérica de un tile.
        /// No incluye cambios de material (esos van a OnMaterialChanged).
        /// </summary>
        public event Action<Vector2Int, TilePropertySnapshot> OnPropertiesChanged;

        // ── Snapshots internos ────────────────────────────────────────────────

        private readonly Dictionary<Vector2Int, StateFlags>           _prevStates    = new();
        private readonly Dictionary<Vector2Int, MaterialType>         _prevMaterials = new();
        private readonly Dictionary<Vector2Int, TilePropertySnapshot> _prevProps     = new();

        private const float PropertyChangeTolerance = 0.1f;

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Material dominante del tile según prioridad de capa: Liquid > Gas > Ground.
        /// Nunca usa tile.material (obsoleto).
        /// </summary>
        private static MaterialType GetDominantMaterial(in TileData tile)
        {
            if (tile.liquidMaterial != MaterialType.EMPTY) return tile.liquidMaterial;
            if (tile.gasMaterial    != MaterialType.EMPTY) return tile.gasMaterial;
            if (tile.groundMaterial != MaterialType.EMPTY) return tile.groundMaterial;
            return MaterialType.EMPTY;
        }

        // ── Snapshot ──────────────────────────────────────────────────────────

        /// <summary>
        /// Captura el estado pre-tick de todos los tiles activos y sus vecinos.
        /// Incluir vecinos evita comparar tiles recién activados contra Zero.
        /// </summary>
        public void Snapshot(PhysicsGrid grid)
        {
            foreach (var pos in grid.ActiveTiles)
                SnapshotTile(pos, grid);

            foreach (var pos in grid.ActiveTiles)
                foreach (var neighbor in grid.GetNeighborPositions(pos))
                    if (!_prevMaterials.ContainsKey(neighbor))
                        SnapshotTile(neighbor, grid);
        }

        private void SnapshotTile(Vector2Int pos, PhysicsGrid grid)
        {
            ref readonly var tile = ref grid.GetTile(pos);
            _prevStates[pos]    = tile.derivedStates;
            _prevMaterials[pos] = GetDominantMaterial(tile);
            _prevProps[pos]     = TilePropertySnapshot.From(tile);
        }

        // ── Dispatch ──────────────────────────────────────────────────────────

        public void Dispatch(PhysicsGrid grid)
        {
            foreach (var pos in grid.ActiveTiles)
            {
                ref readonly var tile = ref grid.GetTile(pos);

                // ── States ────────────────────────────────────────────────────
                if (_prevStates.TryGetValue(pos, out var prevState) &&
                    prevState != tile.derivedStates)
                    OnTileStatesChanged?.Invoke(pos, prevState, tile.derivedStates);

                // ── Material dominante ────────────────────────────────────────
                var prevMat = _prevMaterials.TryGetValue(pos, out var pm) ? pm : MaterialType.EMPTY;
                var currMat = GetDominantMaterial(tile);
                if (prevMat != currMat)
                    OnMaterialChanged?.Invoke(pos, prevMat, currMat);

                // ── Propiedades numéricas ─────────────────────────────────────
                if (OnPropertiesChanged == null) continue;

                var prevP = _prevProps.TryGetValue(pos, out var pp) ? pp : TilePropertySnapshot.Zero;
                var curr  = TilePropertySnapshot.From(tile);

                if (prevP.HasSignificantChange(curr, PropertyChangeTolerance))
                    OnPropertiesChanged.Invoke(pos, curr);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Snapshot ligero de las propiedades numéricas de un tile.
    /// Struct para evitar allocaciones en el hot path.
    /// </summary>
    public readonly struct TilePropertySnapshot
    {
        public readonly float Temperature;
        public readonly float ElectricEnergy;
        public readonly float GasConcentration;
        public readonly float StructuralIntegrity;
        public readonly float LiquidVolume;

        public static readonly TilePropertySnapshot Zero = default;

        private TilePropertySnapshot(float t, float e, float g, float s, float lv)
        {
            Temperature         = t;
            ElectricEnergy      = e;
            GasConcentration   = g;
            StructuralIntegrity = s;
            LiquidVolume        = lv;
        }

        public static TilePropertySnapshot From(in TileData tile) => new(
            tile.temperature,
            tile.electricEnergy,
            tile.gasConcentration,
            tile.structuralIntegrity,
            tile.liquidVolume
        );

        public bool HasSignificantChange(TilePropertySnapshot other, float tolerance) =>
            Mathf.Abs(Temperature         - other.Temperature)         > tolerance ||
            Mathf.Abs(ElectricEnergy      - other.ElectricEnergy)      > tolerance ||
            Mathf.Abs(GasConcentration   - other.GasConcentration)   > tolerance ||
            Mathf.Abs(StructuralIntegrity - other.StructuralIntegrity) > tolerance ||
            Mathf.Abs(LiquidVolume        - other.LiquidVolume)        > tolerance;

        /// <summary>
        /// Intensidad dominante normalizada [0..1].
        /// Usada por PropertyOverlayRenderer para el modo combinado (tecla 9).
        /// LiquidVolume se normaliza contra 1000 (capacidad máxima Deep).
        /// </summary>
        public float DominantIntensity =>
            Mathf.Max(
                Temperature    / 100f,
                ElectricEnergy / 100f,
                GasConcentration / 100f,
                LiquidVolume   / 1000f
            );
    }
}