using UnityEngine;
public class GameManager : MonoBehaviour
{
    public BubbleSpawner bubbleSpawner;

    public static GameManager instance;
    public Dialogue dialogueExample;
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(dialogueExample.CreateCoroutine());
        }
    }
}
