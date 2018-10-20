using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Opsive.UltimateCharacterController.Inventory {
    public class AnimationStateSet : ScriptableObject {
        [Tooltip("An array of all of the possible Animation States.")]
        [SerializeField] protected List<AnimationState> m_AnimationStates = new List<AnimationState>();

        public AnimationState[] AnimationStates {
            get {
                return m_AnimationStates.ToArray();
            }
            set {
                m_AnimationStates.Clear();
                m_AnimationStates.AddRange(value);
            }
        }

        /// <summary>
        /// Gets the <see cref="T:Opsive.UltimateCharacterController.Inventory.AnimationStateSet"/> with the specified id.
        /// </summary>
        /// <param name="id">The animation id representing an item in the AnimationStateSet</param>
        public AnimationState this[int id] {
            get {
                foreach (AnimationState state in AnimationStates) {
                    if (state.ID == id) {
                        return state;
                    }
                }
                return null;
            }
        }

        public int Count {
            get {
                return AnimationStates.Length;
            }
        }

        public int DrawStateSet(int currentId) {
            string[] options = new string[Count + 1];
            options[0] = "Undefined Item: " + currentId;
            int itemIndex = 0;
            for (int i = 1; i < AnimationStates.Length; i++) {
                options[i] = AnimationStates[i].name;
                if ((int)currentId == AnimationStates[i].ID) {
                    options[0] = AnimationStates[i].name + " (current)";
                    itemIndex = i;
                }
            }
            int selectedIndex = EditorGUILayout.Popup(itemIndex, options);
            if (itemIndex != selectedIndex && selectedIndex > 0) {
                currentId = AnimationStates[selectedIndex].ID;
            }
            return currentId;
        }
    }
}