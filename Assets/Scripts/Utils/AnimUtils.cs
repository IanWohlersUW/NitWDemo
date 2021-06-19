using System.Collections;
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
        var obj = GameManager.instance; // I'm... more upset about needing to do this than I should
        Coroutine a = obj.StartCoroutine(first);
        Coroutine b = obj.StartCoroutine(second);
        yield return a;
        yield return b;
    }
}
