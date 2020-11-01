using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public char tileType;
    public Vector2Int roomPosition; // local position of tile inside a room
    public Room room;

    public Unit unit;

    public Pickup pickup;

    public List<Tile> neighborTiles;

    public bool upgradeUsed = false;

    // Gives any tiles position in the board (global position)
    public Vector2Int TileBoardPosition
    {
        get
        {
            return roomPosition + room.originBoardPosition;
        }
    }

    public bool IsAdjacentTo(Tile tile)
    {
        if (room != tile.room)
            return false;

        Vector2Int delta = roomPosition - tile.roomPosition;
        int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        // if distance is exactly 1 in x or y direction then tile is adjacent to this tile
        return distance == 1;
    }

    public bool IsCoveredInJump(Tile to, Unit unit)
    {
        if (room != to.room)
            return false;

        Vector2Int delta = roomPosition - to.roomPosition;
        int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        // if distance is exactly 1 in x or y direction then tile is adjacent to this tile
        return distance <= unit.maxJumpRadius && distance >= unit.minJumpRadius && IsVerticallyOrHorizontallyAligned(to);
    }

    public bool IsCoveredInThrow(Tile to, Pickup pickup)
    {
        if (room != to.room)
            return false;

        Vector2Int delta = roomPosition - to.roomPosition;
        int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        // if distance is exactly 1 in x or y direction then tile is adjacent to this tile
        return distance <= pickup.unit.maxThrowRadius && distance >= pickup.unit.minThrowRadius && IsVerticallyOrHorizontallyAligned(to);
    }

    public bool IsVerticallyOrHorizontallyAligned(Tile to)
    {
        if ((to.roomPosition.x != roomPosition.x) && (to.roomPosition.y != roomPosition.y))
            return false;
        else
            return true;
    }

    public bool IsFloorTile => tileType == ' ';
    public bool IsDoorTile => tileType == 'N' || tileType == 'S' || tileType == 'W' || tileType == 'E';
    public bool IsVictoryTile => tileType == 'V';
    public bool IsLavaTile => tileType == 'L';
    public bool IsUpgradeTile => tileType == 'U';
    public bool IsWalkable => (IsFloorTile || IsDoorTile || IsUpgradeTile) && unit == null;
}
