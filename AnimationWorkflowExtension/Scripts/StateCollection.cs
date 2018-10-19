using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StateCollection", menuName = "Ultimate Character Controller/State Collection", order = 1)]
public class StateCollection : ScriptableObject {
    public StateLabel[] abilities;
    public StateLabel[] itemStateIndexes;
    public StateLabel[] itemIds;
}
