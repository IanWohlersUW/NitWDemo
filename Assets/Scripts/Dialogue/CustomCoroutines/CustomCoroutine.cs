using System.Collections;
using UnityEngine;

public abstract class CustomCoroutine : MonoBehaviour
{
    public abstract IEnumerator CreateCoroutine();
}
