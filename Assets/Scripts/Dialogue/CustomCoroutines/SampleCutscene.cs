using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCutscene : CustomCoroutine
{
    public float time;
    public override IEnumerator CreateCoroutine()
    {
        Debug.Log("Starting sampleCutscene");
        yield return new WaitForSeconds(time);
        Debug.Log($"and finished after waiting for {time} seconds");
        yield return null;
    }
}
