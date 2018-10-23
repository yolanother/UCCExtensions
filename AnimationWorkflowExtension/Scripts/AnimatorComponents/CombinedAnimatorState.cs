using System.Collections.Generic;
using UnityEditor.Animations;

namespace UCCExtensions {
    public class CombinedAnimatorState {
        private AnimatorState m_State;
        private AnimatorStateMachine m_StateMachine;
        private CombinedAnimatorState m_ParentState;
        private AnimatorControllerLayer m_Layer;
        private ActionSet m_ActionSet;
        private List<AnimatorTransitionBase> m_AnyTransitions = new List<AnimatorTransitionBase>();
        private List<AnimatorTransitionBase> m_EntryTransitions = new List<AnimatorTransitionBase>();


        public CombinedAnimatorState() { }

        public CombinedAnimatorState(AnimatorState state, AnimatorControllerLayer layer) {
            m_State = state;
            m_Layer = layer;
        }

        public CombinedAnimatorState(AnimatorStateMachine stateMachine, AnimatorControllerLayer layer) {
            m_StateMachine = stateMachine;
            m_Layer = layer;
        }

        public List<AnimatorTransitionBase> AnyStateTransitions {
            get {
                return m_AnyTransitions;
            }
        }

        public List<AnimatorTransitionBase> EntryTransitions {
            get {
                return m_EntryTransitions;
            }
        }

        public List<AnimatorTransitionBase> Transitions {
            get {
                List<AnimatorTransitionBase> transitions = new List<AnimatorTransitionBase>();
                if (null != m_State) {
                    transitions.AddRange(m_State.transitions);
                }
                if (null != m_StateMachine) {
                    transitions.AddRange(m_StateMachine.GetStateMachineTransitions(m_StateMachine));
                }
                return transitions;
            }
        }

        public CombinedAnimatorState Parent {
            get {
                return m_ParentState;
            }
            set {
                m_ParentState = value;
                m_AnyTransitions.Clear();
                m_EntryTransitions.Clear();
                CombinedAnimatorState parent = m_ParentState;
                while (null != parent) {
                    foreach (AnimatorTransitionBase transition in parent.m_StateMachine.anyStateTransitions) {
                        if (null != transition.destinationState && transition.destinationState == m_State) {
                            m_AnyTransitions.Add(transition);
                        }
                        if (null != transition.destinationStateMachine && transition.destinationStateMachine == m_StateMachine) {
                            m_AnyTransitions.Add(transition);
                        }
                    }
                    foreach (AnimatorTransitionBase transition in parent.m_StateMachine.entryTransitions) {
                        if (null != transition.destinationState && transition.destinationState == m_State) {
                            m_EntryTransitions.Add(transition);
                        }
                        if (null != transition.destinationStateMachine && transition.destinationStateMachine == m_StateMachine) {
                            m_EntryTransitions.Add(transition);
                        }
                    }
                    parent = parent.Parent;
                }
            }
        }

        public string Name {
            get {
                if (null != m_State) return m_State.name;
                if (null != m_StateMachine) return m_StateMachine.name;
                return "";
            }
        }

        public AnimatorControllerLayer Layer {
            get { return m_Layer; }
            set { m_Layer = value; }
        }

        public ActionSet ActionSet {
            get { return m_ActionSet; }
            set { m_ActionSet = value; }
        }

        public AnimatorState AnimatorState {
            get {
                return m_State;
            }
        }

        public AnimatorStateMachine StateMachine {
            get {
                return m_StateMachine;
            }
        }

        public ChildAnimatorState[] ChildStates {
            get {
                return m_StateMachine != null ? m_StateMachine.states : new ChildAnimatorState[0];
            }
        }

        public ChildAnimatorStateMachine[] ChildStateMachines {
            get {
                return m_StateMachine != null ? m_StateMachine.stateMachines : new ChildAnimatorStateMachine[0];
            }
        }

        public bool HasTransition(AnimatorTransitionBase transition) {
            return null != AnimatorState && transition.destinationState == AnimatorState
                || null != StateMachine && transition.destinationStateMachine == StateMachine;
        }
    }
}
