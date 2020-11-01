using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBoard : MonoBehaviour
{
    public List<Room> rooms = new List<Room>();

    public Tile GetTileAt(Vector2Int pos)
    {
        foreach (Room room in rooms)
        {
            foreach (Tile tile in room.tiles)
                if (tile.TileBoardPosition == pos)
                    return tile;
        }
        return null;
    }

    public Vector3 BoardToWorld(Vector2Int boardPosition)
    {
        Vector3 worldPos = new Vector3(boardPosition.x, 0, boardPosition.y) + transform.position;
        return worldPos;
    }

    public Vector2Int WorldToBoard(Vector3 worldPosition)
    {
        Vector3 adjWorldPos = worldPosition - transform.position;
        Vector2Int boardPosition = new Vector2Int(Mathf.RoundToInt(adjWorldPos.x), Mathf.RoundToInt(adjWorldPos.z));
        return boardPosition;
    }
}
