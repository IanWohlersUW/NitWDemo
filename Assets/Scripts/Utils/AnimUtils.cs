using System.Collections;
using System;
using UnityEngine;

public class AnimUtils
{
    // "Zips" two coroutines together. The resulting coroutine plays A, then B
    public static IEnumerator SequenceCoroutines(IEnumerator first, IEnumerator second)
    {
        yield return first;
        yield return second;
    }

    // "Zips" two coroutines together. The resulting coroutine plays A and B simultaneously
    // Finished when *both* children are finished
    public static IEnumerator CombineCoroutines(IEnumerator first, IEnumerator second)
    {
        /*
         * This has a hard dependency on GameManger which sucks.
         * https://answers.unity.com/questions/712423/how-to-yield-on-two-coroutines-simulateously.html
         * Doesn't quite work (nested yields break it)
         * So instead I settled on this solution from:
         * https://www.alanzucconi.com/2017/02/15/nested-coroutines-in-unity/
         * I still think there's a way to do this though without StartCoroutine though :(
         */
        var obj = GameManager.instance;
        Coroutine a = obj.StartCoroutine(first);
        Coroutine b = obj.StartCoroutine(second);
        yield return a;
        yield return b;
    }
    
    // Converts an action into a coroutine
    public static IEnumerator CreateActionCoroutine(Action action)
    {
        action();
        yield return null; // (Note that this action does take a full frame)
    }
}
