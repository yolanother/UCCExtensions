using System;
using System.Collections;
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
                        if (null != set) {
                            int id = stateCollection.ItemIds.DrawStateSet((int)condition.threshold);
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

        private void DrawState(State state, bool showHeader = true, string headerLabel = null) {
            if (showHeader) GUILayout.Label(headerLabel ?? state.Name);

            foreach (AnimatorTransitionBase transition in state.AnyStateTransitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition, "Any State");
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
            foreach (AnimatorTransitionBase transition in state.EntryTransitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition, "Entry");
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
            foreach (AnimatorTransitionBase transition in state.Transitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorTransition(transition);
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

        private void DrawAnimatorTransition(AnimatorTransitionBase transition, string source = null) {

            // This is ugly. Apparently you can't modify the collection directly.
            // The only way I found that worked was to remove conditions and readd
            // on modification. There must be a better way to do it.
            // TODO: Do this the right way.
            List<AnimatorCondition> conditions = new List<AnimatorCondition>();
            bool modified = false;

            string label = GetTransitionLabel(transition, source: source);
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

        private string GetTransitionLabel(AnimatorTransitionBase transition, AnimatorState state = null, string source = null) {
            string label = (source ?? (null == state ? "Transition" : state.name)) + " -> ";
            if (null != transition.destinationState) {
                label += transition.destinationState.name;
            } else if (null != transition.destinationStateMachine) {
                label += transition.destinationStateMachine.name;
            } else if (transition.isExit) {
                label += "Exit";
            }
            return label;
        }

        private static string[] UpdateNames(IEnumerable<string> keys, bool sorted = true) {
            List<string> names = new List<string>();
            foreach (string name in keys) {
                names.Add(name);
            }
            if (sorted) names.Sort();
            return names.ToArray();
        }

        private static U GetOrAdd<T, U>(IDictionary<T, U> dictionary, T name) {
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

        private class State {
            private AnimatorState m_State;
            private AnimatorStateMachine m_StateMachine;
            private State m_ParentState;
            private AnimatorControllerLayer m_Layer;
            private ActionSet m_ActionSet;
            private List<AnimatorTransitionBase> m_AnyTransitions = new List<AnimatorTransitionBase>();
            private List<AnimatorTransitionBase> m_EntryTransitions = new List<AnimatorTransitionBase>();


            public State() { }

            public State(AnimatorState state, AnimatorControllerLayer layer) {
                m_State = state;
                m_Layer = layer;
            }

            public State(AnimatorStateMachine stateMachine, AnimatorControllerLayer layer) {
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
                    if(null != m_StateMachine) {
                        transitions.AddRange(m_StateMachine.GetStateMachineTransitions(m_StateMachine));
                    }
                    return transitions;
                }
            }

            public State Parent {
                get {
                    return m_ParentState;
                } set {
                    m_ParentState = value;
                    m_AnyTransitions.Clear();
                    m_EntryTransitions.Clear();
                    State parent = m_ParentState;
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

            public String Name {
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
        }

        private class ActionSet {
            private string m_Name;
            private List<State> m_States = new List<State>();
            private SortedDictionary<string, AnimatorControllerLayer> m_Layers = new SortedDictionary<string, AnimatorControllerLayer>();
            private string[] m_StateNames = new string[0];

            // This will map to something like
            // Bow (AnimatorGroup) -> Draw (Action) -> Full Body Layer (State)
            private SortedDictionary<string, State> m_LayerToStates = new SortedDictionary<string, State>();

            public string Name {
                get { return m_Name; }
                set { m_Name = value; }
            }

            public List<State> States {
                get { return m_States; }
            }

            public SortedDictionary<string, AnimatorControllerLayer> Layers {
                get {
                    return m_Layers;
                }
            }
        }

        private class AnimatorGroup {
            private string m_Name;
            private SortedDictionary<string, AnimatorControllerLayer> m_Layers = new SortedDictionary<string, AnimatorControllerLayer>();
            private SortedDictionary<string, ActionSet> m_Actions = new SortedDictionary<string, ActionSet>();
            private string[] m_ActionNames = new string[0];

            public string Name {
                get { return m_Name; }
                set { m_Name = value; }
            }

            public SortedDictionary<string, AnimatorControllerLayer> Layers {
                get {
                    return m_Layers;
                }
            }

            public SortedDictionary<string, ActionSet> Actions {
                get {
                    return m_Actions;
                }
            }

            public void AddLayer(AnimatorControllerLayer layer) {
                m_Layers.Add(layer.name, layer);
            }

            public string[] ActionNames {
                get {
                    if(m_ActionNames.Length != m_Layers.Count) {
                        m_ActionNames = UpdateNames(m_Actions.Keys);
                    }
                    return m_ActionNames;
                }

            }
        }

        private Dictionary<object, State> m_States = new Dictionary<object, State>();
        private SortedDictionary<string, AnimatorControllerLayer> m_Layers = new SortedDictionary<string, AnimatorControllerLayer>();
        private SortedDictionary<string, AnimatorGroup> m_AnimatorGroups = new SortedDictionary<string, AnimatorGroup>();
        private int selectedAnimatorGroup;
        private int selectedAction;

        private State AddState(AnimatorStateMachine stateMachine, State parent, AnimatorControllerLayer layer, ActionSet actionSet, bool addChildren = true) {
            State state = new State(stateMachine, layer);
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

        private State AddState(AnimatorState animatorState, State parent, AnimatorControllerLayer layer, ActionSet actionSet) {
            State state = new State(animatorState, layer);
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
                    State layerState = AddState(layer.stateMachine, null, layer, null, false);
                    foreach (ChildAnimatorStateMachine stateMachine in layerState.ChildStateMachines) {
                        State groupState = AddState(stateMachine.stateMachine, layerState, layer, null, false);
                        AnimatorGroup group = GetOrAdd(m_AnimatorGroups, stateMachine.stateMachine.name);
                        group.AddLayer(layer);
                        foreach (ChildAnimatorStateMachine actionMachine in stateMachine.stateMachine.stateMachines) {
                            ActionSet actionSet = GetOrAdd(group.Actions, actionMachine.stateMachine.name);
                            actionSet.Name = actionMachine.stateMachine.name;
                            actionSet.Layers[layer.name] = layer;
                            State state = AddState(actionMachine.stateMachine, groupState, layer, actionSet);
                            actionSet.States.Add(state);
                        }
                        foreach (ChildAnimatorState actionState in stateMachine.stateMachine.states) {
                            ActionSet actionSet = GetOrAdd(group.Actions, actionState.state.name);
                            actionSet.Name = actionState.state.name;
                            actionSet.Layers[layer.name] = layer;
                            State state = AddState(actionState.state, groupState, layer, actionSet);
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

            foreach(State state in actionSet.States) {
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
                        DrawAnimatorTransition(currentSelection as AnimatorStateTransition);
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
