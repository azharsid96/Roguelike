using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public Unit unit;
    public Tile tile;
    public bool inPlayerInventory = true;
    public bool isActive = false;

    private void Update()
    {
        // if pickup in player inventory attach it's transform as the player's child transform. Otherwise, detach itself from player transform
        if (inPlayerInventory)
        {
            transform.parent = unit.transform;
        } else
        {
            transform.parent = null;
        }

    }
}
