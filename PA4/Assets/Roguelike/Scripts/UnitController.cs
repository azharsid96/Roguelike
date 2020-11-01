using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitController : MonoBehaviour
{
    public Unit unit;

    public abstract void PerformTurn(/* context? */);
    /* when we want archer unit to have different AI logic from your warrior unit then you go and make a new subclass of UnitController called ArcherController and in it's perform turn
     * you do the logic to try and get into the same row or column as the player unit and if it's in the same row or column and it has an unobstructed view then you want it to fire 
     * Helper function ideas: IsViewUnobstructed(), IsInSameRowOrColAsPlayer()
    */

    private void Update()
    {
        if (unit.health == 0)
            KillUnit();
    }

    //clears the link between enemy unit and the tile it was currently on
    public void KillUnit()
    {
        // No other unit currently resides on the tile where the this unit (to be killed) used to be
        if (unit.tile.unit != null && unit.tile.unit == unit)
        {
            unit.tile.unit = null;
            unit.tile = null;
            Destroy(unit.gameObject);
        }
        // The tile where this unit (to be killed) used to be is currently occupied by a different unit
        else if (unit.tile.unit != unit)
        {
            unit.tile = null;
            Destroy(unit.gameObject);
        }
        
    }
}
