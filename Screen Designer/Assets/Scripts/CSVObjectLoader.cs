using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SFB;

public class CSVObjectLoader : MonoBehaviour
{
    [System.Serializable]
    public class GameObjectData
    {
        public int id;
        public string primaryName;
        public string secondaryName;
        public int graphicId;
    }

    public List<GameObjectData> objects = new List<GameObjectData>();
    public Dictionary<int, GameObjectData> objectLookup = new Dictionary<int, GameObjectData>();

    public void OpenCSVFile()
    {
        var extensions = new[]
        {
            new ExtensionFilter("CSV Files", "csv"),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select Object CSV File",
            "",
            extensions,
            false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            LoadCSV(paths[0]);
        }
    }

    private void LoadCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found.");
            return;
        }

        objects.Clear();
        objectLookup.Clear();

        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length <= 1)
        {
            Debug.LogWarning("CSV file contains no data.");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');

            if (values.Length < 4)
            {
                Debug.LogWarning($"Invalid row at line {i + 1}");
                continue;
            }

            if (!int.TryParse(values[0], out int parsedId))
            {
                Debug.LogWarning($"Invalid ID at line {i + 1}");
                continue;
            }

            if (!int.TryParse(values[3], out int parsedGraphicId))
            {
                Debug.LogWarning($"Invalid Graphic ID at line {i + 1}");
                continue;
            }

            if (objectLookup.ContainsKey(parsedId))
            {
                Debug.LogWarning($"Duplicate ID detected: {parsedId}");
                continue;
            }

            GameObjectData data = new GameObjectData
            {
                id = parsedId,
                primaryName = values[1].Trim(),
                secondaryName = values[2].Trim(),
                graphicId = parsedGraphicId
            };

            objects.Add(data);
            objectLookup.Add(parsedId, data);
        }

        ValidateIDSequence();

        Debug.Log($"CSV Loaded Successfully. Total Objects: {objects.Count}");
    }

    private void ValidateIDSequence()
    {
        for (int i = 1; i <= objects.Count; i++)
        {
            if (!objectLookup.ContainsKey(i))
            {
                Debug.LogWarning($"Missing ID in sequence: {i}");
            }
        }
    }

    public GameObjectData GetObjectByID(int id)
    {
        if (objectLookup.TryGetValue(id, out GameObjectData data))
            return data;

        Debug.LogWarning($"Object with ID {id} not found.");
        return null;
    }
}
