using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PhysicsSystem.Core;
using PhysicsSystem.Player;

namespace PhysicsSystem.Tests
{
    public class PhysicsSystemTest : MonoBehaviour
    {
        [SerializeField] private SimulationEngine engine;

        private static readonly Vector2Int P_COLLAPSE  = new(2,  2);
        private static readonly Vector2Int P_FIRE      = new(6,  2);
        private static readonly Vector2Int P_SUPPRESS  = new(10, 2);
        private static readonly Vector2Int P_EVAP      = new(14, 2);
        private static readonly Vector2Int P_GAS       = new(18, 2);
        private static readonly Vector2Int P_ELEC      = new(22, 2);
        private static readonly Vector2Int P_PRESSURE  = new(2,  6);
        private static readonly Vector2Int P_HUMID     = new(6,  6);
        private static readonly Vector2Int P_ELECPROP  = new(10, 6);
        private static readonly Vector2Int P_ELECPROP_DST = new(11, 6);

        private static readonly Vector2Int[] P_PRESSURE_NEIGHBORS =
        {
            new(3, 6), new(1, 6), new(2, 7), new(2, 5)
        };

        private Dictionary<Vector2Int, TileData> _baseline = new();

        private bool _r03Triggered = false;
        private bool _r04Triggered = false;
        private float _elecBaselineTemp = 10f;

        private void Start() => StartCoroutine(InitAfterEngine());

        private IEnumerator InitAfterEngine()
        {
            yield return new WaitUntil(() => engine != null && engine.Grid != null);
            SetupTiles();

            yield return new WaitForSeconds(0.15f);
            _r04Triggered = engine.Grid.GetTile(P_ELEC).temperature > _elecBaselineTemp;
            _r03Triggered = engine.Grid.GetTile(P_ELECPROP_DST).electricEnergy > 0f
                         || engine.Grid.GetTile(P_ELECPROP).electricEnergy < _baseline[P_ELECPROP].electricEnergy;

            Debug.Log($"Setup ready | R03={_r03Triggered} | R04={_r04Triggered}");
        }

        private void SetupTiles()
        {
            _r03Triggered = false;
            _r04Triggered = false;

            Set(P_COLLAPSE, new TileData { groundMaterial = MaterialType.WOOD, structuralIntegrity = 5f });
            Set(P_FIRE, new TileData { groundMaterial = MaterialType.WOOD, temperature = 80f, structuralIntegrity = 80f, liquidVolume = 5f });
            Set(P_SUPPRESS, new TileData { groundMaterial = MaterialType.WOOD, temperature = 80f, liquidVolume = 65f, structuralIntegrity = 80f });
            Set(P_EVAP, new TileData { liquidMaterial = MaterialType.WATER, temperature = 100f, liquidVolume = 50f, structuralIntegrity = 50f });

            var evapNeighbors = new Vector2Int[]{ new(15,2), new(13,2), new(14,3), new(14,1) };
            foreach (var n in evapNeighbors)
                Set(n, new TileData { groundMaterial = MaterialType.STONE, temperature = 90f, structuralIntegrity = 80f });

            Set(P_GAS, new TileData { groundMaterial = MaterialType.GAS, gasDensity = 70f, temperature = 65f });

            _elecBaselineTemp = 10f;
            Set(P_ELEC, new TileData { liquidMaterial = MaterialType.WATER, electricEnergy = 100f, temperature = 10f, structuralIntegrity = 100f });

            Set(P_PRESSURE, new TileData { groundMaterial = MaterialType.STONE, gasDensity = 90f, structuralIntegrity = 80f });
            foreach (var n in P_PRESSURE_NEIGHBORS)
                Set(n, new TileData { groundMaterial = MaterialType.STONE, structuralIntegrity = 80f });

            Set(P_HUMID, new TileData { groundMaterial = MaterialType.WOOD, liquidVolume = 70f, temperature = 60f });

            Set(P_ELECPROP,     new TileData { groundMaterial = MaterialType.METAL, electricEnergy = 80f });
            Set(P_ELECPROP_DST, new TileData { groundMaterial = MaterialType.METAL, electricEnergy = 0f });

            _baseline.Clear();
            foreach (var p in AllPositions())
                _baseline[p] = engine.Grid.GetTile(p);
        }

