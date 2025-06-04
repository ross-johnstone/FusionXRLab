using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class RemoteAvatarConfig
{
    public string avatarUrl;
}

public class DynamicAvatarLoader : MonoBehaviour
{
    [Tooltip("URL to JSON config. Leave empty to read from avatarconfig.txt.")]
    public string configUrl = "";

    void Start()
    {
        StartCoroutine(LoadAvatarConfig());
    }

    IEnumerator LoadAvatarConfig()
    {
        // Determine final config URL
        if (string.IsNullOrWhiteSpace(configUrl))
        {
            string path = Path.Combine(Application.persistentDataPath, "avatarconfig.txt");
            if (File.Exists(path))
            {
                string color = File.ReadAllText(path).Trim();
                configUrl = $"https://ross-johnstone.github.io/avatar-configs/{color}.json";
            }
            else
            {
                Debug.LogError("No avatarconfig.txt found and no configUrl set.");
                yield break;
            }
        }

        // Download the JSON config
        UnityWebRequest request = UnityWebRequest.Get(configUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch avatar config: " + request.error);
            yield break;
        }

        RemoteAvatarConfig config = JsonUtility.FromJson<RemoteAvatarConfig>(request.downloadHandler.text);
        if (string.IsNullOrEmpty(config.avatarUrl))
        {
            Debug.LogError("avatarUrl missing from JSON config.");
            yield break;
        }

        // Wait until Ubiq has spawned the avatar with the loader
        Component loader = null;
        float timeout = 10f;
        float timer = 0f;

        while (loader == null && timer < timeout)
        {
            loader = GameObject
                .FindObjectsOfType<Component>()
                .FirstOrDefault(c => c.GetType().Name == "UbiqReadyPlayerMeLoader");

            timer += Time.deltaTime;
            yield return null;
        }

        if (loader == null)
        {
            Debug.LogError("UbiqReadyPlayerMeLoader not found in scene.");
            yield break;
        }

        var type = loader.GetType();

        // Destroy the default avatar GameObject if already spawned
        var avatarField = type.GetField("avatar", BindingFlags.NonPublic | BindingFlags.Instance);
        if (avatarField != null)
        {
            GameObject existingAvatar = avatarField.GetValue(loader) as GameObject;
            if (existingAvatar != null)
            {
                Destroy(existingAvatar);
                avatarField.SetValue(loader, null);
                Debug.Log("Destroyed default avatar that was already spawned.");
            }
        }

        // Inject your custom avatar URL
        var avatarUrlField = type.GetField("avatarUrl", BindingFlags.NonPublic | BindingFlags.Instance);
        if (avatarUrlField == null)
        {
            Debug.LogError("avatarUrl field not found via reflection.");
            yield break;
        }

        avatarUrlField.SetValue(loader, config.avatarUrl);
        Debug.Log($"Injected avatarUrl: {config.avatarUrl}");

        // Call Load(string, bool)
        var loadMethod = type.GetMethod("Load", new[] { typeof(string), typeof(bool) });
        if (loadMethod == null)
        {
            Debug.LogError("Load(string, bool) method not found via reflection.");
            yield break;
        }

        try
        {
            loadMethod.Invoke(loader, new object[] { config.avatarUrl, false });
            Debug.Log("Correct avatar loading triggered.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to invoke Load method: " + e);
        }
    }
}
