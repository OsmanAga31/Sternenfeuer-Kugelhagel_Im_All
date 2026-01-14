using UnityEngine;
using TMPro;
//using JetBrains.Annotations;

public class NameDisplay : MonoBehaviour
{
    public TextMeshProUGUI nameText; // reference to the TextMeshProUGUI component
    //Camera cam; // reference to the main camera

    //void Start()
    //{
    //    cam = Camera.main;
    //}

    // LateUpdate is called after all Update functions have been called
    //void LateUpdate()
    //{
    //    if (cam == null) return;

    //    // make the nameText face the camera
    //    transform.LookAt(cam.transform);
    //    transform.Rotate(0, 180, 0); // to face the camera correctly
    //}

    // this function is called when setting the player's name
    public void SetName(string name)
    {
        nameText.text = name;
    }
}
