using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCutscene : CustomCoroutine
{
    public Animator actor;
    public override IEnumerator CreateCoroutine()
    {
        Debug.Log("Starting sampleCutscene");
        yield return new WaitForSeconds(0.5f);
        Debug.Log("and finished sampleCutscene");
        yield return null;
    }
}
