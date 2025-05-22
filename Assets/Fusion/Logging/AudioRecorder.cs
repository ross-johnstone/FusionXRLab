using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class AudioRecorder : MonoBehaviour
{
    private Dictionary<string, AudioClip> recordedClips = new Dictionary<string, AudioClip>();
    private string savePath;
    private string targetMicName;
    private Dictionary<string, int> recordingPositions = new Dictionary<string, int>();

    private const int MaxRecordingLength = 300; // 5 minutes

    void Start()
    {
        savePath = Application.persistentDataPath;
        Debug.Log("Save path: " + savePath);

#if UNITY_ANDROID
        StartCoroutine(RequestMicPermissionAndStart());
#else
        StartRecordingFromAvailableDevice();
#endif
    }

#if UNITY_ANDROID
    IEnumerator RequestMicPermissionAndStart()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitForSeconds(1f);
        }

        StartRecordingFromAvailableDevice();
    }
#endif

    void StartRecordingFromAvailableDevice()
    {
        string[] recordingDevices = Microphone.devices;
        Debug.Log("Available Microphones: " + string.Join(", ", recordingDevices));

        if (recordingDevices.Length > 0)
        {
            targetMicName = recordingDevices[0];
            StartRecording(targetMicName);
        }
        else
        {
            Debug.LogError("No microphone devices found.");
        }
    }

    public void StartRecording(string deviceName)
    {
        if (!recordedClips.ContainsKey(deviceName))
        {
            // loop = false ensures it doesn’t wrap or duplicate
            AudioClip clip = Microphone.Start(deviceName, false, MaxRecordingLength, 44100);
            recordedClips[deviceName] = clip;
            Debug.Log($"Recording started on {deviceName}");
        }
    }

    public void StopRecording(string deviceName)
    {
        if (recordedClips.ContainsKey(deviceName))
        {
            if (Microphone.IsRecording(deviceName))
            {
                int position = Microphone.GetPosition(deviceName);
                Microphone.End(deviceName);
                SaveTrimmedRecording(deviceName, recordedClips[deviceName], position);
            }

            recordedClips.Remove(deviceName);
            Debug.Log($"Recording stopped on {deviceName}");
        }
    }

    private void SaveTrimmedRecording(string deviceName, AudioClip clip, int samplesRecorded)
    {
        if (clip == null || samplesRecorded <= 0) return;

        float[] data = new float[samplesRecorded * clip.channels];
        clip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create("Trimmed", samplesRecorded, clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        string safeName = deviceName.Replace(" ", "_").Replace("(", "").Replace(")", "");
        string filename = Path.Combine(savePath, $"{safeName}_Recording_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav");

        // Use your external wav saving method here
        SaveWav.Save(filename, trimmedClip);

        Debug.Log($"Recording saved: {filename} — Duration: {samplesRecorded / (float)clip.frequency:F2} seconds");
    }

    void OnApplicationQuit()
    {
        StopAllRecordings();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopAllRecordings();
        }
    }

    private void StopAllRecordings()
    {
        foreach (var device in new List<string>(recordedClips.Keys))
        {
            StopRecording(device);
        }
    }
}
