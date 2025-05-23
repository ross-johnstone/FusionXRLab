using System;
using System.IO;
using UnityEngine;

public static class SaveWav
{
    public static void Save(string filepath, AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("No audio clip to save!");
            return;
        }

        byte[] wavData = ConvertAudioClipToWav(clip);
        File.WriteAllBytes(filepath, wavData);
        Debug.Log($"Saved WAV file: {filepath}");
    }

    private static byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * short.MaxValue);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            bytesData[i * 2] = byteArr[0];
            bytesData[i * 2 + 1] = byteArr[1];
        }

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + bytesData.Length);
            writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
            writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            writer.Write(bytesData.Length);
            writer.Write(bytesData);

            return stream.ToArray();
        }
    }
}