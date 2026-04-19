// Assets/PhysicsSystem/Renderer/OverlayMode.cs
namespace PhysicsSystem.Renderer
{
    /// <summary>
    /// Capas de visualización del PropertyOverlayRenderer.
    /// Teclas 0-9 en runtime para cambiar de capa.
    /// </summary>
    public enum OverlayMode
    {
        None             = 0,  // overlay apagado
        Temperature     = 1,
        GasMaterial      = 2,  // color por tipo de gas, opacidad por densidad
        LiquidMaterial   = 3,  // color por tipo de líquido, opacidad por volumen
        Pressure         = 4,  // gasDensity como presión
        ElectricEnergy   = 5,
        Structural       = 6,  // integridad estructural (daño si < 60%)
        DerivedStates    = 7,  // flags activos como colores
        Activity         = 8,  // debug: tiles en ActiveTiles
        Combined        = 9,  // mezcla aditiva
    }
}
