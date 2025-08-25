using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HoloAI
{
    /// <summary>
    /// Simple web TTS (MP3) via StreamElements. Plays through the provided AudioSource.
    /// On HoloLens device build, this comes out the headset speaker. During PC Remoting,
    /// audio plays from the PC (remoting limitation).
    /// </summary>
    public static class WindowsTTSSpeaker
    {
        private const string TtsUrl = "https://api.streamelements.com/kappa/v2/speech";

        public static void Speak(string text, AudioSource audioSource, Action onStart, Action onDone)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("[TTS] No AudioSource provided.");
                onDone?.Invoke();
                return;
            }

            // Make sure it's 2D so it’s clearly audible
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;

            audioSource.gameObject.GetComponent<MonoBehaviour>()
                .StartCoroutine(SpeakRoutine(text, audioSource, onStart, onDone));
        }

        private static IEnumerator SpeakRoutine(string text, AudioSource audioSource, Action onStart, Action onDone)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                onDone?.Invoke();
                yield break;
            }

            Debug.Log("[TTS] Requesting speech for: " + text);
            onStart?.Invoke();

            // StreamElements outputs MP3
            string url = TtsUrl + $"?voice=Brian&text={UnityWebRequest.EscapeURL(text)}";
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[TTS] Error: " + www.error);
                }
                else
                {
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log("[TTS] Playing clip, length " + clip.length + "s");
                }
            }

            onDone?.Invoke();
        }
    }
}
