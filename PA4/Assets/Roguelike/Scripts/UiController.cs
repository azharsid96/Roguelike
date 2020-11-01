using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{
    public void OnUnitTurnStart(Unit unit)
    {
        Debug.Log($"It's {unit.name}!");
    }
}
