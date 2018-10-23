using System;
using UnityEditor.Animations;
using UnityEngine;

namespace UCCExtensions {
    public class AnimUtil {

        public static Vector3 FindNextPosition(ChildAnimatorStateMachine[] stateMachines) {
            float x = float.MinValue;
            float y = float.MinValue;
            float z = float.MinValue;
            if (null == stateMachines || stateMachines.Length == 0) {
                x = 450;
                y = 50;
                z = 0;
            } else {
                foreach (ChildAnimatorStateMachine child in stateMachines) {
                    x = Math.Max(child.position.x, x);
                    y = Math.Max(child.position.y, y);
                    z = Math.Max(child.position.z, z);
                }
            }
            return new Vector3(x, y + 50, z);
        }

        public static AnimatorStateMachine AddStateMachineToLayer(AnimatorControllerLayer layer, string name) {
            bool exists = false;
            AnimatorStateMachine createdMachine = null;
            foreach (ChildAnimatorStateMachine stateMachine in layer.stateMachine.stateMachines) {
                if (stateMachine.stateMachine.name == name) {
                    exists = true;
                    createdMachine = stateMachine.stateMachine;
                    break;
                }
            }
            if (!exists) {
                createdMachine = layer.stateMachine.AddStateMachine(
                    name,
                    FindNextPosition(layer.stateMachine.stateMachines));
            }

            return createdMachine;
        }

        public static AnimatorStateMachine AddStateMachineToStateMachine(AnimatorStateMachine parent, string name) {
            bool exists = false;
            AnimatorStateMachine createdMachine = null;
            foreach (ChildAnimatorStateMachine stateMachine in parent.stateMachines) {
                if (stateMachine.stateMachine.name == name) {
                    exists = true;
                    createdMachine = stateMachine.stateMachine;
                    break;
                }
            }
            if (!exists) {
                createdMachine = parent.AddStateMachine(
                    name,
                    FindNextPosition(parent.stateMachines));
            }

            return createdMachine;
        }

        public static AnimatorState AddStateToStateMachine(AnimatorStateMachine parent, string name) {
            bool exists = false;
            AnimatorState createdMachine = null;
            foreach (ChildAnimatorState stateMachine in parent.states) {
                if (stateMachine.state.name == name) {
                    exists = true;
                    createdMachine = stateMachine.state;
                    break;
                }
            }
            if (!exists) {
                createdMachine = parent.AddState(
                    name,
                    FindNextPosition(parent.stateMachines));
            }

            return createdMachine;
        }

    }
}
