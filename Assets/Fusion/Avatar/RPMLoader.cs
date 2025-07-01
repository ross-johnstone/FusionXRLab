//Add to avatar prefab RPM Body object if not already present

using System.IO;
using UnityEngine;

public class RPMLoader : MonoBehaviour
{
    private const string PREFS_FILE_NAME = "prefs";
    private const string PLAYER_PREFS_KEY = "avatars.readyplayerme.url";

    void Start()
    {
        var avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        if (avatar == null || !avatar.IsLocal)
        {
            return; // Don't load prefs or set URLs for remote peers
        }

        Debug.Log("[RPMLoader] Start called. Path: " + Application.persistentDataPath);

        string path = Path.Combine(Application.persistentDataPath, PREFS_FILE_NAME);

        if (File.Exists(path))
        {
            string fileContent = File.ReadAllText(path).Trim();
            Debug.Log($"[RPMLoader] Read from file: {fileContent}");

            if (fileContent.StartsWith("avatar_url="))
            {
                string url = fileContent.Substring("avatar_url=".Length).Trim();

                // Save to PlayerPrefs for future use  
                PlayerPrefs.SetString(PLAYER_PREFS_KEY, url);
                PlayerPrefs.Save();

                Debug.Log($"[RPMLoader] Parsed avatar URL: {url}");

                // Try to find the avatar sync component on this object or elsewhere  
                var avatarSync = GetComponent<ReadyPlayerMeSyncUrlAvatar>();
                if (avatarSync == null)
                {
                    avatarSync = Object.FindFirstObjectByType<ReadyPlayerMeSyncUrlAvatar>();
                }

                if (avatarSync != null)
                {
                    avatarSync.SetUrl(url);
                    Debug.Log("[RPMLoader] Avatar URL set on ReadyPlayerMeSyncUrlAvatar.");
                }
                else
                {
                    Debug.LogWarning("[RPMLoader] ReadyPlayerMeSyncUrlAvatar component not found in scene.");
                }
            }
            else
            {
                Debug.LogWarning("[RPMLoader] File content does not start with 'avatar_url='.");
            }
        }
        else
        {
            Debug.LogWarning($"[RPMLoader] File not found at {path}");
        }
    }
}
