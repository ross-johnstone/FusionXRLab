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
    [Tooltip("URL to JSON config. Leave empty to read from Assets/Fusion/Avatar/avatarconfig.txt.")]
    public string configUrl = "";

    private string fallbackAvatarUrl = "https://models.readyplayer.me/6628f7fe3f0967a5dea78574.glb";

    void Start()
    {
        StartCoroutine(LoadAvatarConfig());
    }

    IEnumerator LoadAvatarConfig()
    {
        string finalUrl = configUrl;

        if (string.IsNullOrWhiteSpace(finalUrl))
        {
            string path = Path.Combine(Application.dataPath, "Fusion/Avatar/avatarconfig.txt");

            if (File.Exists(path))
            {
                string color = File.ReadAllText(path).Trim();
                finalUrl = $"https://ross-johnstone.github.io/avatar-configs/{color}.json";
                Debug.Log($"Loaded config from text file: {color} ? {finalUrl}");
            }
            else
            {
                Debug.LogWarning("avatarconfig.txt not found. Using fallback avatar.");
                yield return LoadAvatarDirect(fallbackAvatarUrl);
                yield break;
            }
        }

        UnityWebRequest request = UnityWebRequest.Get(finalUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch avatar config: " + request.error);
            yield return LoadAvatarDirect(fallbackAvatarUrl);
            yield break;
        }

        RemoteAvatarConfig config = null;
        bool jsonFailed = false;

        try
        {
            config = JsonUtility.FromJson<RemoteAvatarConfig>(request.downloadHandler.text);
        }
        catch
        {
            Debug.LogError("Invalid JSON format in config file.");
            jsonFailed = true;
        }

        if (jsonFailed || config == null || string.IsNullOrEmpty(config.avatarUrl))
        {
            Debug.LogWarning("Falling back to default avatar.");
            yield return LoadAvatarDirect(fallbackAvatarUrl);
            yield break;
        }

        yield return LoadAvatarDirect(config.avatarUrl);
    }

    IEnumerator LoadAvatarDirect(string avatarUrl)
    {
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

        try
        {
            // Destroy existing avatar
            var avatarField = type.GetField("avatar", BindingFlags.NonPublic | BindingFlags.Instance);
            if (avatarField != null)
            {
                GameObject existingAvatar = avatarField.GetValue(loader) as GameObject;
                if (existingAvatar != null)
                {
                    Destroy(existingAvatar);
                    avatarField.SetValue(loader, null);
                    Debug.Log("Destroyed previous avatar.");
                }
            }

            // Set avatar URL
            var avatarUrlField = type.GetField("avatarUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            if (avatarUrlField == null)
            {
                Debug.LogError("avatarUrl field not found.");
                yield break;
            }

            avatarUrlField.SetValue(loader, avatarUrl);
            Debug.Log($"Set avatarUrl: {avatarUrl}");

            // Load avatar
            var loadMethod = type.GetMethod("Load", new[] { typeof(string), typeof(bool) });
            if (loadMethod == null)
            {
                Debug.LogError("Load(string, bool) method not found.");
                yield break;
            }

            loadMethod.Invoke(loader, new object[] { avatarUrl, false });
            Debug.Log("Avatar load triggered.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load avatar: " + e.Message);
        }
    }
}
