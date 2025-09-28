using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FoodSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap obstacleTilemap;
    public GameObject foodPrefab;
    public Transform foodParent;
    public HeadMovement snakeHead; // dodaj referencjê do skryptu od wê¿a

    private List<Vector3Int> groundCells = new List<Vector3Int>();

    void Start()
    {
        var bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (groundTilemap.HasTile(cell))
                    groundCells.Add(cell);
            }
        }

        SpawnFood();
    }

    public void SpawnFood()
    {
        List<Vector3Int> freeCells = new List<Vector3Int>();

        foreach (var cell in groundCells)
        {
            if (obstacleTilemap != null && obstacleTilemap.HasTile(cell))
                continue;

            bool occupied = false;
            if (snakeHead != null)
            {
                
                Vector3Int headCell = groundTilemap.WorldToCell(snakeHead.transform.position);
                if (headCell == cell) { occupied = true; }

                
                foreach (Transform seg in snakeHead.bodySegments)
                {
                    Vector3Int segCell = groundTilemap.WorldToCell(seg.position);
                    if (segCell == cell) { occupied = true; break; }
                }
            }

            if (!occupied)
                freeCells.Add(cell);
        }


        if (freeCells.Count == 0)
        {
            Debug.LogWarning("Brak wolnych pól na owoc!");
            return;
        }

        Vector3Int chosen = freeCells[Random.Range(0, freeCells.Count)];
        Vector3 pos = groundTilemap.GetCellCenterWorld(chosen);
        Instantiate(foodPrefab, pos, Quaternion.identity, foodParent);
    }
}
