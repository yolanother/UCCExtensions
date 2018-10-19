using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StateLabel", menuName = "Ultimate Character Controller/State Label", order = 1)]
public class StateLabel : ScriptableObject {
    public int id;
    public string displayName;

    public string Name {
        get {
            if (null != displayName) return displayName;
            return name;
        }
    }
}
