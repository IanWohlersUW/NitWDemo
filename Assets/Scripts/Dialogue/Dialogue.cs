using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
So... if I don't have 

Dialogue accepts a text script and translates it into a coroutine the can be started later

Text (dialogue) is written in a specific syntax documented below:

DIALOGUE
ActorName: words words words
    -The above creates a text bubble above the named actor saying "words words words"
    -Dialogue boxes are progressed by pressing Z
    -An error will be logged if the named actor can't be found in the scene

ANIMATIONS
!ActorName: AnimationName
    -The above plays the named actor's animation, pausing dialogue until animation finished
    -An error will be logged if the named actor or animation can't be found
!Actor1Name: AnimationName, Actor2Name: AnimationName, Actor3Name: AnimationName
    -The above plays the list of animations, pausing dialogue until all are finished

STORING VARIABLES
[True] -> VariableName
    -The above stores the boolean value True into the named variable
    -Currently only supports bools! Trivial to expand to numbers as well
    -


?ActorName
I like you
 FiascoFox: I've heard that one before
 FiascoFox: I like you too kid!
I love you
 !FiascoFox: Blush
 FiascoFox: I've... never had somebody tell me that before
END
 */
public class Dialogue : CustomCoroutine
{
    public TextAreaScript dialogue;
    public DialogueContext context;
    public enum DialogueType { Dialogue, Action, Choice, StoreVariable }
    public override IEnumerator CreateCoroutine() => ParseScript(dialogue.longString);

    private void OnValidate()
    {
        return;
        /*
        var isValid = ParseScript(dialogue.longString).All((ienum) => ienum != null);
        if (!isValid)
            Debug.LogError("Invalid Dialogue!"); // Need to log where it breaks
        else
            Debug.Log("Valid script!");
        */
    }

    public IEnumerator ParseScript(string script)
    {
        var parsers = new Dictionary<DialogueType, Func<IEnumerator<string>, IEnumerator>>()
        {
            { DialogueType.Dialogue, ParseDialogue },
            { DialogueType.Choice, ParseChoice },
            { DialogueType.Action, ParseAction },
            { DialogueType.StoreVariable, ParseStoreVariable }
        };

        var results = new List<IEnumerator>();
        script = script.Replace("\r", ""); // Holy god. This took hours to debug
        var lines = script.Split('\n').Where(line => line.Length > 0).GetEnumerator();
        while (lines.MoveNext())
        {
            var parser = parsers[GetDialogueType(lines.Current)];
            // Parser will extract the next ienumerator, or return NULL
            results.Add(parser(lines));
        }
        if (results.Count == 0)
            return null;
        return results.Aggregate(AnimUtils.SequenceCoroutines);
    }
    private static DialogueType GetDialogueType(string line)
    {
        switch (line[0]) // Type is inferred based on the first character of a line
        {
            case '!':
                return DialogueType.Action;
            case '?':
                return DialogueType.Choice;
            case '[':
                return DialogueType.StoreVariable;
            default:
                return DialogueType.Dialogue;
        }
    }

    // Below are parsing scripts for each line type. The Choice one is fairly complex!
    private IEnumerator ParseDialogue(IEnumerator<string> lines)
    {
        var split = SplitLine(lines.Current);
        if (!split.HasValue)
            return null;
        (Animator actor, string text) = split.Value;
        return GameManager.instance.bubbleSpawner.CreateBubble(text, actor.transform);
    }

    private IEnumerator ParseAction(IEnumerator<string> lines)
    {
        var actorsAndAnims = lines.Current.Substring(1)
            .Split(new string[] { ", " }, StringSplitOptions.None) // Get each (actor: anim) pair
            .Select(SplitLine); // Then locate the animators for each pair
        if (actorsAndAnims.Any(pair => !pair.HasValue))
            return null; // One of the animations was poorly formatted
        var animations = actorsAndAnims
            .Select(pair => pair.Value)
            .Select(pair => pair.actor.PlayAnimation(pair.text.Trim())); // Convert into animations
        if (animations.Any(animation => animation == null))
            return null; // An animation couldn't be located
        return animations.Aggregate(AnimUtils.CombineCoroutines);
    }

    private IEnumerator ParseStoreVariable(IEnumerator<string> lines)
    {
        var split = lines.Current.Split(new string[]{" -> "}, 2, StringSplitOptions.None);
        if (split.Length != 2)
            return null;
        var value = split[0].TrimStart('[').TrimEnd(']');
        if (value != "True" && value != "False")
            return null;
        var variable = context.FindVariable(split[1]);
        if (variable == null)
        {
            Debug.LogWarning($"Unable to locate variable {split[1]}");
            return null;
        }
        Action storeVariable = () => variable.isTrue = (value == "True");
        return AnimUtils.CreateActionCoroutine(storeVariable);
    }

    private IEnumerator ParseChoice(IEnumerator<string> lines)
    {
        var actor = FindActor(lines.Current.Substring(1));
        var choiceLines = ExtractChoiceBlock(lines);
        if (choiceLines == null)
            return null;
        var branches = ProcessChoiceBlock(choiceLines);
        if (branches == null)
            return null;
        var branchCoroutines = branches
            .AsEnumerable()
            .ToDictionary(kvp => kvp.Key, kvp => ParseScript(kvp.Value));
        return CreateChoiceCoroutine(actor, branchCoroutines);
    }

    // Returns the a list of all lines needed to process a choice
    private List<string> ExtractChoiceBlock(IEnumerator<string> lines)
    {
        var choiceSpan = new List<string>();
        while (lines.MoveNext())
        {
            if (lines.Current == "END")
                return choiceSpan;
            choiceSpan.Add(lines.Current);
        }
        return null; // No corresponding "END" block, error out
    }

    // Given all lines in a choice block, split that block into unindented branches
    private Dictionary<string, string> ProcessChoiceBlock(List<string> lines)
    {
        var choices = new Dictionary<string, List<string>>();
        string currBlock = null;
        foreach (string line in lines)
        {
            if (line.StartsWith(" "))
            {
                if (currBlock == null)
                {
                    Debug.LogWarning("Choice block had dialogue before options");
                    return null; // Indented content above choice block
                }
                choices[currBlock].Add(line.Substring(1)); // substring to remove indentation
            }
            else
            {
                currBlock = line;
                choices[currBlock] = new List<string>();
            }
        }
        return choices.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => string.Join("\n", kvp.Value));
    }

    // Converts the processed branches into speech bubble we can interact with
    private IEnumerator CreateChoiceCoroutine(Animator actor, Dictionary<string, IEnumerator> branches)
    {
        int selected = 0;
        Action<int> onSubmit = (int choice) => selected = choice;
        var choices = branches.Keys.ToList();
        yield return GameManager.instance.bubbleSpawner.CreateChoiceBubble(choices, actor.transform, onSubmit);
        yield return branches[choices[selected]];
    }

    private (Animator actor, string text)? SplitLine(string line)
    {
        var split = line.Split(new string[] { ": " }, 2, StringSplitOptions.None );
        if (split.Length != 2)
        {
            Debug.LogWarning($"line [{line}] couldn't be split!");
            return null; // Split failed
        }
        var actor = FindActor(split[0]);
        if (actor == null)
            return null;
        var text = split[1];
        return (actor, text);
    }

    private Animator FindActor(string name)
    {
        // Doing a .Find is really costly- we can memoize this but it runs rarely enough to not matter
        var actor = GameObject.Find(name)?.GetComponent<Animator>();
        if (actor == null)
        {
            Debug.LogWarning($"Couldn't find actor: [{name}]");
            return null;
        }
        return actor;
    }
}