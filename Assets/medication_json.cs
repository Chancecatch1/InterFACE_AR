using UnityEngine;
using System;
using System.IO;

static public class MedicationFinder {
    private static string curProcessID;

    //en
    private static string EN_JSONString = Resources.Load<TextAsset>("medications").ToString();
    private static string EN_JSONString2 = Resources.Load<TextAsset>("medicationslabels").ToString();
    static SimpleJSON.JSONNode en_json = SimpleJSON.JSON.Parse(EN_JSONString);
    static SimpleJSON.JSONNode en_json2 = SimpleJSON.JSON.Parse(EN_JSONString2);

    //fr
    private static string FR_JSONString = Resources.Load<TextAsset>("fr_medications").ToString();
    private static string FR_JSONString2 = Resources.Load<TextAsset>("fr_medicationslabels").ToString();
    static SimpleJSON.JSONNode fr_json = SimpleJSON.JSON.Parse(FR_JSONString);
    static SimpleJSON.JSONNode fr_json2 = SimpleJSON.JSON.Parse(FR_JSONString2);

    static public string[] FindByTag (string a, string lang) {
        string[] returnArr = new string[2];
        SimpleJSON.JSONNode medarr = en_json["medicationModels"];

        if (lang == "fr") {
            medarr = fr_json["medicationModels"];
        }

        // Debug.Log(medarr);
        foreach (SimpleJSON.JSONObject med in medarr) {
            // Debug.Log(med["id"]);
            if ((string )med["id"] == a) {
                returnArr[0] = (FindByName((string) med["label"], lang));
                returnArr[1] = ((string) med["doses"][0]["label"]);
                return returnArr;
            }
        }
        return returnArr;
    }

    static public string FindByName (string a, string lang) {
        SimpleJSON.JSONNode meds = en_json2["MEDICATION"];

        if (lang == "fr") {
            meds = fr_json2["MEDICATION"];
        }

        string ret = meds[a];
        
        if (ret == null) {
            return a;
        } else {
            return ret;
        }        
    }

    static public void setProcessId (string pi) {
        curProcessID = pi;
    }

    static public string getProcessId () {
        return curProcessID;
    }
}