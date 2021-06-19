using System.Collections;
using System.Linq;
using System;
using UnityEngine;

static class AnimatorExtension {
    private static UnityEditor.Animations.AnimatorController GetController(this Animator animator)
        => animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

    public static bool HasAnimation(this Animator animator, string animName, int layer = 0)
        => animator.GetController().animationClips.Any(clip => clip.name == animName);
    // => animator.HasState(layer, Animator.StringToHash(animName)); Maybe... should do it like this still

    public static bool HasParam(this Animator animator, string name, AnimatorControllerParameterType type)
        => animator.GetController().parameters.Any(param => param.type == type && param.name == name);
    public static IEnumerator PlayAnimation(this Animator animator, string animName, int layer = 0)
    {
        if (!animator.HasAnimation(animName, layer))
        {
            Debug.LogWarning($"Animation wasn't found:{animName}, layer:{layer} in animator:{animator}");
            return null;
        }
        return animator.PlayAnimationHelper(animName, layer);
    }

    private static IEnumerator PlayAnimationHelper(this Animator animator, string animName, int layer = 0)
    {
        animator.Play(animName, layer);
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(animName));
        yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsName(animName));
    }

    // Safe IEnumerators for setting parameters of an animator
    public static IEnumerator SetParameter(this Animator animator, string name, bool paramVal)
    {
        if (!animator.HasParam(name, AnimatorControllerParameterType.Bool))
            return null;
        return SetParameterHelper(animator.SetBool, name, paramVal);
    }

    public static IEnumerator SetParameter(this Animator animator, string name, float paramVal)
    {
        if (!animator.HasParam(name, AnimatorControllerParameterType.Float))
            return null;
        return SetParameterHelper(animator.SetFloat, name, paramVal);
    }
    public static IEnumerator SetParameter(this Animator animator, string name, int paramVal)
    {
        if (!animator.HasParam(name, AnimatorControllerParameterType.Int))
            return null;
        return SetParameterHelper(animator.SetInteger, name, paramVal);
    }
    // There's also SetTrigger... uhhh I'll do that later. This is just a demo!

    private static IEnumerator SetParameterHelper<T>(Action<string, T> setParam, string name, T paramVal)
    {
        setParam(name, paramVal);
        yield return null;
    }
}
