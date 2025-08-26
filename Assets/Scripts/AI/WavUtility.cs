using System;
using System.IO;
using UnityEngine;

namespace HoloAI
{
    public static class WavUtility
    {
        /// <summary>
        /// Converts an AudioClip (PCM float) into WAV byte[].
        /// Only the first `samples` frames are exported.
        /// </summary>
        public static byte[] FromAudioClip(AudioClip clip, int samples, int channels, int sampleRate)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int bytesPerSample = 2; // 16-bit PCM
                int byteRate = sampleRate * channels * bytesPerSample;
                int dataSize = samples * channels * bytesPerSample;

                // ---- WAV header ----
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);                // Subchunk1Size
                writer.Write((short)1);          // PCM
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)(channels * bytesPerSample));
                writer.Write((short)16);         // bits per sample

                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                // ---- PCM samples ----
                float[] buffer = new float[samples * channels];
                clip.GetData(buffer, 0);

                foreach (var f in buffer)
                {
                    short s = (short)Mathf.Clamp(f * short.MaxValue, short.MinValue, short.MaxValue);
                    writer.Write(s);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
