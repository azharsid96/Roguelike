using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public GameModel model;
    public UiController uiController;
    public TileBoardGenerator boardGenerator;
    public TileBoard board;

    public Unit player;
    public List<Unit> enemies = new List<Unit>();
    public List<Unit> curRoomEnemies = new List<Unit>();

    public Pickup spear;

    public Camera cam;

    public Room curRoom;

    public Material tileHighlightMaterial;
    public Tile highlightedTile = null;
    public Material highlightedTilePrevMaterial;

    public Text depthText;
    public Text winText;
    public Text loseText;
    public Text healthText;
    public Text manaText;

    public Image jumpImage;
    public Image spearThrowImage;

    public GameObject upgradesPanel;

    public Text optionOne;
    public Text optionTwo;
    public Text optionThree;

    public Dictionary<int, string> upgradeOptions = new Dictionary<int, string>();

    private bool isJumping = false;
    private bool isThrowing = false;
    private bool gameOverState = false;
    private bool moved = true;

    private Color actionImagePrevColor;

    void Start()
    {
        model.OnUnitTurnStart += uiController.OnUnitTurnStart;
        model.RunGameLoop();

        if (cam == null)
            cam = Camera.main;

        board = boardGenerator.Generate();
        Room startRoom = board.rooms[0];
        MoveToRoom(startRoom);

        winText.gameObject.SetActive(false);
        loseText.gameObject.SetActive(false);

        upgradesPanel.SetActive(false);

        upgradeOptions.Add(0, "Restore HP to full");
        upgradeOptions.Add(1, "Increase Max HP");
        upgradeOptions.Add(2, "Increase Max Mana");
        upgradeOptions.Add(3, "Increase Max Jump Radius");
    }

    private void Update()
    {
        if (model.units.Equals(curRoomEnemies))
        ProcessPlayerInput();

        depthText.text = "Current Depth: " + curRoom.depth;
        healthText.text = "Health: " + player.health;
        manaText.text = "Mana: " + player.mana;

        CheckCheatCodes();

        if (spear.isActive && spear.inPlayerInventory)
        {
            CheckSpearMovementAction();
        }

        if (moved)
            CheckEnemyOnLavaTile();

        //for (int i = 0; i < curRoomEnemies.Count; i++)
        ////foreach (Unit enemy in curRoomEnemies)
        //{
        //    Unit enemy = curRoomEnemies[i];
        //    if (enemy.tile.IsLavaTile)
        //    {
        //        StartCoroutine(DoDamage(enemy, enemy.health));
        //        curRoomEnemies.Remove(enemy);
        //        break;
        //    }      
        //}

        //foreach(Unit enemy in curRoomEnemies)
        //{
        //    if (enemy == null)
        //    {
        //        bool edge = true;
        //    }
        //}

    }

    #region Camera

    void FocusCameraOnRoom(Room room)
    {
        Vector2Int min = room.tiles[0].TileBoardPosition;
        Vector2Int max = min;

        foreach(Tile tile in room.tiles)
        {
            Vector2Int tbp = tile.TileBoardPosition;
            if (tbp.x < min.x)
                min.x = tbp.x;
            else if (tbp.x > max.x)
                max.x = tbp.x;

            if (tbp.y < min.y)
                min.y = tbp.y;
            else if (tbp.y > max.y)
                max.y = tbp.y;
        }
        Vector2Int center = (max - min) / 2 + min;

        float cameraHeight = 0;
        if (room.tiles.Count >= 100)
            cameraHeight = 12f;
        else
            cameraHeight = 10f;
        Vector3 camPos = board.BoardToWorld(center) + Vector3.up * cameraHeight;

        moved = false;
        StartCoroutine(MoveTo(cam.transform, camPos, 0.5f));
        
    }

    #endregion

    #region Movement
    void ProcessPlayerInput()
    {
        // Player will not be able to move if game is over
        if (!gameOverState)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float dist;
            if (plane.Raycast(ray, out dist))
            {
                Vector3 worldPos = ray.GetPoint(dist);
                Vector2Int boardPos = board.WorldToBoard(worldPos);
                Tile tile = board.GetTileAt(boardPos);
                HighlightTile(tile);

                if (Input.GetMouseButtonDown(0) && CanMoveTo(player, tile) && !isThrowing)
                {
                    MoveUnitToTile(player, tile);
                }
                // movement for spear only
                else if (Input.GetMouseButtonDown(0) && CanThrowTo(spear, tile) && isThrowing)
                {
                    MovePickupToTile(spear, tile);
                }
            }

            // Collect spear pickup
            if (player.prevTile != null && player.prevTile == spear.tile && !spear.inPlayerInventory)
            {
                spear.inPlayerInventory = true;
                
                if (spear.tile.pickup != null)
                    spear.tile.pickup = null;

                spear.tile = null;
            }

            //PerformWarriorTurn();
        }
    }

    bool CanThrowTo(Pickup pickup, Tile tile)
    {
        if (tile == null || !(tile.IsFloorTile || tile.IsDoorTile || tile.IsVictoryTile || tile.IsUpgradeTile))
            return false;

        if (isThrowing && !pickup.unit.tile.IsCoveredInThrow(tile, pickup))
            return false;

        return true;
    }

    bool CanMoveTo(Unit unit, Tile tile)
    {
        if (tile == null || !(tile.IsFloorTile || tile.IsDoorTile || tile.IsVictoryTile || tile.IsUpgradeTile))
            return false;

        if (tile.unit != null)
            return false;

        // if target tile is not adjacent then unit cannot move there
        if (unit.tile != null && !unit.tile.IsAdjacentTo(tile) && !isJumping)
            return false;

        if (isJumping && unit.tile != null && !unit.tile.IsCoveredInJump(tile, unit))
            return false;

        return true;
    }

    void MoveToRoom(Room room, Door entryDoor = null)
    {
        curRoomEnemies.Clear();

        // Move the player into the start tile
        Tile startTile = entryDoor != null ? entryDoor.tile : room.tiles.Find(t => t.IsFloorTile);

        MoveUnitToTile(player, startTile, false);
        FocusCameraOnRoom(room);
        curRoom = room;

        // Regenerate player mana on entering room
        player.mana = player.maxMana;

        // Spawn units
        int numEnemiesToSpawn = curRoom.depth + 1;
        SpawnEnemyUnits(numEnemiesToSpawn, curRoom);

    }

    void MovePickupToTile(Pickup pickup, Tile target)
    {
        if (isThrowing) {
            isThrowing = false;
            spearThrowImage.color = actionImagePrevColor;
        }

        pickup.tile = target;
        target.pickup = pickup;     // stores a reference to the pickup at this target tile (helps to collect it later)

        moved = false;
        StartCoroutine(MoveTo(pickup.transform, target.transform.position, 0.25f));
        

        pickup.inPlayerInventory = false;

        // if spear landed on a tile occupied by an enemy unit then do damage to that enemy unit
        if (target.unit != null)
        {
            StartCoroutine(DoDamage(target.unit, 1));
            curRoomEnemies.Remove(target.unit);
        }
    }

    void MoveUnitToTile(Unit unit, Tile target, bool entering = true)
    {
        // check if jump action is being used by player
        if (isJumping)
        {
            player.mana -= 2;
            isJumping = false;
            jumpImage.color = actionImagePrevColor;
        }

        // Makes sure spear is not linked to any tile while player has the spear in it's inventory
        if (spear.tile != null && spear.inPlayerInventory)
            spear.tile = null;

        Tile from = null;
        if (unit.tile != null)
        {
            from = unit.tile;
            unit.tile.unit = null;
        }

        unit.tile = target;
        target.unit = unit;
        unit.prevTile = from;       // stores reference to the prev tile

        moved = false;
        StartCoroutine(MoveTo(unit.transform, target.transform.position, 0.25f));
        

        if (target.IsUpgradeTile && unit == player && !target.upgradeUsed)
        {
            GenerateUpgradeTileList();
            upgradesPanel.SetActive(true);
            target.upgradeUsed = true;
        }

        // If player moves into victory tile, player WINS
        if (target.IsVictoryTile && unit == player)
        {
            winText.gameObject.SetActive(true);
            gameOverState = true;
        }

        if (entering)
            OnUnitEnteredTile(unit, target, from);
    }

    void OnUnitEnteredTile(Unit unit, Tile to, Tile from)
    {
        if (to.IsDoorTile && unit == player)
        {
            Door door = to.GetComponent<Door>();
            Room nextRoom = door.connectedDoor.tile.room;
            MoveToRoom(nextRoom, door.connectedDoor);
        }
    }

    #endregion

    void HighlightTile(Tile tile)
    {
        // no need to highlight same tile again
        if (tile == highlightedTile)
            return;

        // restore the old highlighted tile to unhighlighted by changing it's material to unhighlighted tile material
        if (highlightedTile != null)
        {
            Renderer renderer = highlightedTile.GetComponentInChildren<Renderer>();
            renderer.material = highlightedTilePrevMaterial;
        }

        if (tile == null)
        {
            highlightedTile = tile;
            return;
        }
        
        if (!tile.IsFloorTile)
            return;

        // update highlighted tile to new tile
        highlightedTile = tile;

        // update matrial of new highlight tile by changing it to highlighted tile material

        Renderer rndr = highlightedTile.GetComponentInChildren<Renderer>();
        highlightedTilePrevMaterial = rndr.material;
        rndr.material = tileHighlightMaterial;

    }

    void SpawnEnemyUnits(int numEnemiesToSpawn, Room curRoom)
    {
        while (curRoomEnemies.Count != numEnemiesToSpawn)
        {
            // Spawn a random enemy in the current room at a random floor tile
            Tile randomTile = curRoom.tiles[Random.Range(0, curRoom.tiles.Count)];

            while (!randomTile.IsFloorTile || randomTile.unit != null)
            {
                randomTile = curRoom.tiles[Random.Range(0, curRoom.tiles.Count)];
            }

            Unit enemy = Instantiate(enemies[Random.Range(0, enemies.Count)], curRoom.transform);
            enemy.transform.localPosition = new Vector3(randomTile.roomPosition.x, 0, randomTile.roomPosition.y);

            // Make the link between this tile and the enemy to spawn on it
            enemy.tile = randomTile;
            randomTile.unit = enemy;

            curRoomEnemies.Add(enemy);
        }
    }

    #region Upgrade tile implementation

    void GenerateUpgradeTileList()
    {
        int randomKeyOne = Random.Range(0, upgradeOptions.Count);
        optionOne.text = upgradeOptions[randomKeyOne];

        int randomKeyTwo = Random.Range(0, upgradeOptions.Count);
        while (randomKeyTwo == randomKeyOne)
        {
            randomKeyTwo = Random.Range(0, upgradeOptions.Count);
        }
        optionTwo.text = upgradeOptions[randomKeyTwo];

        int randomKeyThree = Random.Range(0, upgradeOptions.Count);
        while (randomKeyThree == randomKeyOne || randomKeyThree == randomKeyTwo)
        {
            randomKeyThree = Random.Range(0, upgradeOptions.Count);
        }
        optionThree.text = upgradeOptions[randomKeyThree];

    }

    public void ProcessSelectedUpgrade()
    {
        string optionName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Text>().text;
        Debug.Log(optionName);

        if (upgradeOptions.ContainsValue(optionName))
        {
            for (int i = 0; i < upgradeOptions.Count; i++)
            {
                if (upgradeOptions[i] == optionName)
                {
                    if (i == 0)
                        player.health = player.maxHealth;
                    else if (i == 1)
                        player.maxHealth += 1;
                    else if (i == 2)
                        player.maxMana += 1;
                    else if (i == 3)
                        player.maxJumpRadius += 1;
                }
            }
        }
        upgradesPanel.SetActive(false);
    }

    #endregion

    #region Player Actions

    public void JumpAction()
    {
        if (player.mana >= 2)
        {
            isJumping = true;
            actionImagePrevColor = jumpImage.color;
            Color tempColor = jumpImage.color;
            tempColor.a = 1f;
            jumpImage.color = tempColor;
        }
    }

    public void AttackAction()
    {
        foreach(Unit enemy in curRoomEnemies)
        {
            // Check if there is an enemy adjacent to the player
            if (player.tile.IsAdjacentTo(enemy.tile))
            {
                //if player's movemetn is not perpendicular to attack or player just moved into a new room to attack an enemy unit then allow it to
                if (player.prevTile.IsVerticallyOrHorizontallyAligned(enemy.tile) || player.prevTile == null || player.prevTile.room != curRoom)
                {
                    StartCoroutine(DoDamage(enemy, 1));
                    curRoomEnemies.Remove(enemy);
                    break;
                }
            }
        }
    }

    public void CheckSpearMovementAction()
    {
        foreach (Unit enemy in curRoomEnemies)
        {
            // Check if there is an enemy adjacent to the player
            if (player.tile.IsAdjacentTo(enemy.tile))
            {
                //if player's movement is not perpendicular to relative enemy position or player just moved into a new room to attack an enemy unit then allow it to
                if (player.prevTile.IsVerticallyOrHorizontallyAligned(enemy.tile) || player.prevTile == null || player.prevTile.room != curRoom)
                {
                    StartCoroutine(DoDamage(enemy, 1));
                    curRoomEnemies.Remove(enemy);
                    break;
                }
            }
        }
    }

    public void PushAction()
    {
        if (player.mana >= 3)
        {
            for (int i = 0; i < curRoomEnemies.Count; i++)
            //foreach (Unit enemy in curRoomEnemies)
            {
                int enemyCount = curRoomEnemies.Count;
                Unit enemy = curRoomEnemies[i];
                // Check if there is an enemy adjacent to the player
                if (player.tile.IsAdjacentTo(enemy.tile))
                {
                    // Player and enemy unit are in same horizontal (x) axis 
                    if (player.tile.roomPosition.x == enemy.tile.roomPosition.x)
                    {
                        Vector2Int playerPos = player.tile.roomPosition;
                        Vector2Int enemyPos = enemy.tile.roomPosition;

                        // Compare their y values to find out what direction enemy needs to be pushed in
                        if (playerPos.y < enemyPos.y)
                        {
                            // Push up

                            // this is the position target needs to be pushed to
                            Vector2Int targetUnitPosition = new Vector2Int(enemy.tile.roomPosition.x, enemy.tile.roomPosition.y + 1);

                            PushUnitVertically(enemy, targetUnitPosition, 1);
                            
                        }
                        else
                        {
                            // Push down

                            Vector2Int targetUnitPosition = new Vector2Int(enemy.tile.roomPosition.x, enemy.tile.roomPosition.y - 1);

                            PushUnitVertically(enemy, targetUnitPosition, -1);

                        }
                        if (curRoomEnemies.Count < enemyCount)
                            break;
                    }
                    // Player and enemy unit are in same vertical (y) axis 
                    else
                    {
                        Vector2Int playerPos = player.tile.roomPosition;
                        Vector2Int enemyPos = enemy.tile.roomPosition;

                        // Compare their x values to find out what direction enemy needs to be pushed in
                        if (playerPos.x < enemyPos.x)
                        {
                            // Push right

                            Vector2Int targetUnitPosition = new Vector2Int(enemy.tile.roomPosition.x + 1, enemy.tile.roomPosition.y);

                            PushUnitHorizontally(enemy, targetUnitPosition, 1);
                        }
                        else
                        {
                            // Push left

                            Vector2Int targetUnitPosition = new Vector2Int(enemy.tile.roomPosition.x - 1, enemy.tile.roomPosition.y);

                            PushUnitHorizontally(enemy, targetUnitPosition, -1);
                        }
                        if (curRoomEnemies.Count < enemyCount)
                            break;
                    }
                }
            }

            player.mana -= 3;
        }
    }

    void PushUnitVertically(Unit unit, Vector2Int targetUnitPosition, int verticalChange = 0)
    {
        // tile at the target position that unit needs to be pushed to
        Tile targetTile = curRoom.GetTileAt(targetUnitPosition);

        if (targetTile.IsLavaTile || targetTile.IsFloorTile || targetTile.IsVictoryTile || targetTile.IsUpgradeTile)
        {
            // There exists an another unit in the tile where this unit is supposed to be pushed into
            if ((targetTile.IsFloorTile || targetTile.IsVictoryTile || targetTile.IsUpgradeTile) && targetTile.unit != null)
            {
                // push this new unit that's already in place of the unit we just pushed into this tile
                Vector2Int newTargetUnitPosition = new Vector2Int(targetUnitPosition.x, targetUnitPosition.y + verticalChange);
                // recursive call to push up
                PushUnitVertically(targetTile.unit, newTargetUnitPosition, verticalChange);
            }
            // enemy unit will either be pushed into a lava tile, an empty victory tile or an empty floor tile
            MoveUnitToTile(unit, targetTile);
        }
        // target tile can be a door tile or a wall tile 
        else
        {
            // enemy unit will bounce off against the wall and take 1 damage staying in their exact position
            StartCoroutine(DoDamage(unit, 1));
            curRoomEnemies.Remove(unit);
        }
    }

    void PushUnitHorizontally(Unit unit, Vector2Int targetUnitPosition, int horizontalChange = 0)
    {
        // tile at the target position that unit needs to be pushed to
        Tile targetTile = curRoom.GetTileAt(targetUnitPosition);

        if (targetTile.IsLavaTile || targetTile.IsFloorTile || targetTile.IsVictoryTile || targetTile.IsUpgradeTile)
        {
            // There exists an another unit in the tile where this unit is supposed to be pushed into
            if ((targetTile.IsFloorTile || targetTile.IsVictoryTile || targetTile.IsUpgradeTile) && targetTile.unit != null)
            {
                // push this new unit that's already in place of the unit we just pushed into this tile
                Vector2Int newTargetUnitPosition = new Vector2Int(targetUnitPosition.x + horizontalChange, targetUnitPosition.y);
                // recursive call to push up
                PushUnitHorizontally(targetTile.unit, newTargetUnitPosition, horizontalChange);
            }
            // enemy unit will either be pushed into a lava tile, an empty victory tile or an empty floor tile
            MoveUnitToTile(unit, targetTile);
        }
        // target tile can be a door tile or a wall tile 
        else
        {
            // enemy unit will bounce off against the wall and take 1 damage staying in their exact position
            StartCoroutine(DoDamage(unit, 1));
            curRoomEnemies.Remove(unit);
        }
    }

    public void SpearThrowAction()
    {
        if(spear.inPlayerInventory)
        {
            if(!spear.isActive)
            {
                spear.gameObject.SetActive(true);
                spear.isActive = true;
            }

            isThrowing = true;

            actionImagePrevColor = spearThrowImage.color;
            Color tempColor = spearThrowImage.color;
            tempColor.a = 1f;
            spearThrowImage.color = tempColor;
        }
    }

    public void EquipSpearAction()
    {
        if (spear.inPlayerInventory)
        {
            if (spear.isActive)
            {
                spear.isActive = false;
                spear.gameObject.SetActive(false);
            }  
            else
            {
                spear.isActive = true;
                spear.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    public void PerformWarriorTurn()
    {
        foreach(Unit enemy in curRoomEnemies)
        {
            if (enemy.gameObject.name.Contains("Warrior"))
            {
                Unit warrior = enemy;
                if (warrior.tile.IsAdjacentTo(player.tile))
                {
                    StartCoroutine(DoDamage(player, 1));
                }
                else
                {
                    List<Tile> path = curRoom.FindPath(warrior.tile, player.tile);
                    if (path.Count > 0)
                    {
                        MoveUnitToTile(warrior, path[0]);
                    }
                }
            }
        }
    }

    public void CheckEnemyOnLavaTile()
    {
        for (int i = 0; i < curRoomEnemies.Count; i++)
        {
            Unit enemy = curRoomEnemies[i];
            if (enemy.tile.IsLavaTile)
            {
                StartCoroutine(DoDamage(enemy, enemy.health));
                curRoomEnemies.Remove(enemy);
                break;
            }
        }
    }

    #region Cheat codes
    void CheckCheatCodes()
    {
        // Restores health and mana to max
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            player.health = player.maxHealth;
            player.mana = player.maxMana;
        }

        // Kills all enemies in current room
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (curRoom != null)
            {
                if (curRoomEnemies.Count > 0)
                {
                    foreach (Unit enemy in curRoomEnemies)
                    {
                        
                        StartCoroutine(DoDamage(enemy, enemy.health));
                    }

                    curRoomEnemies.Clear();
                }
            }
        }
    }

    #endregion

    #region Animation/Effects

    IEnumerator MoveTo(Transform target, Vector3 to, float duration)
    {
        float time = 0;
        Vector3 start = target.position;

        while (time <= duration)
        {
            float t = Mathf.Clamp01(time / duration);
            target.position = Vector3.Lerp(start, to, t);
            yield return new WaitForEndOfFrame();
            time += Time.deltaTime;
        }

        target.position = to;

        moved = true;
    }

    IEnumerator DoDamage(Unit target, int damage)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        Color startMaterialColor = renderer.material.color;

        renderer.material.color = Color.white;
        yield return new WaitForSeconds(0.2f);

        renderer.material.color = startMaterialColor;
        target.health -= damage;
    }

    #endregion
}
