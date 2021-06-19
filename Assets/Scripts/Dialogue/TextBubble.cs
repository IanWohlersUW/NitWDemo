using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class TextBubble : MonoBehaviour
{
    public TextMeshPro words;
    public SpriteRenderer bubble;
    public SpriteRenderer arrow;
    public List<TMP_FontAsset> font; // These fonts are cycled through!

    public IEnumerator AnimateFont(float delay)
    {
        int i = 0;
        while (true)
        {
            i = i % font.Count;
            words.font = font[i];
            i++;
            yield return new WaitForSeconds(delay);
        }
    }

    private void Start()
    {
        StartCoroutine(AnimateFont(0.25f));
    }
    private void Update()
    {
        Position(); // Might be gross to have the words contstantly scaling
    }

    private void Position()
    {
        // All of this sucks! So many magic numbers grrr

        // Scale the bubble to fix text
        var height = 1.5f + 0.35f * words.textInfo.lineCount;
        bubble.gameObject.transform.localScale = new Vector3(1, height, 1);
        // Is there a better way to do this programtically and maintain margins?

        var displacement = bubble.gameObject.transform.localScale.y + 0.75f;
        // Move the bubble up so the arrow is still under it
        bubble.transform.localPosition = Vector3.up * displacement;
        words.transform.localPosition = bubble.transform.localPosition;
    }
}
