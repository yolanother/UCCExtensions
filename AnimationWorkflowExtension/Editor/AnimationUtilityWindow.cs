using System;
using System.Collections.Generic;
using System.Linq;
using Opsive.UltimateCharacterController.Editor.Inspectors.Utility;
using Opsive.UltimateCharacterController.Inventory;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UCCExtensions {
    public class AnimationUtilityWindow : EditorWindow {
        private const bool USE_ITEM_COLLECTION = false;

        private AnimatorController m_AnimatorController;
        private string[] parameterNames;
        private AnimatorControllerParameter[] parameters;
        private Dictionary<string, AnimatorControllerParameter> nameToParameter = new Dictionary<string, AnimatorControllerParameter>();
        private Dictionary<string, int> nameToParameterIndex = new Dictionary<string, int>();

        private Dictionary<UnityEngine.Object, bool> foldouts = new Dictionary<UnityEngine.Object, bool>();

        private UnityEngine.Object currentSelection;

        static GUIStyle s_HeaderStyle;
        public static GUIStyle HeaderGUIStyle {
            get {
                if (s_HeaderStyle == null) {
                    s_HeaderStyle = new GUIStyle("RL Header");
                    // The header background image should stretch with the size of the rect.
                    s_HeaderStyle.fixedHeight = 0;
                    s_HeaderStyle.stretchHeight = true;
                }
                return s_HeaderStyle;
            }
        }

        private static string[] boolConditionNames = {
            "true",
            "false"
        };

        private static AnimatorConditionMode[] boolConditions = {
            AnimatorConditionMode.If,
            AnimatorConditionMode.IfNot
        };

        private static string[] conditionModeNames = {
            "Greater",
            "Less",
            "Equals",
            "Not Equals"
        };
        private static AnimatorConditionMode[] conditionModes = {
            AnimatorConditionMode.Greater,
            AnimatorConditionMode.Less,
            AnimatorConditionMode.Equals,
            AnimatorConditionMode.NotEqual
        };
        private AnimationStateCollection stateCollection;
        private Vector2 scroll;

        public AnimatorController AnimatorController {
            get {
                return m_AnimatorController;
            }
            set {
                m_AnimatorController = value;
                UpdateParameterMappings(m_AnimatorController);
            }
        }

        //
        // Add menu named "My Window" to the Window menu
        [MenuItem("Tools/Opsive/Animation Utility")]
        static void Init() {
            // Get existing open window or if none, make a new one:
            AnimationUtilityWindow window = (AnimationUtilityWindow)EditorWindow.GetWindow(typeof(AnimationUtilityWindow));
            window.titleContent = new GUIContent("Anim Utility");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable() {
            Selection.selectionChanged += SelectionChanged;
            SelectionChanged();
            if (null == stateCollection) {
                stateCollection = UCCEManagerUtility.FindAnimStateColection(this);
            }
        }

        private void OnDisable() {
            Selection.selectionChanged -= SelectionChanged;
        }

        private AnimatorController GetAnimatorController(GameObject gameObject) {
            return GetAnimatorController(gameObject.GetComponent<Animator>());
        }

        private AnimatorController GetAnimatorController(Animator animator) {
            if (null != animator && null != animator.runtimeAnimatorController) {
                var runtimeController = animator.runtimeAnimatorController;
                return AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(runtimeController));
            }
            return null;
        }

        void SelectionChanged() {
            Debug.Log("Selection changed to " + Selection.activeObject + "::" + Selection.activeObject.GetType());

            if (Selection.activeObject is GameObject) {
                AnimatorController controller = GetAnimatorController(Selection.activeGameObject);
                if (null != controller) {
                    AnimatorController = controller;
                }
            } else if (Selection.activeObject is AnimatorStateTransition) {
                currentSelection = Selection.activeObject;
            } else if (Selection.activeObject is AnimatorStateMachine) {
                currentSelection = Selection.activeObject;
            } else if (Selection.activeObject is AnimatorState) {
                currentSelection = Selection.activeObject;
            } else if (Selection.activeObject.GetType().Name == "UnityEditor.Animations.AnimatorDefaultTransition") {
                currentSelection = Selection.activeObject;
            }

            foldouts.Clear();

            UpdateParameterMappings(m_AnimatorController);
            Repaint();
        }

        private void UpdateParameterMappings(AnimatorController controller) {
            if (null != controller) {
                // Not the best way to do this since the animator controller can add
                // parameters, but it will work for now. In most cases new parameters
                // aren't going to be added. We could update this every few frames if
                // necessary. Just don't want to refresh every frame. It should be
                // sufficient to update these once per selection change.
                parameters = new AnimatorControllerParameter[controller.parameters.Length];
                parameterNames = new string[controller.parameters.Length];
                for (int i = 0; i < parameters.Length; i++) {
                    parameters[i] = controller.parameters[i];
                    parameterNames[i] = parameters[i].name;
                    nameToParameter[parameterNames[i]] = parameters[i];
                    nameToParameterIndex[parameterNames[i]] = i;
                }
                Index();
            }
        }

        private bool DrawAnimatorCondition(ref AnimatorCondition condition, out bool remove) {
            bool modified = false;
            remove = false;
            EditorGUILayout.BeginHorizontal();
            {
                if (!nameToParameter.ContainsKey(condition.parameter)) {
                    string name = EditorGUILayout.TextField(condition.parameter);
                    if (name != condition.parameter) {
                        condition.parameter = name;
                        modified = true;
                    }
                    AnimatorConditionMode mode = (AnimatorConditionMode)EditorGUILayout.EnumPopup(condition.mode);
                    if (mode != condition.mode) {
                        condition.mode = mode;
                        modified = true;
                    }
                    string result = EditorGUILayout.TextField("" + condition.threshold);
                    float resultValue;
                    if (float.TryParse(result, out resultValue) && resultValue != condition.threshold) {
                        condition.threshold = resultValue;
                        modified = true;
                    }
                } else {
                    int parameterIndex = nameToParameterIndex[condition.parameter];
                    int index = EditorGUILayout.Popup(parameterIndex, parameterNames);
                    AnimatorControllerParameter parameter = parameters[index];
                    if (index != parameterIndex) {
                        condition.parameter = parameter.name;
                        modified = true;
                    }
                    if (parameter.type == AnimatorControllerParameterType.Bool) {
                        int selectedCondition = Math.Max(0, System.Array.IndexOf(boolConditions, condition.mode));
                        int conditionIdx = EditorGUILayout.Popup(selectedCondition, boolConditionNames);
                        if (conditionIdx != selectedCondition) {
                            condition.mode = boolConditions[conditionIdx];
                            modified = true;
                        }
                    } else if (parameter.type != AnimatorControllerParameterType.Trigger) {
                        int selectedCondition = System.Array.IndexOf(conditionModes, condition.mode);
                        int conditionIdx = EditorGUILayout.Popup(selectedCondition, conditionModeNames);
                        if (conditionIdx != selectedCondition) {
                            condition.mode = conditionModes[conditionIdx];
                            modified = true;
                        }

                        AnimationStateSet set = null;
                        if (parameter.name.ToLower().EndsWith("itemid")) {
                            set = stateCollection.ItemIds;
                        } else if (parameter.name.ToLower().EndsWith("stateindex")) {
                            Debug.Log("State index...");
                            set = stateCollection.ItemStateIndexes;
                        } else if (parameter.name.ToLower().EndsWith("abilityindex")) {
                            set = stateCollection.AbilityIndexes;
                        } else {
                            string result = EditorGUILayout.TextField("" + condition.threshold);
                            float resultValue;
                            if (float.TryParse(result, out resultValue) && resultValue != condition.threshold) {
                                condition.threshold = resultValue;
                                modified = true;
                            }
                        }
                        if (null != set) {
                            int id = set.DrawStateSet((int)condition.threshold);
                            if (id != condition.threshold) {
                                condition.threshold = id;
                                modified = true;
                            }
                        }
                    }
                }

                if (GUILayout.Button(InspectorStyles.DeleteIcon, InspectorStyles.NoPaddingButtonStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                    remove = true;
                    modified = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            return modified;
        }

        private void DrawState(CombinedAnimatorState state, bool showHeader = true, string headerLabel = null) {
            if (showHeader) GUILayout.Label(headerLabel ?? state.Name);

            foreach (AnimatorTransitionBase transition in state.AnyStateTransitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition, "Any State", state);
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
            foreach (AnimatorTransitionBase transition in state.EntryTransitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition, "Entry", state);
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
            foreach (AnimatorTransitionBase transition in state.Transitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition, sourceState: state);
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
            foreach (ChildAnimatorState childState in state.ChildStates) {
                DrawAnimatorState(childState.state);
            }
            foreach (ChildAnimatorStateMachine childState in state.ChildStateMachines) {
                DrawAnimatorStateMachine(childState.stateMachine);
            }
        }

        private void DrawAnimatorStateMachine(AnimatorStateMachine stateMachine, bool showHeader = true, string headerLabel = null) {
            if (m_States.ContainsKey(stateMachine)) {
                DrawState(m_States[stateMachine], showHeader, headerLabel);
            } else {
                if (showHeader) GUILayout.Label(headerLabel ?? stateMachine.name);

                foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions) {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawAnimatorTransition(transition, "Any State");
                    GUILayout.EndVertical();
                    GUILayout.Space(8);
                }
                foreach (AnimatorTransition transition in stateMachine.entryTransitions) {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawAnimatorTransition(transition, "Entry");
                    GUILayout.EndVertical();
                    GUILayout.Space(8);
                }
                foreach (AnimatorTransition transition in stateMachine.GetStateMachineTransitions(stateMachine)) {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawAnimatorTransition(transition);
                    GUILayout.EndVertical();
                    GUILayout.Space(8);
                }
                foreach (ChildAnimatorState state in stateMachine.states) {
                    DrawAnimatorState(state.state);
                }
            }
        }

        private void DrawAnimatorState(AnimatorState animatorState, bool showHeader = true, string headerLabel = null) {
            if (m_States.ContainsKey(animatorState)) {
                DrawState(m_States[animatorState], showHeader, headerLabel);
            } else {
                if (showHeader) GUILayout.Label(headerLabel ?? animatorState.name);
                foreach (AnimatorStateTransition transition in animatorState.transitions) {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawAnimatorTransition(transition);
                    GUILayout.EndVertical();
                    GUILayout.Space(8);
                }
            }
        }

        private void OnFocus() {
            if(null != m_AnimatorController) {
                Index();
            }
        }

        private void DrawAnimatorTransition(AnimatorTransitionBase transition, string source = null, CombinedAnimatorState sourceState = null, bool foldout = true) {

            // This is ugly. Apparently you can't modify the collection directly.
            // The only way I found that worked was to remove conditions and readd
            // on modification. There must be a better way to do it.
            // TODO: Do this the right way.
            List<AnimatorCondition> conditions = new List<AnimatorCondition>();
            bool modified = false;

            string label = GetTransitionLabel(transition, sourceState, source);
            GUILayout.BeginHorizontal();
            {
                if (foldout) {
                    foldouts[transition] = EditorGUILayout.Foldout(CollectionUtil.GetOrAdd(foldouts, transition, false), label);
                } else {
                    GUILayout.Label(label);
                }
                if (!foldout || foldouts[transition]) {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", InspectorStyles.NoPaddingButtonStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                        AnimatorCondition condition = new AnimatorCondition();
                        condition.parameter = parameterNames[0];
                        conditions.Add(condition);
                        modified = true;
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (!foldout || foldouts[transition]) {
                for (int i = 0; i < transition.conditions.Length; i++) {
                    var condition = transition.conditions[i];
                    bool remove;
                    if (DrawAnimatorCondition(ref condition, out remove)) {
                        if (!remove) conditions.Add(condition);
                        modified = true;
                    } else {
                        conditions.Add(transition.conditions[i]);
                    }
                }

                if (modified) {
                    while (transition.conditions.Length > 0) {
                        transition.RemoveCondition(transition.conditions[0]);
                    }

                    foreach (AnimatorCondition condition in conditions) {
                        transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
                    }
                }
            }
        }

        private string GetTransitionLabel(AnimatorTransitionBase transition, CombinedAnimatorState state = null, string source = null) {
            string label = (source ?? "Self") + " -> ";
            if(null != state && state.HasTransition(transition)) {
                label += "Self";
            } else if (null != transition.destinationState) {
                label += transition.destinationState.name;
            } else if (null != transition.destinationStateMachine) {
                label += transition.destinationStateMachine.name;
            } else if (transition.isExit) {
                label += "Exit";
            }
            return label;
        }

        private Dictionary<object, CombinedAnimatorState> m_States = new Dictionary<object, CombinedAnimatorState>();
        private SortedDictionary<string, AnimatorControllerLayer> m_Layers = new SortedDictionary<string, AnimatorControllerLayer>();
        private SortedDictionary<string, AnimatorGroup> m_AnimatorGroups = new SortedDictionary<string, AnimatorGroup>();
        private int selectedAnimatorGroup;
        private int selectedAction;

        private CombinedAnimatorState AddState(AnimatorStateMachine stateMachine, CombinedAnimatorState parent, AnimatorControllerLayer layer, ActionSet actionSet, bool addChildren = true) {
            CombinedAnimatorState state = new CombinedAnimatorState(stateMachine, layer);
            state.Layer = layer;
            state.ActionSet = actionSet;
            state.Parent = parent;
            m_States[stateMachine] = state;
            if (addChildren) {
                foreach (ChildAnimatorState child in stateMachine.states) {
                    AddState(child.state, state, layer, actionSet);
                }
                foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines) {
                    AddState(child.stateMachine, state, layer, actionSet);
                }
            }
            return state;
        }

        private CombinedAnimatorState AddState(AnimatorState animatorState, CombinedAnimatorState parent, AnimatorControllerLayer layer, ActionSet actionSet) {
            CombinedAnimatorState state = new CombinedAnimatorState(animatorState, layer);
            state.Layer = layer;
            state.ActionSet = actionSet;
            state.Parent = parent;
            m_States[animatorState] = state;
            return state;
        }

        private void Index() {
            m_AnimatorGroups.Clear();
            if (m_AnimatorController != null) {
                foreach (AnimatorControllerLayer layer in m_AnimatorController.layers) {
                    m_Layers[layer.name] = layer;
                    CombinedAnimatorState layerState = AddState(layer.stateMachine, null, layer, null, false);
                    foreach (ChildAnimatorStateMachine stateMachine in layerState.ChildStateMachines) {
                        CombinedAnimatorState groupState = AddState(stateMachine.stateMachine, layerState, layer, null, false);
                        AnimatorGroup group = CollectionUtil.GetOrAdd(m_AnimatorGroups, stateMachine.stateMachine.name);
                        group.AddLayer(layer);
                        foreach (ChildAnimatorStateMachine actionMachine in stateMachine.stateMachine.stateMachines) {
                            ActionSet actionSet = CollectionUtil.GetOrAdd(group.Actions, actionMachine.stateMachine.name);
                            actionSet.Name = actionMachine.stateMachine.name;
                            actionSet.Layers[layer.name] = layer;
                            CombinedAnimatorState state = AddState(actionMachine.stateMachine, groupState, layer, actionSet);
                            actionSet.States.Add(state);
                        }
                        foreach (ChildAnimatorState actionState in stateMachine.stateMachine.states) {
                            ActionSet actionSet = CollectionUtil.GetOrAdd(group.Actions, actionState.state.name);
                            actionSet.Name = actionState.state.name;
                            actionSet.Layers[layer.name] = layer;
                            CombinedAnimatorState state = AddState(actionState.state, groupState, layer, actionSet);
                            actionSet.States.Add(state);
                            m_States[actionState.state] = state;
                        }
                    }
                }
            }
        }

        private void DrawRootController() {
            AnimatorGroup animatorGroup;
            ActionSet actionSet;

            GUILayout.BeginHorizontal(); {
                GUILayout.Label("Group", GUILayout.Width(100));
                string[] names = m_AnimatorGroups.Keys.ToArray();
                selectedAnimatorGroup = Math.Min(names.Length - 1, selectedAnimatorGroup);
                selectedAnimatorGroup = EditorGUILayout.Popup(selectedAnimatorGroup, names);
                animatorGroup = m_AnimatorGroups[names[selectedAnimatorGroup]];
            } GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(); {
                GUILayout.Label("Action", GUILayout.Width(100));
                string[] names = animatorGroup.Actions.Keys.ToArray();
                selectedAction = Math.Min(names.Length - 1, selectedAction);
                selectedAction = EditorGUILayout.Popup(selectedAction, names);
                actionSet = animatorGroup.Actions[names[selectedAction]];
            } GUILayout.EndHorizontal();

            foreach(CombinedAnimatorState state in actionSet.States) {
                GUILayout.Label(state.Layer.name);
                if(state.StateMachine != null) {
                    DrawAnimatorStateMachine(state.StateMachine, false);
                } else if(state.AnimatorState != null) {
                    DrawAnimatorState(state.AnimatorState, false);
                }
            }
        }

        private void OnGUI() {
            if (m_AnimatorController == null ) {
                GUILayout.Label("Please select an animator controller.");
                m_AnimatorController = (AnimatorController) EditorGUILayout.ObjectField(m_AnimatorController, typeof(AnimatorController));
                if(null != m_AnimatorController) {
                    Index();
                }
            } else {
                // TODO: This will probably need to move out and shouldn't be done
                // every frame. Performing fine for now.
                scroll = GUILayout.BeginScrollView(scroll); {
                    stateCollection = (AnimationStateCollection)EditorGUILayout.ObjectField(stateCollection, typeof(AnimationStateCollection));
                    if (currentSelection is AnimatorStateTransition) {
                        DrawAnimatorTransition(currentSelection as AnimatorStateTransition, foldout: false);
                    } else if (currentSelection is AnimatorStateMachine) {
                        DrawAnimatorStateMachine(currentSelection as AnimatorStateMachine);
                    } else if (currentSelection is AnimatorState) {
                        DrawAnimatorState(currentSelection as AnimatorState);
                    } else {
                        DrawRootController();
                    }
                } GUILayout.EndScrollView();
            }
        }
    }
}