        private void Set(Vector2Int pos, TileData data) => engine.SetTile(pos, data);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))     PrintFullReport();
            if (Input.GetKeyDown(KeyCode.Space)) { Debug.Log("Restarting..."); StartCoroutine(Restart()); }
            if (Input.GetKeyDown(KeyCode.T))     FindFirstObjectByType<PlayerActions>()?.RequestAction(ActionType.THERMAL_PULSE,  P_FIRE);
            if (Input.GetKeyDown(KeyCode.H))     FindFirstObjectByType<PlayerActions>()?.RequestAction(ActionType.HUMIDIFIER,     P_FIRE);
            if (Input.GetKeyDown(KeyCode.E))     FindFirstObjectByType<PlayerActions>()?.RequestAction(ActionType.ELECTRIC_PULSE, P_ELEC);
        }

        private IEnumerator Restart()
        {
            SetupTiles();
            yield return new WaitForSeconds(0.15f);
            _r04Triggered = engine.Grid.GetTile(P_ELEC).temperature > _elecBaselineTemp;
            _r03Triggered = engine.Grid.GetTile(P_ELECPROP_DST).electricEnergy > 0f
                         || engine.Grid.GetTile(P_ELECPROP).electricEnergy < _baseline[P_ELECPROP].electricEnergy;
            Debug.Log($"Restarted | R03={_r03Triggered} | R04={_r04Triggered}");
        }

        private void PrintFullReport()
        {
            var tc  = engine.Grid.GetTile(P_COLLAPSE);
            var tf  = engine.Grid.GetTile(P_FIRE);
            var ts  = engine.Grid.GetTile(P_SUPPRESS);
            var te  = engine.Grid.GetTile(P_EVAP);
            var tg  = engine.Grid.GetTile(P_GAS);
            var tel = engine.Grid.GetTile(P_ELEC);
            var tp  = engine.Grid.GetTile(P_PRESSURE);
            var th  = engine.Grid.GetTile(P_HUMID);
            var tep = engine.Grid.GetTile(P_ELECPROP);
            var tepd= engine.Grid.GetTile(P_ELECPROP_DST);

            bool r07 = tc.groundMaterial == MaterialType.EMPTY && tc.structuralIntegrity == 0f;
            bool r01 = tf.gasDensity > 0f && tf.temperature > 80f;
            bool r09 = ts.temperature < _baseline[P_SUPPRESS].temperature;
            bool r02 = te.liquidMaterial != MaterialType.WATER;
            bool r10 = tg.gasDensity < _baseline[P_GAS].gasDensity;
            bool r04 = _r04Triggered;
            bool r05 = tp.gasDensity < _baseline[P_PRESSURE].gasDensity;
            bool r08 = th.gasDensity > 0f || th.liquidVolume > 0f;
            bool r03 = _r03Triggered;

            int passed = Count(r07, r01, r09, r02, r10, r04, r05, r08, r03);

            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine($"PHYSICS REPORT t={Time.time:F1}s Tiles={engine.Grid.ActiveTiles.Count} RESULT={passed}/9");
            sb.AppendLine("========================================");
            sb.AppendLine($"R07 {S(r07)}: gmat={tc.groundMaterial} int={tc.structuralIntegrity:F0}");
            sb.AppendLine($"R01 {S(r01)}: temp={tf.temperature:F1} gas={tf.gasDensity:F1} int={tf.structuralIntegrity:F1}");
            sb.AppendLine($"R09 {S(r09)}: temp={ts.temperature:F1} liq={ts.liquidVolume:F1}");
            sb.AppendLine($"R02 {S(r02)}: lmat={te.liquidMaterial} liq={te.liquidVolume:F1}");
            sb.AppendLine($"R10 {S(r10)}: temp={tg.temperature:F1} gas={tg.gasDensity:F1}");
            sb.AppendLine($"R04 {S(r04)}: temp={tel.temperature:F1} elec={tel.electricEnergy:F1}");
            sb.AppendLine($"R05 {S(r05)}: gas={tp.gasDensity:F1} int={tp.structuralIntegrity:F1}");
            sb.AppendLine($"R08 {S(r08)}: liq={th.liquidVolume:F1} gas={th.gasDensity:F1}");
            sb.AppendLine($"R03 {S(r03)}: src={tep.electricEnergy:F1} dst={tepd.electricEnergy:F1}");
            sb.AppendLine("========================================");

            foreach (var pos in AllPositions())
            {
                var t = engine.Grid.GetTile(pos);
                var b = _baseline.ContainsKey(pos) ? _baseline[pos] : default;
                sb.AppendLine(
                    $"{pos}: gmat={t.groundMaterial} lmat={t.liquidMaterial} " +
                    $"T={t.temperature:F1}(d{t.temperature-b.temperature:+0.0;-0.0}) " +
                    $"L={t.liquidVolume:F1}(d{t.liquidVolume-b.liquidVolume:+0.0;-0.0}) " +
                    $"G={t.gasDensity:F1}(d{t.gasDensity-b.gasDensity:+0.0;-0.0}) " +
                    $"E={t.electricEnergy:F1}(d{t.electricEnergy-b.electricEnergy:+0.0;-0.0}) " +
                    $"I={t.structuralIntegrity:F1}"
                );
            }
            sb.AppendLine("========================================");

            Debug.Log(sb.ToString());
        }

        private Vector2Int[] AllPositions() => new[]
        {
            P_COLLAPSE, P_FIRE, P_SUPPRESS, P_EVAP,
            P_GAS, P_ELEC, P_PRESSURE, P_HUMID, P_ELECPROP, P_ELECPROP_DST
        };

        private static int Count(params bool[] vals) { int n = 0; foreach (var v in vals) if (v) n++; return n; }
        private static string S(bool ok) => ok ? "OK" : "FAIL";
    }
}