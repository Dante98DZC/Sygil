// Assets/PhysicsSystem/Renderer/OverlayColorizer.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Renderer
{
    /// <summary>
    /// Lógica de colorización del overlay — clase estática, métodos puros.
    /// Sin estado, sin MonoBehaviour, fácil de testear y extender.
    ///
    /// Cada modo recibe un TileData y devuelve un Color RGBA.
    /// Alpha 0 = tile invisible en el overlay (no tapa el sprite base).
    /// </summary>
    public static class OverlayColorizer
    {
        // ── Colores base por modo ────────────────────────────────────────────
        private static readonly Color _colorPressure      = new Color(0.6f, 0.7f, 1.0f);
        private static readonly Color _colorHumidity      = new Color(0.3f, 0.7f, 1.0f);
        private static readonly Color _colorElectric      = new Color(0.0f, 0.9f, 1.0f);
        private static readonly Color _colorGas           = new Color(0.3f, 1.0f, 0.2f);
        private static readonly Color _colorDamage        = new Color(1.0f, 0.1f, 0.0f);
        private static readonly Color _colorActivity      = new Color(1.0f, 1.0f, 1.0f);

        // Derived states
        private static readonly Color _colorOnFire        = new Color(1.0f, 0.4f, 0.0f);
        private static readonly Color _colorVolatile      = new Color(0.8f, 1.0f, 0.0f);
        private static readonly Color _colorConductive    = new Color(0.0f, 0.8f, 1.0f);
        private static readonly Color _colorWet           = new Color(0.2f, 0.5f, 1.0f);
        private static readonly Color _colorWeak          = new Color(1.0f, 0.2f, 0.2f);
        private static readonly Color _colorCollapsed     = new Color(0.5f, 0.0f, 0.0f);

        // Alpha máximo del overlay — deja ver el sprite base debajo
        private const float MaxAlpha = 0.72f;
        // Umbral mínimo de intensidad para mostrar color (evita ruido)
        private const float Threshold = 0.04f;

        // ────────────────────────────────────────────────────────────────────
        public static Color GetColor(TileData tile, OverlayMode mode, bool isActive = false)
        {
            return mode switch
            {
                OverlayMode.None             => Color.clear,
                OverlayMode.Temperature      => Temperature(tile.temperature),
                OverlayMode.Pressure         => SimpleGradient(tile.gasDensity,   _colorPressure),
                OverlayMode.Humidity         => SimpleGradient(tile.liquidVolume, _colorHumidity),
                OverlayMode.ElectricEnergy   => ElectricGradient(tile.electricEnergy),
                OverlayMode.GasDensity       => SimpleGradient(tile.gasDensity,     _colorGas),
                OverlayMode.StructuralDamage => StructuralDamage(tile.structuralIntegrity),
                OverlayMode.DerivedStates    => DerivedStates(tile.derivedStates),
                OverlayMode.Activity         => Activity(isActive),
                OverlayMode.Combined         => Combined(tile),
                _                            => Color.clear,
            };
        }

        // ── Temperature — gradiente 4 puntos ────────────────────────────────
        // frío: azul oscuro → neutro: negro → caliente: rojo → muy caliente: amarillo
        private static Color Temperature(float value)
        {
            float t = Mathf.Clamp01(value / 100f);
            if (t < Threshold) return Color.clear;

            Color c;
            if (t < 0.40f)
            {
                // azul oscuro → negro
                float s = t / 0.40f;
                c = Color.Lerp(new Color(0.0f, 0.1f, 0.5f), new Color(0.05f, 0.0f, 0.05f), s);
            }
            else if (t < 0.70f)
            {
                // negro → rojo
                float s = (t - 0.40f) / 0.30f;
                c = Color.Lerp(new Color(0.05f, 0.0f, 0.05f), new Color(1.0f, 0.05f, 0.0f), s);
            }
            else
            {
                // rojo → amarillo
                float s = (t - 0.70f) / 0.30f;
                c = Color.Lerp(new Color(1.0f, 0.05f, 0.0f), new Color(1.0f, 0.95f, 0.0f), s);
            }

            c.a = Mathf.Lerp(0.2f, MaxAlpha, t);
            return c;
        }

        // ── Gradiente simple — transparente → color saturado ────────────────
        private static Color SimpleGradient(float value, Color baseColor)
        {
            float t = Mathf.Clamp01(value / 100f);
            if (t < Threshold) return Color.clear;

            Color c = baseColor;
            c.a = Mathf.Lerp(0.15f, MaxAlpha, t);
            return c;
        }

        // ── Electricidad — cian → blanco al saturarse ────────────────────────
        private static Color ElectricGradient(float value)
        {
            float t = Mathf.Clamp01(value / 100f);
            if (t < Threshold) return Color.clear;

            Color c = Color.Lerp(_colorElectric, Color.white, t * 0.6f);
            c.a = Mathf.Lerp(0.15f, MaxAlpha, t);
            return c;
        }

        // ── Daño estructural — solo visible cuando integridad < 60 ──────────
        // Alta integridad = invisible. Baja integridad = rojo intenso.
        private static Color StructuralDamage(float integrity)
        {
            float health = Mathf.Clamp01(integrity / 100f);
            float damage = 1f - health;

            // Solo muestra daño significativo (por debajo del 60% de salud)
            if (damage < 0.40f) return Color.clear;

            float t = Mathf.Clamp01((damage - 0.40f) / 0.60f);
            Color c = Color.Lerp(new Color(1f, 0.5f, 0f), _colorDamage, t);
            c.a = Mathf.Lerp(0.1f, MaxAlpha, t);
            return c;
        }

        // ── Estados derivados — un color por flag, aditivo ──────────────────
        private static Color DerivedStates(StateFlags flags)
        {
            if (flags == StateFlags.NONE) return Color.clear;

            Color c = Color.clear;
            int activeFlags = 0;

            if ((flags & StateFlags.ON_FIRE)            != 0) { c += _colorOnFire;     activeFlags++; }
            if ((flags & StateFlags.VOLATILE)           != 0) { c += _colorVolatile;   activeFlags++; }
            // if ((flags & StateFlags.CONDUCTIVE)         != 0) { c += _colorConductive; activeFlags++; }
            // if ((flags & StateFlags.WET)                != 0) { c += _colorWet;        activeFlags++; }
            if ((flags & StateFlags.STRUCTURALLY_WEAK)  != 0) { c += _colorWeak;       activeFlags++; }
            if ((flags & StateFlags.COLLAPSED)          != 0) { c += _colorCollapsed;  activeFlags++; }

            if (activeFlags == 0) return Color.clear;

            // Normaliza para que múltiples flags no saturen a blanco
            c /= activeFlags;
            c.a = MaxAlpha;
            return c;
        }

        // ── Actividad — debug puro ───────────────────────────────────────────
        private static Color Activity(bool isActive)
        {
            if (!isActive) return Color.clear;
            Color c = _colorActivity;
            c.a = 0.35f;
            return c;
        }

        // ── Combined — mezcla aditiva original ───────────────────────────────
        private static Color Combined(TileData tile)
        {
            float temp  = tile.temperature    / 100f;
            float press = tile.gasDensity    / 100f;
            float elec  = tile.electricEnergy / 100f;
            float gas   = tile.gasDensity     / 100f;

            Color c = Color.clear;
            c += new Color(1f, 0.2f, 0f) * temp;
            c += new Color(1f, 0.9f, 0f) * press;
            c += new Color(0f, 0.8f, 1f) * elec;
            c += new Color(0.2f, 1f, 0.3f) * gas;

            float maxI = Mathf.Max(temp, press, elec, gas);
            c.a = maxI < Threshold ? 0f : maxI * MaxAlpha;

            c.r = Mathf.Clamp01(c.r);
            c.g = Mathf.Clamp01(c.g);
            c.b = Mathf.Clamp01(c.b);
            return c;
        }
    }
}
