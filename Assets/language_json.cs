using UnityEngine;
using System;
using System.IO;

static public class InstructionFinder {
    // private static string path = Application.dataPath + "/en.json";
    // private static string JSONString = File.ReadAllText(path);
    private static string EN_JSONString = Resources.Load<TextAsset>("en").ToString();
    private static string FR_JSONString = Resources.Load<TextAsset>("fr").ToString();
    private static string IT_JSONString = Resources.Load<TextAsset>("it").ToString();

    static public string FindByTag (string a, string lang) {
        SimpleJSON.JSONNode json = SimpleJSON.JSON.Parse(EN_JSONString);
        if (lang == "fr") {
            json = SimpleJSON.JSON.Parse(FR_JSONString);
        } else if (lang == "it") {
            json = SimpleJSON.JSON.Parse(IT_JSONString);
        }

        string instruction = json[a];
        Debug.Log(instruction);
        if (instruction != null) {
            return instruction;
        } else {
            return "Undefined instruction: " + a;
        }
    }
}