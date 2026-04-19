// Assets/PhysicsSystem/Renderer/OverlayMode.cs
namespace PhysicsSystem.Renderer
{
    /// <summary>
    /// Capas de visualización del PropertyOverlayRenderer.
    /// Teclas 0-9 en runtime para cambiar de capa.
    /// </summary>
    public enum OverlayMode
    {
        None            = 0,  // overlay apagado
        Temperature     = 1,
        Pressure        = 2,
        Humidity        = 3,
        ElectricEnergy  = 4,
        GasDensity      = 5,
        StructuralDamage = 6, // integridad invertida — muestra daño, no salud
        DerivedStates   = 7,  // flags activos como colores
        Activity        = 8,  // debug: tiles en ActiveTiles
        Combined        = 9,  // mezcla aditiva — comportamiento original
    }
}
