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

        private AnimatorController animatorController;
        private string[] parameterNames;
        private AnimatorControllerParameter[] parameters;
        private Dictionary<string, AnimatorControllerParameter> nameToParameter = new Dictionary<string, AnimatorControllerParameter>();
        private Dictionary<string, int> nameToParameterIndex = new Dictionary<string, int>();

        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

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
                return animatorController;
            } set {
                animatorController = value;
                UpdateParameterMappings(animatorController);
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
            Debug.Log("Selection changed to " + Selection.activeObject);

            if(Selection.activeObject is GameObject) {
                AnimatorController controller = GetAnimatorController(Selection.activeGameObject);
                if(null != controller) {
                    AnimatorController = controller;
                }
            }

            foldouts.Clear();

            UpdateParameterMappings(animatorController);
            Repaint();
        }

        private void UpdateParameterMappings(AnimatorController controller) {
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

                if(GUILayout.Button(InspectorStyles.DeleteIcon, InspectorStyles.NoPaddingButtonStyle)) {
                    remove = true;
                    modified = true;
                }
            } EditorGUILayout.EndHorizontal();
            return modified;
        }

        private void DrawAnimatorState(AnimatorState state, bool useHeaders = false) {
            GUILayout.Label(state.name);
            foreach(AnimatorStateTransition transition in state.transitions) {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                DrawAnimatorStateTransition(transition);
                GUILayout.EndVertical();
                GUILayout.Space(8);
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
            bool visibleByDefault = !foldouts.ContainsKey(label);
            GUILayout.BeginHorizontal();
            {
                if (visibleByDefault) {
                    GUILayout.Label(label);
                } else {
                    foldouts[label] = EditorGUILayout.Foldout(foldouts[label], label);
                }
                if (visibleByDefault || foldouts[label]) {
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

            if (visibleByDefault || foldouts[label]) {
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

        private void OnGUI() {
            if (animatorController == null ) {
                GUILayout.Label("Please select an animator controller.");
                animatorController = (AnimatorController) EditorGUILayout.ObjectField(animatorController, typeof(AnimatorController));
            } else {
                scroll = GUILayout.BeginScrollView(scroll); {
                    stateCollection = (AnimationStateCollection)EditorGUILayout.ObjectField(stateCollection, typeof(AnimationStateCollection));
                    if (Selection.activeObject is AnimatorStateTransition) {
                        var transition = Selection.activeObject as AnimatorStateTransition;
                        if (foldouts.Count == 0) {
                            int index = 0;
                            foreach (AnimatorCondition condition in transition.conditions) {
                                foldouts[transition.name + index++] = false;
                            }
                        }
                        DrawAnimatorStateTransition(transition);
                    } else if (Selection.activeObject is AnimatorState) {
                        AnimatorState animatorState = Selection.activeObject as AnimatorState;
                        if (foldouts.Count == 0) {
                            foreach (AnimatorStateTransition transition in animatorState.transitions) {
                                string name = GetTransitionLabel(transition);
                                foldouts[name] = false;
                                int index = 0;
                                foreach(AnimatorCondition condition in transition.conditions) {
                                    foldouts[name + index++] = false;
                                }
                            }
                        }
                        DrawAnimatorState(animatorState);
                    }
                } GUILayout.EndScrollView();
            }
        }
    }
}
