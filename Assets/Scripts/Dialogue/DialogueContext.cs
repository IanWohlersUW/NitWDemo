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
    [SerializeField]
    private List<BoolReference> variables;

    [Serializable]
    public class NamedCoroutine
    {
        public string name;
        public CustomCoroutine coroutine;
    }
    [SerializeField]
    private List<NamedCoroutine> coroutines;

    public BoolReference FindVariable(string name)
    {
        int index = variables.FindIndex(variable => variable.name == name);
        if (index < 0)
            return null;
        return variables[index];
    }
}
