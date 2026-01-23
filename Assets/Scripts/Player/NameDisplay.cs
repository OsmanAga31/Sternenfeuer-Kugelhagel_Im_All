using UnityEngine;
using TMPro;
//using JetBrains.Annotations;

public class NameDisplay : MonoBehaviour
{
    public TextMeshProUGUI nameText; // reference to the TextMeshProUGUI component

    // this function is called when setting the player's name
    public void SetName(string name)
    {
        nameText.text = name;
    }
}
