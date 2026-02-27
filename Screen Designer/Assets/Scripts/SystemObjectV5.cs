using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class SystemObjectV5 : MonoBehaviour
{
    [Header("Prefabs & Parent")]
    public GameObject stationObjectPrefab;         // The visual Station Object prefab
    public Transform parentStation;                // Canvas or container for station objects
    public GameObject manipulatorPrefab;           // Prefab: Manipulator_StationObject
    public Transform parentManipulator;            // Parent container for manipulators (UI panel)

    [Header("CSV Loader Reference")]
    public CSVObjectLoader csvLoader;
    public int startID;
    public int lastID;

    [Header("Options")]
    public bool autoClearBeforeSpawn = true;

    [Header("Debug / UI")]
    public Text statusText;

    [Header("Green Trail Reference")]
    public GreenTrail myGreenTrail;

    [Header("Toggle Group Reference")]
    public ToggleGroup stationToggleGroup;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<GameObject> spawnedManipulators = new List<GameObject>();

    // ==============================
    // PUBLIC BUTTON ENTRY
    // ==============================
    public void SpawnObjectsFromCSV()
    {
        if (!ValidateReferences())
            return;

        if (csvLoader.objects.Count == 0)
        {
            Debug.LogWarning("[SystemObject] CSV contains no objects.");
            UpdateStatus("CSV is empty.");
            return;
        }

        if (autoClearBeforeSpawn)
            ClearSpawnedObjects();

        // -----------------------------
        // Calculate Start and Last ID
        // -----------------------------
        int minID = int.MaxValue;
        int maxID = int.MinValue;

        foreach (var data in csvLoader.objects)
        {
            if (data.id < minID)
                minID = data.id;

            if (data.id > maxID)
                maxID = data.id;
        }

        startID = minID;
        lastID = maxID;

        Debug.Log($"[SystemObject] StartID: {startID} | LastID: {lastID}");

        int spawnCount = 0;

        foreach (var data in csvLoader.objects)
        {
            // ----------------------
            // 1. Spawn Station Object
            // ----------------------
            GameObject newObj = Instantiate(stationObjectPrefab, parentStation);
            newObj.name = $"StationObject_{data.id}";

            MyObjectID idComponent = newObj.GetComponent<MyObjectID>();
            if (idComponent == null)
            {
                Debug.LogError("[SystemObject] Prefab missing MyObjectID component.");
                Destroy(newObj);
                continue;
            }

            idComponent.myID = data.id;
            if (idComponent.primaryName != null)
                idComponent.primaryName.text = data.primaryName;
            if (idComponent.secondaryName != null)
                idComponent.secondaryName.text = data.secondaryName;

            spawnedObjects.Add(newObj);

            // ----------------------
            // 2. Spawn Manipulator
            // ----------------------
            if (manipulatorPrefab != null && parentManipulator != null)
            {
                GameObject manipObj = Instantiate(manipulatorPrefab, parentManipulator);
                manipObj.name = $"Manipulator_{data.id}";

                StationObjectManipulatorV5 manipScript = manipObj.GetComponent<StationObjectManipulatorV5>();
                if (manipScript != null)
                {
                    manipScript.greenTrail = myGreenTrail;
                    manipScript.Initialize(idComponent); // Link manipulator to the station object
                    manipScript.SetToggleGroup(stationToggleGroup);
                    spawnedManipulators.Add(manipObj);
                }
                else
                {
                    Debug.LogError("[SystemObject] Manipulator prefab missing StationObjectManipulator script.");
                    Destroy(manipObj);
                }
            }

            spawnCount++;
            Debug.Log($"[SystemObject] Spawned StationObject {newObj.name} | ID: {data.id}");
        }

        UpdateStatus($"Spawned {spawnCount} objects.");
    }

    // ==============================
    // CLEAR FUNCTION
    // ==============================
    public void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
            if (obj != null)
                Destroy(obj);
        spawnedObjects.Clear();

        foreach (var manip in spawnedManipulators)
            if (manip != null)
                Destroy(manip);
        spawnedManipulators.Clear();

        UpdateStatus("Cleared spawned objects and manipulators.");
    }

    // ==============================
    // VALIDATION
    // ==============================
    private bool ValidateReferences()
    {
        if (stationObjectPrefab == null)
        {
            Debug.LogError("[SystemObject] stationObjectPrefab is missing.");
            return false;
        }

        if (parentStation == null)
        {
            Debug.LogError("[SystemObject] parentStation is missing.");
            return false;
        }

        if (csvLoader == null)
        {
            Debug.LogError("[SystemObject] csvLoader reference is missing.");
            return false;
        }

        if (manipulatorPrefab == null)
        {
            Debug.LogWarning("[SystemObject] manipulatorPrefab not assigned. Manipulators will not be created.");
        }

        if (parentManipulator == null && manipulatorPrefab != null)
        {
            Debug.LogWarning("[SystemObject] parentManipulator not assigned. Manipulators will not be created.");
        }

        return true;
    }

    // ==============================
    // STATUS UPDATE
    // ==============================
    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}