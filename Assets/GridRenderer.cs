using UnityEngine;
using UnityEngine.Tilemaps;

public class GridRenderer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private int gridSize = 50;
    [SerializeField] private Color gridColor = Color.gray;
    [SerializeField] private float lineWidth = 0.05f;

    void OnDrawGizmosSelected()
    {
        if (tilemap == null) return;
        
        Gizmos.color = gridColor;
        
        for (int i = 0; i <= gridSize; i++)
        {
            Vector3 start = new Vector3(i, 0, 0);
            Vector3 end = new Vector3(i, gridSize, 0);
            Gizmos.DrawLine(start, end);
            
            start = new Vector3(0, i, 0);
            end = new Vector3(gridSize, i, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}