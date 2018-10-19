/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using System;
using UnityEngine;

namespace Opsive.UltimateCharacterController.Inventory
{
    /// <summary>
    /// An ItemType is a static representation of an item. Each item that interacts with the inventory must have an ItemType.
    /// </summary>
    [Serializable]
    public class AnimationState
    {
        public int ID;
        public string name;
    }
}