using UnityEngine;

public class StationSelectionControllerV5 : MonoBehaviour
{
    public GreenTrail greenTrail;

    private StationObjectManipulatorV5 currentSelected;

    public void SelectStation(StationObjectManipulatorV5 manip)
    {
        if (manip == null || greenTrail == null)
            return;

        currentSelected = manip;

        if (manip.rectStationObject != null)
        {
            greenTrail.pointB = manip.rectStationObject;
            Debug.Log($"Selected Station ID: {manip.idText.text}");
        }
    }
}