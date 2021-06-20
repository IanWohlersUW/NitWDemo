using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Dialogue accepts a text script and translates it into a coroutine that can be started at any time

Dialogue also has a context variable. If our dialogue is dependent on ScriptableObjects or elements
in the scene, we can pass references to them via our context. If not, it's fine to leave context empty

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
    -The above plays the named animations simultaneously, pausing dialogue until ALL are finished

STORING VARIABLES
[True] -> VariableName
    -The above stores the boolean value True into the named variable
    -Currently only supports bools! Trivial to expand to numbers as well
    -An error logged if the variable store in doesn't exist in the dialogue context

[True] -> Actor.ParameterName
    -The above stores the given boolean value into the actor's animator paramter
    -An error logged if the named parameter doesn't exist

CHOICES
?ActorName
I like you
 FiascoFox: I've heard that one before
 FiascoFox: I like you too kid!
I love you
 !FiascoFox: Blush
 FiascoFox: I've... never had somebody tell me that before
END
FiascoFox: Sorry, I have to go!
    -The above gives the player choose between "I like you" and "I love you"
    -The indented block below the selected choice is played next. Then dialogue resumes below END
    -Choice blocks can nested if you want to create a more complex dialogue tree
    -An error is logged if an END line isn't included to indicate the end of the last choice
 */
public class Dialogue : CustomCoroutine
{
    // We should give these fields a [NotNull] tag from https://github.com/redbluegames/unity-notnullattribute
    public TextAreaScript dialogue;
    public BubbleSpawner bubbleSpawner; // Required for actually creating bubbles
    public DialogueContext context;
    public enum DialogueType { Dialogue, Animation, Choice, StoreVariable }
    public override IEnumerator CreateCoroutine() => ParseScript(dialogue.longString)
        .Aggregate(AnimUtils.SequenceCoroutines);

    private void OnValidate()
    {
        /*
         * A null enum represents a parse that failed.
         * Because all animators and scene references are looked up before executing the coroutine
         * if this check passes have a runtime garauntee the generated coroutine won't crash
         * (Unless referenced gameObjects are destroyed in the scene prior to execution)
         */
        if (bubbleSpawner == null)
        {
            Debug.LogError("Dialogue needs a reference to bubbleSpawner");
            return;
        }
        var isValid = ParseScript(dialogue.longString).All((ienum) => ienum != null);
        if (!isValid)
            Debug.LogError("Invalid Dialogue!");
        else
            Debug.Log("Valid script!");
    }

    private List<IEnumerator> ParseScript(string script)
    {
        var parsers = new Dictionary<DialogueType, Func<IEnumerator<string>, IEnumerator>>()
        {
            { DialogueType.Dialogue, ParseDialogue },
            { DialogueType.Choice, ParseChoice },
            { DialogueType.Animation, ParseAnimation },
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
        return results;
    }

    private static DialogueType GetDialogueType(string line)
    {
        switch (line[0]) // Type is inferred based on the first character of a line
        {
            case '!':
                return DialogueType.Animation;
            case '?':
                return DialogueType.Choice;
            case '[':
                return DialogueType.StoreVariable;
            default:
                return DialogueType.Dialogue;
        }
    }

    // --Below are parsing scripts for each line type. The Choice one is fairly complex!--

    /*
     * Creates a speech bubble above the actor holding their line. Closes when "Z" pressed.
     * Automatically sets the "Talking" of the given actor to true until dialogue finished.
     */
    private IEnumerator ParseDialogue(IEnumerator<string> lines)
    {
        var split = SplitLine(lines.Current);
        if (!split.HasValue)
            return null;
        (Animator actor, string text) = split.Value;
        return ParseDialogueHelper(actor, text);
    }

    private IEnumerator ParseDialogueHelper(Animator actor, string text)
    {
        var talkingParam = "Talking"; // This parameter will automatically be set true if present
        bool canTalk = actor.HasParam(talkingParam, AnimatorControllerParameterType.Bool);
        if (canTalk)
            actor.SetBool(talkingParam, true);
        yield return bubbleSpawner.CreateBubble(text, actor.transform);
        if (canTalk)
            actor.SetBool(talkingParam, false);
    }

    /*
     * Parses out a given animation line. All animations looked up before execution.
     * All animations on the line will be played simultaneously, waits until the last is finished
     */
    private IEnumerator ParseAnimation(IEnumerator<string> lines)
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

    /*
     * Parses out a variable storage line. Currently only supports bools, but with better parsing
     * and overrides, could support all primitive types.
     * Values are stored either into an animator parameter or value reference (boolReference)
     */
    private IEnumerator ParseStoreVariable(IEnumerator<string> lines)
    {
        var split = lines.Current.Split(new string[]{" -> "}, 2, StringSplitOptions.None);
        if (split.Length != 2)
            return null;
        var valueStr = split[0].TrimStart('[').TrimEnd(']');
        if (valueStr != "True" && valueStr != "False")
        {
            Debug.LogWarning("Only 'True' or 'False' can be supplied as variable values");
            return null;
        }
        var value = valueStr == "True";
        var varName = split[1].Split('.');
        if (varName.Length == 1)
            return StoreVariable(varName[0], value);
        else if (varName.Length == 2)
            return StoreParameter(varName[0], varName[1], value);
        Debug.LogWarning($"Unable to parse varName {split[1]}");
        return null;
    }

    private IEnumerator StoreParameter(string actorName, string paramName, bool value)
    {
        var actor = FindActor(actorName);
        if (actor == null)
            return null;
        if (!actor.HasParam(paramName, AnimatorControllerParameterType.Bool))
            Debug.LogWarning($"Parameter {paramName} does not exist on {actorName}");
        Action setParameter = () => actor.SetBool(paramName, value);
        return AnimUtils.CreateActionCoroutine(setParameter);
    }

    private IEnumerator StoreVariable(string varName, bool value)
    {
        int index = context.variables.FindIndex(contextVar => contextVar.name == name);
        if (index < 0)
        {
            Debug.LogWarning($"Unable to locate variable {varName}");
            return null;
        }
        var variable = context.variables[index];
        Action storeVariable = () => variable.isTrue = value;
        return AnimUtils.CreateActionCoroutine(storeVariable);
    }

    /*
     * Parses a full choice block, see docs above for choice syntax.
     * Choice blocks can be infinitely nested and are evaluated recursively
     */
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
            .ToDictionary(kvp => kvp.Key, kvp => ParseScript(kvp.Value).Aggregate(AnimUtils.SequenceCoroutines));
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
        yield return bubbleSpawner.CreateChoiceBubble(choices, actor.transform, onSubmit);
        yield return branches[choices[selected]];
    }

    /* 
     * Splits a ": " separated line into its actor and text. Returns None if split failed or
     * the actor couldn't be found
     */
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

    // Looks up the named actor (Animator) in our scene. Returns null if they couldn't be found
    private Animator FindActor(string name)
    {
        /*
         * Doing a .Find is really costly- we can memoize this to speed up performance
         * Because FindActor is only run while parsing the dialogue the first time this shouldn't
         * have a big impact on performance either way though
         */
        var actor = GameObject.Find(name)?.GetComponent<Animator>();
        if (actor == null)
        {
            Debug.LogWarning($"Couldn't find actor: [{name}]");
            return null;
        }
        return actor;
    }
}