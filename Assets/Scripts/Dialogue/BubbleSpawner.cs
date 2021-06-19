﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    public TextBubble bubblePrefab;

    public IEnumerator CreateBubble(string text, Vector3 position)
    {
        var bubble = Instantiate(bubblePrefab, position, Quaternion.identity);
        yield return new WaitForSeconds(0.1f);
        yield return FillBubble(text, bubble);
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        // then detroy the dialogue
        Destroy(bubble.gameObject);
    }

    public IEnumerator CreateBubble(string text, Transform character) =>
        CreateBubble(text, character.position + Vector3.up);

    public IEnumerator CreateChoiceBubble(List<string> choices, Vector3 position, Action<int> submitChoice)
    {
        var bubble = Instantiate(bubblePrefab, position, Quaternion.identity);
        yield return new WaitForSeconds(0.1f);
        int choice = 0;
        while (!Input.GetKeyDown(KeyCode.Z))
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
                choice = (choice + 1) % choices.Count; // % so choices wrap around
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                choice = (choice - 1 + choices.Count) % choices.Count; // + count b/c negative undefined

            if (choices[choice] != bubble.words.text)
                yield return FillBubble(choices[choice], bubble); // Render new choices
            else
                yield return null;
        }
        submitChoice(choice); // let clients of this method decide how to handle the input
        Destroy(bubble.gameObject);
    }

    public IEnumerator CreateChoiceBubble(List<string> choices, Transform character, Action<int> submitChoice)
        => CreateChoiceBubble(choices, character.position + Vector3.up, submitChoice);

    private IEnumerator FillBubble(string text, TextBubble bubble)
    {
        float delay = 0.02f;
        bubble.words.text = "";
        for (int i = 0; i < text.Length; i++)
        {
            bubble.words.text += text[i];
            yield return new WaitForSeconds(delay);
        }
    }
}
