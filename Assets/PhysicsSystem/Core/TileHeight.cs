namespace PhysicsSystem.Core
{
    /// <summary>
    /// Elevación discreta de un tile. Define geometría vertical, no física.
    /// Paso y visión son propiedades de MaterialDefinition, no de TileHeight.
    /// </summary>
    public enum TileHeight
    {
        Deep   = -2,  // foso profundo, abismo
        Shallow = -1, // charco, barro, depresión leve
        Ground =  0,  // suelo base transitable
        Low    =  1,  // muro bajo, roca, arbusto
        Wall   =  2,  // pared estándar
        Tall   =  3   // columna, árbol alto, torre
    }
}