using UnityEngine;
using Ubiq.Messaging;
using System.Collections.Generic;

public class AudioController : MonoBehaviour
{
    private AudioSource roomAudioSource;
    private AudioClip[] availableAudioClips;
    private NetworkContext context;
    private bool isPlaying = false;

    public bool IsPlaying => isPlaying;
    public NetworkId Id { get; } = NetworkId.Unique();

    void Start()
    {
        // Register for network messages
        context = NetworkScene.Register(this);
        Debug.Log($"[AudioController] Network: Registered with ID {context.Id}");

        // Setup audio source
        roomAudioSource = gameObject.AddComponent<AudioSource>();
        roomAudioSource.spatialBlend = 1f;
        roomAudioSource.minDistance = 1f;
        roomAudioSource.maxDistance = 20f;
        roomAudioSource.rolloffMode = AudioRolloffMode.Linear;
        roomAudioSource.playOnAwake = false;
        roomAudioSource.loop = false;
    }

    public void InitializeAudio(AudioClip[] clips)
    {
        availableAudioClips = clips;
        Debug.Log($"[AudioController] Initialized with {clips.Length} audio clips");
    }

    public void PlayClip(int clipIndex)
    {
        try
        {
            if (availableAudioClips == null || 
                availableAudioClips.Length == 0 || 
                clipIndex < 0 || 
                clipIndex >= availableAudioClips.Length)
            {
                Debug.LogWarning("[AudioController] No valid audio clip selected");
                return;
            }

            AudioClip selectedClip = availableAudioClips[clipIndex];
            if (selectedClip == null)
            {
                Debug.LogWarning("[AudioController] Selected audio clip is null");
                return;
            }

            // Send the play command to all clients
            if (context.Id.Valid)
            {
                var message = new AudioMessage
                {
                    clipIndex = clipIndex,
                    shouldPlay = true
                };
                context.SendJson(message);
                Debug.Log($"[AudioController] Network: Sent audio play command for clip {selectedClip.name}");
            }
            else
            {
                Debug.LogWarning("[AudioController] Network: Context invalid - cannot send audio command");
            }

            // Play locally
            PlayAudioLocally(selectedClip);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AudioController] Error playing audio: {e.Message}\n{e.StackTrace}");
        }
    }

    public void StopAudio()
    {
        try
        {
            // Send the stop command to all clients
            if (context.Id.Valid)
            {
                var message = new AudioMessage
                {
                    clipIndex = -1,
                    shouldPlay = false
                };
                context.SendJson(message);
                Debug.Log("[AudioController] Network: Sent audio stop command");
            }

            // Stop locally
            if (roomAudioSource != null && roomAudioSource.isPlaying)
            {
                roomAudioSource.Stop();
                isPlaying = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AudioController] Error stopping audio: {e.Message}\n{e.StackTrace}");
        }
    }

    private void PlayAudioLocally(AudioClip clip)
    {
        if (roomAudioSource != null)
        {
            if (roomAudioSource.isPlaying)
            {
                roomAudioSource.Stop();
            }

            roomAudioSource.clip = clip;
            roomAudioSource.Play();
            isPlaying = true;
            Debug.Log($"[AudioController] Playing audio clip: {clip.name}");
        }
    }

    private struct AudioMessage
    {
        public int clipIndex;
        public bool shouldPlay;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        try
        {
            var msg = message.FromJson<AudioMessage>();
            Debug.Log($"[AudioController] Network: Received audio command - Play: {msg.shouldPlay}, Clip Index: {msg.clipIndex}");

            if (msg.shouldPlay && msg.clipIndex >= 0 && msg.clipIndex < availableAudioClips.Length)
            {
                AudioClip clip = availableAudioClips[msg.clipIndex];
                if (clip != null)
                {
                    PlayAudioLocally(clip);
                }
            }
            else
            {
                if (roomAudioSource != null && roomAudioSource.isPlaying)
                {
                    roomAudioSource.Stop();
                    isPlaying = false;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AudioController] Error processing audio message: {e.Message}\n{e.StackTrace}");
        }
    }

    void Update()
    {
        // Check if audio has finished playing
        if (isPlaying && roomAudioSource != null && !roomAudioSource.isPlaying)
        {
            isPlaying = false;
            Debug.Log("[AudioController] Audio clip finished playing");
        }
    }
} 