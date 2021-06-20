using System.Collections;
using UnityEngine;

/*
 * A custom coroutine is basically a gameObject wrapper for any coroutine we'd like to pass around
 * 
 * This is done mainly because coroutines usually depend on the scene they're in (so they're not
 * easily serializable), but I'd still like to pass references of them around via the Unity editor
 */
public abstract class CustomCoroutine : MonoBehaviour
{
    public abstract IEnumerator CreateCoroutine();
}
