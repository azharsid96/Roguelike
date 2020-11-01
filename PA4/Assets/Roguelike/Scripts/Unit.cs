using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Tile tile;
    public Tile prevTile;

    public int health = 3;
    public int maxHealth = 3;

    public int mana = 5;
    public int maxMana = 5;

    public int minJumpRadius = 2;
    public int maxJumpRadius = 2;

    public int minThrowRadius = 2;
    public int maxThrowRadius = 3;

    //public bool pickupActive = false;
    //public bool pickupInInventory = true;

    public List<Action> actions;    // set this on the prefab on each unit (player, archer, warrior)
    /* 
    If it's a warrior unit we can set on it's prefab that it has a melee attack action and it has a move action (or maybe one move action per direction) 
    Actions can be added to a list as you get a new upgrade (in player's case) or we might need to modify an action
    */
}
