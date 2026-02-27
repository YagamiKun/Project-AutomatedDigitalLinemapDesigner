using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Assertions.Must;

public class StationObjectManipulatorV5 : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField primaryInput;
    public TMP_InputField secondaryInput;
    public TMP_Text idText;
    public Toggle myToggle;
    public int myID;

    [Header("Station Object Reference")]
    public GreenTrail greenTrail;
    public RectTransform rectStationObject;  // NEW: stores RectTransform of linked StationObject
    public bool boolToCurrentStation;

    [Header("Station Selection Controller Reference")]
    public StationObjectManipulatorV5 currentSelected;

    private MyObjectID linkedObject;

    /// <summary>
    /// Initialize the manipulator with a target StationObject
    /// </summary>
    public void Initialize(MyObjectID target)
    {
        linkedObject = target;

        if (linkedObject == null)
        {
            Debug.LogError("[StationObjectManipulator] Initialize called with null target.");
            return;
        }

        // Assign RectTransform reference
        rectStationObject = linkedObject.GetComponent<RectTransform>();
        if (rectStationObject == null)
            Debug.LogWarning("[StationObjectManipulator] Linked StationObject has no RectTransform.");

        // Populate UI fields
        idText.text = target.myID.ToString();
        myID = int.Parse(idText.text);

        // Pass PointA to GreenTrail reference
        if (myID == 1)
        {
            greenTrail.pointA = rectStationObject;
        }
        primaryInput.text = target.primaryName != null ? target.primaryName.text : "";
        secondaryInput.text = target.secondaryName != null ? target.secondaryName.text : "";

        // Register listeners
        primaryInput.onValueChanged.AddListener(OnPrimaryChanged);
        secondaryInput.onValueChanged.AddListener(OnSecondaryChanged);
    }

    public void SetToggleGroup(ToggleGroup group)
    {
        if (myToggle != null)
            myToggle.group = group;
    }

    private void OnPrimaryChanged(string value)
    {
        if (linkedObject != null && linkedObject.primaryName != null)
            linkedObject.primaryName.text = value;
    }

    private void OnSecondaryChanged(string value)
    {
        if (linkedObject != null && linkedObject.secondaryName != null)
            linkedObject.secondaryName.text = value;
    }

  

    public void SetValueChange()
    {
       
        

        boolToCurrentStation = myToggle.isOn;
        if (myToggle==true)
        {
            greenTrail.pointB = rectStationObject;
            //setToCurrentStation=true;
        }

        if (myToggle != true)
        {
            //setToCurrentStation = false;
        }
    }
}