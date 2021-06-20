using UnityEngine;
using System.Collections;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Dialogue dialogueExample;
    public bool canMove;

    private void Awake()
    {
        instance = this; // Hacky singleton pattern but its fine
        canMove = true;
        Debug.Log("Press Space to start dialogue! Press Z to progress diloauge. Arrows to choose");
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove && Input.GetKeyDown(KeyCode.Space))
        {
            var dialogue = dialogueExample.CreateCoroutine();
            StartCoroutine(DisableControlsUntilDone(dialogue));
        }
    }

    IEnumerator DisableControlsUntilDone(IEnumerator sequence)
    {
        canMove = false;
        yield return sequence;
        canMove = true;
    }
}
