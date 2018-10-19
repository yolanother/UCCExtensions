using System.Collections;
using System.Collections.Generic;
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


        private string[] conditionTypes = {
            "Greater",
            "Less",
            "Equals",
            "Not Equals"
        };
        private AnimationStateCollection stateCollection;

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
            //Debug.Log("Selection changed to " + Selection.activeObject);

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

        private void DrawStateSet(AnimationStateSet set, int currentId) {
            string[] options = new string[stateCollection.ItemIds.Count + 1];
            options[0] = "Undefined Item: " + currentId;
            int itemIndex = 0;
            for (int i = 1; i < set.AnimationStates.Length; i++) {
                options[i] = set.AnimationStates[i].name;
                if ((int)currentId == set.AnimationStates[i].ID) {
                    itemIndex = i;
                }
            }
            itemIndex = EditorGUILayout.Popup(itemIndex, options);
        }

        private void DrawAnimatorCondition(AnimatorCondition condition) {
            EditorGUILayout.BeginHorizontal(); {
                int index = EditorGUILayout.Popup(nameToParameterIndex[condition.parameter], parameterNames);
                AnimatorControllerParameter parameter = parameters[index];
                if(parameter.type == AnimatorControllerParameterType.Bool) {
                    bool toggled = EditorGUILayout.Toggle(condition.threshold > 0);
                } else if (parameter.type != AnimatorControllerParameterType.Trigger) {
                    int conditionIdx = EditorGUILayout.Popup((int) condition.mode - (int) AnimatorConditionMode.Greater - 1, conditionTypes);

                    if (parameter.name.EndsWith("ItemID")) {
                        DrawStateSet(stateCollection.ItemIds, (int)condition.threshold);
                    } else if (parameter.name.EndsWith("StateIndex")) {
                        DrawStateSet(stateCollection.ItemStateIndexes, (int)condition.threshold);
                    } else if (parameter.name.EndsWith("AbilityIndex")) {
                        DrawStateSet(stateCollection.AbilityIndexes, (int)condition.threshold);
                    } else {
                        EditorGUILayout.TextField("" + condition.threshold);
                    }
                }

            } EditorGUILayout.EndHorizontal();
        }

        private void DrawAnimatorStateTransition(AnimatorStateTransition transition) {
            GUILayout.Label("Transition to " + transition.destinationState.name);
            foreach(AnimatorCondition condition in transition.conditions) {
                DrawAnimatorCondition(condition);
            }
        }

        private void OnGUI() {
            if (animatorController == null ) {
                GUILayout.Label("Please select an animator controller.");
                animatorController = (AnimatorController) EditorGUILayout.ObjectField(animatorController, typeof(AnimatorController));
            } else {
                stateCollection = (AnimationStateCollection)EditorGUILayout.ObjectField(stateCollection, typeof(AnimationStateCollection));
                if (Selection.activeObject is AnimatorStateTransition) {
                    Debug.Log("Selected AnimatorStateTransition");
                    DrawAnimatorStateTransition(Selection.activeObject as AnimatorStateTransition);
                }
            }
        }
    }
}
