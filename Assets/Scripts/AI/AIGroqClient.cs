using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.IO;
using TMPro;

namespace HoloAI
{
    /// <summary>
    /// Groq client: uploads WAV → transcribes with Whisper → chats with Llama3.
    /// </summary>
    public static class AIGroqClient
    {
        private const string ChatEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private const string SttEndpoint  = "https://api.groq.com/openai/v1/audio/transcriptions";

        /// <param name="onStt">
        ///   Called with the STT transcript; return the final userPrompt string to send to chat,
        ///   or return null to abort (e.g., transcript too short).
        /// </param>
        public static IEnumerator TranscribeAndChat(
            string apiKey,
            string sttModel,
            string chatModel,
            byte[] wavData,
            string systemPrompt,
            Func<string, string> onStt,
            Action<string> onOk,
            Action<string> onFail)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onFail?.Invoke("Groq API key is empty.");
                yield break;
            }

            // ---- 1) Speech-to-text (Whisper) ----
            var form = new WWWForm();
            form.AddField("model", sttModel);
            form.AddBinaryData("file", wavData, "speech.wav", "audio/wav");

            using (var sttReq = UnityWebRequest.Post(SttEndpoint, form))
            {
                sttReq.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return sttReq.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (sttReq.result != UnityWebRequest.Result.Success)
#else
                if (sttReq.isNetworkError || sttReq.isHttpError)
#endif
                {
                    onFail?.Invoke("STT error: " + sttReq.error + "\n" + sttReq.downloadHandler.text);
                    yield break;
                }

                string sttJson = sttReq.downloadHandler.text;
                string transcript = "";
                try
                {
                    var parsed = JSON.Parse(sttJson);
                    transcript = parsed?["text"]?.Value ?? "";
                }
                catch (Exception e)
                {
                    onFail?.Invoke("STT parse error: " + e.Message + "\nRaw: " + sttJson);
                    yield break;
                }

                string userPrompt = onStt?.Invoke(transcript);
                if (string.IsNullOrEmpty(userPrompt))
                    yield break; // caller decided to abort

                // ---- 2) Chat completion ----
                yield return ChatOnce(apiKey, chatModel, systemPrompt, userPrompt, onOk, onFail);
            }
        }

        public static IEnumerator ChatOnce(
            string apiKey,
            string model,
            string systemPrompt,
            string userPrompt,
            Action<string> onOk,
            Action<string> onFail)
        {
            var root = new JSONObject();
            root["model"] = model;

            var msgs = new JSONArray();

            var sys = new JSONObject();
            sys["role"] = "system";
            sys["content"] = systemPrompt;
            msgs.Add(sys);

            var user = new JSONObject();
            user["role"] = "user";
            user["content"] = userPrompt;
            msgs.Add(user);

            root["messages"]   = msgs;
            root["temperature"] = 0.1f;
            root["max_tokens"]  = 96;
            root["stream"]      = false;

            byte[] body = Encoding.UTF8.GetBytes(root.ToString());

            using (var req = new UnityWebRequest(ChatEndpoint, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    onFail?.Invoke("Chat error: " + req.error + "\n" + req.downloadHandler.text);
                    yield break;
                }

                string json = req.downloadHandler.text;
                try
                {
                    var parsed  = JSON.Parse(json);
                    var content = parsed?["choices"]?[0]?["message"]?["content"]?.Value;
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        onFail?.Invoke("Empty chat response.");
                    }
                    else
                    {
                        onOk?.Invoke(content.Trim());
                    }
                }
                catch (Exception e)
                {
                    onFail?.Invoke("Chat parse error: " + e.Message + "\nRaw: " + json);
                }
            }
        }
    }
}
// key provider (mj)
public static class GroqKeyProvider
{
    private const string FileName = "keys.json";

    public static string GetApiKey()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                JSONNode node = JSON.Parse(json);
                string key = node?["groqApiKey"]?.Value;
                if (!string.IsNullOrEmpty(key)) return key;
            }
            catch { }
        }

        string pp = PlayerPrefs.GetString("GROQ_API_KEY", null);
        if (!string.IsNullOrEmpty(pp)) return pp;

        #if UNITY_EDITOR
        string env = System.Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (!string.IsNullOrEmpty(env)) return env;
        #endif

        return null;
    }

    public static bool SaveApiKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        try
        {
            JSONObject obj = new JSONObject();
            obj["groqApiKey"] = key.Trim();
            string path = Path.Combine(Application.persistentDataPath, FileName);
            File.WriteAllText(path, obj.ToString());
            PlayerPrefs.SetString("GROQ_API_KEY", key.Trim());
            PlayerPrefs.Save();
            return true;
        }
        catch { return false; }
    }
}