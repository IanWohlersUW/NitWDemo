using System.Collections;
using System.Linq;
using System;
using UnityEngine;

/*
 * Extension methods mainly designed to make looking up animations/parameters simpler and
 * layer-agnostic
 */
static class AnimatorExtension {
    private static UnityEditor.Animations.AnimatorController GetController(this Animator animator)
        => animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

    public static bool HasAnimation(this Animator animator, string animName) => 
        Enumerable.Range(0, animator.layerCount)
        .Any(layer => animator.HasState(layer, Animator.StringToHash(animName)));

    public static bool HasParam(this Animator animator, string name, AnimatorControllerParameterType type)
        => animator.GetController().parameters.Any(param => param.type == type && param.name == name);
    public static IEnumerator PlayAnimation(this Animator animator, string animName)
    {
        if (!animator.HasAnimation(animName))
        {
            Debug.LogWarning($"Animation wasn't found:{animName}, in animator:{animator}");
            return null;
        }
        return animator.PlayAnimationHelper(animName);
    }

    private static IEnumerator PlayAnimationHelper(this Animator animator, string animName)
    {
        animator.Play(animName);
        yield return new WaitUntil(() => animator.InAnimation(animName));
        yield return new WaitUntil(() => !animator.InAnimation(animName));
    }

   private static bool InAnimation(this Animator animator, string animName) =>
        Enumerable.Range(0, animator.layerCount)
        .Any(layer => animator.GetCurrentAnimatorStateInfo(layer).IsName(animName));
}
