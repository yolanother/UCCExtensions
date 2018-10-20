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

        private bool DrawStateSet(AnimationStateSet set, ref AnimatorCondition condition) {
            int currentId = (int)condition.threshold;
            string[] options = new string[stateCollection.ItemIds.Count + 1];
            options[0] = "Undefined Item: " + currentId;
            int itemIndex = 0;
            for (int i = 1; i < set.AnimationStates.Length; i++) {
                options[i] = set.AnimationStates[i].name;
                if ((int)currentId == set.AnimationStates[i].ID) {
                    options[0] = set.AnimationStates[i].name + " (current)";
                    itemIndex = i;
                }
            }
            int selectedIndex = EditorGUILayout.Popup(itemIndex, options);
            if(itemIndex != selectedIndex && selectedIndex > 0) {
                condition.threshold = set.AnimationStates[selectedIndex].ID;
                return true;
            }
            return false;
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
                        bool toggled = condition.threshold > 0;
                        bool toggleChanged = EditorGUILayout.Toggle(toggled);
                        if (toggleChanged != toggled) {
                            condition.threshold = toggleChanged ? 1 : 0;
                            modified = true;
                        }
                    } else if (parameter.type != AnimatorControllerParameterType.Trigger) {
                        int selectedCondition = System.Array.IndexOf(conditionModes, condition.mode);
                        int conditionIdx = EditorGUILayout.Popup(selectedCondition, conditionModeNames);
                        if(conditionIdx != selectedCondition) {
                            condition.mode = conditionModes[conditionIdx];
                            modified = true;
                        }
                        if (parameter.name.EndsWith("ItemID")) {
                            modified |= DrawStateSet(stateCollection.ItemIds, ref condition);
                        } else if (parameter.name.EndsWith("StateIndex")) {
                            modified |= DrawStateSet(stateCollection.ItemStateIndexes, ref condition);
                        } else if (parameter.name.EndsWith("AbilityIndex")) {
                            modified |= DrawStateSet(stateCollection.AbilityIndexes, ref condition);
                        } else {
                            string result = EditorGUILayout.TextField("" + condition.threshold);
                            float resultValue;
                            if (float.TryParse(result, out resultValue) && resultValue != condition.threshold) {
                                condition.threshold = resultValue;
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
                GUILayout.Space(20);
            }
        }

        private void DrawAnimatorStateTransition(AnimatorStateTransition transition) {
            // This is ugly. Apparently you can't modify the collection directly.
            // The only way I found that worked was to remove conditions and readd
            // on modification. There must be a better way to do it.
            // TODO: Do this the right way.
            List<AnimatorCondition> conditions = new List<AnimatorCondition>();
            bool modified = false;

            GUILayout.BeginHorizontal();
            {
                if (null != transition.destinationState) {
                    GUILayout.Label("Transition->" + transition.destinationState.name);
                } else if (null != transition.destinationStateMachine) {
                    GUILayout.Label("Transition->" + transition.destinationStateMachine.name);
                } else if (transition.isExit) {
                    GUILayout.Label("Transition->Exit");
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", InspectorStyles.NoPaddingButtonStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                    AnimatorCondition condition = new AnimatorCondition();
                    condition.parameter = parameterNames[0];
                    conditions.Add(condition);
                    modified = true;
                }
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < transition.conditions.Length; i++) {
                var condition = transition.conditions[i];
                bool remove;
                if(DrawAnimatorCondition(ref condition, out remove)) {
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

        private void OnGUI() {
            if (animatorController == null ) {
                GUILayout.Label("Please select an animator controller.");
                animatorController = (AnimatorController) EditorGUILayout.ObjectField(animatorController, typeof(AnimatorController));
            } else {
                scroll = GUILayout.BeginScrollView(scroll); {
                    stateCollection = (AnimationStateCollection)EditorGUILayout.ObjectField(stateCollection, typeof(AnimationStateCollection));
                    if (Selection.activeObject is AnimatorStateTransition) {
                        DrawAnimatorStateTransition(Selection.activeObject as AnimatorStateTransition);
                    } else if (Selection.activeObject is AnimatorState) {
                        DrawAnimatorState(Selection.activeObject as AnimatorState);
                    }
                } GUILayout.EndScrollView();
            }
        }
    }
}
