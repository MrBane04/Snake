using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FoodSpawner : MonoBehaviour
{
    public Tilemap groundTilemap;      // tilemapa z polami, na których mog¹ pojawiæ siê owoce
    public Tilemap obstacleTilemap;    // tilemapa z przeszkodami (opcjonalnie)
    public GameObject foodPrefab;      // prefab owoca
    public Transform foodParent;       // opcjonalnie - parent dla wszystkich owoców

    private List<Vector3Int> groundCells = new List<Vector3Int>();

    void Start()
    {
        // zbierz wszystkie pola z groundTilemap
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

        // spawn 1 owoca na start
        SpawnFood();
    }

    public void SpawnFood()
    {
        // lista wolnych pól (na razie tylko eliminujemy przeszkody)
        List<Vector3Int> freeCells = new List<Vector3Int>();

        foreach (var cell in groundCells)
        {
            if (obstacleTilemap != null && obstacleTilemap.HasTile(cell))
                continue;

            freeCells.Add(cell);
        }

        if (freeCells.Count == 0)
        {
            Debug.LogWarning("Brak wolnych pól na owoc!");
            return;
        }

        Vector3Int chosen = freeCells[Random.Range(0, freeCells.Count)];
        Vector3 worldPos = groundTilemap.GetCellCenterWorld(chosen);

        Instantiate(foodPrefab, worldPos, Quaternion.identity, foodParent);
    }
}