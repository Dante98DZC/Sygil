using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDebugger2 : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileBase _testTile; // arrastra cualquier tile aquí

    private void Start()
    {
        StartCoroutine(Check());
    }
    private System.Collections.IEnumerator Check()
{
    yield return null;
    yield return null;
    
    // ¿Qué tile hay en (0,0) después del renderer?
    var tile = _tilemap.GetTile(new Vector3Int(0, 0, 0));
    Debug.Log($"Tile en (0,0) post-renderer: {tile}");
    Debug.Log($"Tile nombre: {(tile != null ? tile.name : "NULL")}");
    
    // Fuerza un tile visible encima para confirmar
    _tilemap.SetTile(new Vector3Int(0, 0, 0), _testTile);
}
}