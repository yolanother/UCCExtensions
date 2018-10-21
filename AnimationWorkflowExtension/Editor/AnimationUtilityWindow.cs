using System;
using System.Collections;
using System.Collections.Generic;
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
            } set {
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
            if(null == stateCollection) {
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

            if(Selection.activeObject is GameObject) {
                AnimatorController controller = GetAnimatorController(Selection.activeGameObject);
                if(null != controller) {
                    AnimatorController = controller;
                }
            } else if(Selection.activeObject is AnimatorStateTransition) {
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
            }
        }

        private bool DrawAnimatorCondition(ref AnimatorCondition condition, out bool remove) {
            bool modified = false;
            remove = false;
            EditorGUILayout.BeginHorizontal(); {
                if (!nameToParameter.ContainsKey(condition.parameter)) {
                    string name = EditorGUILayout.TextField(condition.parameter);
                    if (name != condition.parameter) {
                        condition.parameter = name;
                        modified = true;
                    }
                    AnimatorConditionMode mode = (AnimatorConditionMode) EditorGUILayout.EnumPopup(condition.mode);
                    if(mode != condition.mode) {
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
                    if(index != parameterIndex) {
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
                        if(conditionIdx != selectedCondition) {
                            condition.mode = conditionModes[conditionIdx];
                            modified = true;
                        }

                        AnimationStateSet set = null;
                        if (parameter.name.EndsWith("ItemID")) {
                            set = stateCollection.ItemIds;
                        } else if (parameter.name.EndsWith("StateIndex")) {
                            set = stateCollection.ItemStateIndexes;
                        } else if (parameter.name.EndsWith("AbilityIndex")) {
                            set = stateCollection.AbilityIndexes;
                        } else {
                            string result = EditorGUILayout.TextField("" + condition.threshold);
                            float resultValue;
                            if (float.TryParse(result, out resultValue) && resultValue != condition.threshold) {
                                condition.threshold = resultValue;
                                modified = true;
                            }
                        }
                        if(null != set) {
                            int id = stateCollection.ItemIds.DrawStateSet((int) condition.threshold);
                            if(id != condition.threshold) {
                                condition.threshold = id;
                                modified = true;
                            }
                        }
                    }
                }

                if(GUILayout.Button(InspectorStyles.DeleteIcon, InspectorStyles.NoPaddingButtonStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                    remove = true;
                    modified = true;
                }
            } EditorGUILayout.EndHorizontal();
            return modified;
        }

        private void DrawAnimatorStateMachine(AnimatorStateMachine stateMachine) {
            GUILayout.Label(stateMachine.name);
            foreach (AnimatorTransition transition in stateMachine.GetStateMachineTransitions(stateMachine)) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition);
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
        }

        private void DrawAnimatorState(AnimatorState animatorState) {
            GUILayout.Label(animatorState.name);
            foreach(AnimatorStateTransition transition in animatorState.transitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorStateTransition(transition);
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
        }

        private void DrawAnimatorTransition(AnimatorTransition transition) {

            // This is ugly. Apparently you can't modify the collection directly.
            // The only way I found that worked was to remove conditions and readd
            // on modification. There must be a better way to do it.
            // TODO: Do this the right way.
            List<AnimatorCondition> conditions = new List<AnimatorCondition>();
            bool modified = false;

            string label = GetTransitionLabel(transition);
            GUILayout.BeginHorizontal();
            {
                foldouts[transition] = EditorGUILayout.Foldout(GetOrAdd(foldouts, transition, false), label);
                if (foldouts[transition]) {
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

            if (foldouts[transition]) {
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

        private void DrawAnimatorStateTransition(AnimatorStateTransition transition) {

            // This is ugly. Apparently you can't modify the collection directly.
            // The only way I found that worked was to remove conditions and readd
            // on modification. There must be a better way to do it.
            // TODO: Do this the right way.
            List<AnimatorCondition> conditions = new List<AnimatorCondition>();
            bool modified = false;

            string label = GetTransitionLabel(transition);
            GUILayout.BeginHorizontal();
            {
                foldouts[transition] = EditorGUILayout.Foldout(GetOrAdd(foldouts, transition, false), label);
                if (foldouts[transition]) {
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

            if (foldouts[transition]) {
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

        private string GetTransitionLabel(AnimatorStateTransition transition, AnimatorState state = null) {
            string label = (null == state ? "Transition" : state.name) + " -> ";
            if (null != transition.destinationState) {
                label += transition.destinationState.name;
            } else if (null != transition.destinationStateMachine) {
                label += transition.destinationStateMachine.name;
            } else if (transition.isExit) {
                label += "Exit";
            }
            return label;
        }

        private string GetTransitionLabel(AnimatorTransition transition, AnimatorState state = null) {
            string label = (null == state ? "Transition" : state.name) + " -> ";
            if (null != transition.destinationState) {
                label += transition.destinationState.name;
            } else if (null != transition.destinationStateMachine) {
                label += transition.destinationStateMachine.name;
            } else if (transition.isExit) {
                label += "Exit";
            }
            return label;
        }


        private static string[] UpdateLayerNames(IEnumerable<string> keys, bool sorted = true) {
            List<string> names = new List<string>();
            foreach(string name in keys) {
                names.Add(name);
            }
            if(sorted) names.Sort();
            return names.ToArray();
        }

        private static U GetOrAdd<T, U>(Dictionary<T, U> dictionary, T name) {
            U group;
            if (!dictionary.TryGetValue(name, out group)) {
                group = (U)typeof(U).GetConstructor(new Type[0]).Invoke(new object[0]);
                dictionary[name] = group;
            }
            return group;
        }

        private static U GetOrAdd<T, U>(Dictionary<T, U> dictionary, T name, U defaultValue) {
            U group;
            if (!dictionary.TryGetValue(name, out group)) {
                group = defaultValue;
                dictionary[name] = group;
            }
            return group;
        }

        private Dictionary<string, AnimatorGroup> m_AnimatorGroups = new Dictionary<string, AnimatorGroup>();
        private string[] m_AnimatorGroupNames = new string[0];
        private int selectedAnimatorGroup;
        private int selectedActionGroup;

        private class StateSet {
            public AnimatorState state;
            public AnimatorStateMachine stateMachine;
            public AnimatorControllerLayer layer;
            public StateGroup group;
            public string name;

            public StateSet(AnimatorState state, AnimatorControllerLayer layer, StateGroup group) {
                this.state = state;
                this.layer = layer;
                this.group = group;
            }

            public StateSet(AnimatorStateMachine stateMachine, AnimatorControllerLayer layer, StateGroup group) {
                this.stateMachine = stateMachine;
                this.layer = layer;
                this.group = group;
            }

            public bool IsStateMachineSet {
                get {
                    return stateMachine != null;
                }
            }

            public bool IsStateSet {
                get {
                    return state != null;
                }
            }
        }

        private class StateGroup {
            private Dictionary<string, AnimatorControllerLayer> layers = new Dictionary<string, AnimatorControllerLayer>();
            private Dictionary<string, HashSet<StateSet>> states = new Dictionary<string, HashSet<StateSet>>();
            public string name;

            public void Add(AnimatorState state, AnimatorControllerLayer layer) {
                layers[layer.name] = layer;
                StateSet stateSet = new StateSet(state, layer, this);
                GetOrAdd(states, layer.name).Add(stateSet);
                GetOrAdd(states, state.name).Add(stateSet);
            }

            public void Add(AnimatorStateMachine state, AnimatorControllerLayer layer) {
                layers[layer.name] = layer;
                StateSet stateSet = new StateSet(state, layer, this);
                GetOrAdd(states, layer.name).Add(stateSet);
                GetOrAdd(states, state.name).Add(stateSet);
            }

            public Dictionary<string, AnimatorControllerLayer> Layers {
                get {
                    return layers;
                }
            }

            public Dictionary<string, HashSet<StateSet>> States {
                get {
                    return states;
                }
            }
        }

        private class AnimatorGroup {
            private Dictionary<string, AnimatorControllerLayer> m_Layers = new Dictionary<string, AnimatorControllerLayer>();
            private Dictionary<string, AnimatorState> m_AnimatorStates = new Dictionary<string, AnimatorState>();
            private Dictionary<string, StateGroup> m_StateGroups = new Dictionary<string, StateGroup>();
            private string[] m_LayerNames = new string[0];
            private string[] m_StateGroupNames = new string[0];

            public void Add(AnimatorControllerLayer layer, AnimatorStateMachine stateMachine) {
                m_Layers[layer.name] = layer;
                m_LayerNames = UpdateLayerNames(m_Layers.Keys);

                foreach (ChildAnimatorState state in stateMachine.states) {
                    StateGroup group = GetOrAdd(m_StateGroups, state.state.name);
                    group.name = state.state.name;
                    group.Add(state.state, layer);
                }

                foreach (ChildAnimatorStateMachine state in stateMachine.stateMachines) {
                    StateGroup group = GetOrAdd(m_StateGroups, state.stateMachine.name);
                    group.name = state.stateMachine.name;
                    group.Add(state.stateMachine, layer);
                }

                m_StateGroupNames = UpdateLayerNames(m_StateGroups.Keys);
            }

            public string[] StateGroupNames {
                get {
                    return m_StateGroupNames;
                }
            }

            public Dictionary<string, StateGroup> StateGroups {
                get {
                    return m_StateGroups;
                }
            }
        }

        private void Index() {
            m_AnimatorGroups.Clear();
            m_AnimatorGroupNames = new string[0];
            if(null != m_AnimatorController) {
                foreach(AnimatorControllerLayer layer in m_AnimatorController.layers) {
                    foreach (ChildAnimatorStateMachine stateMachine in layer.stateMachine.stateMachines) {
                        GetOrAdd(m_AnimatorGroups, stateMachine.stateMachine.name).Add(layer, stateMachine.stateMachine);
                    }
                }
                m_AnimatorGroupNames = UpdateLayerNames(m_AnimatorGroups.Keys);
            }
        }

        private void DrawRootController() {
            GUILayout.BeginHorizontal(); {
                GUILayout.Label("Action Group", GUILayout.Width(100));
                selectedAnimatorGroup = Math.Min(m_AnimatorGroupNames.Length - 1, selectedAnimatorGroup);
                selectedAnimatorGroup = EditorGUILayout.Popup(selectedAnimatorGroup, m_AnimatorGroupNames);
            } GUILayout.EndHorizontal();

            AnimatorGroup animGroup = m_AnimatorGroups[m_AnimatorGroupNames[selectedAnimatorGroup]];

            GUILayout.BeginHorizontal(); {
                GUILayout.Label("Action", GUILayout.Width(100));
                selectedActionGroup = Math.Min(animGroup.StateGroupNames.Length - 1, selectedActionGroup);
                selectedActionGroup = EditorGUILayout.Popup(selectedActionGroup, animGroup.StateGroupNames);
                
            } GUILayout.EndHorizontal();
            StateGroup group = animGroup.StateGroups[animGroup.StateGroupNames[selectedActionGroup]];
           
            GUILayout.Label(group.name);
            GUILayout.BeginHorizontal(); {
                GUILayout.Space(20);
                GUILayout.BeginVertical(); {
                    foreach (string layer in group.States.Keys) {
                        GUILayout.Label(layer);
                        foreach (StateSet state in group.States[layer]) {
                            if (state.IsStateMachineSet) {
                                DrawAnimatorStateMachine(state.stateMachine);
                            } else if (state.IsStateSet) {
                                DrawAnimatorState(state.state);
                            }
                        }
                    }
                } GUILayout.EndVertical();
            } GUILayout.EndHorizontal();
        }

        private void OnGUI() {
            if (m_AnimatorController == null ) {
                GUILayout.Label("Please select an animator controller.");
                m_AnimatorController = (AnimatorController) EditorGUILayout.ObjectField(m_AnimatorController, typeof(AnimatorController));
            } else {
                scroll = GUILayout.BeginScrollView(scroll); {
                    stateCollection = (AnimationStateCollection)EditorGUILayout.ObjectField(stateCollection, typeof(AnimationStateCollection));
                    if (currentSelection is AnimatorStateTransition) {
                        DrawAnimatorStateTransition(currentSelection as AnimatorStateTransition);
                    } else if (currentSelection is AnimatorState) {
                        Debug.Log("DrawAnimatorState");
                        DrawAnimatorState(currentSelection as AnimatorState);
                    } else if(null != m_AnimatorController) {
                        Index();
                        DrawRootController();
                    }
                } GUILayout.EndScrollView();
            }
        }
    }
}
