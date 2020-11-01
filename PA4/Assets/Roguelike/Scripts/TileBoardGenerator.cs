using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileBoardGenerator : MonoBehaviour
{
    public int seed = 0;
    public int victoryDepth = 2;
    public int maxTries = 100;
    public Text seedText;

    public List<TextAsset> roomAssets;

    [System.Serializable]
    public struct TileData
    {
        public char tileType;
        public Tile prefab;
    }
    public List<TileData> tileDatas;

    public TileBoard board;

    private void Update()
    {
        seedText.text = "Seed Value: " + seed;
    }

    public TileBoard Generate()
    {
        if (seed == 0)
            seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        // create new tile board
        GameObject tileObj = new GameObject("Tile Board");
        board = tileObj.AddComponent<TileBoard>();

        // create a starting room
        int roomIndex = Random.Range(0, roomAssets.Count);
        string strRoom = roomAssets[roomIndex].text;
        Room startRoom = CreateRoomFromString(strRoom, Vector2Int.zero);

        StoreNeighboringTiles(startRoom);

        // for each unconnected door in the start room, recursively create a new room
        GenerateConnectedRooms(startRoom, 0);
        RemoveUnconnectedDoors();

        return board;
    }

    private Room CreateRoomFromString(string strRoom, Vector2Int roomOriginBoardPos)
    {
        // create a new room
        GameObject roomObj = new GameObject($"Room {board.rooms.Count}");
        roomObj.transform.SetParent(board.transform);
        roomObj.transform.localPosition = new Vector3(roomOriginBoardPos.x, 0, roomOriginBoardPos.y);        // could be taken out

        Room room = roomObj.AddComponent<Room>();
        room.strRoom = strRoom;
        room.originBoardPosition = roomOriginBoardPos;          // added in -- seems right to be used here
        board.rooms.Add(room);

        // iterate through each character in room string and spawn a tile matching that type
        Vector2Int roomPos = Vector2Int.zero;
        for (int i = 0; i < strRoom.Length; i++)
        {
            char tileType = strRoom[i];

            if (tileType == '\n') {
                roomPos.x = 0;
                roomPos.y++;
                continue;
            }

            if (tileType == 'x')
            {
                roomPos.x++;
                continue;
            }

            SpawnTile(tileType, room, roomPos);

            roomPos.x++;
        }

        return room;
    }

    private float GetTileRotation(char tileType)
    {
        float rot = 0;
        switch(tileType)
        {
            case 'N':
            case 'S':
                rot = 90;
                break;

        }
        return rot;
    }

    private Tile SpawnTile(char tileType, Room room, Vector2Int roomPos)
    {
        TileData tileData = tileDatas.Find(td => td.tileType == tileType);
        if (tileData.prefab == null)
            return null;

        Tile tile = Instantiate(tileData.prefab, room.transform);
        tile.transform.localPosition = new Vector3(roomPos.x, 0, roomPos.y);
        float yRotation = GetTileRotation(tileType);
        tile.transform.rotation = Quaternion.Euler(0, yRotation, 0);

        tile.tileType = tileType;
        tile.room = room;
        room.tiles.Add(tile);

        tile.roomPosition = roomPos;        // added in -- seems right to be used here

        Door door = tile.GetComponent<Door>();
        if (door != null)
        {
            room.doors.Add(door);
            door.tile = tile;
        }

        return tile;
    }

    private bool DoDoorsMatch(Door a, Door b)
    {
        return (a.tile.tileType == 'W' && b.tile.tileType == 'E') ||
                (a.tile.tileType == 'E' && b.tile.tileType == 'W') ||
                (a.tile.tileType == 'N' && b.tile.tileType == 'S') ||
                (a.tile.tileType == 'S' && b.tile.tileType == 'N');
    }

    private char GetMatchingDoor(Door a)
    {
        switch(a.tile.tileType)
        {
            case 'W': return 'E';
            case 'E': return 'W';
            case 'N': return 'S';
            case 'S': return 'N';
        }
        throw new System.InvalidOperationException();
    }

    // returns the room position for the given tile type
    private Vector2Int? FindTileCoordinate(string strRoom, char tileType)
    {
        Vector2Int curPos = Vector2Int.zero;
        for(int i = 0; i < strRoom.Length; i++)
        {
            char curTileType = strRoom[i];

            if (curTileType == tileType)
            {
                return curPos;
            }

            if (curTileType == '\n')
            {
                curPos.x = 0;
                curPos.y++;
                continue;
            }

            curPos.x++;
        }

        return null;
    }

    private void GenerateConnectedRooms(Room room, int depth)
    {
        for (int i = 0; i < room.doors.Count; i++)
        {
            // if a room's door is unconnected
            if (room.doors[i].connectedDoor == null)
            {
                Room newRoom = TryCreateConnectedRoom(room.doors[i]);
                if (newRoom != null)
                {
                    // assign room it's depth
                    newRoom.depth = depth + 1;
                    
                    if (newRoom.depth == victoryDepth)      // this should be newRoom.depth == victoryDepth but the logic needs to be fixed here
                    {
                        GenerateVictoryTile(newRoom);
                    }

                    StoreNeighboringTiles(newRoom);
                    GenerateUpgradeTile(newRoom);
                }

                if (newRoom && depth <= victoryDepth - 2)
                {
                    GenerateConnectedRooms(newRoom, depth + 1);
                }
            }
        }
    }

    private bool IsDoor(char tileType)
    {
        switch (tileType)
        {
            case 'N': return true;
            case 'S': return true;
            case 'W': return true;
            case 'E': return true;
        }
        return false;
    }

    private bool IsRoomSpawnPositionOk(string strRoom, Vector2Int originBoardPosition)
    {
        //  calculate tile board position (global position)
        //  look up any existing tile at that position
        //  check if existing tile can overlap with this tile

        // type1 == type2: OK                  <-- Both tiles are either floor tiles or wall tiles
        // type1 != type2: NOT OK              <-- Some exceptions to this are for e.g. the case below of two diff types of door tiles but since they are matching door tiles its OK
        // type1 == matchingDoor(type2): OK    <-- Both tiles are doors and they are the matching type of doors
        // if both doors but don't match: NOT OK
        // if type1 is Floor tile and type2 is not floor tile: NOT OK

        Vector2Int curTileRoomPos = Vector2Int.zero;

        // for each tile in strRoom
        for (int i = 0; i < strRoom.Length; i++)
        {
            char curTileType = strRoom[i];

            if (curTileType ==  '\n')
            {
                curTileRoomPos.x = 0;
                curTileRoomPos.y++;
                continue;
            }

            Vector2Int curTileBoardPos = curTileRoomPos + originBoardPosition;    // gets the tile's position on tile board (global position)

            // check if a tile already exists on curTileBoardPos
            //  if YES, check if existing tile CAN or CANNOT overlap with curTileType

            for (int j = 0; j < board.rooms.Count; j++)
            {
                Room curRoom = board.rooms[j];
                Tile existingTile = curRoom.tiles.Find(dt => dt.TileBoardPosition == curTileBoardPos);

                // A tile already exists in the same tile board position as the curTileBoardPos
                if (existingTile != null)
                {
                    //  Both tile types are different
                    if (existingTile.tileType != curTileType)
                    {
                        Door door = existingTile.GetComponent<Door>();
                        bool existingTileIsDoor = door != null;
                        bool curTileIsDoor = IsDoor(curTileType);

                        // Neither tile is door and both tile types are different : NOT OK -- Case A
                        if (!curTileIsDoor && !existingTileIsDoor)
                            return false;

                        // If ONLY ONE of the two tiles is of door type : NOT OK -- Case D
                        if ((curTileIsDoor && !existingTileIsDoor) || (!curTileIsDoor && existingTileIsDoor))
                            return false;

                        // if both tiles are a door
                        if (curTileIsDoor && existingTileIsDoor)
                        {
                            // if both tiles are doors but not of matching type : NOT OK -- Case E
                            if (GetMatchingDoor(door) != curTileType)
                                return false;
                            else
                            {
                                // if both tiles are doors and of matching type BUT the existing door tile is already connected to some other door : NOT OK -- Case F
                                if (door.connectedDoor != null)
                                    return false;
                            }
                            // if we get here then both tiles are doors AND of matching type AND the existing door tile is unconnected : OK -- Case G
                        }
                    }
                    // Both tile types are the same
                    else
                    {
                        Door door = existingTile.GetComponent<Door>();
                        // if both tiles are a door since they are of the same tile type they cannot be matching doors : NOT OK -- Case C
                        if (door)
                        {
                            return false;
                        }
                        // if we get here then both tiles are not doors but still of similar type (wall tiles, floor tiles) : OK -- Case B
                    }
                }
                // if we get here then there is no existing tile in place of curTileBoardPos : OK -- Case H
            }

            curTileRoomPos.x++;
            
        }

        return true;
    }

    private Room TryCreateConnectedRoom(Door door)
    {
        int tries = 0;
        while (tries <= maxTries)
        {
            tries++;

            // find what type of door tile the matching door needs to be
            char matchingDoorType = GetMatchingDoor(door);
            // select a room at random and iterate through it to find matching door and return that matching door's room position
            string strRoom = roomAssets[Random.Range(0, roomAssets.Count)].text;
            // local room position of matching door within that room (if a room with matching door is found)
            Vector2Int? matchingDoorRoomPos = FindTileCoordinate(strRoom, matchingDoorType);

            if (!matchingDoorRoomPos.HasValue)  // constraint 1: room must have a matching door
                continue;

            // constraint 2: rooms must line up on the doors without overlapping
            // door A.TileBoardPosition == B.TileBoardPosition
            // room B originBoardPosition
            // tbp = rp + robp
            Vector2Int roomBOriginBoardPosition = door.tile.TileBoardPosition - matchingDoorRoomPos.Value;      // board position of room B to overlap matching door with current door

            if (IsRoomSpawnPositionOk(strRoom, roomBOriginBoardPosition))
            {
                Room newRoom = CreateRoomFromString(strRoom, roomBOriginBoardPosition);
                // Connect the two matching doors
                Door newConnectedDoor = newRoom.doors.Find(dt => dt.tile.tileType == matchingDoorType);
                if (newConnectedDoor != null)
                {
                    door.connectedDoor = newConnectedDoor;
                    newConnectedDoor.connectedDoor = door;
                }
                return newRoom;
            }
        }
        return null;
    }

    private void RemoveUnconnectedDoors()
    {
        int doorCount = 0;
        // Go through the entire tile board, find any rooms that have doors that aren't connected to anything and replace them with walls
        for (int i = 0; i < board.rooms.Count; i++)
        {
            Room curRoom = board.rooms[i];
            if (curRoom.gameObject.name == "Room 14")
            {
                bool bp = true;
            }
            doorCount = 0;
            for (int j = 0; j < curRoom.doors.Count; j++)
            {
                Door curDoor = curRoom.doors[j];
                if (curDoor.connectedDoor == null)
                {
                    //Destroy(curDoor.tile.gameObject);
                    ReplaceTile(curDoor.tile, '|', curRoom);
                    //SpawnTile('|', curRoom, curDoor.tile.roomPosition);
                }
                if (curRoom.GetTileAt(curDoor.tile.roomPosition).IsDoorTile)
                    doorCount++;
            }
            // Generates victory tiles in rooms that could not be expanded to the victory depth due to surpassing maxTries to spawn a new room 
            if (doorCount == 1 && curRoom.depth < victoryDepth && !curRoom.victoryRoom)
                GenerateVictoryTile(curRoom);
        }
    }

    public void ReplaceTile(Tile oldTile, char newTileType, Room room)
    {
        int tileIndex = room.tiles.IndexOf(oldTile);
        room.tiles.RemoveAt(tileIndex);
        Destroy(oldTile.gameObject);

        TileData tileData = tileDatas.Find(td => td.tileType == newTileType);

        Tile tile = Instantiate(tileData.prefab, room.transform);
        tile.transform.localPosition = new Vector3(oldTile.roomPosition.x, 0, oldTile.roomPosition.y);
        float yRotation = GetTileRotation(newTileType);
        tile.transform.rotation = Quaternion.Euler(0, yRotation, 0);

        tile.tileType = newTileType;
        tile.room = room;
        room.tiles.Insert(tileIndex, tile);

        tile.roomPosition = oldTile.roomPosition;
    }

    private void GenerateVictoryTile(Room curRoom)
    {
        Tile randomTile = curRoom.tiles[Random.Range(0, curRoom.tiles.Count)];

        while (randomTile.tileType != ' ' || randomTile.IsUpgradeTile)
        {
            randomTile = curRoom.tiles[Random.Range(0, curRoom.tiles.Count)];
        }

        ReplaceTile(randomTile, 'V', curRoom);

        curRoom.victoryRoom = true;
    }

    private void StoreNeighboringTiles(Room room)
    {
        for (int i = 0; i < room.tiles.Count; i++)
        {
            Tile curTile = room.tiles[i];
            for (int j = 0; j < room.tiles.Count; j++)
            {
                // if the tile checked against the curTile is adjacent to it while not being the same as it, add it to the curTile's neighbors list
                if (curTile != room.tiles[j] && curTile.IsAdjacentTo(room.tiles[j]))
                {
                    curTile.neighborTiles.Add(room.tiles[j]);
                }
            }
        }
    }

    private void GenerateUpgradeTile(Room curRoom)
    {
        Tile randomTile = curRoom.tiles[Random.Range(0, curRoom.tiles.Count)];

        while (randomTile.tileType != ' ' || randomTile.IsVictoryTile)
        {
            randomTile = curRoom.tiles[Random.Range(0, curRoom.tiles.Count)];
        }

        ReplaceTile(randomTile, 'U', curRoom);
    }
}
