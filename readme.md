# Ian Wohlers Coding Sample
## Night in the Woods Dialogue System
While cover letters are great, I wanted to provide a concrete example of my coding style, and what it'd be like to collaborate with me. This whole repo was coded up over the last three days under the premise "if I had nothing to go off of, how would I start to build Night in the Woods?" (I had no prior knowledge of tools used to develop NitW like Yarn Spinner)
# Guiding Principles:
Night in the Woods is a *very* writing-heavy game. I figured architecture I plan out for this dialogue system should be usable by a writer - I tried to come up with a syntax that looked like writing stage notes because I assumed that'd be most intuitive for an writer. Basically I knew I needed a text parsed, and I envisioned a system that could take in a full script of text and output a single coroutine that, when started, animates that entire scene.

# Composing Coroutines
I was inspired by [Alan Zucconi' article on nested coroutines](https://www.alanzucconi.com/2017/02/15/nested-coroutines-in-unity/) when thinking about how to sequence dialogue. I started by defining an operation to sequence two coroutines in order:
```
public static IEnumerator SequenceCoroutines(IEnumerator first, IEnumerator second)
{
    yield return first;
    yield return second;
}
```
Imagine we have a basic coroutine that logs a dialogue line, then waits for a user to press "Space"
```
public static IEnumerator SpeechBubble(string text)
{
    Debug.Log(text);
    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
}
```
Stitching these speech bubbles together shouldn't be tricky now. To explain with an awful diagram, imagine we want to sequence IEnumerators:
A = SpeechBubble("Hi Mae!"), B = A = SpeechBubble("How are u"), C = SpeechBubble("Bye!")
Here's a diagram of iEnumerators A and B - both just blocks that wait for user input.
[Fig1]
Then here's how we visualize the output of OutputIEnum = SequenceCoroutines(A, B)
[Fig2]
Finally we want to add on C, so FinalIEnum = SequenceCoroutines(OutputIEnum, C)
[Fig3]
That final iEnum, when executed will play the lines "Hi Mae! | How are u | Bye!", in order, waiting for the user to press Space at each break.
In summary, say we have a List<IEnumerator> Blocks - then we can sequence them into a single coroutine by doing: `var result = blocks.Aggregate(SequenceCoroutines)`. From here on out we can visualize dialogue as a sequence of coroutine "blocks" we're executing in order - whether those are dialogue bubbles, animations, or so forth. As long as its a coroutine it can be a part of our dialogue system.
# Creating a Parser
(See Dialogue.cs for a full breakdown of the dialogue syntax)
Above we defined dialogue as a sequence of coroutines. The parser's job, then, is to convert blocks of written text into coroutines. Playing creating speech bubbles, dialogue choices, playing animations, and setting variables/flags were all made into coroutines. Ideally I should've translated the text into an AST, then defined functions that convert nodes into coroutines, but the parsing so far isn't complicated enought to warrant that (though caching an AST would be neat). The implementation details here are pretty boring, besides handling choices.
