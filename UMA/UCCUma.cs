using System.Collections.Generic;
using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Input;
using Opsive.UltimateCharacterController.Motion;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public class UCCUma : MonoBehaviour {
    [SerializeField]
    public MonoBehaviour[] runtimeEnabledBehaviours;

    private UnityInput unityInput;
    private UltimateCharacterLocomotionHandler locomotionHandler;
    private UltimateCharacterLocomotion locomotion;
    private CharacterIK characterIk;
    private Animator animator;
    private RuntimeAnimatorController runtimeAnimationController;
    private DynamicCharacterAvatar avatar;

    // Use this for initialization
    void Start () {
        unityInput = GetComponent<UnityInput>();
        locomotionHandler = GetComponent<UltimateCharacterLocomotionHandler>();
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        characterIk = GetComponent<CharacterIK>();

        animator = GetComponent<Animator>();
        runtimeAnimationController = animator.runtimeAnimatorController;

        UpdateAvatar();

        SetUmaReady(false);
    }

    private void UpdateAvatar() {
        if (null == avatar) {
            avatar = GetComponent<DynamicCharacterAvatar>();
            if (null != avatar) {
                List<UMARecipeBase> recipes = new List<UMARecipeBase>();
                for (int idx = avatar.umaAdditionalRecipes.Length - 1; idx >= 0; idx--) {
                    UMARecipeBase recipe = avatar.umaAdditionalRecipes[idx];
                    Debug.Log(recipe.name);
                    if (recipe.name != "CapsuleColliderRecipe") {
                        recipes.Add(recipe);
                    }
                }
                avatar.umaAdditionalRecipes = recipes.ToArray();
                avatar.CharacterCreated.AddListener(OnUmaReady);
                avatar.CharacterDestroyed.AddListener(OnUmaDestroyed);
                avatar.raceAnimationControllers.defaultAnimationController = runtimeAnimationController;
            }
        }
    }

    private void Update() {
        UpdateAvatar();
    }

    private void OnDestroy() {
        avatar.CharacterCreated.RemoveListener(OnUmaReady);
        avatar.CharacterDestroyed.RemoveListener(OnUmaDestroyed);
    }

    private void SetUmaReady(bool ready) {
        if (null != unityInput) {
            unityInput.enabled = ready;
        }
        if (null != locomotionHandler) {
            locomotionHandler.enabled = ready;
        }
        if (null != locomotion) {
            locomotion.enabled = ready;
        }
        if (null != characterIk) {
            characterIk.enabled = ready;
        }

        foreach(MonoBehaviour behaviour in runtimeEnabledBehaviours) {
            if (null != behaviour) {
                behaviour.enabled = ready;
            }
        }
    }

    private void OnUmaReady(UMAData data) {
        SetUmaReady(true);
    }

    private void OnUmaDestroyed(UMAData data) {
        SetUmaReady(false);
    }
}
