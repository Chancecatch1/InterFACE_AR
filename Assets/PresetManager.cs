// Saving, Loading and Resetting the coordinates of the objects in the scene (mj)

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class PoseData { public Vector3 pos; public Quaternion rot; public Vector3 scale; }
[Serializable] public class ObjectPose { public string name; public PoseData world; } // world-space pose only
[Serializable] public class PresetPayload { public string name = "default"; public List<ObjectPose> objects = new List<ObjectPose>(); }

public class PresetManager : MonoBehaviour
{
    [Header("Targets to save/load")]
    public List<Transform> targets = new List<Transform>();

    // scene default (for reset)
    struct ScenePose { public Vector3 pos; public Quaternion rot; public Vector3 scale; }
    Dictionary<Transform, ScenePose> _sceneDefaults = new Dictionary<Transform, ScenePose>();

    #if UNITY_EDITOR
    [Header("Save Path (Editor only)")]
    public bool useProjectFolderInEditor = true;

    string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    string PresetDir => useProjectFolderInEditor
        ? Path.Combine(ProjectRoot, "Presets") //CPR_WithPad/Presets
        : Path.Combine(Application.persistentDataPath, "Presets");
    #else
    string PresetDir => Path.Combine(Application.persistentDataPath, "Presets");
    #endif

    string PresetPath(string name = "default") => Path.Combine(PresetDir, name + ".json");

    void Start()
    {
        foreach (var t in targets)
        {
            if (t == null) continue;
            _sceneDefaults[t] = new ScenePose { pos = t.position, rot = t.rotation, scale = t.localScale };
        }
    }

    public void SaveButton() => SavePreset("default");
    public void LoadButton() => LoadPreset("default");
    public void ResetButton() => ResetToSceneDefaults();

    public void SavePreset(string name)
    {
        var payload = new PresetPayload { name = name, objects = new List<ObjectPose>() };

        foreach (var t in targets)
        {
            if (t == null) continue;
            var pose = new PoseData
            {
                pos = t.position,
                rot = t.rotation,
                scale = t.localScale
            };
            payload.objects.Add(new ObjectPose { name = t.name, world = pose });
        }

        Directory.CreateDirectory(PresetDir);
        var json = JsonUtility.ToJson(payload, true);
        File.WriteAllText(PresetPath(name), json);
        Debug.Log("Preset saved: " + PresetPath(name));
    }

    public void LoadPreset(string name)
    {
        var path = PresetPath(name);
        if (!File.Exists(path)) { Debug.LogWarning("Preset not found: " + path); return; }

        var payload = JsonUtility.FromJson<PresetPayload>(File.ReadAllText(path));
        foreach (var op in payload.objects)
        {
            var t = targets.Find(x => x != null && x.name == op.name); // name based matching
            if (t == null) continue;
            if (op.world == null)
            {
                Debug.LogWarning($"Preset object '{op.name}' has no world pose. Please re-save the preset.");
                continue;
            }

            t.position = op.world.pos;
            t.rotation = op.world.rot;
            t.localScale = op.world.scale;
        }
        Debug.Log("Preset loaded: " + path);
    }

    public void ResetToSceneDefaults()
    {
        foreach (var kv in _sceneDefaults)
        {
            var t = kv.Key;
            if (t == null) continue;
            var p = kv.Value;
            t.position = p.pos; t.rotation = p.rot; t.localScale = p.scale;
        }
        Debug.Log("Preset reset to scene defaults.");
    }
}