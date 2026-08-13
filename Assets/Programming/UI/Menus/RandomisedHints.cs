using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RandomisedHints : MonoBehaviour
{
    [SerializeField] List<string> HintTexts = new List<string>();

    public TextMeshProUGUI HintText;

    public int currentHintIndex = -1;

    // Start is called before the first frame update
    void OnEnable()
    {
        RandomiseHint();
    }

    public void RandomiseHint()
    {
        int randomHint = Random.Range(0, HintTexts.Count);
        if (randomHint == currentHintIndex)
        {
            randomHint++;
            if(randomHint >  HintTexts.Count - 1)
            {
                randomHint = 0;
            }
        }
        currentHintIndex = randomHint;

        string hintText = HintTexts[randomHint];

        HintText.text = hintText;
    }
}