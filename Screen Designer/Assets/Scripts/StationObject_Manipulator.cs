using UnityEngine;
using TMPro;

public class StationObjectManipulator : MonoBehaviour
{
    public TMP_InputField primaryInput;
    public TMP_InputField secondaryInput;
    public TMP_Text idText;
    

    private MyObjectID linkedObject;

    public void Initialize(MyObjectID target)
    {
        linkedObject = target;

        idText.text = target.myID.ToString();
        primaryInput.text = target.primaryName.text;
        secondaryInput.text = target.secondaryName.text;

        primaryInput.onValueChanged.AddListener(OnPrimaryChanged);
        secondaryInput.onValueChanged.AddListener(OnSecondaryChanged);
    }

    void OnPrimaryChanged(string value)
    {
        if (linkedObject != null && linkedObject.primaryName != null)
            linkedObject.primaryName.text = value;
    }

    void OnSecondaryChanged(string value)
    {
        if (linkedObject != null && linkedObject.secondaryName != null)
            linkedObject.secondaryName.text = value;
    }
}