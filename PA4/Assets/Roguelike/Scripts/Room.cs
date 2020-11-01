using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public Vector2Int originBoardPosition;

    [TextArea]
    public string strRoom;
    
    public List<Tile> tiles = new List<Tile>();
    public List<Door> doors = new List<Door>();
    public int depth = 0;

    public bool victoryRoom = false;

    public Tile GetTileAt(Vector2Int localPositionInRoom)
    {
        foreach (Tile tile in tiles)
        {
            if (tile.roomPosition == localPositionInRoom)
            {
                return tile;
            }
        }
        return null;
    }

    // Implementation of Dijkstra's Algorithm
    public List<Tile> FindPath(Tile start, Tile end)
    {
        MinPriorityQueue<Tile, int> queue = new MinPriorityQueue<Tile, int>();
        // from prev tile to cur tile dictionary
        Dictionary<Tile, Tile> prev = new Dictionary<Tile, Tile>();

        foreach(Tile t in tiles)
        {
            if (!t.IsWalkable && t != start)
                continue;

            queue.Add(t, int.MaxValue);
            prev.Add(t, null);
        }

        queue.Add(start, 0);

        while (queue.Count > 0)
        {
            int uDist;
            Tile u = queue.Pop(out uDist);

            foreach(Tile v in u.neighborTiles)
            {
                if (!queue.Contains(v))
                    continue;

                int vDist = uDist + 1;
                if (vDist < queue.GetValue(v))
                {
                    queue.Add(v, vDist);
                    prev[v] = u;
                }
            }
        }

        Tile cur = prev[end];
        List<Tile> path = new List<Tile>();
        while(cur != null)
        {
            path.Insert(0, cur);
            cur = prev[cur];
        }

        return path;
    }
}
