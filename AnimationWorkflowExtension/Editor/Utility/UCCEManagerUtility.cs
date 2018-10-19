using System.Collections;
using System.Collections.Generic;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Editor.Managers;
using UnityEngine;
using UnityEditor;

public class UCCEManagerUtility : MonoBehaviour {
    private const string c_AnimatonStateCollectionGUID = "14c6e9653df2c4eb0a3d3383d0284c7d";
    public static string LastAnimationStateCollectionGUIDString { get { return c_AnimatonStateCollectionGUID; } }

    /// <summary>
    /// Searches for the default item collection.
    /// </summary>
    public static AnimationStateCollection FindAnimStateColection(ScriptableObject editorWindow) {

        // Retrieve the last used AnimStateColection.
        var lastAnimStateColectionGUID = EditorPrefs.GetString(LastAnimationStateCollectionGUIDString, string.Empty);
        if (!string.IsNullOrEmpty(lastAnimStateColectionGUID)) {
            var lastAnimStateColectionPath = AssetDatabase.GUIDToAssetPath(lastAnimStateColectionGUID);
            if (!string.IsNullOrEmpty(lastAnimStateColectionPath)) {
                var animStateColection = AssetDatabase.LoadAssetAtPath(lastAnimStateColectionPath, typeof(AnimationStateCollection)) as AnimationStateCollection;
                if (animStateColection != null) {
                    return animStateColection;
                }
            }
        }

        // The GUID should remain consistant.
        var animStateColectionPath = AssetDatabase.GUIDToAssetPath(c_AnimatonStateCollectionGUID);
        if (!string.IsNullOrEmpty(animStateColectionPath)) {
            var animStateColection = AssetDatabase.LoadAssetAtPath(animStateColectionPath, typeof(AnimationStateCollection)) as AnimationStateCollection;
            if (animStateColection != null) {
                return animStateColection;
            }
        }

        // The item collection doesn't have the expected guid. Try to find the asset based on the path.
        animStateColectionPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(editorWindow))).Replace("Editor/Managers", "Demo/Inventory/DemoAnimStateColection.asset");
        if (System.IO.File.Exists(Application.dataPath + "/" + animStateColectionPath.Substring(7))) {
            return AssetDatabase.LoadAssetAtPath(animStateColectionPath, typeof(AnimationStateCollection)) as AnimationStateCollection;
        }

        // Last chance: use resources to try to find the AnimStateColection.
        var animStateColections = Resources.FindObjectsOfTypeAll<AnimationStateCollection>();
        if (animStateColections != null && animStateColections.Length > 0) {
            return animStateColections[0];
        }

        return null;
    }
}
