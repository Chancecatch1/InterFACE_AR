using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class MedicationEvent : MonoBehaviour
{
    // JArray algorithm = JArray.Parse("[{\"id\":1,\"instruction\":[\"Begin bag-mask ventilation and given oxygen\",\"Attach monitor/defibrillator\"],\"epinephrine\":false,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":2,\"contents\":[\"VF/pVT\",\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":9,\"contents\":[\"Asystole/PEA\",\"Epinephrine ASAP\",\"CPR 2 min\"]}}},{\"id\":2,\"instruction\":[\"VP/pVT\",\"Shock\",\"CPR 2 min\",\"IV/IO Access\"],\"epinephrine\":false,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":5,\"contents\":[\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":12,\"contents\":[\"Return of spontaneous circulation?\"]}}},{\"id\":5,\"instruction\":[\"Shock\",\"CPR 2 min\",\"Epinephrine every 3-5 min\",\"Consider advanced airway\"],\"epinephrine\":true,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":7,\"contents\":[\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":12,\"contents\":[\"Return of spontaneous circulation?\"]}}},{\"id\":7,\"instruction\":[\"Shock\",\"CPR 2 min\",\"Amiodarone or lidocaine\",\"Treat reversible causes\"],\"epinephrine\":true,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":5,\"contents\":[\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":12,\"contents\":[\"Return of spontaneous circulation?\"]}}},{\"id\":12,\"instruction\":[],\"epinephrine\":false,\"cpr\":false,\"question\":{\"title\":\"Check Return of spontaneous circulation (ROSC)\",\"y\":{\"goto\":999,\"contents\":[\"Post-Cardiac Arrest Care checklist\"]},\"n\":{\"goto\":10,\"contents\":[\"CPR 2 min\"]}}},{\"id\":9,\"instruction\":[\"Asytole/PEA\",\"Epinephrine ASAP\",\"CPR 2 min\",\"IV/IO access\",\"Epinephrine every 3-5 min\",\"Consider advanced airway and capnography\"],\"epinephrine\":true,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":7,\"contents\":[\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":11,\"contents\":[\"CPR 2 min\"]}}},{\"id\":10,\"instruction\":[\"CPR 2 min\",\"IV/IO access\",\"Epinephrine every 3-5 min\",\"Consider advanced airway and capnography\"],\"epinephrine\":true,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":7,\"contents\":[\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":11,\"contents\":[\"CPR 2 min\"]}}},{\"id\":11,\"instruction\":[\"CPR 2 min\",\"Treat reversible causes\"],\"epinephrine\":true,\"cpr\":true,\"question\":{\"title\":\"Rythm shockable?\",\"y\":{\"goto\":7,\"contents\":[\"Shock\",\"CPR 2 min\"]},\"n\":{\"goto\":12,\"contents\":[\"Return of spontaneous circulation?\"]}}}]");
    // Start is called before the first frame update
    TextMeshProUGUI AmiCount;
    TextMeshProUGUI AtroCount;
    TextMeshProUGUI EpiCount;
    TextMeshProUGUI LidoCount;
    TextMeshProUGUI FenCount;
    TextMeshProUGUI KenCount;
    TextMeshProUGUI MidCount;
    TextMeshProUGUI MorCount;
    TextMeshProUGUI RocCount;
    TextMeshProUGUI SucCount;
    TextMeshProUGUI CalGCount;
    TextMeshProUGUI CalCCount;
    TextMeshProUGUI SalCount;
    TextMeshProUGUI SodCount;
    TextMeshProUGUI InsCount;
    TextMeshProUGUI GluCount;

    int max_count = 9999;
    string processID;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator addMedications(string medicationId, string doseId, string instanceID)
    {
            string processID = MedicationFinder.getProcessId();
            string url = $"https://interface-ar.unige.ch/care-processes/{processID}/cpr/medications/{medicationId}/doses/{doseId}/instances/{instanceID}?status=READY";
            using (UnityWebRequest www = UnityWebRequest.Post(url, "", "application/json"))
            {
                www.method = "PATCH";
                yield return www.SendWebRequest();
                try {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    Debug.Log("Medication prepared!");
                }
                } catch (Exception e) {
                    Debug.Log(e);
                }
            }
    }

    IEnumerator minusMedications(string medicationId, string doseId, string instanceID)
    {
            string processID = MedicationFinder.getProcessId();
            string url = "https://interface-ar.unige.ch/care-processes/" + processID + "/cpr/medications/" + medicationId + "/doses/" + doseId + "/instances/" + instanceID;
            using (UnityWebRequest www = UnityWebRequest.Delete(url))
            {
                yield return www.SendWebRequest();
                try {
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError(www.error);
                    }
                    else
                    {
                        Debug.Log("Medication canceled!");
                    }
                } catch (Exception e) {
                    Debug.Log(e);
                }
            }
    }

    IEnumerator preAdd(string processId, string medicationId, string doseId) {
        string URL = "https://interface-ar.unige.ch//care-processes/" + processId + "/cpr/medications";
         using(UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                // GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                // TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                // txt.text = "Communication Error";

                // sessionArr.Add(myInstance);
            }
            else
            //sucess
            {
                string json = request.downloadHandler.text;
                SimpleJSON.JSONNode medications = SimpleJSON.JSON.Parse(json);
                SimpleJSON.JSONNode meds = medications["medicationModels"];
                foreach(SimpleJSON.JSONObject med in meds) {
                    if ((string) med["id"] == medicationId) {
                        foreach(SimpleJSON.JSONNode doses in med["doses"]) {
                            if ((string) doses["id"] == doseId) {
                                foreach (SimpleJSON.JSONNode di in doses["doseInstances"]) {
                                    if (di["status"] == "PREPARING" || di["status"] == "AUTO_PREPARING") {
                                    // if (di["status"] == "PREPARING" && di["autoPrescribed"] == false) {
                                        StartCoroutine(addMedications(medicationId, doseId, di["id"]));
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }

    IEnumerator preMinus(string processId, string medicationId, string doseId) {
        string URL = "https://interface-ar.unige.ch//care-processes/" + processId + "/cpr/medications";
         using(UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                // GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                // TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                // txt.text = "Communication Error";

                // sessionArr.Add(myInstance);
            }
            else
            //sucess
            {
                string json = request.downloadHandler.text;
                SimpleJSON.JSONNode medications = SimpleJSON.JSON.Parse(json);
                SimpleJSON.JSONNode meds = medications["medicationModels"];
                foreach(SimpleJSON.JSONObject med in meds) {
                    if ((string) med["id"] == medicationId) {
                        foreach(SimpleJSON.JSONNode doses in med["doses"]) {
                            if ((string) doses["id"] == doseId) {
                                // OLD (mj prev): cancel only when PREPARING or READY and autoPrescribed == false
                                // foreach (SimpleJSON.JSONNode di in doses["doseInstances"]) {
                                //     // Allow cancel when instance is PREPARING or already set to READY (after plus / mj)
                                //     string st = di["status"];
                                //     bool auto = false;
                                //     try { auto = di["autoPrescribed"]; } catch {}
                                //     if ((st == "PREPARING" || st == "READY") && auto == false) {
                                //         StartCoroutine(minusMedications(medicationId, doseId, di["id"]));
                                //         break;
                                //     }
                                // }

                                // NEW: also allow AUTO_PREPARING and drop autoPrescribed filter; add debug logs
                                {
                                    bool issued = false;
                                    foreach (SimpleJSON.JSONNode di in doses["doseInstances"]) {
                                        string st = di["status"];
                                        string idStr = di["id"];
                                        bool auto = false;
                                        try { auto = di["autoPrescribed"]; } catch {}
                                        Debug.Log($"preMinus di status={st}, autoPrescribed={auto}, id={idStr}");
                                        if (st == "PREPARING" || st == "AUTO_PREPARING" || st == "READY") {
                                            StartCoroutine(minusMedications(medicationId, doseId, idStr));
                                            issued = true;
                                            break;
                                        }
                                    }
                                    if (!issued) {
                                        Debug.LogWarning($"preMinus: No cancellable instances found for med={medicationId}, dose={doseId}. Expected PREPARING/AUTO_PREPARING/READY.");
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }

    public void plusAmi() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "1", "1"));
        
    }

    public void minusAmi() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "1", "1"));
    }

    public void plusAtro() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "2", "2"));
    }

    public void minusAtro() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "2", "2"));

    }

    public void plusEpi() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "5", "6"));
    }

    public void minusEpi() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "5", "6"));
    }

    public void plusLido() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "12", "18"));
    }

    public void minusLido() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "12", "18"));
    }

    public void plusFen() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "7", "9"));
    }

    public void minusFen() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "7", "9"));
    }

    public void plusKen() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "11", "16"));
    }

    public void minusKen() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "11", "16"));
    }

    public void plusMid() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "13", "19"));
    }

    public void minusMid() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "13", "19"));
    }

    public void plusMor() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "14", "21"));
    }

    public void minusMor() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "14", "21"));
    }

    public void plusRoc() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "16", "24"));
    }

    public void minusRoc() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "16", "24"));
    }

    public void plusSuc() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "19", "29"));
    }

    public void minusSuc() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "19", "29"));
    }

    public void plusCalG() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "4", "4"));
    }

    public void minusCalG() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "4", "4"));
    }

    public void plusCalG100() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "4", "5"));
    }

    public void minusCalG100() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "4", "5"));
    }

    public void plusCalC() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "3", "3"));
    }

    public void minusCalC() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "3", "3"));
    }

    public void plusSal() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "17", "25"));
    }

    public void minusSal() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "17", "25"));
    }

    public void plusSod() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "18", "26"));
    }

    public void minusSod() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "18", "26"));
    }

    public void plusSod2() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "18", "28"));
    }

    public void minusSod2() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "18", "28"));
    }

    public void plusIns() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "9", "14"));
    }

    public void minusIns() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "9", "14"));
    }

    public void plusGlu() {
        string processID = MedicationFinder.getProcessId();
        if (processID == null) return;
        StartCoroutine(preAdd(processID, "8", "13"));
    }

    public void minusGlu() {
        string processID = MedicationFinder.getProcessId();
        Debug.Log(processID);
        if (processID == null) return;
        StartCoroutine(preMinus(processID, "8", "13"));
    }
}
