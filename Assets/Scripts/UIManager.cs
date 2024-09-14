using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    [SerializeField]
    private Text resultText;

    public void SetResultText(string text) {
        resultText.text = text;
    }
}