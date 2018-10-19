using System.Collections.Generic;
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
    }
}