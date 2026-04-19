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

        // vecinos sólidos para P_PRESSURE
        private static readonly Vector2Int[] P_PRESSURE_NEIGHBORS =
        {
            new(3, 6), new(1, 6), new(2, 7), new(2, 5)
        };

        private Dictionary<Vector2Int, TileData> _baseline = new();

        // R03/R04 se verifican por delta capturado en el primer tick
        private bool _r03Triggered = false;
        private bool _r04Triggered = false;
        private float _elecBaselineTemp = 10f;

        private void Start() => StartCoroutine(InitAfterEngine());

        private IEnumerator InitAfterEngine()
        {
            yield return new WaitUntil(() => engine != null && engine.Grid != null);
            SetupTiles();

            // Captura R03 y R04 después de 1 tick (0.15s cubre FAST tick)
            yield return new WaitForSeconds(0.15f);
            _r04Triggered = engine.Grid.GetTile(P_ELEC).temperature > _elecBaselineTemp;
            _r03Triggered = engine.Grid.GetTile(P_ELECPROP_DST).electricEnergy > 0f
                         || engine.Grid.GetTile(P_ELECPROP).electricEnergy < _baseline[P_ELECPROP].electricEnergy;

            Debug.Log($"✅ Setup listo | R03 capturado={_r03Triggered} | R04 capturado={_r04Triggered}");
            Debug.Log("R=reporte | SPACE=reiniciar | T=thermal | H=humidifier | E=electric");
        }

        private void SetupTiles()
        {
            _r03Triggered = false;
            _r04Triggered = false;

            // R07 — colapso estructural
            Set(P_COLLAPSE, new TileData { material = MaterialType.WOOD, structuralIntegrity = 5f });

            // R01 — combustión
            Set(P_FIRE, new TileData { material = MaterialType.WOOD, temperature = 80f, structuralIntegrity = 80f, humidity = 5f });

            // R09 — supresión por humedad
            Set(P_SUPPRESS, new TileData { material = MaterialType.WOOD, temperature = 80f, humidity = 65f, structuralIntegrity = 80f });

            // R02 — evaporación
            Set(P_EVAP, new TileData { material = MaterialType.WATER, temperature = 100f, humidity = 50f, structuralIntegrity = 50f });
                var evapNeighbors = new Vector2Int[]{ new(15,2), new(13,2), new(14,3), new(14,1) };
                foreach (var n in evapNeighbors)
            Set(n, new TileData { material = MaterialType.STONE, temperature = 90f, structuralIntegrity = 80f });
            // R10 — ignición de gas
            Set(P_GAS, new TileData { material = MaterialType.GAS, gasDensity = 70f, temperature = 65f });

            // R04 — electricidad en agua (con integridad alta para no colapsar)
            _elecBaselineTemp = 10f;
            Set(P_ELEC, new TileData { material = MaterialType.WATER, electricEnergy = 100f, temperature = 10f, structuralIntegrity = 100f });

            // R05/R06 — presión con vecinos sólidos de alta integridad
            Set(P_PRESSURE, new TileData { material = MaterialType.STONE, pressure = 90f, structuralIntegrity = 80f });
            foreach (var n in P_PRESSURE_NEIGHBORS)
                Set(n, new TileData { material = MaterialType.STONE, structuralIntegrity = 80f });

            // R08 — vaporización por humedad
            Set(P_HUMID, new TileData { material = MaterialType.WOOD, humidity = 70f, temperature = 60f });

            // R03 — propagación eléctrica (receptor adyacente)
            Set(P_ELECPROP,     new TileData { material = MaterialType.METAL, electricEnergy = 80f });
            Set(P_ELECPROP_DST, new TileData { material = MaterialType.METAL, electricEnergy = 0f  });

            _baseline.Clear();
            foreach (var p in AllPositions())
                _baseline[p] = engine.Grid.GetTile(p);
            _baseline[P_ELECPROP_DST] = engine.Grid.GetTile(P_ELECPROP_DST);
        }

        private void Set(Vector2Int pos, TileData data) => engine.SetTile(pos, data);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))     PrintFullReport();
            if (Input.GetKeyDown(KeyCode.Space)) { Debug.Log("↺ Reiniciando..."); StartCoroutine(ReiniciarCoroutine()); }
            if (Input.GetKeyDown(KeyCode.T))     FindFirstObjectByType<PlayerActions>()?.RequestAction(ActionType.THERMAL_PULSE,  P_FIRE);
            if (Input.GetKeyDown(KeyCode.H))     FindFirstObjectByType<PlayerActions>()?.RequestAction(ActionType.HUMIDIFIER,     P_FIRE);
            if (Input.GetKeyDown(KeyCode.E))     FindFirstObjectByType<PlayerActions>()?.RequestAction(ActionType.ELECTRIC_PULSE, P_ELEC);
        }

        private IEnumerator ReiniciarCoroutine()
        {
            SetupTiles();
            yield return new WaitForSeconds(0.15f);
            _r04Triggered = engine.Grid.GetTile(P_ELEC).temperature > _elecBaselineTemp;
            _r03Triggered = engine.Grid.GetTile(P_ELECPROP_DST).electricEnergy > 0f
                         || engine.Grid.GetTile(P_ELECPROP).electricEnergy < _baseline[P_ELECPROP].electricEnergy;
            Debug.Log($"↺ Reiniciado | R03={_r03Triggered} | R04={_r04Triggered}");
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

            bool r07 = tc.material == MaterialType.EMPTY && tc.structuralIntegrity == 0f;
            bool r01 = tf.gasDensity > 0f && tf.temperature > 80f;
            bool r09 = ts.temperature < _baseline[P_SUPPRESS].temperature;
            bool r02 = te.material != MaterialType.WATER;
            bool r10 = tg.gasDensity < _baseline[P_GAS].gasDensity;
            bool r04 = _r04Triggered;
            bool r05 = tp.pressure < _baseline[P_PRESSURE].pressure;
            bool r08 = th.gasDensity > 0f || th.pressure > 0f;
            bool r03 = _r03Triggered;

            int passed = Count(r07, r01, r09, r02, r10, r04, r05, r08, r03);

            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  PHYSICS SYSTEM REPORT  t={Time.time:F1}s  ActiveTiles={engine.Grid.ActiveTiles.Count}  RESULTADO={passed}/9 {(passed==9?"✅ TODO OK":$"⚠️ {9-passed} FALLO(S)")}");
            sb.AppendLine("╠═══════╦════════════════════════════╦═══════════════════════════════════════════════════════╣");
            sb.AppendLine("║ REGLA ║ CONDICIÓN                  ║ VALORES ACTUALES                                      ║");
            sb.AppendLine("╠═══════╬════════════════════════════╬═══════════════════════════════════════════════════════╣");
            sb.AppendLine($"║ R07 {S(r07)} ║ mat=EMPTY int=0            ║ mat={tc.material,-8} int={tc.structuralIntegrity:F0} states=[{tc.derivedStates}]");
            sb.AppendLine($"║ R01 {S(r01)} ║ gas>0 temp>80              ║ temp={tf.temperature:F1} gas={tf.gasDensity:F1} int={tf.structuralIntegrity:F1} states=[{tf.derivedStates}]");
            sb.AppendLine($"║ R09 {S(r09)} ║ temp < baseline(80)        ║ temp={ts.temperature:F1}(Δ{ts.temperature-_baseline[P_SUPPRESS].temperature:+0.0;-0.0}) hum={ts.humidity:F1} states=[{ts.derivedStates}]");
            sb.AppendLine($"║ R02 {S(r02)} ║ mat != WATER               ║ mat={te.material,-8} hum={te.humidity:F1} pres={te.pressure:F1} states=[{te.derivedStates}]");
            sb.AppendLine($"║ R10 {S(r10)} ║ gasDensity < baseline(70)  ║ temp={tg.temperature:F1}(Δ{tg.temperature-_baseline[P_GAS].temperature:+0.0;-0.0}) gas={tg.gasDensity:F1} states=[{tg.derivedStates}]");
            sb.AppendLine($"║ R04 {S(r04)} ║ temp subió en primer tick  ║ temp={tel.temperature:F1}(Δ{tel.temperature-_elecBaselineTemp:+0.0;-0.0}) elec={tel.electricEnergy:F1} states=[{tel.derivedStates}]");
            sb.AppendLine($"║ R05 {S(r05)} ║ pres < baseline(90)        ║ pres={tp.pressure:F1}(Δ{tp.pressure-_baseline[P_PRESSURE].pressure:+0.0;-0.0}) int={tp.structuralIntegrity:F1} states=[{tp.derivedStates}]");
            sb.AppendLine($"║ R08 {S(r08)} ║ gas>0 o pres>0             ║ hum={th.humidity:F1} gas={th.gasDensity:F1} pres={th.pressure:F1} states=[{th.derivedStates}]");
            sb.AppendLine($"║ R03 {S(r03)} ║ elec propagó a vecino      ║ src={tep.electricEnergy:F1} dst={tepd.electricEnergy:F1}(Δ{tepd.electricEnergy-_baseline[P_ELECPROP_DST].electricEnergy:+0.0;-0.0}) states=[{tep.derivedStates}]");
            sb.AppendLine("╠═══════╩════════════════════════════╩═══════════════════════════════════════════════════════╣");
            sb.AppendLine("║ DIAGNÓSTICO DELTA POR TILE                                                                                    ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════════════════════╣");
            foreach (var pos in AllPositions())
            {
                var t = engine.Grid.GetTile(pos);
                var b = _baseline.ContainsKey(pos) ? _baseline[pos] : default;
                sb.AppendLine(
                    $"║ {pos,-7} mat={t.material,-6} " +
                    $"T={t.temperature,5:F1}(Δ{t.temperature-b.temperature:+0.0;-0.0}) " +
                    $"P={t.pressure,5:F1}(Δ{t.pressure-b.pressure:+0.0;-0.0}) " +
                    $"H={t.humidity,5:F1}(Δ{t.humidity-b.humidity:+0.0;-0.0}) " +
                    $"E={t.electricEnergy,5:F1}(Δ{t.electricEnergy-b.electricEnergy:+0.0;-0.0}) " +
                    $"G={t.gasDensity,5:F1}(Δ{t.gasDensity-b.gasDensity:+0.0;-0.0}) " +
                    $"I={t.structuralIntegrity,5:F1}(Δ{t.structuralIntegrity-b.structuralIntegrity:+0.0;-0.0})"
                );
            }
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════════════════════╝");

            Debug.Log(sb.ToString());
        }

        private Vector2Int[] AllPositions() => new[]
        {
            P_COLLAPSE, P_FIRE, P_SUPPRESS, P_EVAP,
            P_GAS, P_ELEC, P_PRESSURE, P_HUMID, P_ELECPROP, P_ELECPROP_DST
        };

        private static int Count(params bool[] vals) { int n = 0; foreach (var v in vals) if (v) n++; return n; }
        private static string S(bool ok) => ok ? "✅" : "❌";
    }
}