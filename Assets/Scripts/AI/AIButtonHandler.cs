using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;

namespace HoloAI
{
    public class AIButtonHandler : MonoBehaviour
    {
        [Header("Groq")]
        [SerializeField] private string groqApiKey = "YOUR_GROQ_KEY_HERE";
        [SerializeField] private string chatModel = "llama3-8b-8192";
        [SerializeField] private string sttModel = "whisper-large-v3"; // Groq STT

        [Header("Optional UI")]
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI transcriptLabel;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private AudioClip micClip;
        private bool isRecording;
        private string activeDevice = null;
        private int sampleRate = 16000;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

        private void OnDestroy()
        {
            if (isRecording) StopMicrophone();
        }

        // Button OnClick → hook this
        public void OnAIClick()
        {
            if (isRecording) StopAndProcess();
            else StartRecording();
        }

        private void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                SetStatus("❌ No microphone detected.");
                return;
            }

            // Pick the best device
            activeDevice = PickBestDevice();
            sampleRate = PickSafeSampleRate();

            micClip = Microphone.Start(activeDevice, false, 30, sampleRate);
            isRecording = true;

            SetStatus($"🎙️ Recording with [{activeDevice}] at {sampleRate} Hz… press again to stop");
        }

        private void StopAndProcess()
        {
            if (!isRecording) return;
            StartCoroutine(StopAndProcessRoutine());
        }

        private IEnumerator StopAndProcessRoutine()
        {
            isRecording = false;

            // wait for mic buffer to finalize
            yield return new WaitForSeconds(0.2f);

            int pos = Microphone.GetPosition(activeDevice);
            Microphone.End(activeDevice);

            if (pos <= 0 || micClip == null)
            {
                SetStatus("❌ No audio captured.");
                yield break;
            }

            float[] samples = new float[pos];
            micClip.GetData(samples, 0);

            // Debugging: check loudness
            float rms = Mathf.Sqrt(samples.Average(s => s * s));
            Debug.Log($"[AI] Recorded {pos} frames, ch={micClip.channels}, sr={micClip.frequency}, RMS={rms:F4}");
            DumpFirstSamples(samples, 10);

            if (rms < 0.01f)
            {
                SetStatus("Please try again, nothing was heard.");
                yield break;
            }

            byte[] wavData = WavUtility.FromAudioClip(micClip, pos, micClip.channels, micClip.frequency);

            string systemPrompt = "You are a concise CPR trainer assistant. Reply in ONE short sentence.";
            SetStatus("📝 Transcribing + answering…");

            yield return AIGroqClient.TranscribeAndChat(
                groqApiKey, sttModel, chatModel, wavData, systemPrompt,
                onStt: transcript =>
                {
                    Debug.Log("[AI] Transcript: " + transcript);
                    if (transcriptLabel != null) transcriptLabel.text = transcript;
                    return transcript; // send transcript to chat
                },
                onOk: reply =>
                {
                    Debug.Log("🤖 Groq Reply: " + reply);
                    SetStatus("AI: " + reply);
                    WindowsTTSSpeaker.Speak(reply, audioSource,
                        onStart: () => Debug.Log("[TTS] Speaking"),
                        onDone: () => SetStatus("Ready"));
                },
                onFail: err =>
                {
                    SetStatus("AI error");
                    Debug.LogError(err);
                });
        }

        private void StopMicrophone()
        {
            if (isRecording)
            {
                Microphone.End(activeDevice);
                isRecording = false;
            }
        }

        // === Helpers ===
        private string PickBestDevice()
        {
            // Prefer HoloLens or Windows default
            foreach (var d in Microphone.devices)
            {
                if (d.ToLower().Contains("hololens")) return d;
            }
            return Microphone.devices[0];
        }

        private int PickSafeSampleRate()
        {
            // Try 16k (good for Whisper). If unsupported, Unity will fallback internally.
            return 16000;
        }

        private void DumpFirstSamples(float[] data, int count)
        {
            var preview = data.Take(count).Select(v => v.ToString("F4"));
            Debug.Log("[AI] First samples: " + string.Join(", ", preview));
        }

        private void SetStatus(string s)
        {
            if (statusLabel != null) statusLabel.text = s;
            Debug.Log("[AI] " + s);
        }
    }
}
