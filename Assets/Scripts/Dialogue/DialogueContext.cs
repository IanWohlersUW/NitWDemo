using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/*
 * Dialogue can be given a "context" - Basically a bunch of variables, coroutines, or other
 * junk you'd like to reference in your script
 * 
 * For example - if you want to store a value, you'll need to have the variable in context first
 */
[Serializable]
public class DialogueContext
{
    public List<BoolReference> variables;
    public List<CustomCoroutine> coroutines;
    /*
     * While coroutines currently goes unused, it'd be trivial to extend Dialogue.cs to support it
     * This means that our dialogue parser handles most scripts, but if we wanted to play a special
     * cutscene, QTE, etc we could roll that into a CustomCoroutine and inject it into the middle
     * of our dialogue seamlessly. 
     */
}
