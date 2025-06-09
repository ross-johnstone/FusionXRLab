
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class AvatarConfigWriter : EditorWindow
{
    private string[] displayOptions = new[]
    {
        "🟠 Orange – Participant",
        "🟣 Purple – Participant",
        "🔴 Red – Participant",
        "💚 Green – Participant",
        "💙 Blue – Participant",
        "💖 Pink – Participant",
        "🟡 Yellow – Facilitator"
    };

    private string[] colorValues = new[]
    {
        "orange",
        "purple",
        "red",
        "green",
        "blue",
        "pink",
        "yellow"
    };

    private int selectedIndex = 0;

    [MenuItem("Tools/Avatar Config/Set Config")]
    public static void ShowWindow()
    {
        GetWindow<AvatarConfigWriter>("Avatar Config");
    }

    void OnGUI()
    {
        GUILayout.Label("Select Avatar Config", EditorStyles.boldLabel);
        selectedIndex = EditorGUILayout.Popup("Avatar Color", selectedIndex, displayOptions);

        if (GUILayout.Button("Write to avatarconfig.txt"))
        {
            string folderPath = Path.Combine(Application.dataPath, "Fusion/Avatar");
            string filePath = Path.Combine(folderPath, "avatarconfig.txt");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            File.WriteAllText(filePath, colorValues[selectedIndex]);
            Debug.Log($"✅ Wrote '{colorValues[selectedIndex]}' to avatarconfig.txt");
        }
    }
}
#endif