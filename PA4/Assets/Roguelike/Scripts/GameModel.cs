using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModel : MonoBehaviour
{
    public List<Unit> units = new List<Unit>();
    public int currentUnitIndex = -1;

    public Unit CurrentUnit
    {
        get
        {
            if (currentUnitIndex < 0 || currentUnitIndex > units.Count)
                return null;
            return units[currentUnitIndex];
        }
    }

    public void RunGameLoop()
    {
        StartCoroutine(Run_Coroutine());
    }

    public System.Action<Unit> OnUnitTurnStart;

    public bool IsGameOver => false;

    public float turnDelay = 1f;

    IEnumerator Run_Coroutine()
    {
        while (!IsGameOver)
        {
            currentUnitIndex = (currentUnitIndex + 1) % units.Count;

            Unit unit = CurrentUnit;
            if (unit == null)
            {
                yield return null;
                continue;
            }

            OnUnitTurnStart?.Invoke(unit);
            // TODO: current unit takes it's turn

            yield return new WaitForSeconds(turnDelay);
        }
    }
}
