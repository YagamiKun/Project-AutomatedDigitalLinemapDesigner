using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class SystemObject : MonoBehaviour
{
    [Header("Prefabs & Parent")]
    public GameObject stationObjectPrefab;
    public Transform parentStation;

    [Header("CSV Loader Reference")]
    public CSVObjectLoader csvLoader;

    [Header("Options")]
    public bool autoClearBeforeSpawn = true;

    [Header("Debug / UI")]
    public Text statusText;

    private List<GameObject> spawnedObjects = new List<GameObject>();

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

        int spawnCount = 0;

        foreach (var data in csvLoader.objects)
        {
            GameObject newObj = Instantiate(stationObjectPrefab, parentStation);
            newObj.name = $"StationObject_{data.id}";

            MyObjectID idComponent = newObj.GetComponent<MyObjectID>();
            if (idComponent == null)
            {
                Debug.LogError($"[SystemObject] Prefab missing MyObjectID component.");
                Destroy(newObj);
                continue;
            }

            // Assign ID
            idComponent.myID = data.id;

            // Assign Text
            if (idComponent.primaryName != null)
                idComponent.primaryName.text = data.primaryName;

            if (idComponent.secondaryName != null)
                idComponent.secondaryName.text = data.secondaryName;

            // FUTURE: Hook for graphic assignment
            // AssignGraphic(idComponent, data.graphicId);

            spawnedObjects.Add(newObj);
            spawnCount++;

            Debug.Log($"[SystemObject] Spawned {newObj.name} | ID: {data.id}");
        }

        UpdateStatus($"Spawned {spawnCount} objects.");
    }

    // ==============================
    // CLEAR FUNCTION
    // ==============================
    public void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedObjects.Clear();

        UpdateStatus("Cleared spawned objects.");
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

    // ==============================
    // FUTURE EXTENSION (OPTIONAL)
    // ==============================
    /*
    private void AssignGraphic(MyObjectID idComponent, int graphicId)
    {
        // Example:
        // Sprite sprite = GraphicDatabase.GetSprite(graphicId);
        // if (idComponent.image1 != null)
        //     idComponent.image1.sprite = sprite;
    }
    */
}