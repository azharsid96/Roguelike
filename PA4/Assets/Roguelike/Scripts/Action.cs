using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Action : MonoBehaviour
{
    public string actionType;
    public abstract void Perform(/* context? */);       // each action sub-class can then perform that action based on whatever type of action it is
}
