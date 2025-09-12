using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;

using Microsoft.MixedReality.GraphicsTools;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit;

using EvtSource;

public class EventManager : MonoBehaviour
{
    private Queue<Action> m_queueAction = new Queue<Action>();
    private float timeActivated = float.MinValue;
    // Add a deduplication set at class scope
    HashSet<string> _confirmedOnce = new HashSet<string>();

    // public Transform head;
    // public Transform origin;
    // public Transform target;

    // public Transform head;
    // public Transform origin;
    // public Transform target;
    // public InputActionProperty recenterButton;

    SimpleJSON.JSONNode medications;
    SimpleJSON.JSONNode algoritms;
    // SimpleJSON.JSONNode algoImg = new SimpleJSON.JSONNode();
    Dictionary<string,CanvasElementRoundedRect> algoImg = new Dictionary<string,CanvasElementRoundedRect>();

    TextMeshProUGUI timer1;
    TextMeshProUGUI timer2;

    TextMeshProUGUI Doc_Cur_1;
    TextMeshProUGUI Doc_Cur_2;
    TextMeshProUGUI Doc_Cur_3;
    TextMeshProUGUI Doc_Next_1;
    TextMeshProUGUI Doc_Next_2;
    TextMeshProUGUI Doc_Next_3;
    TextMeshProUGUI Nurse_Cur_1;
    TextMeshProUGUI Nurse_Cur_2;
    TextMeshProUGUI Nurse_Cur_3;
    TextMeshProUGUI Nurse_Next_1;
    TextMeshProUGUI Nurse_Next_2;
    TextMeshProUGUI Nurse_Next_3;
    //Medications
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
    TextMeshProUGUI CalG100Count;
    TextMeshProUGUI CalCCount;
    TextMeshProUGUI SalCount;
    TextMeshProUGUI SodCount;
    TextMeshProUGUI Sod2Count;
    TextMeshProUGUI InsCount;
    TextMeshProUGUI GluCount;
    //Medications
    TextMeshProUGUI CardiacRhythm;
    TextMeshProUGUI CurrentSession;

    CanvasElementRoundedRect CPR_Plate;
    CanvasElementRoundedRect Epi_Plate;

    CanvasElementRoundedRect CPR_1;
    CanvasElementRoundedRect CHECK_PACE_1;
    CanvasElementRoundedRect VP_PVT;
    CanvasElementRoundedRect CHOC_1;
    CanvasElementRoundedRect CPR_2;
    CanvasElementRoundedRect CHECK_PACE_PULSE_2;
    CanvasElementRoundedRect CHOC_2;
    CanvasElementRoundedRect CPR_3;
    CanvasElementRoundedRect CHECK_PACE_PULSE_3;
    CanvasElementRoundedRect CHOC_3;
    CanvasElementRoundedRect CPR_4;
    CanvasElementRoundedRect ASYSTOLIE;
    CanvasElementRoundedRect EPINEPHRINE;
    CanvasElementRoundedRect CPR_5;
    CanvasElementRoundedRect CHECK_PACE_PULSE_4;
    CanvasElementRoundedRect CPR_6;
    CanvasElementRoundedRect CHECK_PACE_PULSE_5;
    CanvasElementRoundedRect CHOC_4;
    CanvasElementRoundedRect ASYSTOLIE2;
    CanvasElementRoundedRect ROSC;

    GameObject medUI;
    GameObject noti;
    GameObject sessions;
    GameObject sessionContainer;
    Transform notiTransform;
    Transform sessionsTransform;
    RawImage resTabOrderIcon;
    RawImage intTabOrderIcon;
    RawImage hypTabOrderIcon;

    public Material[] mat = new Material[13];
    public GameObject notiCprPref;
    public GameObject notiEpiPref;
    public GameObject notiMedPref;
    public GameObject sessionPref;
    /*
    *CanvasBackplate
    *CPRBorderCanvasBackplate //CPR less 10 sec
    *CPROriginCanvasBackplate //CPR Original
    *EpiBorderCanvasBackplate //Epi less 10 sec
    *EpiOriginCanvasBackplate //Epi Original
    *RedBorderCanvasBackplate //0 sec left flash for 5 secs
    *RedBorderCanvasBackplate //0 sec left flash for 5 secs without border
    */

    double time1 = 0;
    double time2 = 0;

    // Update is called once per frame

    public SocketIOUnity socket;

    double cprStartTimestamp = 0;
    double epiStartTimestamp = 0;
    double prev_cprStartTimestamp = 0;
    double prev_epiStartTimestamp = 0;

    bool cpr_5sec = false;
    bool epi_5sec = false;

    bool cpr_5sec_coroutine = false;
    bool epi_5sec_coroutine = false;


    bool boolTogglePen = false;

    // CHANGE NOTE (2025-09-02, mj): Split Nurse UI source flags for Current vs Next.
    bool useServerHintsForNurseCurrent = false;  // HINT mode test: use server hints for current panel
    bool useServerHintsForNurseNext = false;    // server hints for next panel
    // Max number of Next Steps items to display (default 3 to fill all slots)
    int MAX_NEXT_STEPS = 3;
    // De-duplication controls (defaults keep previous behavior)
    bool dedupNextSteps = true;
    bool dedupAgainstCurrent = true;

    ArrayList notiArr = new ArrayList();
    ArrayList notiCprArr = new ArrayList();
    ArrayList notiEpiArr = new ArrayList();
    ArrayList sessionArr = new ArrayList();

    // CHANGE NOTE (2025-09-09, mj): add queue for nurse next panel
    // Accumulates hints/overflow in order of arrival, removing duplicates.
    List<string> nurseNextQueue = new List<string>();
    List<string> lastHintNext = new List<string>();
    // DEBUG OFF by default: Glucose highlight diagnostics (can be toggled if needed)
    bool debugGlucoseHighlight = false; // set true to enable logs
    void DebugLogGlucoseHighlight(string message) { if (debugGlucoseHighlight) Debug.Log(message); }
    // track previous ordered state per med id for nurse next panel
    Dictionary<int, bool> prevOrderedByMedId = new Dictionary<int, bool>();

    string CanonicalizeSimple(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Replace("•", " ").Trim().ToLowerInvariant();
        try { s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " "); } catch {}
        return s;
    }

    void NurseNextAppendIfNew(string item)
    {
        string key = CanonicalizeSimple(item);
        if (string.IsNullOrWhiteSpace(key)) return;
        for (int i = 0; i < nurseNextQueue.Count; i++)
        {
            if (CanonicalizeSimple(nurseNextQueue[i]) == key) return; // already exists
        }
        nurseNextQueue.Add(item);
    }

    void NurseNextPruneTo(System.Collections.Generic.HashSet<string> expectedKeys)
    {
        var kept = new List<string>(nurseNextQueue.Count);
        for (int i = 0; i < nurseNextQueue.Count; i++)
        {
            var it = nurseNextQueue[i];
            if (expectedKeys.Contains(CanonicalizeSimple(it))) kept.Add(it);
        }
        nurseNextQueue = kept;
    }

    void NurseNextRender()
    {
        // NEXT panel shows FIFO excluding current head
        if (Nurse_Next_1 != null) Nurse_Next_1.text = nurseNextQueue.Count > 1 ? nurseNextQueue[1] : "";
        if (Nurse_Next_2 != null) Nurse_Next_2.text = nurseNextQueue.Count > 2 ? nurseNextQueue[2] : "";
        if (Nurse_Next_3 != null) Nurse_Next_3.text = nurseNextQueue.Count > 3 ? nurseNextQueue[3] : "";
    }

    void NurseNextRemoveByMedName(string medDisplayName)
    {
        if (string.IsNullOrWhiteSpace(medDisplayName)) return;
        string key = CanonicalizeSimple(medDisplayName);
        var kept = new List<string>(nurseNextQueue.Count);
        for (int i = 0; i < nurseNextQueue.Count; i++)
        {
            var it = nurseNextQueue[i];
            if (CanonicalizeSimple(it).IndexOf(key, System.StringComparison.Ordinal) >= 0)
            {
                // remove administered med entries
                continue;
            }
            kept.Add(it);
        }
        nurseNextQueue = kept;
    }

    EventSourceReader evt;

    private string filePath;

    //Multi language support
    //en, fr
    string lang = "en";
    SimpleJSON.JSONNode multi;
    // Patient weight (kg) from patientModel; used for dose computations when server parameter is absent
    double bodyWeightKg = 0;
    // CHANGE NOTE (2025-09-12, mj): Rendering throttling for Nurse medication panel
    // Avoid excessive re-renders when SSE arrives while maintaining state
    float _lastMedUiRenderTime = 0f;
    float _minMedRenderIntervalSec = 0.3f;

    // CHANGE NOTE (2025-09-12, mj): Keep track of last SSE medication snapshot and initialization status
    SimpleJSON.JSONNode _lastMedicationModelFromSse = null;
    bool _medInitDone = false;

    // Helper to robustly find TMP by tag, even if tag is on a parent
    TextMeshProUGUI GetTMPByTag(string tagName)
    {
        var go = GameObject.FindWithTag(tagName);
        if (go == null)
        {
            Debug.LogWarning($"[EventManager] GameObject with tag '{tagName}' not found or inactive.");
            return null;
        }
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) return tmp;
        tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            Debug.LogWarning($"[EventManager] Tag '{tagName}' is on '{go.name}', not on the TMP itself. Using child TMP '{tmp.gameObject.name}'. Consider moving the tag to that TMP object.");
            return tmp;
        }
        Debug.LogWarning($"[EventManager] Tag '{tagName}' found on '{go.name}' but no TextMeshProUGUI on it or children.");
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadTranslations(lang);
        ReplaceTexts(lang);

        filePath = Path.Combine(Application.persistentDataPath, $"{DateTime.Now.ToString("yyyy-MM-dd")}.csv");

        if (!File.Exists(filePath))
        {
            // LogEvent("Started", $"{gameObject.name}, 0, {unixTime}, {DateTime.Now.ToLocalTime()}");
            string header = "Event,Object Name, Duration, UnixTime, DateTime, SessionID";
            File.WriteAllText(filePath, header + "\n");
        }

        if (GameObject.FindWithTag("CPRTimer") != null) {
            timer1 = GameObject.FindWithTag("CPRTimer").GetComponent<TextMeshProUGUI>();
        }

        if (GameObject.FindWithTag("EpiTimer") != null) {
           timer2 = GameObject.FindWithTag("EpiTimer").GetComponent<TextMeshProUGUI>();
        }

        // CHANGE NOTE (2025-09-05, mj)
        // Query only Doctor or Nurse tags based on active scene name.

        var activeSceneName = SceneManager.GetActiveScene().name.ToLowerInvariant();
        bool isDoctorScene = activeSceneName.Contains("doctor");
        bool isNurseScene  = activeSceneName.Contains("nurse");

        // DEBUG LOG: Active scene info and Medication_List root quick check
        try
        {
            var sceneNameRaw = SceneManager.GetActiveScene().name;
            Debug.Log($"[EventManager] ActiveScene='{sceneNameRaw}', isNurse={isNurseScene}, isDoctor={isDoctorScene}");
            if (isNurseScene)
            {
                var rootProbe = FindMedicationListRoot();
                Debug.Log($"[EventManager] Medication_List root probe => {(rootProbe != null ? rootProbe.name : "null")}");
            }
        }
        catch {}

        if (isDoctorScene)
        {
            Doc_Cur_1 = GetTMPByTag("Doc_Cur_1");
            Doc_Cur_2 = GetTMPByTag("Doc_Cur_2");
            Doc_Cur_3 = GetTMPByTag("Doc_Cur_3");

            Doc_Next_1 = GetTMPByTag("Doc_Next_1");
            Doc_Next_2 = GetTMPByTag("Doc_Next_2");
            Doc_Next_3 = GetTMPByTag("Doc_Next_3");
        }

        if (isNurseScene)
        {
            Nurse_Cur_1 = GetTMPByTag("Nurse_Cur_1");
            Nurse_Cur_2 = GetTMPByTag("Nurse_Cur_2");
            Nurse_Cur_3 = GetTMPByTag("Nurse_Cur_3");

            Nurse_Next_1 = GetTMPByTag("Nurse_Next_1");
            Nurse_Next_2 = GetTMPByTag("Nurse_Next_2");
            Nurse_Next_3 = GetTMPByTag("Nurse_Next_3");
        }

        if (GameObject.FindWithTag("CardiacRhythm") != null) {
           CardiacRhythm = GameObject.FindWithTag("CardiacRhythm").GetComponent<TextMeshProUGUI>();
        }

        if (GameObject.FindWithTag("CurrentSession") != null) {
           CurrentSession = GameObject.FindWithTag("CurrentSession").GetComponent<TextMeshProUGUI>();
        }

        if (GameObject.FindWithTag("CPRTimerPlate") != null) {
           CPR_Plate = GameObject.FindWithTag("CPRTimerPlate").GetComponent<CanvasElementRoundedRect>();
        }

        if (GameObject.FindWithTag("EpiTimerPlate") != null) {
           Epi_Plate = GameObject.FindWithTag("EpiTimerPlate").GetComponent<CanvasElementRoundedRect>();
        }

        if (GameObject.Find("CPR_1") != null) {
           CPR_1 = GameObject.Find("CPR_1").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHECK_PACE_1") != null) {
           CHECK_PACE_1 = GameObject.Find("CHECK_PACE_1").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("VP_PVT") != null) {
           VP_PVT = GameObject.Find("VP_PVT").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHOC_1") != null) {
           CHOC_1 = GameObject.Find("CHOC_1").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CPR_2") != null) {
           CPR_2 = GameObject.Find("CPR_2").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHECK_PACE_PULSE_2") != null) {
           CHECK_PACE_PULSE_2 = GameObject.Find("CHECK_PACE_PULSE_2").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHOC_2") != null) {
           CHOC_2 = GameObject.Find("CHOC_2").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CPR_3") != null) {
           CPR_3 = GameObject.Find("CPR_3").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHECK_PACE_PULSE_3") != null) {
           CHECK_PACE_PULSE_3 = GameObject.Find("CHECK_PACE_PULSE_3").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHOC_3") != null) {
           CHOC_3 = GameObject.Find("CHOC_3").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CPR_4") != null) {
           CPR_4 = GameObject.Find("CPR_4").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("ASYSTOLIE") != null) {
           ASYSTOLIE = GameObject.Find("ASYSTOLIE").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("EPINEPHRINE") != null) {
           EPINEPHRINE = GameObject.Find("EPINEPHRINE").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CPR_5") != null) {
           CPR_5 = GameObject.Find("CPR_5").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHECK_PACE_PULSE_4") != null) {
           CHECK_PACE_PULSE_4 = GameObject.Find("CHECK_PACE_PULSE_4").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CPR_6") != null) {
           CPR_6 = GameObject.Find("CPR_6").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHECK_PACE_PULSE_5") != null) {
           CHECK_PACE_PULSE_5 = GameObject.Find("CHECK_PACE_PULSE_5").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("CHOC_4") != null) {
           CHOC_4 = GameObject.Find("CHOC_4").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("ASYSTOLIE2") != null) {
           ASYSTOLIE2 = GameObject.Find("ASYSTOLIE2").GetComponent<CanvasElementRoundedRect>();
        }
        if (GameObject.Find("ROSC") != null) {
           ROSC = GameObject.Find("ROSC").GetComponent<CanvasElementRoundedRect>();
        }

        if (GameObject.FindWithTag("ResTabOrderIcon") != null) {
           resTabOrderIcon = GameObject.FindWithTag("ResTabOrderIcon").GetComponent<RawImage>();
        }
        
        if (GameObject.FindWithTag("IntTabOrderIcon") != null) {
           intTabOrderIcon = GameObject.FindWithTag("IntTabOrderIcon").GetComponent<RawImage>();
        }

        if (GameObject.FindWithTag("HypTabOrderIcon") != null) {
           hypTabOrderIcon = GameObject.FindWithTag("HypTabOrderIcon").GetComponent<RawImage>();
        }    

        algoImg.Add("START_CPR",CPR_1);
        algoImg.Add("CHECK_PACE_1",CHECK_PACE_1);
        algoImg.Add("VP_PVT",VP_PVT);
        algoImg.Add("CHOC_1",CHOC_1);
        algoImg.Add("CPR_2",CPR_2);
        algoImg.Add("CHECK_PACE_PULSE_2",CHECK_PACE_PULSE_2);
        algoImg.Add("CHOC_2",CHOC_2);
        algoImg.Add("CPR_3",CPR_3);
        algoImg.Add("CHECK_PACE_PULSE_3",CHECK_PACE_PULSE_3);
        algoImg.Add("CHOC_3",CHOC_3);
        algoImg.Add("CPR_4",CPR_4);
        algoImg.Add("ASYSTOLIE",ASYSTOLIE);
        algoImg.Add("EPINEPHRINE",EPINEPHRINE);
        algoImg.Add("CPR_5",CPR_5);
        algoImg.Add("CHECK_PACE_PULSE_4",CHECK_PACE_PULSE_4);
        algoImg.Add("CPR_6",CPR_6);
        algoImg.Add("CHECK_PACE_PULSE_5",CHECK_PACE_PULSE_5);
        algoImg.Add("CHOC_4",CHOC_4);
        algoImg.Add("ASYSTOLIE2",ASYSTOLIE2);
        algoImg.Add("ROSC",ROSC);

        medUI = GameObject.FindWithTag("Medication_UI");
        noti = GameObject.FindWithTag("Notifications");
        sessions = GameObject.FindWithTag("Sessions");
        sessionContainer = GameObject.FindWithTag("SessionContainer");

        // Optional: silence tag warnings in non-matching scenes
        bool verboseMissingTagLogs = activeSceneName.Contains("hmd_");
        /*
        // Temporarily commented out to silence CS8321 (declared but never used)
        TextMeshProUGUI GetTMPByTagQuiet(string tagName)
        {
            var go = GameObject.FindWithTag(tagName);
            if (go == null)
            {
                if (verboseMissingTagLogs)
                    Debug.LogWarning($"[EventManager] GameObject with tag '{tagName}' not found or inactive.");
                return null;
            }
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
            tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            return tmp;
        }
        */

        if (noti != null) {
            notiTransform = noti.transform;
        }

        if (sessions != null) {
            sessionsTransform = sessions.transform;
        }

        var uri = new Uri("http://136.159.140.66");

        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Path = "/cpr/socket.io"
        });

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("socket.OnConnected");
        };

        // socket.On("currentStatus", response => currentStatus(response));

        // socket.On("medication", response => medication(response));

        untogglePenMode();

        Debug.Log("Connecting...");
        // socket.Connect();

        getSessions();
    }

    void LoadTranslations(string lang)
    {
        if (lang == "en") {
            return;
        }

        string multilang = Resources.Load<TextAsset>("multilang").ToString();
        SimpleJSON.JSONNode multilang_json = SimpleJSON.JSON.Parse(multilang);
        multi = multilang_json[lang];

        Debug.Log($"LoadTranslations: {multilang}");
    }

    void ReplaceTexts(string lang)
    {
        if (lang == "en") {
            return;
        }

        if (multi != null) {
            GameObject[] allGameObjects = FindObjectsOfType<GameObject>(true);  // true: inactive objects
            foreach (var go in allGameObjects)
            {
                TextMeshProUGUI textObj = go.GetComponent<TextMeshProUGUI>();
                if (textObj != null)
                {
                    string originalText = textObj.text?.Trim();

                    if (!string.IsNullOrEmpty(originalText))
                    {
                        // if there is a translated text, replace it, else log
                        if (multi[originalText] != null)
                        {
                            textObj.text = multi[originalText];
                        }
                        else
                        {
                            Debug.LogWarning("Missing Translation Text: " + originalText);
                        }
                    }
                }
            }
        }
    }

    string FindMultiLang (string originalText) {
        if (multi == null) return originalText;
        originalText = originalText?.Trim();

        if (!string.IsNullOrEmpty(originalText))
        {
            // if there is a translated text, replace it, else log
            if (multi[originalText] != null)
            {
                return multi[originalText];
            }
            else
            {
                return originalText;
            }
        } else {
            return originalText;
        }
    }
    

/*
*    type: 0 medication
*    type: 1 cpr
*    type: 2 epi
*/

    // CHANGE NOTE (2025-09-05, mj)
    // Reuse the same formatting as Current/Next Steps for notifications (type 0), hiding calc terms and showing only the final value.
    string ComposeDoseForNotification(string dose)
    {
        if (string.IsNullOrWhiteSpace(dose)) return dose;

        string NormalizeUnitsLocal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Replace("cc", "mL").Replace("CC", "mL");
            s = s.Replace("ML", "mL").Replace("Ml", "mL").Replace("ml", "mL");
            s = s.Replace("MG", "mg").Replace("Mg", "mg");
            return s.Trim();
        }

        string s = NormalizeUnitsLocal(dose);
        int idx = s.LastIndexOf('=');
        if (idx >= 0 && idx < s.Length - 1)
        {
            string right = s.Substring(idx + 1).Trim();
            if (!string.IsNullOrWhiteSpace(right))
            {
                var m = System.Text.RegularExpressions.Regex.Match(right, @"([0-9]+(?:\.[0-9]+)?\s*(mg|mcg|g|mL|J|mEq|U|units))\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) return NormalizeUnitsLocal(m.Groups[1].Value);
                return right;
            }
        }
        {
            var m = System.Text.RegularExpressions.Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?\s*(mg|mcg|g|mL|J|mEq|U|units))\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return NormalizeUnitsLocal(m.Groups[1].Value);
        }
        return s;
    }
    void UpdateNoti (string name, string dose, int type) {
        if (noti != null && notiTransform != null) {
            if (type == 0 && notiMedPref != null) {
                GameObject myInstance = Instantiate(notiMedPref, notiTransform);

                notiArr.Add(myInstance);
                StartCoroutine(Remove_Noti(myInstance, 0));
                TextMeshProUGUI txt = myInstance.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                string prettyDose = ComposeDoseForNotification(dose);
                if (lang == "en") {
                    txt.text = name + "\n" + prettyDose + " given";
                }
                else if (lang == "fr"){
                    txt.text = name + "\n" + prettyDose + " administré";
                }
            } else if (type == 1 && notiCprPref != null) {
                GameObject myInstance = Instantiate(notiCprPref, notiTransform);

                notiCprArr.Add(myInstance);

                StartCoroutine(Remove_Noti(myInstance, 1));
            } else if (type == 2 && notiEpiPref != null) {
                GameObject myInstance = Instantiate(notiEpiPref, notiTransform);

                notiEpiArr.Add(myInstance);

                StartCoroutine(Remove_Noti(myInstance, 2));
            }
        }
        Debug.Log("name: " + name + ", dose: " + ComposeDoseForNotification(dose));
    }

    void UpdateUI(SimpleJSON.JSONNode obj)
    {
        if (obj["cursorOption"] != null) {
            if (obj["cursorOption"] == "PVT") {
                if (CardiacRhythm != null) {
                    CardiacRhythm.text = FindMultiLang("Cardiac Rhythm") + ": " + FindMultiLang("pVT");
                }
            } else if (obj["cursorOption"] == "VF") {
                if (CardiacRhythm != null) {
                    CardiacRhythm.text = FindMultiLang("Cardiac Rhythm") + ": " + FindMultiLang("VF");
                }
            } else if (obj["cursorOption"] == "ASYSTOLE") {
                if (CardiacRhythm != null) {
                    CardiacRhythm.text = FindMultiLang("Cardiac Rhythm") + ": " + FindMultiLang("Asystole");
                }
            } else if (obj["cursorOption"] == "PEA") {
                if (CardiacRhythm != null) {
                    CardiacRhythm.text = FindMultiLang("Cardiac Rhythm") + ": " + FindMultiLang("PEA");
                }
            }
        }
    }

    void UpdateInstructions(SimpleJSON.JSONNode obj) //response["cprHintModel"];
    {
    try
        {
            Init_Tasks();

            // Add this inside UpdateInstructions(...) just after Init_Tasks();
            // CHANGE NOTE (2025-09-01, mj): Normalize units for display and parsing.
            string NormalizeUnits(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                // Volume
                s = s.Replace("cc", "mL").Replace("CC", "mL");
                s = s.Replace("ML", "mL").Replace("Ml", "mL").Replace("ml", "mL");
                // Mass
                s = s.Replace("MG", "mg").Replace("Mg", "mg");
                // Weight
                s = s.Replace("KG", "kg").Replace("Kg", "kg");
                // Ensure consistent spacing like "J/kg" (keep as is by default)
                return s;
            }

            // Extract only the final numeric result part from a calculation string
            // e.g., "0.01 mg/kg (0.1 ml/kg) = 0.87 mg" -> "0.87 mg"
            string ExtractFinalValue(string param)
            {
                if (string.IsNullOrWhiteSpace(param)) return "";
                int idx = param.LastIndexOf('=');
                string s = (idx >= 0 ? param.Substring(idx + 1) : param).Trim();
                if (!string.IsNullOrWhiteSpace(s)) return NormalizeUnits(s);

                // Fallback: try to grab trailing numeric+unit even without '='
                var m = Regex.Match(param, @"([0-9]+(?:\.[0-9]+)?\s*(mg|mcg|g|mL|ml|J|mEq|U|units))\s*$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    return NormalizeUnits(m.Groups[1].Value.Trim());
                }
                return "";
            }

            // Clean calc-like fragments from base text
            string CleanCalcFromText(string text, string param)
            {
                if (string.IsNullOrWhiteSpace(text)) return text ?? "";
                string s = text;
                if (!string.IsNullOrWhiteSpace(param))
                {
                    try { s = Regex.Replace(s, Regex.Escape(param), "", RegexOptions.IgnoreCase); } catch {}
                }
                // Remove parentheses segments containing /kg
                s = Regex.Replace(s, @"\([^)]*?/\s*kg[^)]*?\)", "", RegexOptions.IgnoreCase);
                // Remove ' = ...' segments
                s = Regex.Replace(s, @"\s*=\s*[^\n:]+", "", RegexOptions.IgnoreCase);
                // Remove absolute energy pieces like ' at 200J' or '(200J)'
                s = Regex.Replace(s, @"\s+at\s*\d+(?:\.\d+)?\s*J", "", RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"\s*\(\s*\d+(?:\.\d+)?\s*J\s*\)", "", RegexOptions.IgnoreCase);
                // Remove generic unitized calc fragments (mg/mL/J, optionally /kg)
                s = Regex.Replace(s, @"\b\d+(?:\.\d+)?\s*(mg|mcg|g|mL|ml|J)\s*(?:/\s*kg)?", "", RegexOptions.IgnoreCase);
                // Cleanup spaces and trailing colon
                while (s.Contains("  ")) s = s.Replace("  ", " ");
                if (s.EndsWith(":")) s = s.Substring(0, s.Length - 1);
                return s.Trim();
            }

            // Extract per-kg energy like '2 J/kg' from a string
            string ExtractPerKgJ(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                var m = Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*J\s*/\s*kg", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    string num = m.Groups[1].Value;
                    return $"{num} J/kg";
                }
                return "";
            }

            // Optional: smart wrap for long medication lines
            // Rules:
            // - If short enough: return as-is
            // - Prefer break after '=' so final dose moves to next line
            // - If still too long, break after medication name (before per‑kg)
            // - Cap at max 3 lines total
            string WrapIfLong(string composed)
            {
                if (string.IsNullOrWhiteSpace(composed)) return composed;
                // Bind numbers and units to avoid bad breaks
                try
                {
                    composed = Regex.Replace(composed, @"(\d+(?:\.[0-9]+)?)\s+(mg|mcg|g|mL|J|mEq|U|units)\b", "$1\u00A0$2", RegexOptions.IgnoreCase);
                    composed = Regex.Replace(composed, @"(\d+(?:\.[0-9]+)?)\s+J\s*/\s*kg", "$1\u00A0J/kg", RegexOptions.IgnoreCase);
                }
                catch {}

                int limit = 28;
                if (composed.Length <= limit) return composed;

                string result = composed;

                // Step 1: break after '=' if present
                int eq = result.IndexOf('=');
                if (eq >= 0 && eq < result.Length - 1)
                {
                    string left = result.Substring(0, eq + 1).TrimEnd();
                    string right = result.Substring(eq + 1).TrimStart();
                    result = left + "\n" + right;
                }

                // Step 2: if longest line still too long, break after medication name (before per‑kg)
                string[] lines = result.Split('\n');
                int maxLen = 0; for (int i = 0; i < lines.Length; i++) if (lines[i].Length > maxLen) maxLen = lines[i].Length;
                if (maxLen > limit)
                {
                    // Use original (no newlines) to find med name and per‑kg
                    string raw = composed.Replace("\n", " ");
                    var m = Regex.Match(raw, @"\b(\d+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL)\s*/\s*kg\b", RegexOptions.IgnoreCase);
                    if (m.Success && m.Index > 0)
                    {
                        string left = raw.Substring(0, m.Index).TrimEnd();
                        string right = raw.Substring(m.Index).TrimStart();
                        result = left + "\n" + right;
                    }
                }

                // Step 3: if still too long, and we have '=', split the second part to a third line
                lines = result.Split('\n');
                maxLen = 0; for (int i = 0; i < lines.Length; i++) if (lines[i].Length > maxLen) maxLen = lines[i].Length;
                if (maxLen > limit && lines.Length < 3)
                {
                    // Try to split last line around '=' to keep <= 3 lines
                    int last = lines.Length - 1;
                    int eq2 = lines[last].IndexOf('=');
                    if (eq2 >= 0 && eq2 < lines[last].Length - 1)
                    {
                        string l = lines[last].Substring(0, eq2 + 1).TrimEnd();
                        string r = lines[last].Substring(eq2 + 1).TrimStart();
                        lines[last] = l;
                        result = string.Join("\n", lines) + "\n" + r;
                    }
                }

                // Ensure no more than 3 lines
                lines = result.Split('\n');
                if (lines.Length > 3)
                {
                    result = string.Join("\n", new string[]{ lines[0], lines[1], lines[2] });
                }
                return result;
            }

            // CHANGE NOTE (2025-09-12, mj): UI-safe wrapper enforcing chunk binding (no visible tokens)
            // - Convert standard spaces within critical tokens to non-breaking spaces so TMP keeps chunks intact
            // - Apply WrapIfLong at the end to keep 3-line rule
            string WrapMedicationLineForUI(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                string txt = s;
                try
                {
                    // Bind final dose: "= 125 mg" -> "= 125\u00A0mg"
                    txt = Regex.Replace(txt, @"=\s*([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL|J|mEq|U|units)\b", m =>
                    {
                        var num = m.Groups[1].Value;
                        var unit = m.Groups[2].Value;
                        return "= " + num + "\u00A0" + unit; // invisible NBSP in UI
                    }, RegexOptions.IgnoreCase);

                    // Bind per-kg mass/vol: "0.01 mg/kg" -> "0.01\u00A0mg/kg"
                    txt = Regex.Replace(txt, @"\b([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL)\s*/\s*kg\b", m =>
                    {
                        var num = m.Groups[1].Value;
                        var unit = m.Groups[2].Value;
                        return num + "\u00A0" + unit + "/kg";
                    }, RegexOptions.IgnoreCase);
                }
                catch {}

                return WrapIfLong(txt);
            }

            string TryComputePerKgFinal(string s, double weightKg)
            {
                if (string.IsNullOrWhiteSpace(s) || weightKg <= 0) return "";
                s = NormalizeUnits(s);
                // Prefer mass-based doses if present (mg/mcg/g per kg)
                var mMass = Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g)\s*/\s*kg", RegexOptions.IgnoreCase);
                if (mMass.Success)
                {
                    double v = 0;
                    double.TryParse(mMass.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v);
                    double total = v * weightKg;
                    string unit = mMass.Groups[2].Value.ToLowerInvariant();
                    if (unit == "g")
                    {
                        return total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " g";
                    }
                    if (unit == "mcg")
                    {
                        return total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mcg";
                    }
                    return total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mg";
                }
                // Then volume per kg (mL or cc)
                var mVol = Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mL|ml|cc)\s*/\s*kg", RegexOptions.IgnoreCase);
                if (mVol.Success)
                {
                    double v = 0;
                    double.TryParse(mVol.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v);
                    double total = v * weightKg;
                    return total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mL";
                }
                return "";
            }

            // CHANGE NOTE (2025-09-02, mj): Extract a single per-kg token (prefer mass, else volume) without computing.
            string ExtractPerKgMass(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = NormalizeUnits(s);
                var m = Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g)\s*/\s*kg", RegexOptions.IgnoreCase);
                if (!m.Success) return "";
                string val = m.Groups[1].Value;
                string unit = m.Groups[2].Value.ToLowerInvariant();
                if (unit == "g" || unit == "mg" || unit == "mcg")
                {
                    return val + " " + unit + "/kg";
                }
                return "";
            }
            string ExtractPerKgVol(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = NormalizeUnits(s);
                var m = Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mL|ml|cc)\s*/\s*kg", RegexOptions.IgnoreCase);
                if (!m.Success) return "";
                string val = m.Groups[1].Value;
                return val + " mL/kg";
            }

            // CHANGE NOTE (2025-09-02, mj): Tidy spacing util for formatting.
            string TidySpacing(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                s = Regex.Replace(s, @"(?<!\s)\( ", " ( " );
                s = Regex.Replace(s, @"\(\s+", "(");
                s = Regex.Replace(s, @"\s+\)", ")");
                s = Regex.Replace(s, @":\s*", ": ");
                s = Regex.Replace(s, @"[ \t]{2,}", " ");
                return s.Trim();
            }

            // CHANGE NOTE (2025-09-02, mj): Nurse Advanced Preparation specific post-formatting.
            // Keep general wording unchanged; only standardize units/spacing.
            // CHANGE NOTE (2025-09-05, mj): Prevent bad wraps by binding number and unit with a non-breaking space.
            string FormatAdvancedPreparationNurse(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                var text = NormalizeUnits(s);
                text = TidySpacing(text);
                try
                {
                    text = Regex.Replace(text, @"(\d+(?:\.[0-9]+)?)\s+(mg|mcg|g|mL|J|mEq|U|units)\b", "$1\u00A0$2", RegexOptions.IgnoreCase);
                    text = Regex.Replace(text, @"(\d+(?:\.[0-9]+)?)\s+J\s*/\s*kg", "$1\u00A0J/kg", RegexOptions.IgnoreCase);
                    // Remove noisy UI tokens like "IconPreparing"
                    text = Regex.Replace(text, @"\b\w*IconPreparing\b", "", RegexOptions.IgnoreCase);
                    text = Regex.Replace(text, @"\bIcon\b", "", RegexOptions.IgnoreCase);
                    while (text.IndexOf("  ") >= 0) text = text.Replace("  ", " ");
                }
                catch {}
                return text;
            }

            // CHANGE NOTE (2025-09-01, mj): Compose display text with optional computed final.
            string ComposeTextFinal(SimpleJSON.JSONNode n)
            {
                if (n == null) return "";

                // CHANGE NOTE (2025-09-02, mj): Process plain string tags through the same dose-selection logic.
                // Some Nurse Next hints arrive as a raw tag (string). Previously we returned the raw template, so dose rules were skipped.
                string tag = null;
                string baseRaw = "";
                string param = null;

                if (n.IsString)
                {
                    tag = n.Value;
                    baseRaw = InstructionFinder.FindByTag(tag, lang);
                }
                else
                {
                    tag = n["hintType"]?.Value;
                    baseRaw = string.IsNullOrWhiteSpace(tag) ? "" : InstructionFinder.FindByTag(tag, lang);
                    param = n["hintParameter"]?.Value;
                }

                string text = NormalizeUnits(baseRaw);
                if (!string.IsNullOrWhiteSpace(param)) param = NormalizeUnits(param);

                // Detect shock energy using unmodified raw template/param
                // Only trigger for actual energy instructions (defibrillation/shock) or when J/kg is present.
                // Note: Do NOT rely on word boundaries for 'defibrill*' since words like 'defibrillation' and 'defibrillate' would fail \b at the substring.
                bool isJPerKg =
                    Regex.IsMatch(baseRaw ?? "", @"J\s*/\s*kg", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(param ?? "", @"J\s*/\s*kg", RegexOptions.IgnoreCase);
                bool isShockWord =
                    Regex.IsMatch(baseRaw ?? "", @"defibrill", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(baseRaw ?? "", @"shock", RegexOptions.IgnoreCase);
                bool looksLikeShockEnergy = isJPerKg || isShockWord;
                if (looksLikeShockEnergy)
                {
                    Debug.Log($"[EventManager] ComposeTextFinal detect: tag={tag}, isJPerKg={isJPerKg}, isShockWord={isShockWord}, base='{baseRaw}', param='{param}'");
                }

                string final = "";
                if (looksLikeShockEnergy)
                {
                    // Extract per-kg energy once (e.g., "2 J/kg")
                    string perKg = ExtractPerKgJ(param);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgJ(text);

                    // Extract absolute J from param first, excluding J/kg; else from base text
                    string finalJ = null;
                    try
                    {
                        var rxAbsJ = new Regex(@"(\d+(?:\.[0-9]+)?)\s*J(?![\s\u00A0]*/[\s\u00A0]*kg)", RegexOptions.IgnoreCase);
                        if (!string.IsNullOrWhiteSpace(param))
                        {
                            var ms = rxAbsJ.Matches(param);
                            if (ms.Count > 0)
                            {
                                var s = ms[ms.Count - 1].Groups[1].Value;
                                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                                    finalJ = v.ToString("0.##", CultureInfo.InvariantCulture) + " J";
                            }
                        }
                        if (finalJ == null && !string.IsNullOrWhiteSpace(baseRaw))
                        {
                            var ms2 = rxAbsJ.Matches(baseRaw);
                            if (ms2.Count > 0)
                            {
                                var s = ms2[ms2.Count - 1].Groups[1].Value;
                                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                                    finalJ = v.ToString("0.##", CultureInfo.InvariantCulture) + " J";
                            }
                        }
                    }
                    catch {}

                    // If neither param nor base text has absolute J, keep previous simple per-kg rule
                    if (!string.IsNullOrWhiteSpace(finalJ))
                    {
                        // Clean base text and compose: "<text> at <finalJ> (<perKg>)"
                        text = CleanCalcFromText(text, param);
                        // Remove trailing 'at' if left after stripping energy
                        try { text = Regex.Replace(text ?? "", @"\s+at\s*$", "", RegexOptions.IgnoreCase).Trim(); } catch {}
                        string combined = string.IsNullOrWhiteSpace(text) ? ("at " + finalJ) : ($"{text} at {finalJ}");
                        if (!string.IsNullOrWhiteSpace(perKg)) combined += $" ({perKg})";
                        combined = TidySpacing(combined);
                        Debug.Log($"[EventManager] ComposeTextFinal shock: tag={tag} => '{combined}'");
                        return combined;
                    }

                    if (!string.IsNullOrWhiteSpace(perKg))
                    {
                        // Legacy fallback: show only per-kg
                        text = CleanCalcFromText(text, param);
                        string combined = string.IsNullOrWhiteSpace(text) ? perKg : ($"{text}: {perKg}");
                        Debug.Log($"[EventManager] ComposeTextFinal shock: tag={tag} => '{combined}'");
                        return combined;
                    }

                    // Fallback: no per-kg present -> strip absolute J and calc fragments from the base text and return it
                    text = Regex.Replace(text, @"\s+at\s*\d+(?:\.\d+)?\s*J", "", RegexOptions.IgnoreCase).Trim();
                    text = CleanCalcFromText(text, param);
                    return text;
                }
                else
                {
                    // 1) Prefer explicit parameter's final
                    final = ExtractFinalValue(param);
                    // 2) Otherwise compute from per-kg in tag/param, only if weight is known
                    if (string.IsNullOrWhiteSpace(final) && bodyWeightKg > 0)
                    {
                        string source = !string.IsNullOrWhiteSpace(param) ? param : baseRaw;
                        final = TryComputePerKgFinal(source, bodyWeightKg);
                    }

                    if (!string.IsNullOrWhiteSpace(final))
                    {
                        // Clean and show only final dose alongside general wording
                        text = CleanCalcFromText(text, param);
                    }
                    else
                    {
                        // No final available: choose ONE of the per-kg tokens (prefer mass over volume)
                        string src = !string.IsNullOrWhiteSpace(param) ? param : baseRaw;
                        string perKg = ExtractPerKgMass(src);
                        if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgVol(src);
                        if (!string.IsNullOrWhiteSpace(perKg))
                        {
                            text = CleanCalcFromText(text, param);
                            final = perKg; // reuse final slot for unified output path
                        }
                    }
                }

                // Hide parameter-only hints (avoid standalone calc lines)
                if (string.IsNullOrWhiteSpace(text)) return "";

                if (!string.IsNullOrWhiteSpace(final))
                    return CleanMedNameForDisplay($"{text}: {final}");

                return CleanMedNameForDisplay(text) ?? "";
            }

            // CHANGE NOTE (2025-09-10, mj): Update Doctor-specific text composer
            // Rules: remove verb/colon, prioritize result value + keep 1 calc fragment
            // Shock: "defibrillation at 110J (2J/kg)" / Med: "Epinephrine 0.01mg/kg = 0.55 mg"
            string ComposeTextForDoctor(SimpleJSON.JSONNode n)
            {
                if (n == null) return "";

                string tag = null;
                string baseRaw = "";
                string param = null;

                if (n.IsString)
                {
                    tag = n.Value;
                    baseRaw = InstructionFinder.FindByTag(tag, lang);
                }
                else
                {
                    tag = n["hintType"]?.Value;
                    baseRaw = string.IsNullOrWhiteSpace(tag) ? "" : InstructionFinder.FindByTag(tag, lang);
                    param = n["hintParameter"]?.Value;
                }

                string text = NormalizeUnits(baseRaw);
                if (!string.IsNullOrWhiteSpace(param)) param = NormalizeUnits(param);

                // Doctor token spacing: only apply to per-kg·J absolute values (preserve final mg/mL)
                string TightenDoctorTokens(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return s;
                    try
                    {
                        // X mg/kg, X mL/kg, X J/kg -> remove whitespace
                        s = System.Text.RegularExpressions.Regex.Replace(s, @"(\d+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL)\s*/\s*kg", m =>
                        {
                            var num = m.Groups[1].Value;
                            var unit = m.Groups[2].Value;
                            unit = unit == "ml" ? "mL" : unit; // safe replacement
                            return num + unit + "/kg";
                        }, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        s = System.Text.RegularExpressions.Regex.Replace(s, @"(\d+(?:\.[0-9]+)?)\s*J\s*/\s*kg", "$1J/kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        // Absolute energy X J -> remove whitespace
                        s = System.Text.RegularExpressions.Regex.Replace(s, @"(\d+(?:\.[0-9]+)?)\s*J\b", "$1J", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                    catch {}
                    return s.Trim();
                }

                // Shock/energy stage detection
                bool isJPerKg =
                    System.Text.RegularExpressions.Regex.IsMatch(text ?? "", @"J\s*/\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                    System.Text.RegularExpressions.Regex.IsMatch(param ?? "", @"J\s*/\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                bool isShockWord =
                    System.Text.RegularExpressions.Regex.IsMatch(text ?? "", @"defibrill|shock", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (isJPerKg || isShockWord)
                {
                    // per-kg energy extraction
                    string perKgJ = ExtractPerKgJ(param);
                    if (string.IsNullOrWhiteSpace(perKgJ)) perKgJ = ExtractPerKgJ(text);

                    // Extract absolute J: param first (last value), else text (last value)
                    string finalJ = null;
                    try
                    {
                        var rxAbsJ = new System.Text.RegularExpressions.Regex(@"(\d+(?:\.[0-9]+)?)\s*J(?![\s\u00A0]*/[\s\u00A0]*kg)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (!string.IsNullOrWhiteSpace(param))
                        {
                            var jsParam = new System.Collections.Generic.List<double>();
                            foreach (System.Text.RegularExpressions.Match m in rxAbsJ.Matches(param))
                            {
                                if (double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)) jsParam.Add(v);
                            }
                            if (jsParam.Count > 0)
                            {
                                var v = jsParam[jsParam.Count - 1];
                                finalJ = v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " J";
                            }
                        }
                        if (finalJ == null && !string.IsNullOrWhiteSpace(text))
                        {
                            var jsText = new System.Collections.Generic.List<double>();
                            foreach (System.Text.RegularExpressions.Match m in rxAbsJ.Matches(text))
                            {
                                if (double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)) jsText.Add(v);
                            }
                            if (jsText.Count > 0)
                            {
                                var v = jsText[jsText.Count - 1];
                                finalJ = v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " J";
                            }
                        }
                    }
                    catch {}

                    // per-kg only, no absolute value, and weight exists -> calculate
                    if (string.IsNullOrWhiteSpace(finalJ) && !string.IsNullOrWhiteSpace(perKgJ) && bodyWeightKg > 0)
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(perKgJ, @"(\d+(?:\.[0-9]+)?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pv))
                        {
                            double res = pv * bodyWeightKg;
                            finalJ = res.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " J";
                        }
                    }

                    // Basic text cleanup: remove verb/colon/calc parts
                    string baseName = text;
                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"^\s*(order|prepare|administer|give|for)\b\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    baseName = CleanCalcFromText(baseName, param);
                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\bfor\b\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    // Remove trailing 'at' if left after stripping energy
                    try { baseName = System.Text.RegularExpressions.Regex.Replace(baseName ?? "", @"\s+at\s*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim(); } catch {}
                    baseName = baseName.Replace(":", " ").Trim();
                    // Fix capitalization: defibrillation → Defibrillation (consistent case)
                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\bdefibrillation\b", "Defibrillation", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (string.IsNullOrWhiteSpace(baseName)) baseName = "defibrillation"; // fallback

                    // Final composition: "<base> at <absoluteJ> (<perKgJ>)"
                    string composed = baseName;
                    if (!string.IsNullOrWhiteSpace(finalJ)) composed += " at " + finalJ;
                    if (!string.IsNullOrWhiteSpace(perKgJ)) composed += " (" + perKgJ + ")";
                    composed = TidySpacing(composed);
                    return TightenDoctorTokens(composed);
                }
                else
                {
                    // Drug route: prefer mg/kg, else mL/kg; if result value, '= value'
                    string medName = text;
                    medName = System.Text.RegularExpressions.Regex.Replace(medName, @"^\s*(order|prepare|administer|give)\b\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    medName = CleanCalcFromText(medName, param);
                    medName = medName.Replace(":", " ").Trim();
                    // Sentence start capitalization
                    if (!string.IsNullOrEmpty(medName))
                    {
                        try { medName = char.ToUpperInvariant(medName[0]) + (medName.Length > 1 ? medName.Substring(1) : ""); } catch {}
                    }

                    // per-kg selection (mass → volume → other)
                    string perKg = ExtractPerKgMass(!string.IsNullOrWhiteSpace(param) ? param : baseRaw);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgMass(text);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgVol(!string.IsNullOrWhiteSpace(param) ? param : baseRaw);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgVol(text);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgOther(!string.IsNullOrWhiteSpace(param) ? param : baseRaw);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgOther(text);

                    // Final volume
                    string final = ExtractFinalValue(param);
                    if (string.IsNullOrWhiteSpace(final) && bodyWeightKg > 0)
                    {
                        string source = !string.IsNullOrWhiteSpace(param) ? param : baseRaw;
                        final = TryComputePerKgFinal(source, bodyWeightKg);
                    }

                    // Do NOT normalize units on name part
                    string namePart = CleanMedNameForDisplay(medName);
                    string dosePart = "";
                    if (!string.IsNullOrWhiteSpace(perKg)) dosePart += (dosePart.Length>0?" ":"") + perKg;
                    if (!string.IsNullOrWhiteSpace(final)) dosePart += (dosePart.Length>0?" = ":"=") + final;
                    dosePart = TidySpacing(dosePart);
                    string composed = string.IsNullOrWhiteSpace(dosePart) ? namePart : (namePart + " " + dosePart);
                    return TightenDoctorTokens(composed);
                }
            }

            // Canonicalize content for dedup (keep digits, remove bullets, collapse whitespace, lower-case)
            string Canonicalize(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                s = NormalizeUnits(s);
                s = s.Replace("•", "").Trim().ToLowerInvariant();
                var parts = s.Split(new char[]{' ','\t','\r','\n'}, System.StringSplitOptions.RemoveEmptyEntries);
                return string.Join(" ", parts);
            }

            // Build dedup key using tag + canonicalized composed text, so only fully identical entries dedup
            string DedupKeyFromNode(SimpleJSON.JSONNode n)
            {
                if (n == null) return "";
                // Use the same composed display string for dedup so Doctor/Nurse panels dedup consistently
                string tag0 = "";
                if (n.IsString)
                {
                    tag0 = n.Value ?? "";
                }
                else
                {
                    tag0 = n["hintType"]?.Value ?? "";
                }
                string display = ComposeTextFinal(n);
                return tag0 + "|" + Canonicalize(display);
            }


            
            if (obj["cprNurseHintsModel"] == null) {
                
            } else if (useServerHintsForNurseCurrent) {
                // CHANGE NOTE (2025-09-02, mj): Guarded by useServerHintsForNurseCurrent.
                // Why: Avoid overwriting Medication Orders populated by medication() while PREPARING/READY.
                SimpleJSON.JSONNode instrunctions = obj["cprNurseHintsModel"]["primaryHints"];
                Debug.Log(instrunctions);

                for(int i = 0; i < instrunctions.Count; i++) 
                {
                    /* Old Code below
                    string instruction = instrunctions[i];
                    if (!String.IsNullOrWhiteSpace(instruction)) {
                        instruction = InstructionFinder.FindByTag(instruction, lang);
                    }
                    */
                    // Compose: General text + final value (if any) then UI wrapping
                    string instruction = ComposeTextFinal(instrunctions[i]);
                    instruction = WrapMedicationLineForUI(instruction);
                    
                    if (i == 0) {
                        if (Nurse_Cur_1 != null) {
                            Nurse_Cur_1.text = instruction.Replace("1)", "•");
                            if (String.IsNullOrWhiteSpace(Nurse_Cur_1.text.Replace("•", ""))) {
                                Nurse_Cur_1.text = "";
                            }
                        }
                    } else if (i == 1) {
                        if (Nurse_Cur_2 != null) {
                            Nurse_Cur_2.text = instruction.Replace("2)", "•");
                            if (String.IsNullOrWhiteSpace(Nurse_Cur_2.text.Replace("•", ""))) {
                                Nurse_Cur_2.text = "";
                            }
                        }
                    } else if (i == 2) {
                        if (Nurse_Cur_3 != null) {
                            Nurse_Cur_3.text = instruction.Replace("3)", "•");
                            if (String.IsNullOrWhiteSpace(Nurse_Cur_3.text.Replace("•", ""))) {
                                Nurse_Cur_3.text = "";
                            }
                        }
                    }
                }
            }

            if (obj["cprNurseHintsModel"] == null) {
                
            } else if (useServerHintsForNurseNext) {
                // CHANGE NOTE (2025-09-09, mj): Nurse_Next accumulates hints/overflow arrival order queue.
                SimpleJSON.JSONNode instrunctions = obj["cprNurseHintsModel"]["nextStepHints"];

                // build current set for dedup to avoid duplicates with Current(Primary)
                var curSet = new System.Collections.Generic.HashSet<string>();
                var curNodesN = obj["cprNurseHintsModel"]["primaryHints"];
                if (curNodesN != null)
                {
                    for (int ci = 0; ci < curNodesN.Count; ci++)
                    {
                        string ck = DedupKeyFromNode(curNodesN[ci]);
                        if (!string.IsNullOrWhiteSpace(ck)) curSet.Add(ck);
                    }
                }

                var seen = new System.Collections.Generic.HashSet<string>();
                var hintVals = new List<string>();

                for (int i = 0; i < instrunctions.Count; i++)
                {
                    if (hintVals.Count >= MAX_NEXT_STEPS) break;
                    string val = ComposeTextFinal(instrunctions[i]);
                    val = FormatAdvancedPreparationNurse(val);
                    val = WrapMedicationLineForUI(val);
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    // Nurse Next: keep only medication-like lines (avoid generic icons/prompts)
                    bool looksLikeDose = Regex.IsMatch(val, @"\b(mg|mcg|g|mL|mEq|U|units)\b", RegexOptions.IgnoreCase) ||
                                          val.IndexOf("/kg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          val.IndexOf('=') >= 0;
                    if (!looksLikeDose) continue;

                    string key = DedupKeyFromNode(instrunctions[i]);
                    if (dedupAgainstCurrent && curSet.Contains(key)) continue;
                    if (dedupNextSteps && !seen.Add(key)) continue;

                    hintVals.Add(val);
                }
                lastHintNext = hintVals; // remember newest hints
                for (int h = 0; h < hintVals.Count; h++) NurseNextAppendIfNew(hintVals[h]); // append to queue
                NurseNextRender(); // render now
            }

            if (obj["cprLeaderHintsModel"] == null) {
                
            } else {
                SimpleJSON.JSONNode instrunctions = obj["cprLeaderHintsModel"]["primaryHints"];

                for(int i = 0; i < instrunctions.Count; i++) 
                {
                    /*Old code below
                    string instruction = instrunctions[i];
                    if (!String.IsNullOrWhiteSpace(instruction)) {
                        instruction = InstructionFinder.FindByTag(instruction, lang);
                    }
                    */
                    // Compose: For Doctor, include simple calc without colon then apply chunk-binding wrapping
                    string instruction = ComposeTextForDoctor(instrunctions[i]);
                    instruction = WrapMedicationLineForUI(instruction);

                    if (i == 0) {
                        if (Doc_Cur_1 != null) {
                            Doc_Cur_1.text = instruction.Replace("1)", "•");
                            Debug.Log($"[EventManager] Doc_Cur_1 <= '{Doc_Cur_1.text}'");
                            if (String.IsNullOrWhiteSpace(Doc_Cur_1.text.Replace("•", ""))) {
                                Doc_Cur_1.text = "";
                            }
                        }
                    } else if (i == 1) {
                        if (Doc_Cur_2 != null) {
                            Doc_Cur_2.text = instruction.Replace("2)", "•");
                            Debug.Log($"[EventManager] Doc_Cur_2 <= '{Doc_Cur_2.text}'");
                            if (String.IsNullOrWhiteSpace(Doc_Cur_2.text.Replace("•", ""))) {
                                Doc_Cur_2.text = "";
                            }
                        }
                    } else if (i == 2) {
                        if (Doc_Cur_3 != null) {
                            Doc_Cur_3.text = instruction.Replace("3)", "•");
                            Debug.Log($"[EventManager] Doc_Cur_3 <= '{Doc_Cur_3.text}'");
                            if (String.IsNullOrWhiteSpace(Doc_Cur_3.text.Replace("•", ""))) {
                                Doc_Cur_3.text = "";
                            }
                        }
                    }
                }
            }

            if (obj["cprLeaderHintsModel"] == null) {
                
            } else {
                SimpleJSON.JSONNode instrunctions = obj["cprLeaderHintsModel"]["nextStepHints"];

                // Doctor Next Steps: General text + final value, de-dup (by tag+text), cap to MAX_NEXT_STEPS
                var curDocSet = new System.Collections.Generic.HashSet<string>();
                var curNodesD = obj["cprLeaderHintsModel"]["primaryHints"];
                if (curNodesD != null)
                {
                    for (int ci = 0; ci < curNodesD.Count; ci++)
                    {
                        string ck = DedupKeyFromNode(curNodesD[ci]);
                        if (!string.IsNullOrWhiteSpace(ck)) curDocSet.Add(ck);
                    }
                }

                var seenDoc = new System.Collections.Generic.HashSet<string>();
                int filledDoc = 0;

                // initialize
                if (Doc_Next_1 != null) Doc_Next_1.text = "";
                if (Doc_Next_2 != null) Doc_Next_2.text = "";
                if (Doc_Next_3 != null) Doc_Next_3.text = "";

                for(int i = 0; i < instrunctions.Count; i++) 
                {
                    if (filledDoc >= MAX_NEXT_STEPS) break; 

                    // Doctor: include simple calc and no colon; apply chunk-binding wrapping
                    string val = ComposeTextForDoctor(instrunctions[i]);
                    val = WrapMedicationLineForUI(val);
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    string key = DedupKeyFromNode(instrunctions[i]);
                    if (dedupAgainstCurrent && curDocSet.Contains(key)) continue; 
                    if (dedupNextSteps && !seenDoc.Add(key)) continue;

                    if (filledDoc == 0 && Doc_Next_1 != null) {
                        Doc_Next_1.text = val;
                        filledDoc++;
                        continue;
                    }
                    if (filledDoc == 1 && Doc_Next_2 != null) {
                        Doc_Next_2.text = val;
                        filledDoc++;
                        continue;
                    }
                    if (filledDoc == 2 && Doc_Next_3 != null) {
                        Doc_Next_3.text = val;
                        filledDoc++;
                        continue;
                    }
                }
            }
        } catch (Exception e) {
            Debug.Log(e);
        } finally {
            // index++;
        }
    }

    public void Init_Tasks()
    {
        if (Doc_Cur_1 != null) {
            Doc_Cur_1.text = "";
        }
        if (Doc_Cur_2 != null) {
            Doc_Cur_2.text = "";
        }
        if (Doc_Cur_3 != null) {
            Doc_Cur_3.text = "";
        }
        if (Doc_Next_1 != null) {
            Doc_Next_1.text = "";
        }
        if (Doc_Next_2 != null) {
            Doc_Next_2.text = "";
        }
        if (Doc_Next_3 != null) {
            Doc_Next_3.text = "";
        }
        if (Nurse_Cur_1 != null) {
            Nurse_Cur_1.text = "";
        }
        if (Nurse_Cur_2 != null) {
            Nurse_Cur_2.text = "";
        }
        if (Nurse_Cur_3 != null) {
            Nurse_Cur_3.text = "";
        }
        if (Nurse_Next_1 != null) {
            Nurse_Next_1.text = "";
        }
        if (Nurse_Next_2 != null) {
            Nurse_Next_2.text = "";
        }
        if (Nurse_Next_3 != null) {
            Nurse_Next_3.text = "";
        }
    }

    public void UpdateClock()
    {
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();
        bool onOff = ((int) (Time.time * 10)) % 6 == 0 || ((int) (Time.time * 10)) % 6 == 1 || ((int) (Time.time * 10)) % 6 == 2;

        // if (timer1 != null && time1 > 0) {
        //     //time1 -= Time.deltaTime;
        //     time1 = (cprStartTimestamp - unixTime) / 1000;
        //     string min = ((int)time1 / 60 % 60 ).ToString();
        //     if (min.Length == 1) {
        //         min = "0" + min;
        //     }
        //     string sec = ((int)time1 % 60 ).ToString();
        //     if (sec.Length == 1) {
        //         sec = "0" + sec;
        //     }
        //     timer1.text = min + ":" + sec;
        // } else if (timer1 != null && time1 <= 0 && timer1.text != "00:00"){
        //     timer1.text = "00:00";
        // }

        // if (timer1 != null && cprStartTimestamp != 0) {
        //     if ((int)time1 <= 0) {
        //         if (onOff) {
        //             if (cpr_5sec == false) {
        //                 if (cpr_5sec_coroutine == false)
        //                 {
        //                     StartCoroutine(SetCPR_5Sec(true));
        //                     //Initial reach to 0 sec
        //                     if (notiCprArr.Count == 0) {
        //                         UpdateNoti("", "", 1);
        //                     }
        //                 }
        //                 CPR_Plate.material = mat[5];
        //             }
        //         } else {
        //             CPR_Plate.material = mat[6];
        //         }
        //     } else if ((int)time1 <= 10) {
        //         if (onOff) {
        //             CPR_Plate.material = mat[1];
        //         } else {
        //             CPR_Plate.material = mat[2];
        //         }
        //     } else if ((int)time1 > 10) {
        //         CPR_Plate.material = mat[2];
        //     }
        // }

        if (timer1 != null) {
            //time2 -= Time.deltaTime;
            time1 = (cprStartTimestamp - unixTime) / 1000;
            if (time1 > 0) {
                string min = ((int)time1 / 60 % 60 ).ToString();
                if (min.Length == 1) {
                    min = "0" + min;
                }
                string sec = ((int)time1 % 60 ).ToString();
                if (sec.Length == 1) {
                    sec = "0" + sec;
                }
                    timer1.text = min + ":" + sec;
            } else if (cprStartTimestamp != 0) {
                double time1_temp = time1 * -1;
                string min = ((int)time1_temp / 60 % 60 ).ToString();
                if (min.Length == 1) {
                    min = "0" + min;
                }
                string sec = ((int)time1_temp % 60 ).ToString();
                if (sec.Length == 1) {
                    sec = "0" + sec;
                }
                timer1.text = "-" + min + ":" + sec;

                for (int i = 0; i < notiCprArr.Count; i++) {
                    GameObject temp = (GameObject)notiCprArr[i];
                    TextMeshProUGUI txt = temp.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    txt.text = "-" + min + ":" + sec;
                }
            } else if (timer1 != null && time1 <= 0 && cprStartTimestamp == 0) {
            // } else if (timer1 != null && time2 <= 0 && timer2.text != "00:00"){
                timer1.text = "00:00";
                CPR_Plate.material = mat[2];
            }
        }
        if (timer1 != null && cprStartTimestamp != 0) {
            if ((int)time1 <= 0) {
                if (onOff) {
                    if (cpr_5sec == false) {
                        if (cpr_5sec_coroutine == false)
                        {
                            StartCoroutine(SetCPR_5Sec(true));
                            if (notiCprArr.Count == 0) {
                                UpdateNoti("", "", 1);
                            }
                        }
                        CPR_Plate.material = mat[5];
                    }
                } else {
                    CPR_Plate.material = mat[6];
                }
            } else if ((int)time1 <= 10) {
                if (onOff) {
                    CPR_Plate.material = mat[1];
                } else {
                    CPR_Plate.material = mat[2];
                }
            } else if ((int)time1 > 10) {
                CPR_Plate.material = mat[2];
            }
        }

        ///CPR end
        ///Epi start

        if (timer2 != null) {
            //time2 -= Time.deltaTime;
            time2 = (epiStartTimestamp - unixTime) / 1000;
            if (time2 > 0) {
                string min = ((int)time2 / 60 % 60 ).ToString();
                if (min.Length == 1) {
                    min = "0" + min;
                }
                string sec = ((int)time2 % 60 ).ToString();
                if (sec.Length == 1) {
                    sec = "0" + sec;
                }
                    timer2.text = min + ":" + sec;
            } else if (epiStartTimestamp != 0) {
                double time2_temp = time2 * -1;
                string min = ((int)time2_temp / 60 % 60 ).ToString();
                if (min.Length == 1) {
                    min = "0" + min;
                }
                string sec = ((int)time2_temp % 60 ).ToString();
                if (sec.Length == 1) {
                    sec = "0" + sec;
                }
                timer2.text = "-" + min + ":" + sec;

                for (int i = 0; i < notiEpiArr.Count; i++) {
                    GameObject temp = (GameObject) notiEpiArr[i];
                    TextMeshProUGUI txt = temp.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    txt.text = "-" + min + ":" + sec;
                }
            } else if (timer2 != null && time2 <= 0 && epiStartTimestamp == 0) {
            // } else if (timer2 != null && time2 <= 0 && timer2.text != "00:00"){
                timer2.text = "00:00";
                Epi_Plate.material = mat[4];
            }
        }
        if (timer2 != null && epiStartTimestamp != 0) {
            if ((int)time2 <= 0) {
                if (onOff) {
                    if (epi_5sec == false) {
                        if (epi_5sec_coroutine == false)
                        {
                            StartCoroutine(SetEpi_5Sec(true));
                            if (notiEpiArr.Count == 0) {
                                UpdateNoti("", "", 2);
                            }
                        }
                        Epi_Plate.material = mat[5];
                    }
                } else {
                    Epi_Plate.material = mat[6];
                }
            } else if ((int)time2 <= 10) {
                if (onOff) {
                    Epi_Plate.material = mat[3];
                } else {
                    Epi_Plate.material = mat[4];
                }
            } else if ((int)time2 > 10) {
                Epi_Plate.material = mat[4];
            }
        }
    }

    void FlashNoti() {

        bool onOff = ((int) (Time.time * 10)) % 6 == 0 || ((int) (Time.time * 10)) % 6 == 1 || ((int) (Time.time * 10)) % 6 == 2;

        for (int i = 0; i < notiArr.Count; i++) {
            GameObject temp = (GameObject) notiArr[i];
            if (onOff) {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[7];
            } else {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[8];
            }
        }

        for (int i = 0; i < notiCprArr.Count; i++) {
            GameObject temp = (GameObject) notiCprArr[i];
            if (onOff) {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[5];
            } else {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[6];
            }
        }

        for (int i = 0; i < notiEpiArr.Count; i++) {
            GameObject temp = (GameObject) notiEpiArr[i];
            if (onOff) {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[5];
            } else {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[6];
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateClock();
        FlashNoti();
        while (m_queueAction.Count > 0)
        {
            m_queueAction.Dequeue().Invoke();
        }
    }

    public void ToMain()
    { 
        SceneManager.LoadScene("main_scene");
    }

    public void Doctor1()
    { 
        SceneManager.LoadScene("hmd_doctor_1");
    }

    public void Doctor2()
    { 
        SceneManager.LoadScene("hmd_doctor_2");
    }

    public void Doctor3()
    { 
        SceneManager.LoadScene("hmd_doctor_3");
    }

    public void Doctor4()
    { 
        SceneManager.LoadScene("hmd_doctor_4");
    }

    public void Doctor5()
    { 
        SceneManager.LoadScene("hmd_doctor_5");
    }

    public void Doctor6()
    { 
        SceneManager.LoadScene("hmd_doctor_6");
    }

    public void Doctor7()
    { 
        SceneManager.LoadScene("hmd_doctor_7");
    }

    public void Doctor8()
    { 
        SceneManager.LoadScene("hmd_doctor_8");
    }

    public void Doctor9()
    { 
        SceneManager.LoadScene("hmd_doctor_9");
    }


    public void Nurse1()
    { 
        SceneManager.LoadScene("hmd_nurse_1");
    }

    public void Nurse2()
    { 
        SceneManager.LoadScene("hmd_nurse_2");
    }

    public void Nurse3()
    { 
        SceneManager.LoadScene("hmd_nurse_3");
    }

    public void Nurse4()
    { 
        SceneManager.LoadScene("hmd_nurse_4");
    }

    public void Nurse5()
    { 
        SceneManager.LoadScene("hmd_nurse_5");
    }

    public void initializeSessions()
    {
        Debug.Log(sessionArr.Count);
        //Initialize session list
        int sessionCount = sessionArr.Count;

        for (int i = 0; i < sessionCount; i++) {
            GameObject go = (GameObject) sessionArr[0];

            go.SetActive(false);
            sessionArr.RemoveAt(0);
            Destroy(go, 0.0f);
        }
        
        Debug.Log(sessionArr.Count);
    }

    public void getSessions()
    {
        //Initialize session list
        initializeSessions();

        //Connection for the live streaming

        if (sessions != null && sessionsTransform != null) {
            if (sessionPref != null) {
                GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                txt.text = FindMultiLang("Receiving sessions from server...");

                sessionArr.Add(myInstance);
            }
        }

        StartCoroutine(GetProcesses("https://interface-ar.unige.ch/care-processes"));
    }

    IEnumerator GetProcesses(string URL)
    {
        using(UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                txt.text = FindMultiLang("Communication Error");

                sessionArr.Add(myInstance);
            }
            else
            {
                string json = request.downloadHandler.text;
                SimpleJSON.JSONNode sessionJSONArr = SimpleJSON.JSON.Parse(json);
                initializeSessions();

                foreach (SimpleJSON.JSONObject sessionJSON in sessionJSONArr)
                {
                    Debug.Log(sessionJSON);
                    if (sessions != null && sessionsTransform != null) {
                        if (sessionPref != null) {
                            GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                            TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                            txt.text = sessionJSON["shortCode"];

                            sessionArr.Add(myInstance);
                            PressableButton btn = myInstance.transform.GetComponent<PressableButton>();
                            btn.OnClicked.AddListener(() => {
                                Init_Tasks();
                                startConnection(sessionJSON["processId"], sessionJSON["shortCode"]);
                                StartCoroutine(algoInit(sessionJSON["processId"]));
                                StartCoroutine(medicationInitialize(sessionJSON["processId"]));
                                StartCoroutine(timerInit(sessionJSON["processId"]));
                            });
                        }
                    }
                }
                if (sessionPref != null) {
                    if (sessionJSONArr.Count == 0) {
                        GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                        TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                        txt.text = FindMultiLang("There are currently no active sessions.");

                        sessionArr.Add(myInstance);
                    }
                }
            }

        }
    }

    public void currentStatus (SimpleJSON.JSONNode response) {
#if UNITY_EDITOR
        Debug.Log("currentStatus");
        Debug.Log(response);
#endif

        // Debug.Log("idx: " + idx);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();

        SimpleJSON.JSONNode cprProtocolModel = response["cprProtocolModel"];
        SimpleJSON.JSONNode cprHintModel = response["cprHintModel"];
        SimpleJSON.JSONNode patientModel = response["patientModel"];

#if UNITY_EDITOR
        Debug.Log(cprProtocolModel);
#endif

        if (cprProtocolModel != null) {
            m_queueAction.Enqueue(() => {
                UpdateUI(cprProtocolModel);
                algo(cprProtocolModel["steps"]);
            });

            SimpleJSON.JSONNode cursor = cprProtocolModel["cursor"];

            if (cursor != null) {
                // "cursor":{"type":"TASK","stepId":"EXIT","status":"COMPLETED","subType":"GENERIC"}
                if (cursor["type"] == "TASK" && cursor["stepId"] == "EXIT" && cursor["status"] == "COMPLETED") {
                    //End of Session
                    CurrentSession.text = FindMultiLang("None");
                    // MedicationFinder.setProcessId(processId);
                    epiStartTimestamp = 0;
                    prev_epiStartTimestamp = 0;
                    cprStartTimestamp = 0;
                    prev_cprStartTimestamp = 0;

                    medications = null;

                    if (resTabOrderIcon != null) resTabOrderIcon.enabled = false;
                    if (intTabOrderIcon != null) intTabOrderIcon.enabled = false;
                    if (hypTabOrderIcon != null) hypTabOrderIcon.enabled = false;
                }
            }
        }

        if (cprHintModel != null) {
            m_queueAction.Enqueue(() => {
                UpdateInstructions(cprHintModel);
            });
        }

        if (patientModel != null) {
            string weightStr = patientModel["weight"];
            if (weightStr != null){
                // CHANGE NOTE (2025-09-01, mj): Persist body weight for dose computations later in the same session.
                double w;
                if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out w)) {
                    bodyWeightKg = w;
                }
                m_queueAction.Enqueue(() => {
                    GameObject bw = GameObject.Find("BodyWeight");
                    if (bw != null) {
                        TextMeshProUGUI txt = bw.GetComponent<TextMeshProUGUI>();
                        if (lang == "fr")
                            txt.text = $" Poids corporel : {weightStr} kg";
                        else
                            txt.text = $" Body weight: {weightStr} kg";
                    }
                });
            }
        }

        // Detect Confirmation of Medication
        SimpleJSON.JSONNode cprMedicationModel = response["cprMedicationModel"];
        if (cprMedicationModel != null) {
            // CHANGE NOTE (2025-09-12, mj): SSE medication update optimization
            // Instead of requesting new medication model on every SSE, buffer and apply only after initialization is complete
            _lastMedicationModelFromSse = cprMedicationModel;
            if (_medInitDone)
            {
                m_queueAction.Enqueue(() => medication(_lastMedicationModelFromSse));
            }
            SimpleJSON.JSONNode meds = cprMedicationModel["medicationModels"];
            if (meds.Count == 1) {
                    string medID = meds[0]["id"];
                    var medinfo = MedicationFinder.FindByTag(medID, lang);
                    Debug.Log(medID);
                    string id = medinfo[0];
                    string dose = medinfo[1];
                    
                    if (meds[0]["doses"] != null && meds[0]["doses"].Count > 0) {
                        string lit = meds[0]["doses"][0]["lastInjectionTime"];
                        SimpleJSON.JSONNode di = meds[0]["doses"][0]["doseInstances"];
                        if (lit != null && di != null) {
                            if (di[0]["injectionTime"] == lit && di[0]["status"] == "DONE") {
                                if (meds[0]["doses"][0]["label"] != null) {
                                    m_queueAction.Enqueue(() => UpdateNoti(id, meds[0]["doses"][0]["label"], 0));
                                } else {
                                    m_queueAction.Enqueue(() => UpdateNoti(id, dose, 0));
                                }
                            } 
                        }
                    }
            }

            // CONFIRM event detection (DONE + latest)
            if (meds != null) {
                foreach (SimpleJSON.JSONNode med in meds) {
                    string medIdStr = med["id"];
                    int medIdNum = med["id"];
                    var medinfo = MedicationFinder.FindByTag(medIdStr, lang);
                    string medName = medinfo[0];
                    string defaultDose = medinfo[1];

                    if (med["doses"] == null) continue;
                    foreach (SimpleJSON.JSONNode dose in med["doses"]) {
                        string last = dose["lastInjectionTime"];
                        if (string.IsNullOrEmpty(last)) continue;

                        string doseIdStr = dose["id"];
                        SimpleJSON.JSONNode diArr = dose["doseInstances"];
                        if (diArr == null) continue;

                        foreach (SimpleJSON.JSONNode di in diArr) {
                            string status = di["status"];
                            string inj = di["injectionTime"];
                            if (status == "DONE" && inj == last) {
                                string key = medIdStr + ":" + doseIdStr + ":" + inj;
                                if (_confirmedOnce.Add(key)) {
                                    string doseLabel = dose["label"];
                                    if (doseLabel == null) doseLabel = defaultDose;

                                    bool isAmio = false;
                                    try { isAmio = medIdNum == 1; } catch {}
                                    bool isEpi = false;
                                    try { isEpi = medIdNum == 5; } catch {}

                                    bool callUpdateNoti = meds.Count > 1; // avoid duplicate noti with the single-med branch
                                    m_queueAction.Enqueue(() => {
                                        if (isAmio) HighlightAmiodaroneOrder(false);
                                        if (isEpi) {
                                            HighlightEpinephrineOrder(false);
                                            // dismiss Epi overdue notification immediately
                                            DismissEpiOverdueNoti();
                                        }
                                        ConfirmFlashOrderDisplay(medName);
                                        // CHANGE NOTE (2025-09-09, mj): dismiss Nurse_Next immediately when medication is completed
                                        NurseNextRemoveByMedName(medName);
                                        NurseNextRender();
                                        if (callUpdateNoti) UpdateNoti(medName, doseLabel, 0);
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        // m_queueAction.Enqueue(() => medication(cprMedicationModel));
    
        SimpleJSON.JSONNode cprTimersModel = cprProtocolModel["cprTimersModel"];

        Debug.Log(cprTimersModel);
        try {
            if (cprTimersModel != null) {
                if (cprTimersModel["adrenalineTimer"] != null) {
                    DateTime dt = DateTime.Parse((string) cprTimersModel["adrenalineTimer"]);
                    epiStartTimestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();

                    time2 = (epiStartTimestamp - unixTime) / 1000;
                    Debug.Log("adrenalineTimer");
                    Debug.Log(time2);
                    Debug.Log("adrenalineTimer");
                    if (prev_epiStartTimestamp != epiStartTimestamp) {
                        StartCoroutine(SetEpi_5Sec(false));
                        epi_5sec = false;
                        epi_5sec_coroutine = false;
                        prev_epiStartTimestamp = epiStartTimestamp;
                    }
                }
            }

            if (cprTimersModel != null) {
                // even if server doesn't send cprTimerOn, consider it a start just by cprTimer's existence
                bool cprOn = false; try { if (cprTimersModel["cprTimerOn"] != null) cprOn = cprTimersModel["cprTimerOn"].AsBool; } catch {}
                var cprTimerNode = cprTimersModel["cprTimer"];
                string cprTimerStr = cprTimerNode;

                if (!string.IsNullOrWhiteSpace(cprTimerStr)) {
                    // timer start/restart
                    DateTime dt = DateTime.Parse(cprTimerStr);
                    cprStartTimestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                    time1 = (cprStartTimestamp - unixTime) / 1000;
                    Debug.Log("cprTimer");
                    Debug.Log(time1);
                    Debug.Log("cprTimer");
                    if (prev_cprStartTimestamp != cprStartTimestamp) {
                        StartCoroutine(SetCPR_5Sec(false));
                        // dismiss notification and flag immediately
                        DismissCprOverdueNoti();
                        prev_cprStartTimestamp = cprStartTimestamp;
                    }
                } else if (cprTimersModel["cprTimerOn"] != null && cprOn == false) {
                    // timer OFF explicitly
                    cprStartTimestamp = 0;
                    prev_cprStartTimestamp = 0;
                    cpr_5sec = false;
                    cpr_5sec_coroutine = false;
                }
            }
        } catch (Exception e) {
            Debug.Log(e);
        }

        // CHANGE NOTE (2025-09-12, mj): Rendering throttling for Nurse medication panel
        // Avoid excessive re-renders when SSE arrives while maintaining state
        m_queueAction.Enqueue(() => {
            if (IsNurseSceneActive()) {
                if (Time.unscaledTime - _lastMedUiRenderTime >= _minMedRenderIntervalSec) {
                    _lastMedUiRenderTime = Time.unscaledTime;
                    var snapshot = _lastMedicationModelFromSse != null ? _lastMedicationModelFromSse : medications;
                    if (snapshot != null) medication(snapshot);
                }
            }
        });
    }

    public Material materialFinder (string status){
        if (status == "OPEN") {
            return mat[10];
        }
        if (status == "IN_PROGRESS") {
            return mat[11];
        }
        if (status == "COMPLETED") {
            return mat[12];
        }

        return mat[9];
    }

    public void algo (SimpleJSON.JSONNode algo) {
       if (algo == null) {
           return;
       }
       try {
            //Medication Noti
            if (algo != null && mat[9] != null) {
                SimpleJSON.JSONNode storedAlgo = algoritms;
                foreach(SimpleJSON.JSONNode a in algo) {//response
                    //using json
                    foreach (SimpleJSON.JSONNode sa in storedAlgo) {//json
                        if (sa["stepId"] == a["stepId"]) {
                            sa["status"] = a["status"];
                            Debug.Log("==================================");
                            Debug.Log(sa["stepId"]);
                            Debug.Log(sa["status"]);
                            Debug.Log(sa["subType"]);
                            Debug.Log(sa["subType"] != null);
                            Debug.Log(sa["subType"] == "GENERIC");
                            Debug.Log(sa["status"] == "OPEN");
                            Debug.Log("==================================");
                            if (algoImg.ContainsKey(sa["stepId"])){
                                if (sa["subType"] != null && sa["subType"] == "GENERIC" && sa["status"] == "OPEN"){
                                    if (sa["stepId"] == "ASYSTOLIE") {
                                        algoImg["ASYSTOLIE2"].material = mat[9];
                                        if (algoImg["ASYSTOLIE2"] != null) {
                                            algoImg["ASYSTOLIE2"].transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = Color.black;
                                        }
                                    }
                                    algoImg[sa["stepId"]].material = mat[9];
                                    algoImg[sa["stepId"]].transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = Color.black;
                                } else {
                                    if (sa["stepId"] == "ASYSTOLIE") {
                                        algoImg["ASYSTOLIE2"].material = materialFinder(sa["status"]);
                                        if (materialFinder(sa["status"]) == mat[9]) {
                                            algoImg["ASYSTOLIE2"].transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = Color.black;
                                        } else {
                                            algoImg["ASYSTOLIE2"].transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = Color.white;
                                        }
                                    }
                                    algoImg[sa["stepId"]].material = materialFinder(sa["status"]);
                                    if (materialFinder(sa["status"]) == mat[9]) {
                                        algoImg[sa["stepId"]].transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = Color.black;
                                    } else {
                                        algoImg[sa["stepId"]].transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = Color.white;
                                    }
                                }
                            }
                        }
                    }
                }
            }
       } catch (Exception e) {
            Debug.Log(e);
       }
    }
    public void medication (SimpleJSON.JSONNode cprMedicationModel) {
       if (cprMedicationModel == null) {
           return;
       }
       try {
           // CHANGE NOTE (2025-09-10, mj): Diagnostic timeline logger for medication status
           // Logs PREPARING/ORDERED/READY/DONE transitions per med/dose to help highlight debugging
           // removed timeline debug logs
            //Medication Noti
            if (cprMedicationModel["medicationModels"] != null) {
                SimpleJSON.JSONNode meds = cprMedicationModel["medicationModels"];
                // Debug.Log(meds);

                //update medication orders for nurses' version
                foreach(SimpleJSON.JSONNode med in meds) {
                    string medID_ = med["id"];
                    var medinfo = MedicationFinder.FindByTag(medID_, lang);
                    // Debug.Log(medID_);
                    string id = medinfo[0];

                    //to avoid dup
                    /*if (Nurse_Cur_1 != null && Nurse_Cur_2 != null && Nurse_Cur_3 != null &&
                        id != Nurse_Cur_1.text && id != Nurse_Cur_2.text && id != Nurse_Cur_3.text) {
                        if (med["doses"] != null && med["doses"].Count > 0) {
                            foreach(SimpleJSON.JSONNode dose in med["doses"]) {
                                if (dose["doseInstances"] != null && dose["doseInstances"].Count > 0) {
                                    foreach(SimpleJSON.JSONNode doseInstance in dose["doseInstances"]) {
                                        if (doseInstance["status"] == "PREPARING") {
                                            // display medication order on nurse screen
                                            if (Nurse_Cur_1.text == "") {
                                                Nurse_Cur_1.text = id;
                                            } else if (Nurse_Cur_2.text == "") {
                                                Nurse_Cur_2.text = id;
                                            } else if (Nurse_Cur_3.text == "") {
                                                Nurse_Cur_3.text = id;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }*/
                }

                SimpleJSON.JSONNode storedMedJson = medications["medicationModels"];
                foreach(SimpleJSON.JSONNode med in meds) {//response
                    //using json
                    foreach (SimpleJSON.JSONNode medjson in storedMedJson) {//json
                        if (medjson["id"] == med["id"]) {
                            medjson["doses"] = med["doses"];
                            //Couldn't just copy the entire doses if there is an existing...
                        }
                    }
                }

                // SimpleJSON.JSONNode storedMedJson = medications["medicationModels"];
                // SimpleJSON.JSONNode tempMedJson = SimpleJSON.JSONNode.Parse(medications["medicationModels"].ToString());
                // foreach(SimpleJSON.JSONNode med in meds) {//response
                //     //using json
                //     foreach (SimpleJSON.JSONNode medjson in tempMedJson) {//json
                //         if (medjson["id"] == med["id"]) {
                //             if (medjson["doses"].Count > 0) {
                //                 foreach (SimpleJSON.JSONNode dose in med["doses"]) {
                //                     medjson["doses"].Add(dose);
                //                 }
                //             } else {
                //                 medjson["doses"] = med["doses"];
                //             }
                //             //Couldn't just copy the entire doses if there is an existing doses...
                //         }
                //     }
                // }

                // storedMedJson = tempMedJson;

                // CHANGE NOTE (2025-09-02, mj): Hybrid Nurse UI sources.
                // If server hints drive current, block medication() from writing current by starting iii=3; else allow with iii=0.
                // If server hints drive next, block medication() from writing next by starting jjj=3; else allow with jjj=0.
                int iii = useServerHintsForNurseCurrent ? 3 : 0;
                int jjj = useServerHintsForNurseNext ? 3 : 0;
                int resCount = 0;
                int intCount = 0;
                int hypCount = 0;
                
                //Medication count update for sync
                for (int i = 0; i < storedMedJson.Count; i++) {
                    //medications doses

                    int medID = storedMedJson[i]["id"];
                    foreach (SimpleJSON.JSONNode _doses in storedMedJson[i]["doses"]) {//json
                        int val = _doses["readyCounter"];
                        int preVal = _doses["preparingCounter"];
                        int doseID = _doses["id"];

                        // AmiCount 1
                        // AtroCount 2
                        // EpiCount 5
                        // LidoCount 12
                        
                        // Amiodarone
                        // Atropine
                        // Epinephrine
                        // Lidocaine

                        // Fentanyl
                        // Ketamine
                        // Midazolam
                        // Morphine
                        // Rocuronium
                        // Succinylcholine

                        // 10% Calcium Gluconate
                        // 10% Calcium Chloride
                        // Salbutamol
                        // 8.4% Sodium Bicarb
                        // Insulin
                        // Glucose
if (medID == 1) {
                            if (AmiCount == null && GameObject.FindWithTag("AmiCount") != null) {
                                AmiCount = GameObject.FindWithTag("AmiCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (AmiCount != null) {
                                AmiCount.text = val.ToString();
                            }

                            if (preVal > 0) {
                                resCount++;
                                if (Nurse_Cur_1 != null && iii == 0) { Nurse_Cur_1.text = FindMultiLang("Amiodarone") + " 125mg"; iii++; }
                                else if (Nurse_Cur_2 != null && iii == 1) { Nurse_Cur_2.text = FindMultiLang("Amiodarone") + " 125mg"; iii++; }
                                else if (Nurse_Cur_3 != null && iii == 2) { Nurse_Cur_3.text = FindMultiLang("Amiodarone") + " 125mg"; iii++; }
                                }
                                
                                // CHANGE NOTE (2025-09-04, mj)
                                // highlight the medication row when ORDERED (PREPARING/AUTO_PREPARING) and remove it when READY.
                                {
                                    SimpleJSON.JSONNode medNode1 = storedMedJson[i];
                                    bool hasReady = MedHasStatus(medNode1, "READY");
                                    bool isOrdered = MedHasAnyPreparing(medNode1); // PREPARING or AUTO_PREPARING

                                    bool highlight = isOrdered && !hasReady;

                                    HighlightAmiodaroneOrder(highlight);
                                    if (!highlight) {
                                        // not ordered or already ready -> revert to normal
                                        SetOrderNormalFor(FindMultiLang("Amiodarone"));
                                    }
                                }
                        }

                        if (medID == 2) {
                            if (AtroCount == null && GameObject.FindWithTag("AtroCount") != null) {
                                AtroCount = GameObject.FindWithTag("AtroCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (AtroCount != null) {
                                AtroCount.text = val.ToString();
                            }
                            if (preVal > 0) {
                                resCount++;
                                if (Nurse_Next_1 != null && jjj == 0) {
                                    Nurse_Next_1.text = FindMultiLang("Atropine") + " 0.5 mL";
                                    jjj++;
                                } else if (Nurse_Next_2 != null && jjj == 1) {
                                    Nurse_Next_2.text = FindMultiLang("Atropine") + " 0.5 mL";
                                    jjj++;
                                } else if (Nurse_Next_3 != null && jjj == 2) {
                                    Nurse_Next_3.text = FindMultiLang("Atropine") + " 0.5 mL";
                                    jjj++;
                                }
                            }
                        }
                        if (medID == 5) {
                            if (EpiCount == null && GameObject.FindWithTag("EpiCount") != null) {
                                EpiCount = GameObject.FindWithTag("EpiCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (EpiCount != null) {
                                EpiCount.text = val.ToString();
                            }
                            if (preVal > 0) {
                                resCount++;
                                if (Nurse_Cur_1 != null && iii == 0) {
                                    Nurse_Cur_1.text = FindMultiLang("Epinephrine") + " 0.25 mg";
                                    iii++;
                                } else if (Nurse_Cur_2 != null && iii == 1) {
                                    Nurse_Cur_2.text = FindMultiLang("Epinephrine") + " 0.25 mg";
                                    iii++;
                                } else if (Nurse_Cur_3 != null && iii == 2) {
                                    Nurse_Cur_3.text = FindMultiLang("Epinephrine") + " 0.25 mg";
                                    iii++;
                                }
                            }
                            // CHANGE NOTE (2025-09-04, mj)
                            // Medication row should be yellow when ORDERED and removed when READY. DONE should not affect.

                            {
                                SimpleJSON.JSONNode medNode5 = storedMedJson[i];
                                bool hasReady = MedHasStatus(medNode5, "READY");
                                bool isOrdered = MedHasAnyPreparing(medNode5);

                                bool highlight = isOrdered && !hasReady;

                                HighlightEpinephrineOrder(highlight);
                                if (!highlight) {
                                    SetOrderNormalFor(FindMultiLang("Epinephrine"));
                                }
                            }
                        }
                        if (medID == 12) {
                            if (LidoCount == null && GameObject.FindWithTag("LidoCount") != null) {
                                LidoCount = GameObject.FindWithTag("LidoCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (LidoCount != null) {
                                LidoCount.text = val.ToString();
                            }
                            if (preVal > 0) {
                                resCount++;
                                if (Nurse_Next_1 != null && jjj == 0) {
                                    Nurse_Next_1.text = FindMultiLang("Lidocaine") + " 25 mg";
                                    jjj++;
                                } else if (Nurse_Next_2 != null && jjj == 1) {
                                    Nurse_Next_2.text = FindMultiLang("Lidocaine") + " 25 mg";
                                    jjj++;
                                } else if (Nurse_Next_3 != null && jjj == 2) {
                                    Nurse_Next_3.text = FindMultiLang("Lidocaine") + " 25 mg";
                                    jjj++;
                                }
                            }
                        }
                        // FenCount 7
                        // KenCount 11
                        // MidCount 13
                        // MorCount 14
                        // RocCount 16
                        // SucCount 19
                        if (medID == 7) {
                            if (doseID == 9) {
                                if (FenCount == null && GameObject.FindWithTag("FenCount") != null) {
                                    FenCount = GameObject.FindWithTag("FenCount").GetComponent<TextMeshProUGUI>();
                                }
                                
                                if (FenCount != null) {
                                    FenCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    intCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Fentanyl") + " 100 mcg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Fentanyl") + " 100 mcg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Fentanyl") + " 100 mcg";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 11) {
                            if (doseID == 16) {
                                if (KenCount == null && GameObject.FindWithTag("KenCount") != null) {
                                    KenCount = GameObject.FindWithTag("KenCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (KenCount != null) {
                                    KenCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    intCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Ketamine") + " 50 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Ketamine") + " 50 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Ketamine") + " 50 mg";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 13) {
                            if (doseID == 19) {
                                if (MidCount == null && GameObject.FindWithTag("MidCount") != null) {
                                    MidCount = GameObject.FindWithTag("MidCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (MidCount != null) {
                                    MidCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    intCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Midazolam") + " 5 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Midazolam") + " 5 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Midazolam") + " 5 mg";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 14) {
                            if (doseID == 21) {
                                if (MorCount == null && GameObject.FindWithTag("MorCount") != null) {
                                    MorCount = GameObject.FindWithTag("MorCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (MorCount != null) {
                                    MorCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    intCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Morphine") + " 2.5 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Morphine") + " 2.5 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Morphine") + " 2.5 mg";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 16) {
                            if (doseID == 24) {
                                if (RocCount == null && GameObject.FindWithTag("RocCount") != null) {
                                    RocCount = GameObject.FindWithTag("RocCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (RocCount != null) {
                                    RocCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    intCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Rocuronium") + " 25 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Rocuronium") + " 25 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Rocuronium") + " 25 mg";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 19) {
                            if (doseID == 29) {
                                if (SucCount == null && GameObject.FindWithTag("SucCount") != null) {
                                    SucCount = GameObject.FindWithTag("SucCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (SucCount != null) {
                                    SucCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    intCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Succinylcholine") + " 50 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Succinylcholine") + " 50 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Succinylcholine") + " 50 mg";
                                        jjj++;
                                    }
                                }
                            }
                        }

                        // CalGCount 4
                        // CalG100Count 5
                        // CalCCount 3
                        // SalCount 17
                        // SodCount 18
                        // InsCount 9
                        // GluCount 8

                        if (medID == 4) {
                            if (doseID == 4) {
                                if (CalGCount == null && GameObject.FindWithTag("CalGCount") != null) {
                                    CalGCount = GameObject.FindWithTag("CalGCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (CalGCount != null) {
                                    CalGCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    hypCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("10% Calcium Gluconate") + " 1,500 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("10% Calcium Gluconate") + " 1,500 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("10% Calcium Gluconate") + " 1,500 mg";
                                        jjj++;
                                    }
                                }
                            } 
                            
                            if (doseID == 5) {
                                if (CalG100Count == null && GameObject.FindWithTag("CalG100Count") != null) {
                                    CalG100Count = GameObject.FindWithTag("CalG100Count").GetComponent<TextMeshProUGUI>();
                                }
                                if (CalG100Count != null) {
                                    CalG100Count.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    hypCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("10% Calcium Gluconate") + " 2,500 mg";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("10% Calcium Gluconate") + " 2,500 mg";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("10% Calcium Gluconate") + " 2,500 mg";
                                        jjj++;
                                    }
                                }
                            }
                        }

                        if (medID == 3) {
                            if (CalCCount == null && GameObject.FindWithTag("CalCCount") != null) {
                                CalCCount = GameObject.FindWithTag("CalCCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (CalCCount != null) {
                                CalCCount.text = val.ToString();
                            }
                            if (preVal > 0) {
                                hypCount++;
                                if (Nurse_Next_1 != null && jjj == 0) {
                                    Nurse_Next_1.text = FindMultiLang("10% Calcium Chloride") + " 500 mg";
                                    jjj++;
                                } else if (Nurse_Next_2 != null && jjj == 1) {
                                    Nurse_Next_2.text = FindMultiLang("10% Calcium Chloride") + " 500 mg";
                                    jjj++;
                                } else if (Nurse_Next_3 != null && jjj == 2) {
                                    Nurse_Next_3.text = FindMultiLang("10% Calcium Chloride") + " 500 mg";
                                    jjj++;
                                }
                            }
                        }
                        if (medID == 17) {
                            if (doseID == 25) {
                                if (SalCount == null && GameObject.FindWithTag("SalCount") != null) {
                                    SalCount = GameObject.FindWithTag("SalCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (SalCount != null) {
                                    SalCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    hypCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("Salbutamol") + " 0.75 mL";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("Salbutamol") + " 0.75 mL";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("Salbutamol") + " 0.75 mL";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 18) {
                            if (doseID == 26) {
                                if (SodCount == null && GameObject.FindWithTag("SodCount") != null) {
                                    SodCount = GameObject.FindWithTag("SodCount").GetComponent<TextMeshProUGUI>();
                                }
                                if (SodCount != null) {
                                    SodCount.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    hypCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("8.4% Sodium Bicarb") + " 25 mEq";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("8.4% Sodium Bicarb") + " 25 mEq";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("8.4% Sodium Bicarb") + " 25 mEq";
                                        jjj++;
                                    }
                                }
                            }
                            
                            if (doseID == 28) {
                                if (Sod2Count == null && GameObject.FindWithTag("Sod2Count") != null) {
                                    Sod2Count = GameObject.FindWithTag("Sod2Count").GetComponent<TextMeshProUGUI>();
                                }
                                if (Sod2Count != null) {
                                    Sod2Count.text = val.ToString();
                                }
                                if (preVal > 0) {
                                    hypCount++;
                                    if (Nurse_Next_1 != null && jjj == 0) {
                                        Nurse_Next_1.text = FindMultiLang("8.4% Sodium Bicarb") + " 50 mEq";
                                        jjj++;
                                    } else if (Nurse_Next_2 != null && jjj == 1) {
                                        Nurse_Next_2.text = FindMultiLang("8.4% Sodium Bicarb") + " 50 mEq";
                                        jjj++;
                                    } else if (Nurse_Next_3 != null && jjj == 2) {
                                        Nurse_Next_3.text = FindMultiLang("8.4% Sodium Bicarb") + " 50 mEq";
                                        jjj++;
                                    }
                                }
                            }
                        }
                        if (medID == 9) {
                            if (InsCount == null && GameObject.FindWithTag("InsCount") != null) {
                                InsCount = GameObject.FindWithTag("InsCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (InsCount != null) {
                                InsCount.text = val.ToString();
                            }
                            if (preVal > 0) {
                                hypCount++;
                                if (Nurse_Next_1 != null && jjj == 0) {
                                    Nurse_Next_1.text = FindMultiLang("Insulin") + " 25 U";
                                    jjj++;
                                } else if (Nurse_Next_2 != null && jjj == 1) {
                                    Nurse_Next_2.text = FindMultiLang("Insulin") + " 25 U";
                                    jjj++;
                                } else if (Nurse_Next_3 != null && jjj == 2) {
                                    Nurse_Next_3.text = FindMultiLang("Insulin") + " 25 U";
                                    jjj++;
                                }
                            }
                        }
                        if (medID == 8) {
                            if (GluCount == null && GameObject.FindWithTag("GluCount") != null) {
                                GluCount = GameObject.FindWithTag("GluCount").GetComponent<TextMeshProUGUI>();
                            }
                            if (GluCount != null) {
                                GluCount.text = val.ToString();
                            }
                            if (preVal > 0) {
                                hypCount++;
                                if (Nurse_Next_1 != null && jjj == 0) {
                                    Nurse_Next_1.text = FindMultiLang("Glucose") + " 50 mL";
                                    jjj++;
                                } else if (Nurse_Next_2 != null && jjj == 1) {
                                    Nurse_Next_2.text = FindMultiLang("Glucose") + " 50 mL";
                                    jjj++;
                                } else if (Nurse_Next_3 != null && jjj == 2) {
                                    Nurse_Next_3.text = FindMultiLang("Glucose") + " 50 mL";
                                    jjj++;
                                }
                            }
                        }
                    }
                }

                if (iii == 0) {
                    if (Nurse_Cur_1 != null) {
                        Nurse_Cur_1.text = "";
                    }
                    if (Nurse_Cur_2 != null) {
                        Nurse_Cur_2.text = "";
                    }
                    if (Nurse_Cur_3 != null) {
                        Nurse_Cur_3.text = "";
                    }
                } else if (iii == 1) {
                    if (Nurse_Cur_2 != null) {
                        Nurse_Cur_2.text = "";
                    }
                    if (Nurse_Cur_3 != null) {
                        Nurse_Cur_3.text = "";
                    }
                } else if (iii == 2) {
                    if (Nurse_Cur_3 != null) {
                        Nurse_Cur_3.text = "";
                    }
                }

                if (jjj == 0) {
                    if (Nurse_Next_1 != null) {
                        Nurse_Next_1.text = "";
                    }
                    if (Nurse_Next_2 != null) {
                        Nurse_Next_2.text = "";
                    }
                    if (Nurse_Next_3 != null) {
                        Nurse_Next_3.text = "";
                    }
                } else if (jjj == 1) {
                    if (Nurse_Next_2 != null) {
                        Nurse_Next_2.text = "";
                    }
                    if (Nurse_Next_3 != null) {
                        Nurse_Next_3.text = "";
                    }
                } else if (jjj == 2) {
                    if (Nurse_Next_3 != null) {
                        Nurse_Next_3.text = "";
                    }
                }

                // CHANGE NOTE (2025-09-09, mj): Apply highlight to all medications
                // Enables highlighting for all medication orders, not just Amio/Epi.
                for (int hi = 0; hi < storedMedJson.Count; hi++) {
                    int medIdForHighlight = storedMedJson[hi]["id"];
                    // Skip Amio/Epi as they're handled above
                    if (medIdForHighlight == 1 || medIdForHighlight == 5) continue;
                    
                    SimpleJSON.JSONNode medNodeH = storedMedJson[hi];
                    bool hasReadyH = MedHasStatus(medNodeH, "READY");
                    bool isOrderedH = MedHasAnyPreparing(medNodeH);
                    bool highlightH = isOrderedH && !hasReadyH;
                    // removed debug
                    
                    // If this med requires Name+Dose matching, highlight rows per dose (e.g., 1500 mg / 2500 mg)
                    if (_requireDoseMedIds.Contains(medIdForHighlight))
                    {
                        string medName = null;
                        if (_uiNameByMedId.TryGetValue(medIdForHighlight, out var nm))
                        {
                            try { medName = FindMultiLang(nm); } catch { medName = nm; }
                        }
                        // Collect all PREPARING/AUTO_PREPARING dose tokens for this medication
                        var prepDoseTokens = new System.Collections.Generic.List<string>();
                        try
                        {
                            var doses = medNodeH["doses"];
                            if (doses != null)
                            {
                                for (int di = 0; di < doses.Count; di++)
                                {
                                    var diArr = doses[di]["doseInstances"];
                                    if (diArr == null) continue;
                                    bool hasPrep = false;
                                    for (int ii = 0; ii < diArr.Count; ii++)
                                    {
                                        string st = diArr[ii]["status"];
                                        if (st == "PREPARING" || st == "AUTO_PREPARING") { hasPrep = true; break; }
                                    }
                                    if (!hasPrep) continue;
                                    string lbl = doses[di]["label"];
                                    string tok = ExtractFinalDoseToken(lbl);
                                    if (!string.IsNullOrWhiteSpace(tok) && IsDoseAllowedForMed(medIdForHighlight, tok))
                                    {
                                        prepDoseTokens.Add(tok);
                                    }
                                }
                            }
                        }
                        catch {}

                        // First clear known dose rows to avoid stale highlights
                        if (!string.IsNullOrWhiteSpace(medName))
                        {
                            if (medIdForHighlight == 4) { ApplyMedicationDoseRowHighlight(medName, "1500 mg", false); ApplyMedicationDoseRowHighlight(medName, "2500 mg", false); }
                            else if (medIdForHighlight == 18) { ApplyMedicationDoseRowHighlight(medName, "25 mEq", false); ApplyMedicationDoseRowHighlight(medName, "50 mEq", false); }
                        }

                        if (highlightH && !string.IsNullOrWhiteSpace(medName) && prepDoseTokens.Count > 0)
                        {
                            for (int k = 0; k < prepDoseTokens.Count; k++)
                            {
                                ApplyMedicationDoseRowHighlight(medName, prepDoseTokens[k], true);
                            }
                        }
                        else
                        {
                            // Also ensure any name-only highlight is cleared
                            if (!string.IsNullOrWhiteSpace(medName)) { ApplyMedicationNameOnlyHighlight(medName, false); }
                            // Finally, ensure id-based path is off
                            HighlightMedicationOrderById(medIdForHighlight, false);
                        }
                    }
                    else
                    {
                        if (medIdForHighlight == 8)
                        {
                            // Glucose: force row anchoring by current final dose if available, else fallback to simple ID name-only
                            string medName = null;
                            if (_uiNameByMedId.TryGetValue(medIdForHighlight, out var nm)) { try { medName = FindMultiLang(nm); } catch { medName = nm; } }
                            string doseLabel = null;
                            try
                            {
                                var doses = medNodeH["doses"]; if (doses != null) { for (int di = 0; di < doses.Count && doseLabel == null; di++) { var diArr = doses[di]["doseInstances"]; if (diArr == null) continue; for (int ii = 0; ii < diArr.Count; ii++) { string st = diArr[ii]["status"]; if (st == "PREPARING" || st == "AUTO_PREPARING") { doseLabel = doses[di]["label"]; break; } } } }
                            }
                            catch {}
                            string finalDose = ExtractFinalDoseToken(doseLabel);
                            if (highlightH) { ApplyMedicationRowByNameOnly(medName, true); }
                            else { ApplyMedicationRowByNameOnly(medName, false); }
                        }
                        else
                        {
                            // General meds: row-level highlight anchored by name leaf (safer)
                            string medName = null;
                            if (_uiNameByMedId.TryGetValue(medIdForHighlight, out var nmGen)) { try { medName = FindMultiLang(nmGen); } catch { medName = nmGen; } }
                            if (!string.IsNullOrWhiteSpace(medName))
                            {
                                ApplyMedicationRowByNameOnly(medName, highlightH);
                            }
                            else
                            {
                                HighlightMedicationOrderById(medIdForHighlight, highlightH);
                            }
                        }
                    }
                    if (!highlightH) {
                        SetOrderNormalForId(medIdForHighlight);
                    }
                }

                // CHANGE NOTE (2025-09-09, mj): Nurse Current 3-row rule and Next overflow implementation
                try
                {
                    if (IsNurseSceneActive())
                    {
                        var orderedOthers = new List<string>();
                        var newlyOrdered = new List<string>();
                        // Local formatter: unify Nurse display rule with Doctor (one calc shown)
                        string ComposeNurseMedLine(SimpleJSON.JSONNode medNode, string medName)
                        {
                            if (string.IsNullOrWhiteSpace(medName)) medName = "";
                            string doseLabel = null;
                            try
                            {
                                var doses = medNode != null ? medNode["doses"] : null;
                                if (doses != null)
                                {
                                    for (int di = 0; di < doses.Count && doseLabel == null; di++)
                                    {
                                        var diArr = doses[di]["doseInstances"];
                                        if (diArr == null) continue;
                                        for (int ii = 0; ii < diArr.Count; ii++)
                                        {
                                            string st = diArr[ii]["status"];
                                            if (st == "PREPARING" || st == "AUTO_PREPARING")
                                            {
                                                doseLabel = doses[di]["label"];
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch {}

                            string perKg = null;
                            string final = null;
                            try { perKg = ExtractPerKgMass(doseLabel); } catch {}
                            if (string.IsNullOrWhiteSpace(perKg)) { try { perKg = ExtractPerKgVol(doseLabel); } catch {} }
                            if (string.IsNullOrWhiteSpace(perKg)) { try { perKg = ExtractPerKgOther(doseLabel); } catch {} }
                            try { final = ExtractFinalDoseToken(doseLabel); } catch {}

                            // Do NOT normalize units on the name part
                            string namePart = CleanMedNameForDisplay(medName);
                            string dosePart = "";
                            if (!string.IsNullOrWhiteSpace(perKg)) dosePart += (dosePart.Length>0?" ":"") + perKg;
                            if (!string.IsNullOrWhiteSpace(final)) dosePart += (dosePart.Length>0?" = ":"=") + final;
                            try { dosePart = NormalizeUnits(dosePart); dosePart = TidySpacing(dosePart); } catch {}
                            string composed = string.IsNullOrWhiteSpace(dosePart) ? namePart : (namePart + " " + dosePart);
                            return WrapMedicationLineForUI(composed);
                        }

                        // Collect non-Amio/Epi ordered meds
                        for (int oi = 0; oi < storedMedJson.Count; oi++)
                        {
                            int medIdNum2 = storedMedJson[oi]["id"];
                            if (medIdNum2 == 1 || medIdNum2 == 5) continue; // Skip Amio/Epi
                            var medNode = storedMedJson[oi];
                            bool hasReady2 = MedHasStatus(medNode, "READY");
                            bool isOrdered2 = MedHasAnyPreparing(medNode);
                            if (isOrdered2 && !hasReady2)
                            {
                                string medIdStr2 = medNode["id"];
                                var medinfo2 = MedicationFinder.FindByTag(medIdStr2, lang);
                                string medName2 = (medinfo2 != null && medinfo2.Length > 0) ? medinfo2[0] : medIdStr2;
                                medName2 = FindMultiLang(medName2);
                                string line2 = ComposeNurseMedLine(medNode, medName2);
                                orderedOthers.Add(line2);
                                bool wasOrderedBefore = false;
                                try { wasOrderedBefore = prevOrderedByMedId.ContainsKey(medIdNum2) && prevOrderedByMedId[medIdNum2]; } catch {}
                                if (!wasOrderedBefore)
                                {
                                    newlyOrdered.Add(line2); // newly ordered this frame
                                }
                            }
                        }
                        // update snapshot
                        for (int oi = 0; oi < storedMedJson.Count; oi++)
                        {
                            int mid = storedMedJson[oi]["id"];
                            if (mid == 1 || mid == 5) continue;
                            bool hasReadyX = MedHasStatus(storedMedJson[oi], "READY");
                            bool isOrderedX = MedHasAnyPreparing(storedMedJson[oi]);
                            prevOrderedByMedId[mid] = isOrderedX && !hasReadyX;
                        }

                        // update queue with all ordered others
                        // append only newly ordered to preserve order
                        for (int k = 0; k < newlyOrdered.Count; k++)
                        {
                            NurseNextAppendIfNew(newlyOrdered[k]);
                        }
                        var expectedKeys = new System.Collections.Generic.HashSet<string>();
                        for (int k = 0; k < orderedOthers.Count; k++)
                        {
                            expectedKeys.Add(CanonicalizeSimple(orderedOthers[k]));
                        }
                        // optionally retain hints
                        for (int h = 0; h < lastHintNext.Count; h++)
                        {
                            expectedKeys.Add(CanonicalizeSimple(lastHintNext[h]));
                        }
                        NurseNextPruneTo(expectedKeys);

                        // CHANGE NOTE (2025-09-11, mj): Nurse Current/Next dynamic fill with priority (Epi > Amio)
                        bool epiOrderedNow = false; 
                        bool amioOrderedNow = false;
                        SimpleJSON.JSONNode epiNodeRef = null;
                        SimpleJSON.JSONNode amioNodeRef = null;
                        try
                        {
                            for (int scan = 0; scan < storedMedJson.Count; scan++)
                            {
                                int mid2 = storedMedJson[scan]["id"];
                                if (mid2 != 1 && mid2 != 5) continue;
                                var node2 = storedMedJson[scan];
                                bool hasReady2 = MedHasStatus(node2, "READY");
                                bool isOrdered2 = MedHasAnyPreparing(node2);
                                if (mid2 == 5) { epiOrderedNow = isOrdered2 && !hasReady2; if (epiOrderedNow) epiNodeRef = node2; }
                                else if (mid2 == 1) { amioOrderedNow = isOrdered2 && !hasReady2; if (amioOrderedNow) amioNodeRef = node2; }
                            }
                        }
                        catch {}

                        var currentItems = new System.Collections.Generic.List<string>(3);
                        if (epiOrderedNow)
                        {
                            string epiTxt = ComposeNurseMedLine(epiNodeRef, FindMultiLang("Epinephrine"));
                            currentItems.Add(epiTxt);
                        }
                        if (amioOrderedNow)
                        {
                            string amioTxt = ComposeNurseMedLine(amioNodeRef, FindMultiLang("Amiodarone"));
                            currentItems.Add(amioTxt);
                        }

                        int consumedFromQueue = 0;
                        for (int qi = 0; qi < nurseNextQueue.Count && currentItems.Count < 3; qi++)
                        {
                            currentItems.Add(nurseNextQueue[qi]);
                            consumedFromQueue++;
                        }

                        bool allowWriteCurrent = !useServerHintsForNurseCurrent;
                        bool allowWriteNext = !useServerHintsForNurseNext;

                        if (allowWriteCurrent)
                        {
                            if (Nurse_Cur_1 != null) Nurse_Cur_1.text = currentItems.Count > 0 ? currentItems[0] : "";
                            if (Nurse_Cur_2 != null) Nurse_Cur_2.text = currentItems.Count > 1 ? currentItems[1] : "";
                            if (Nurse_Cur_3 != null) Nurse_Cur_3.text = currentItems.Count > 2 ? currentItems[2] : "";
                        }

                        if (allowWriteNext)
                        {
                            int baseIdx = consumedFromQueue;
                            if (Nurse_Next_1 != null) Nurse_Next_1.text = nurseNextQueue.Count > baseIdx ? nurseNextQueue[baseIdx] : "";
                            if (Nurse_Next_2 != null) Nurse_Next_2.text = nurseNextQueue.Count > (baseIdx + 1) ? nurseNextQueue[baseIdx + 1] : "";
                            if (Nurse_Next_3 != null) Nurse_Next_3.text = nurseNextQueue.Count > (baseIdx + 2) ? nurseNextQueue[baseIdx + 2] : "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[EventManager] Nurse_Cur_3/Next overflow processing failed: " + ex);
                }
            }
            // CHANGE NOTE (2025-09-10, mj): toggle menu clock icon (ON when highlight exists)
            // rule: if there is any medication with isOrdered && !hasReady(= highlight), turn on the corresponding menu icon
            // menu mapping: Res(1,5) / Interv(7,11,13,14,16,19) / HyperK(3,4,8,9,17,18)
            try
            {
                bool resHasHL = false, intHasHL = false, hypHasHL = false;
                var arr = (medications != null) ? medications["medicationModels"] : null;
                if (arr != null)
                {
                    foreach (SimpleJSON.JSONNode medNodeX in arr)
                    {
                        int mid = medNodeX["id"];
                        bool hasReadyX = MedHasStatus(medNodeX, "READY");
                        bool isOrderedX = MedHasAnyPreparing(medNodeX);
                        bool highlightX = isOrderedX && !hasReadyX;
                        if (!highlightX) continue; // only when an actual highlightable med exists

                        // Dose-aware gating for Gluconate/Bicarb: require a visible row (name+dose) to exist
                        if (mid == 4 || mid == 18)
                        {
                            string nameX = null;
                            if (_uiNameByMedId.TryGetValue(mid, out var nmX))
                            {
                                try { nameX = FindMultiLang(nmX); } catch { nameX = nmX; }
                            }
                            // find current PREPARING dose label
                            string doseLblX = null;
                            try
                            {
                                var dosesX = medNodeX["doses"];
                                if (dosesX != null)
                                {
                                    for (int di = 0; di < dosesX.Count && doseLblX == null; di++)
                                    {
                                        var diArrX = dosesX[di]["doseInstances"];
                                        if (diArrX == null) continue;
                                        for (int ii = 0; ii < diArrX.Count; ii++)
                                        {
                                            string stx = diArrX[ii]["status"];
                                            if (stx == "PREPARING" || stx == "AUTO_PREPARING")
                                            {
                                                doseLblX = dosesX[di]["label"];
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch {}

                            string finalX = ExtractFinalDoseToken(doseLblX);
                            bool rowVisible = !string.IsNullOrWhiteSpace(nameX) && !string.IsNullOrWhiteSpace(finalX) && RowHasNameAndDose(nameX, finalX);
                            if (!rowVisible) continue; // no visible row => do not light the clock
                        }

                        if (mid == 1 || mid == 5) resHasHL = true; // Resuscitation group
                        else if (mid == 7 || mid == 11 || mid == 13 || mid == 14 || mid == 16 || mid == 19) intHasHL = true; // Interventions group
                        else if (mid == 3 || mid == 4 || mid == 8 || mid == 9 || mid == 17 || mid == 18) hypHasHL = true; // Hyper-K group
                    }
                }

                if (resTabOrderIcon != null) resTabOrderIcon.enabled = resHasHL;
                if (intTabOrderIcon != null) intTabOrderIcon.enabled = intHasHL;
                if (hypTabOrderIcon != null) hypTabOrderIcon.enabled = hypHasHL;
            }
            catch {}
        } catch (Exception e) {
            Debug.Log(e);
        }
    }

    public void refMed() {
        medication(medications);
    }

    IEnumerator medicationInitialize(string processId) {
        string URL = "https://interface-ar.unige.ch/care-processes/" + processId + "/cpr/medications";
         using(UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                txt.text = FindMultiLang("Communication Error");

                sessionArr.Add(myInstance);
            }
            else
            //sucess
            {
                string json = request.downloadHandler.text;
                medications = SimpleJSON.JSON.Parse(json);
                Debug.Log(medications);
                _medInitDone = true; // mark initialization complete
                // CHANGE NOTE (2025-09-12, mj): Apply buffered SSE snapshot after initialization (main thread queue)
                if (_lastMedicationModelFromSse != null)
                {
                    m_queueAction.Enqueue(() => medication(_lastMedicationModelFromSse));
                }
                else
                {
                    m_queueAction.Enqueue(() => medication(medications));
                }
            }

        }
    }

    IEnumerator algoInit(string processId) {
        string URL = "https://interface-ar.unige.ch/care-processes/" + processId;
         using(UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                txt.text = FindMultiLang("Communication Error");

                sessionArr.Add(myInstance);
            }
            else
            //sucess
            {
                string cur = request.downloadHandler.text;
                SimpleJSON.JSONNode json = SimpleJSON.JSON.Parse(cur);
                algoritms = json["cprProtocolModel"]["steps"];
                Debug.Log(algoritms);
                algo(algoritms);
            }

        }
    }

    IEnumerator timerInit(string processId) {
        string URL = "https://interface-ar.unige.ch/care-processes/" + processId;
         using(UnityWebRequest request = UnityWebRequest.Get(URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                GameObject myInstance = Instantiate(sessionPref, sessionsTransform);
                TextMeshProUGUI txt = myInstance.transform.GetChild(2).transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                txt.text = FindMultiLang("Communication Error");

                sessionArr.Add(myInstance);
            }
            else
            //sucess
            {
                string cur = request.downloadHandler.text;
                SimpleJSON.JSONNode json = SimpleJSON.JSON.Parse(cur);
                currentStatus(json);
            }

        }
    }

    public void startConnection (string processId, string shortCode)
    {
        CurrentSession.text = shortCode;
        MedicationFinder.setProcessId(processId);
        epiStartTimestamp = 0;
        prev_epiStartTimestamp = 0;
        cprStartTimestamp = 0;
        prev_cprStartTimestamp = 0;
        
        string URL = "https://interface-ar.unige.ch/care-processes/" + processId + "/live";
        if (evt != null) {
            evt.Dispose();
        }
        evt = new EventSourceReader(new Uri(URL)).Start();
        evt.MessageReceived += (object sender, EventSourceMessageEventArgs e) => {
#if UNITY_EDITOR
            Debug.Log($"{e.Event} : {e.Message}");
#endif
            SimpleJSON.JSONNode json = SimpleJSON.JSON.Parse(e.Message);
            currentStatus(json);
        };
        evt.Disconnected += async (object sender, DisconnectEventArgs e) => {
            Debug.Log($"Retry: {e.ReconnectDelay} - Error: {e.Exception}");
            await Task.Delay(e.ReconnectDelay);
            evt.Start(); // Reconnect to the same URL
        };
        // StartCoroutine(connectSession(processId));
    }

    public void StartGazeHover(GameObject gameObject)
    { 
        // Debug.Log("Started GazeHover");
        // Debug.Log(gameObject.name);
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        timeActivated = Time.time;
        Debug.Log($"Started, {gameObject.name}, 0, {unixTime}, {DateTime.Now.ToLocalTime()}, {CurrentSession.text}");
        // sw.WriteLine($"Started, {gameObject.name}, 0, {unixTime}, {DateTime.Now.ToLocalTime()}");
        LogEvent("Started", $"{gameObject.name}, 0, {unixTime}, {DateTime.Now.ToLocalTime()}, {CurrentSession.text}");
    }

    public void EndGazeHover(GameObject gameObject)
    {
        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        Debug.Log($"Ended, {gameObject.name}, {Time.time - timeActivated}, {unixTime}, {DateTime.Now.ToLocalTime()}, {CurrentSession.text}");
        LogEvent("Ended", $"{gameObject.name}, {Time.time - timeActivated}, {unixTime}, {DateTime.Now.ToLocalTime()}, {CurrentSession.text}");
    }

    // public void ResetCenter()
    // {
    //     Vector3 offset = head.position - origin.position;
    //     offset.y = 0;
    //     origin.position = target.position - offset;

    //     Vector3 targetForward = target.forward;
    //     targetForward.y = 0;
    //     Vector3 cameraForward = head.forward;
    //     cameraForward.y = 0;

    //     float angle = Vector3.SignedAngle(cameraForward, targetForward, Vector3.up);

    //     origin.RotateAround(head.position, Vector3.up, angle);
    // }

    IEnumerator SetCPR_5Sec(bool val)
    {
        cpr_5sec_coroutine = val;
        //Print the time of when the function is first called.
        Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(10);

        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
        cpr_5sec = val;
        Debug.Log("cpr_5sec: " + cpr_5sec);
    }

    IEnumerator SetEpi_5Sec(bool val)
    {
        epi_5sec_coroutine = val;
        //Print the time of when the function is first called.
        Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(10);

        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
        epi_5sec = val;
        Debug.Log("epi_5sec: " + epi_5sec);
    }

    public void toggleSessionContainer() {
        if (sessionContainer != null) {
            if (sessionContainer.activeSelf) {
                sessionContainer.SetActive(false);
            } else {
                getSessions();
                sessionContainer.SetActive(true);
            } 
        }
    }

    public void togglePenMode()
    {
        if (boolTogglePen == false) {
            StartCoroutine(togglePen1Sec());
            Debug.Log("Here");

            // old code
            // FontIconSelector fis = GameObject.FindWithTag("PenToggle").transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).transform.GetChild(1).GetComponent<FontIconSelector>();
            
            GameObject pen = GameObject.FindWithTag("PenToggle");
            if (pen == null) { 
                // CHANGE NOTE (2025-09-05, mj): Skip if PenToggle is not found.
                return; 
            }
            FontIconSelector fis = pen.GetComponentInChildren<FontIconSelector>(true);
            if (fis == null) { 
                Debug.LogWarning("FontIconSelector not found under PenToggle"); 
                return; 
            }

            // Disable TeamScreenCanvas, BedCanvas, MonitorCanvas on togglePenMode
            // GameObject tsc = GameObject.FindWithTag("TeamScreenCanvas");
            // GameObject tst = GameObject.FindWithTag("TeamScreenText");
            // if (tsc != null) {
            //     CanvasElementRoundedRect cer = tsc.GetComponent<CanvasElementRoundedRect>();
            //     TextMeshProUGUI tmpug = tst.GetComponent<TextMeshProUGUI>();
            //     cer.enabled = false;
            //     tmpug.enabled = false;
            // }

            // GameObject bcvs = GameObject.FindWithTag("BedCanvas");
            // GameObject bt = GameObject.FindWithTag("BedText");
            // if (bcvs != null) {
            //     CanvasElementRoundedRect cer = bcvs.GetComponent<CanvasElementRoundedRect>();
            //     TextMeshProUGUI tmpug = bt.GetComponent<TextMeshProUGUI>();
            //     cer.enabled = false;                
            //     tmpug.enabled = false;
            // }

            // GameObject mcvs = GameObject.FindWithTag("MonitorCanvas");
            // GameObject mt = GameObject.FindWithTag("MonitorText");
            // if (mcvs != null) {
            //     CanvasElementRoundedRect cer = mcvs.GetComponent<CanvasElementRoundedRect>();
            //     TextMeshProUGUI tmpug = mt.GetComponent<TextMeshProUGUI>();
            //     cer.enabled = false;                
            //     tmpug.enabled = false;
            // }

            fis.CurrentIconName = "Icon 85";

            GameObject[] gos = GameObject.FindGameObjectsWithTag("HasBoundsControl");
            GameObject[] hges = GameObject.FindGameObjectsWithTag("HasGazeEvt");

            foreach (GameObject go in gos)
            {
                BoundsControl bc = go.GetComponent<BoundsControl>();
                ObjectManipulator om = go.GetComponent<ObjectManipulator>();
                bc.enabled = false;
                om.enabled = false;
            }

            foreach (GameObject hge in hges)
            {
                BoxCollider bc = hge.GetComponent<BoxCollider>();
                MRTKBaseInteractable mbi = hge.GetComponent<MRTKBaseInteractable>();
                bc.enabled = true;
                mbi.enabled = true;
            }
        }
    }

    public void untogglePenMode()
    {
        if (boolTogglePen == false) {
            StartCoroutine(togglePen1Sec());
            Debug.Log("There");
            // old code
            // FontIconSelector fis = GameObject.FindWithTag("PenToggle").transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).transform.GetChild(1).GetComponent<FontIconSelector>();

            GameObject pen = GameObject.FindWithTag("PenToggle");
            if (pen == null) { 
                // CHANGE NOTE (2025-09-05, mj): Skip if PenToggle is not found.
                return; 
            }
            FontIconSelector fis = pen.GetComponentInChildren<FontIconSelector>(true);
            if (fis == null) { 
                Debug.LogWarning("FontIconSelector not found under PenToggle"); 
                return; 
            }
            
            // Disable TeamScreenCanvas, BedCanvas, MonitorCanvas on untogglePenMode
            // GameObject tsc = GameObject.FindWithTag("TeamScreenCanvas");
            // GameObject tst = GameObject.FindWithTag("TeamScreenText");
            // if (tsc != null) {
            //     CanvasElementRoundedRect cer = tsc.GetComponent<CanvasElementRoundedRect>();
            //     TextMeshProUGUI tmpug = tst.GetComponent<TextMeshProUGUI>();

            //     cer.enabled = true;
            //     tmpug.enabled = true;
            // }

            // GameObject bcvs = GameObject.FindWithTag("BedCanvas");
            // GameObject bt = GameObject.FindWithTag("BedText");
            // if (bcvs != null) {
            //     CanvasElementRoundedRect cer = bcvs.GetComponent<CanvasElementRoundedRect>();
            //     TextMeshProUGUI tmpug = bt.GetComponent<TextMeshProUGUI>();

            //     cer.enabled = true;
            //     tmpug.enabled = true;
            // }

            // GameObject mcvs = GameObject.FindWithTag("MonitorCanvas");
            // GameObject mt = GameObject.FindWithTag("MonitorText");
            // if (mcvs != null) {
            //     CanvasElementRoundedRect cer = mcvs.GetComponent<CanvasElementRoundedRect>();
            //     TextMeshProUGUI tmpug = mt.GetComponent<TextMeshProUGUI>();
            //     cer.enabled = true;                
            //     tmpug.enabled = true;
            // }


            fis.CurrentIconName = "Icon 138";

            GameObject[] gos = GameObject.FindGameObjectsWithTag("HasBoundsControl");
            GameObject[] hges = GameObject.FindGameObjectsWithTag("HasGazeEvt");

            foreach (GameObject go in gos)
            {
                BoundsControl bc = go.GetComponent<BoundsControl>();
                ObjectManipulator om = go.GetComponent<ObjectManipulator>();
                bc.enabled = true;
                om.enabled = true;
            }

            foreach (GameObject hge in hges)
            {
                BoxCollider bc = hge.GetComponent<BoxCollider>();
                MRTKBaseInteractable mbi = hge.GetComponent<MRTKBaseInteractable>();
                bc.enabled = false;
                mbi.enabled = false;
            }
        }
    }

    public void minimizeMed()
    {
        if (medUI != null) {
            // medUI.SetActive(true);
            // yield return new WaitForSeconds(0.2f);
            // Vector3 origScale = medUI.transform.localScale;
            // Vector3 largerScale = origScale + new Vector3(0.2f, 0.2f, 0f);
            // for(float t = 0f; t < 6f; t += 6f * Time.deltaTime / effectTime)
            // {
            //     float v = Mathf.PingPong(t, 1f);
            //     medUI.transform.localScale = Vector3.Lerp(origScale, largerScale, v);
            //     yield return null;
            // }
            // medUI.transform.localScale = origScale;
            // yield return new WaitForSeconds(0.2f);
            medUI.SetActive(false);
        }
    }

    public void maximizeMed()
    {
        if (medUI != null) {
            // medUI.SetActive(false);
            // yield return new WaitForSeconds(0.2f);
            // Vector3 origScale = medUI.transform.localScale;
            // Vector3 largerScale = origScale + new Vector3(0.2f, 0.2f, 0f);
            // for(float t = 0f; t < 6f; t += 6f * Time.deltaTime / effectTime)
            // {
            //     float v = Mathf.PingPong(t, 1f);
            //     medUI.transform.localScale = Vector3.Lerp(largerScale, origScale, v);
            //     yield return null;
            // }
            // medUI.transform.localScale = largerScale;
            // yield return new WaitForSeconds(0.2f);
            medUI.SetActive(true);
        }
    }

    IEnumerator Remove_Noti(GameObject go, int type)
    {
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(10);
        go.SetActive(false);
        if (type == 0) {
            notiArr.Remove(go);
            Destroy(go, 0.0f);
        } else if (type == 1) {
            notiCprArr.Remove(go);
            Destroy(go, 0.0f);
        } else if (type == 2) {
            notiEpiArr.Remove(go);
            Destroy(go, 0.0f);
        }
        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    // CHANGE NOTE (2025-09-10, mj): Epinephrine OVERDUE noti dismiss immediately
    void DismissEpiOverdueNoti()
    {
        try
        {
            for (int i = notiEpiArr.Count - 1; i >= 0; i--)
            {
                GameObject go = (GameObject)notiEpiArr[i];
                notiEpiArr.RemoveAt(i);
                if (go != null)
                {
                    go.SetActive(false);
                    Destroy(go, 0.0f);
                }
            }
            // reset epi flashing state
            epi_5sec = false;
            epi_5sec_coroutine = false;
        }
        catch {}
    }

    // CHANGE NOTE (2025-09-11, mj): CPR OVERDUE noti dismiss immediately (on timer restart)
    void DismissCprOverdueNoti()
    {
        try
        {
            for (int i = notiCprArr.Count - 1; i >= 0; i--)
            {
                GameObject go = (GameObject)notiCprArr[i];
                notiCprArr.RemoveAt(i);
                if (go != null)
                {
                    go.SetActive(false);
                    Destroy(go, 0.0f);
                }
            }
            cpr_5sec = false;
            cpr_5sec_coroutine = false;
        }
        catch {}
    }


    IEnumerator togglePen1Sec()
    {
        boolTogglePen = true;
        yield return new WaitForSeconds(1);
        boolTogglePen = false;
    }

    public void LogEvent(string eventName, string value)
    {
        string logEntry = $"{eventName},{value}";
        File.AppendAllText(filePath, logEntry + "\n");

        Debug.Log("Logged: " + logEntry);
    }

    // CHANGE NOTE (2025-09-04, mj)
    // Only PREPARING highlight is shown; confirm flash removed
    void ConfirmFlashOrderDisplay(string medDisplayName, float seconds = 1.6f)
    {
        // intentionally left blank (no-op)
    }

    IEnumerator FlashGreen(TMPro.TextMeshProUGUI t, float seconds)
    {
        var origColor = t.color;
        var origStyle = t.fontStyle;

        t.fontStyle = TMPro.FontStyles.Bold;
        t.color = new Color(0.60f, 1f, 0.60f, 1f);

        yield return new WaitForSeconds(seconds);

        t.color = origColor;
        t.fontStyle = origStyle;
    }

    // CHANGE NOTE (2025-09-04, mj)
    // The yellow highlight was not sufficiently visible. Apply a richer amber color and a black outline to improve readability.

    void ApplyOrderHighlight(TMPro.TextMeshProUGUI t, string key, bool on)
    {
        if (t == null || string.IsNullOrEmpty(t.text)) return;
        if (t.text.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            try
            {
                // Stronger amber highlight color
                var highlightColor = new Color(1f, 0.85f, 0.1f, 1f); // ~#FFDA1A

                t.fontStyle = on ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                t.color = on ? highlightColor : Color.white;

                // Per-instance TMP material for outline
                var mat = t.fontMaterial; // creates an instance if needed
                if (on)
                {
                    // Outline width ~0.2 (tune as needed); outline color nearly black
                    mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.2f);
                    mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.9f));
                }
                else
                {
                    mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0f);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EventManager] ApplyOrderHighlight outline failed: {e}");
                // Fallback to color/style only
                t.fontStyle = on ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                t.color = on ? new Color(1f, 0.85f, 0.1f, 1f) : Color.white;
            }
        }
    }

    void SetOrderNormalFor(string medDisplayName)
    {
        void Apply(TMPro.TextMeshProUGUI t)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) return;
            if (t.text.IndexOf(medDisplayName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                t.fontStyle = TMPro.FontStyles.Normal;
                t.color = Color.white;
                try
                {
                    var mat = t.fontMaterial; // per-instance
                    mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0f);
                }
                catch {}
            }
        }
        Apply(Nurse_Cur_1); Apply(Nurse_Cur_2); Apply(Nurse_Cur_3);
        Apply(Nurse_Next_1); Apply(Nurse_Next_2); Apply(Nurse_Next_3);
    }

    bool MedHasStatus(SimpleJSON.JSONNode medNode, string status)
    {
        try
        {
            if (medNode == null || medNode["doses"] == null) return false;
            foreach (SimpleJSON.JSONNode dose in medNode["doses"]) {
                var diArr = dose["doseInstances"];
                if (diArr == null) continue;
                foreach (SimpleJSON.JSONNode di in diArr) {
                    string st = di["status"];
                    if (st == status) return true;
                }
            }
        }
        catch {}
        return false;
    }

    bool MedHasAnyPreparing(SimpleJSON.JSONNode medNode)
    {
        return MedHasStatus(medNode, "PREPARING") || MedHasStatus(medNode, "AUTO_PREPARING");
    }

    // CHANGE NOTE (2025-09-05, mj)
    // highlight ALL texts in the Amiodarone/Epinephrine row inside Medication_List.

    class OriginalTMPState { public Color color; public TMPro.FontStyles style; }
    Dictionary<int, OriginalTMPState> _origMedListTMP = new Dictionary<int, OriginalTMPState>();

    // UI label overrides by medication id for name-only matching (per HoloLens display)
    Dictionary<int, string> _uiNameByMedId = new Dictionary<int, string>
    {
        { 1, "Amiodarone" },
        { 2, "Atropine" },
        { 3, "10% Calcium Chloride" },
        { 4, "10% Calcium Gluconate" },
        { 5, "Epinephrine" },
        { 6, "Etomidate" },
        { 7, "Fentanyl" },
        { 8, "Glucose D10W" },
        { 9, "Insulin (starting dose)" },
        { 10, "KCI" },
        { 11, "Ketamine" },
        { 12, "Lidocaine" },
        { 13, "Midazolam" },
        { 14, "Morphine" },
        { 15, "Normal Saline" },
        { 16, "Rocuronium" },
        { 17, "Salbutamol (Albuterol)" },
        { 18, "8.4% Sodium Bicarb" },
        { 19, "Succinylcholine" },
    };

    // Optional alternative UI name tokens per medication id (for split/variant labels)
    System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>> _uiAltNamesByMedId =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>
        {
            { 8, new System.Collections.Generic.List<string>{ "Glucose (starting dose)", "Glucose", "Glucose D10W" } },
        };

    // Hard highlight helper: apply bold+amber + face/outline + vertex color to ensure visibility
    void ApplyTMPHardHighlight(TMPro.TextMeshProUGUI leaf, bool on)
    {
        if (leaf == null) return;
        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        int id = leaf.GetInstanceID();
        if (!_origMedListTMP.ContainsKey(id))
        {
            _origMedListTMP[id] = new OriginalTMPState { color = leaf.color, style = leaf.fontStyle };
        }
        try
        {
            // Ensure per-instance material
            leaf.enableVertexGradient = false;
            leaf.fontMaterial = new Material(leaf.fontMaterial);
            var mat = leaf.fontMaterial;
            if (on)
            {
                leaf.fontStyle = TMPro.FontStyles.Bold;
                leaf.color = amber;
                try { mat.SetColor(TMPro.ShaderUtilities.ID_FaceColor, amber); } catch {}
                try { mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.1f); mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color(0f,0f,0f,0.9f)); } catch {}
                leaf.ForceMeshUpdate(true);
                // Vertex colors
                var mi = leaf.textInfo.meshInfo;
                for (int m = 0; m < mi.Length; m++)
                {
                    var cols = mi[m].colors32;
                    if (cols == null) continue;
                    var c32 = (Color32)amber;
                    for (int k = 0; k < cols.Length; k++) cols[k] = c32;
                }
                leaf.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
            }
            else
            {
                var orig = _origMedListTMP[id];
                if (orig != null)
                {
                    leaf.fontStyle = orig.style;
                    leaf.color = orig.color;
                }
                try { mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0f); } catch {}
                leaf.ForceMeshUpdate(true);
                // Reset vertex colors to white
                var mi = leaf.textInfo.meshInfo;
                for (int m = 0; m < mi.Length; m++)
                {
                    var cols = mi[m].colors32;
                    if (cols == null) continue;
                    var c32w = (Color32)Color.white;
                    for (int k = 0; k < cols.Length; k++) cols[k] = c32w;
                }
                leaf.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
            }
        }
        catch {}
    }

    // Force apply hard highlight for Glucose (id=8) by name-only scanning
    void ForceApplyHardHighlightGlucose()
    {
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return;
        if (!_uiAltNamesByMedId.TryGetValue(8, out var alts) || alts == null || alts.Count == 0) return;
        var tmps = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmps == null) return;
        int matched = 0;
        for (int i = 0; i < tmps.Length; i++)
        {
            var leaf = tmps[i];
            if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
            string txt = leaf.text;
            bool match = false;
            for (int a = 0; a < alts.Count && !match; a++)
            {
                if (txt.IndexOf(alts[a], System.StringComparison.OrdinalIgnoreCase) >= 0) match = true;
            }
            if (!match) continue;
            ApplyTMPHardHighlight(leaf, true);
            matched++;
        }
        if (debugGlucoseHighlight && matched == 0) DebugLogGlucoseHighlight("[GlucoseHL] no matches found for alt names.");
    }

    // Row index by medication id
    System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Transform>> _rowsByMedId =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Transform>>();


    // CHANGE NOTE (2025-09-10, mj): dose-required meds expanded (name + dose must co-exist)
    // Per-medication policy: whether this medication requires Name+Dose (AND) matching
    System.Collections.Generic.HashSet<int> _requireDoseMedIds = new System.Collections.Generic.HashSet<int>{ 4, 18 };

    // Cache for Medication_List root to avoid repeated scanning
    Transform _medListRootCache = null;

    Transform FindMedicationListRoot()
    {
        if (_medListRootCache != null) return _medListRootCache;
        GameObject go = GameObject.Find("Medication_List (1)");
        if (go == null) go = GameObject.Find("Medication_List");
        if (go == null) go = GameObject.Find("Medication_List 3");
        if (go != null) { Debug.Log($"[EventManager] Medication_List root (by name): {go.name}"); _medListRootCache = go.transform; return _medListRootCache; }

        // Heuristic scan: find a container that contains standard headers like Drug/Dose/Volume/Strength/Instructions
        Transform best = null;
        int bestScore = 0;
        var all = GameObject.FindObjectsOfType<Transform>(true);
        foreach (var t in all)
        {
            if (t == null) continue;
            var tmps = t.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmps == null || tmps.Length == 0) continue;
            int score = 0;
            for (int i = 0; i < tmps.Length; i++)
            {
                var tmp = tmps[i];
                if (tmp == null) continue;
                var s = tmp.text;
                if (string.IsNullOrEmpty(s)) continue;
                s = s.Trim();
                if (s.Equals("Drug", StringComparison.OrdinalIgnoreCase)) score++;
                else if (s.Equals("Strength", StringComparison.OrdinalIgnoreCase)) score++;
                else if (s.Equals("Dose", StringComparison.OrdinalIgnoreCase)) score++;
                else if (s.Equals("Volume", StringComparison.OrdinalIgnoreCase)) score++;
                else if (s.Equals("Instructions", StringComparison.OrdinalIgnoreCase) || s.Equals("lnstructions", StringComparison.OrdinalIgnoreCase)) score++;
            }
            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }
        if (bestScore >= 3) { Debug.Log($"[EventManager] Medication_List root (heuristic): {best.name}, score={bestScore}"); _medListRootCache = best; return _medListRootCache; }
        return null;
    }

    bool IsNurseSceneActive()
    {
        try
        {
            var name = SceneManager.GetActiveScene().name;
            return !string.IsNullOrEmpty(name) &&
                   (name.IndexOf("nurse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(name, "Nurse_scene", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    // Build medId→rows index by scanning Medication_List
    void BuildMedicationRowIdIndex()
    {
        _rowsByMedId.Clear();
        var root = FindMedicationListRoot();
        if (root == null) return;

        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        // Build canonical key per medId (multilang only)
        var medKeyById = new System.Collections.Generic.Dictionary<int, string>();
        foreach (var kv in _uiNameByMedId)
        {
            try
            {
                medKeyById[kv.Key] = Canon(FindMultiLang(kv.Value));
            }
            catch
            {
                medKeyById[kv.Key] = Canon(kv.Value);
            }
        }

        var groups = root.GetComponentsInChildren<Transform>(true);
        for (int gi = 0; gi < groups.Length; gi++)
        {
            var g = groups[gi];
            if (g == null || g == root) continue;
            var tmps = g.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            int cnt = tmps != null ? tmps.Length : 0;
            if (cnt == 0) continue;
            if (cnt > 80) continue;

            var sb = new System.Text.StringBuilder(256);
            for (int t = 0; t < tmps.Length; t++)
            {
                var gt = tmps[t];
                if (gt == null || string.IsNullOrEmpty(gt.text)) continue;
                sb.Append(Canon(gt.text));
            }
            string canon = sb.ToString();
            if (string.IsNullOrEmpty(canon)) continue;

            foreach (var kv in medKeyById)
            {
                string key = kv.Value;
                if (string.IsNullOrEmpty(key)) continue;
                if (canon.IndexOf(key, System.StringComparison.Ordinal) >= 0)
                {
                    if (!_rowsByMedId.TryGetValue(kv.Key, out var list) || list == null)
                    {
                        list = new System.Collections.Generic.List<Transform>();
                        _rowsByMedId[kv.Key] = list;
                    }
                    if (!list.Contains(g)) list.Add(g);
                    try
                    {
                        var mr = g.gameObject.GetComponent<MedicationRow>();
                        if (mr == null) mr = g.gameObject.AddComponent<MedicationRow>();
                        mr.medId = kv.Key;
                    }
                    catch {}
                    break; // one med per row
                }
            }
        }
    }


    // Highlight all texts in the row(s) by medId
    void ApplyMedicationListRowHighlightById(int medId, bool on)
    {
        if (medId <= 0) return;
        
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return;

        if (!_rowsByMedId.TryGetValue(medId, out var rows) || rows == null || rows.Count == 0)
        {
            BuildMedicationRowIdIndex();
            _rowsByMedId.TryGetValue(medId, out rows);
        }
        if (rows == null || rows.Count == 0)
        {
            // If this med requires dose-aware matching, skip name-only fallback to avoid false positives
            if (_requireDoseMedIds.Contains(medId)) return;
            // Fallback to name-based for non-dose-required meds
            if (_uiNameByMedId.TryGetValue(medId, out var name)) { try { ApplyMedicationListRowHighlight(FindMultiLang(name), on); } catch {} }
            return;
        }
        
        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        for (int r = 0; r < rows.Count; r++)
        {
            var tmps = rows[r].GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int j = 0; j < tmps.Length; j++)
            {
                var t = tmps[j];
                if (t == null) continue;
                int id = t.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = t.color, style = t.fontStyle };
                }
                if (on)
                {
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            t.fontStyle = orig.style;
                            t.color = orig.color;
                        }
                    }
                    catch {}
                }
            }
        }
    }


    // Helper to get GameObject path
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform t = obj.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    // Glucose (starting dose) row-level highlight by UI tokens (independent of server dose value)
    void ApplyGlucoseRowHighlight(bool on)
    {
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return;

        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        var tmpsAll = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmpsAll == null || tmpsAll.Length == 0) return;

        // Prefer anchoring from a leaf that contains "D10W" to avoid matching 'glucose' in other rows (e.g., insulin instructions)
        for (int i = 0; i < tmpsAll.Length; i++)
        {
            var leaf = tmpsAll[i];
            if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
            if (leaf.text.IndexOf("D10W", System.StringComparison.OrdinalIgnoreCase) < 0) continue;

            // ascend to row-like container (2..80 texts) or MedicationRow marker
            Transform cursor = leaf.transform;
            Transform rowRoot = null;
            for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
            {
                try { var mr = cursor.gameObject.GetComponent<MedicationRow>(); if (mr != null) { rowRoot = cursor; break; } } catch {}
                cursor = cursor.parent;
            }
            if (rowRoot == null)
            {
                cursor = leaf.transform;
                for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
                {
                    var desc = cursor.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                    int cnt = desc != null ? desc.Length : 0;
                    if (cnt >= 2 && cnt <= 80) { rowRoot = cursor; break; }
                    cursor = cursor.parent;
                }
            }
            if (rowRoot == null) continue;

            var rowTmps = rowRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int j = 0; j < rowTmps.Length; j++)
            {
                var t = rowTmps[j];
                if (t == null) continue;
                int id = t.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = t.color, style = t.fontStyle };
                }
                if (on)
                {
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            t.fontStyle = orig.style;
                            t.color = orig.color;
                        }
                    }
                    catch {}
                }
            }
            return; // only first matched row
        }
    }

    // Reset highlight by medId
    void SetOrderNormalForId(int medId)
    {
        if (_uiNameByMedId.TryGetValue(medId, out var nm))
        {
            string name = null; try { name = FindMultiLang(nm); } catch { name = nm; }
            // Clear name-only highlight first
            ApplyMedicationNameOnlyHighlight(name, false);
            // For dose-required meds, also clear both known dose rows to avoid residual highlights
            if (medId == 4) { ApplyMedicationDoseRowHighlight(name, "1500 mg", false); ApplyMedicationDoseRowHighlight(name, "2500 mg", false); }
            else if (medId == 18) { ApplyMedicationDoseRowHighlight(name, "25 mEq", false); ApplyMedicationDoseRowHighlight(name, "50 mEq", false); }
        }
        else
        {
            ApplyMedicationListRowHighlightById(medId, false);
        }
    }

    void ApplyMedicationListRowHighlight(string medKey, bool on)
    {
        if (string.IsNullOrEmpty(medKey)) return;
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return; // highlight is only allowed inside Medication_List

        // String normalization: lowercase + remove non-alphanumeric (keep numbers, ignore line breaks/spaces)
        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        string key = Canon(medKey);
        // Guard very short keys (e.g., "ns") to avoid false positives like "instructions"
        if (key.Length < 3) return;
        if (string.IsNullOrEmpty(key)) return;

        // Leaf-first approach: find leaf containing key, then traverse up to apply to row(s)
        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        var tmps = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

        for (int i = 0; i < tmps.Length; i++)
        {
            var leaf = tmps[i];
            if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
            string leafCanon = Canon(leaf.text);
            if (leafCanon.IndexOf(key, System.StringComparison.Ordinal) < 0) continue;

            // Find row(s): prefer groups with 2~80 texts, apply to leaf only if no suitable row found
            Transform rowRoot = leaf.transform;
            Transform cursor = leaf.transform;
            for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
            {
                var desc = cursor.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                int cnt = desc != null ? desc.Length : 0;
                if (cnt >= 2 && cnt <= 80) { rowRoot = cursor; break; }
                cursor = cursor.parent;
            }

            var rowTmps = rowRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int j = 0; j < rowTmps.Length; j++)
            {
                var t = rowTmps[j];
                if (t == null) continue;
                int id = t.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = t.color, style = t.fontStyle };
                }
                if (on)
                {
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            t.fontStyle = orig.style;
                            t.color = orig.color;
                        }
                    }
                    catch {}
                }
            }
        }
    }

    // Name-only highlight: highlight only the TMPs that contain the medication name (leaf-level), not the whole row
    void ApplyMedicationNameOnlyHighlight(string medKey, bool on)
    {
        if (string.IsNullOrEmpty(medKey)) return;
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) { Debug.LogWarning("[EventManager] Medication_List root not found; skip name-only highlight."); return; }

        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        string key = Canon(medKey);
        if (key.Length < 3) return; // avoid false positives like 'ns' in 'instructions'

        var tmps = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmps == null || tmps.Length == 0) return;

        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        for (int i = 0; i < tmps.Length; i++)
        {
            var leaf = tmps[i];
            if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
            string leafCanon = Canon(leaf.text);
            if (leafCanon.IndexOf(key, System.StringComparison.Ordinal) < 0) continue;

            int id = leaf.GetInstanceID();
            if (!_origMedListTMP.ContainsKey(id))
            {
                _origMedListTMP[id] = new OriginalTMPState { color = leaf.color, style = leaf.fontStyle };
            }
            if (on)
            {
                leaf.fontStyle = TMPro.FontStyles.Bold;
                leaf.color = amber;
            }
            else
            {
                try
                {
                    var orig = _origMedListTMP[id];
                    if (orig != null)
                    {
                        leaf.fontStyle = orig.style;
                        leaf.color = orig.color;
                    }
                }
                catch {}
            }
        }
    }

    // Name-on-row highlight: find the row using the dose token leaf, then highlight only the med name leaves within that row.
    void ApplyMedicationNameOnRow(string medNameKey, string doseToken, bool on)
    {
        if (string.IsNullOrWhiteSpace(medNameKey) || string.IsNullOrWhiteSpace(doseToken)) return;
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) { Debug.LogWarning("[EventManager] Medication_List root not found; skip name-on-row highlight."); return; }

        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        string medKey = Canon(medNameKey);
        string doseKey = Canon(doseToken);
        if (medKey.Length < 3 || string.IsNullOrEmpty(doseKey)) return;

        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        var groups = root.GetComponentsInChildren<Transform>(true);
        for (int gi = 0; gi < groups.Length; gi++)
        {
            var g = groups[gi];
            if (g == null || g == root) continue;
            var tmps = g.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmps == null || tmps.Length == 0 || tmps.Length > 80) continue;

            // Step 1: find the dose leaf to anchor the row selection
            Transform rowRoot = null;
            for (int t = 0; t < tmps.Length; t++)
            {
                var leaf = tmps[t];
                if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
                string leafCanonDose = Canon(leaf.text);
                if (leafCanonDose.IndexOf(doseKey, System.StringComparison.Ordinal) < 0) continue;
                // ascend to row-like container (2..80 texts)
                Transform cursor = leaf.transform;
                Transform foundRowWithMarker = null;
                for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
                {
                    // Prefer explicit MedicationRow component if present
                    try { var mr = cursor.gameObject.GetComponent<MedicationRow>(); if (mr != null) { foundRowWithMarker = cursor; break; } } catch {}
                    cursor = cursor.parent;
                }
                cursor = leaf.transform;
                for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
                {
                    var desc = cursor.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                    int cnt = desc != null ? desc.Length : 0;
                    if (cnt >= 2 && cnt <= 80) { rowRoot = cursor; break; }
                    cursor = cursor.parent;
                }
                if (foundRowWithMarker != null) rowRoot = foundRowWithMarker;
                if (rowRoot != null) break;
            }
            if (rowRoot == null) continue;

            // Step 2: within that row, highlight only med name leaves – stop after first match to avoid multiple rows
            var rowTmps = rowRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int j = 0; j < rowTmps.Length; j++)
            {
                var leaf = rowTmps[j];
                if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
                string leafCanon = Canon(leaf.text);
                bool isNameMatch = leafCanon.IndexOf(medKey, System.StringComparison.Ordinal) >= 0;
                if (!isNameMatch && _uiAltNamesByMedId.TryGetValue(8, out var alts))
                {
                    for (int ai = 0; ai < alts.Count && !isNameMatch; ai++)
                    {
                        var ak = Canon(alts[ai]);
                        if (!string.IsNullOrEmpty(ak) && leafCanon.IndexOf(ak, System.StringComparison.Ordinal) >= 0) isNameMatch = true;
                    }
                }
                if (!isNameMatch) continue;
                int id = leaf.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = leaf.color, style = leaf.fontStyle };
                }
                if (on)
                {
                    leaf.fontStyle = TMPro.FontStyles.Bold;
                    leaf.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            leaf.fontStyle = orig.style;
                            leaf.color = orig.color;
                        }
                    }
                    catch {}
                }
                return; // ensure exactly one row gets applied
            }
        }
    }

    // Row-by-name highlight: anchor from the name leaf and apply to the entire row (all 5 cells)
    void ApplyMedicationRowByNameOnly(string medNameKey, bool on)
    {
        if (string.IsNullOrWhiteSpace(medNameKey)) return;
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return;

        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        string nameKey = Canon(medNameKey);
        if (nameKey.Length < 3) return;

        var groups = root.GetComponentsInChildren<Transform>(true);
        var amber = new Color(1f, 0.85f, 0.1f, 1f);
        for (int gi = 0; gi < groups.Length; gi++)
        {
            var g = groups[gi];
            if (g == null || g == root) continue;
            var tmps = g.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmps == null || tmps.Length == 0 || tmps.Length > 80) continue;

            // Step 1: find the name leaf inside this group
            Transform rowRoot = null;
            for (int t = 0; t < tmps.Length; t++)
            {
                var leaf = tmps[t];
                if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
                string leafCanon = Canon(leaf.text);
                if (leafCanon.IndexOf(nameKey, System.StringComparison.Ordinal) < 0) continue;
                // ascend to row-like container (2..80 texts) or MedicationRow marker
                Transform cursor = leaf.transform;
                Transform foundRowWithMarker = null;
                for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
                {
                    try { var mr = cursor.gameObject.GetComponent<MedicationRow>(); if (mr != null) { foundRowWithMarker = cursor; break; } } catch {}
                    cursor = cursor.parent;
                }
                cursor = leaf.transform;
                for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
                {
                    var desc = cursor.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                    int cnt = desc != null ? desc.Length : 0;
                    if (cnt >= 2 && cnt <= 80) { rowRoot = cursor; break; }
                    cursor = cursor.parent;
                }
                if (foundRowWithMarker != null) rowRoot = foundRowWithMarker;
                if (rowRoot != null) break;
            }
            if (rowRoot == null) continue;

            // Step 2: apply to entire row TMPs
            var rowTmps = rowRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int j = 0; j < rowTmps.Length; j++)
            {
                var t = rowTmps[j];
                if (t == null) continue;
                int id = t.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = t.color, style = t.fontStyle };
                }
                if (on)
                {
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            t.fontStyle = orig.style;
                            t.color = orig.color;
                        }
                    }
                    catch {}
                }
            }
            return; // one row only
        }
    }

    // Check if there exists a row that contains both the medication name and the dose token
    bool RowHasNameAndDose(string medNameKey, string doseToken)
    {
        if (string.IsNullOrWhiteSpace(medNameKey) || string.IsNullOrWhiteSpace(doseToken)) return false;
        if (!IsNurseSceneActive()) return false;
        var root = FindMedicationListRoot();
        if (root == null) return false;

        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        string medKey = Canon(medNameKey);
        string doseKey = Canon(doseToken);
        if (medKey.Length < 3 || string.IsNullOrEmpty(doseKey)) return false;

        var groups = root.GetComponentsInChildren<Transform>(true);
        for (int gi = 0; gi < groups.Length; gi++)
        {
            var g = groups[gi];
            if (g == null || g == root) continue;
            var tmps = g.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmps == null || tmps.Length == 0 || tmps.Length > 80) continue;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            for (int t = 0; t < tmps.Length; t++)
            {
                var gt = tmps[t];
                if (gt == null || string.IsNullOrEmpty(gt.text)) continue;
                sb.Append(Canon(gt.text));
            }
            string canon = sb.ToString();
            if (canon.IndexOf(medKey, System.StringComparison.Ordinal) >= 0 && canon.IndexOf(doseKey, System.StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    // CHANGE NOTE (2025-09-10, mj): Dose-row highlight anchors to the dose leaf to avoid table-wide highlight; used for ids 4,18
    // Dose-aware row highlighting inside Medication_List only (med name + dose)
    void ApplyMedicationDoseRowHighlight(string medNameKey, string doseKey, bool on)
    {
        if (string.IsNullOrWhiteSpace(medNameKey) || string.IsNullOrWhiteSpace(doseKey)) return;
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return;

        string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLowerInvariant();
            try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", ""); } catch {}
            return s.Trim();
        }

        string medKey = Canon(medNameKey);
        string doseCanonKey = CanonDoseForCompare(doseKey);
        if (string.IsNullOrEmpty(medKey) || string.IsNullOrEmpty(doseCanonKey)) return;

        var tmpsAll = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmpsAll == null || tmpsAll.Length == 0) return;

        // 1) Find a leaf that contains the dose token (canonicalized), then ascend to the nearest row container
        for (int i = 0; i < tmpsAll.Length; i++)
        {
            var leaf = tmpsAll[i];
            if (leaf == null || string.IsNullOrEmpty(leaf.text)) continue;
            string leafDoseCanon = CanonDoseForCompare(leaf.text);
            if (leafDoseCanon.IndexOf(doseCanonKey, System.StringComparison.Ordinal) < 0) continue;

            // Try to locate an explicit MedicationRow marker first
            Transform rowRoot = null;
            Transform cursor = leaf.transform;
            Transform foundRowWithMarker = null;
            for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
            {
                try { var mr = cursor.gameObject.GetComponent<MedicationRow>(); if (mr != null) { foundRowWithMarker = cursor; break; } } catch {}
                cursor = cursor.parent;
            }
            if (foundRowWithMarker != null) rowRoot = foundRowWithMarker;
            if (rowRoot == null)
            {
                cursor = leaf.transform;
                for (int hop = 0; hop < 8 && cursor != null && cursor != root; hop++)
                {
                    var desc = cursor.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                    int cnt = desc != null ? desc.Length : 0;
                    if (cnt >= 2 && cnt <= 80) { rowRoot = cursor; break; }
                    cursor = cursor.parent;
                }
            }
            if (rowRoot == null) continue;

            // Ensure the same row also contains the medication name (guard against false positives)
            bool rowHasMedName = false;
            var rowTmps = rowRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int j = 0; j < rowTmps.Length; j++)
            {
                var t = rowTmps[j];
                if (t == null || string.IsNullOrEmpty(t.text)) continue;
                string canon = Canon(t.text);
                if (canon.IndexOf(medKey, System.StringComparison.Ordinal) >= 0) { rowHasMedName = true; break; }
            }
            if (!rowHasMedName) continue;

            // 2) Apply highlight to the entire row only
            var amber = new Color(1f, 0.85f, 0.1f, 1f);
            for (int j = 0; j < rowTmps.Length; j++)
            {
                var t = rowTmps[j];
                if (t == null) continue;
                int id = t.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = t.color, style = t.fontStyle };
                }
                if (on)
                {
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            t.fontStyle = orig.style;
                            t.color = orig.color;
                        }
                    }
                    catch {}
                }
            }
            return; // exactly one row per call
        }
    }

    // NEW: Dose-only row highlighting; inside Medication_List only
    void ApplyDoseOnlyRowHighlight(string doseKey, bool on)
    {
        if (string.IsNullOrWhiteSpace(doseKey)) return;
        if (!IsNurseSceneActive()) return;
        var root = FindMedicationListRoot();
        if (root == null) return;

        string dosePattern = doseKey.Trim();
        var groups = root.GetComponentsInChildren<Transform>(true);
        var amber = new Color(1f, 0.85f, 0.1f, 1f);

        for (int gi = 0; gi < groups.Length; gi++)
        {
            var g = groups[gi];
            if (g == null || g == root) continue;
            var tmps = g.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            int cnt = tmps != null ? tmps.Length : 0;
            if (cnt == 0 || cnt > 80) continue;

            bool containsDose = false;
            for (int t = 0; t < tmps.Length; t++)
            {
                var gt = tmps[t];
                if (gt == null || string.IsNullOrEmpty(gt.text)) continue;
                // Case-insensitive exact substring match of dose text
                if (gt.text.IndexOf(dosePattern, System.StringComparison.OrdinalIgnoreCase) >= 0) { containsDose = true; break; }
            }
            if (!containsDose) continue;

            for (int j = 0; j < tmps.Length; j++)
            {
                var t = tmps[j];
                if (t == null) continue;
                int id = t.GetInstanceID();
                if (!_origMedListTMP.ContainsKey(id))
                {
                    _origMedListTMP[id] = new OriginalTMPState { color = t.color, style = t.fontStyle };
                }
                if (on)
                {
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.color = amber;
                }
                else
                {
                    try
                    {
                        var orig = _origMedListTMP[id];
                        if (orig != null)
                        {
                            t.fontStyle = orig.style;
                            t.color = orig.color;
                        }
                    }
                    catch {}
                }
            }
        }
    }

    // Extract the trailing numeric+unit token from a dose label (e.g., "5 ml/kg = 110 ml" -> "110 mL")
    string ExtractFinalDoseToken(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        try
        {
            var m = System.Text.RegularExpressions.Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?\s*(mg|mcg|g|mL|ml|J|mEq|U|units))\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string val = m.Groups[1].Value.Trim();
                // normalize ml/ML to mL for visual consistency
                val = val.Replace("ml", "mL").Replace("ML", "mL");
                return val;
            }
        }
        catch {}
        return null;
    }

    // Canonicalize a dose token for comparison: remove spaces/commas/dots, lower-case, normalize units
    string CanonDoseForCompare(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim();
        try { s = System.Text.RegularExpressions.Regex.Replace(s, @"[\s,\.]", ""); } catch {}
        s = s.ToLowerInvariant();
        s = s.Replace("ml", "ml");
        s = s.Replace("mg", "mg");
        s = s.Replace("meq", "meq");
        return s;
    }

    // Allow-list per medication for final dose token (canonical form)
    bool IsDoseAllowedForMed(int medId, string finalDoseToken)
    {
        string t = CanonDoseForCompare(finalDoseToken);
        if (string.IsNullOrEmpty(t)) return false;
        if (medId == 4)
        {
            // 10% Calcium Gluconate: allow only 1500 mg or 2500 mg
            return t == "1500mg" || t == "2500mg";
        }
        if (medId == 18)
        {
            // 8.4% Sodium Bicarb: allow only 25 mEq or 50 mEq
            return t == "25meq" || t == "50meq";
        }
        return true; // other meds: no gating
    }

    void HighlightAmiodaroneOrder(bool on)
    {
        // Row-level highlight by name (apply to entire row of 5 cells)
        if (_uiNameByMedId.TryGetValue(1, out var nm))
        {
            string name = null; try { name = FindMultiLang(nm); } catch { name = nm; }
            ApplyMedicationListRowHighlight(name, on);
        }
    }


    void HighlightEpinephrineOrder(bool on)
    {
        // Row-level highlight by name (apply to entire row of 5 cells)
        if (_uiNameByMedId.TryGetValue(5, out var nm))
        {
            string name = null; try { name = FindMultiLang(nm); } catch { name = nm; }
            ApplyMedicationListRowHighlight(name, on);
        }
    }
    
    // CHANGE NOTE (2025-09-09, mj): add generic highlight method for all medications

    void HighlightMedicationOrder(string medName, bool on)
    {
        if (string.IsNullOrEmpty(medName)) return;
        ApplyMedicationNameOnlyHighlight(medName, on);
    }

    // Generic ID-based highlighter
    void HighlightMedicationOrderById(int medId, bool on)
    {
        if (medId <= 0) return;
        if (_uiNameByMedId.TryGetValue(medId, out var nm))
        {
            string name = null; try { name = FindMultiLang(nm); } catch { name = nm; }
            ApplyMedicationNameOnlyHighlight(name, on);
        }
    }

    // CHANGE NOTE (2025-09-11, mj): Class-scope helpers for Nurse/Doctor formatting
    // These mirror the local versions inside UpdateInstructions for use in other code paths.
    string NormalizeUnits(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        s = s.Replace("cc", "mL").Replace("CC", "mL");
        s = s.Replace("ML", "mL").Replace("Ml", "mL").Replace("ml", "mL");
        s = s.Replace("MG", "mg").Replace("Mg", "mg");
        s = s.Replace("KG", "kg").Replace("Kg", "kg");
        return s;
    }

    string TidySpacing(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        try
        {
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?<!\s)\( ", " ( ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\(\s+", "(");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+\)", ")");
            s = System.Text.RegularExpressions.Regex.Replace(s, @":\s*", ": ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[ \t]{2,}", " ");
        }
        catch {}
        return s.Trim();
    }

    string ExtractPerKgMass(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = NormalizeUnits(s);
        var m = System.Text.RegularExpressions.Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g)\s*/\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return "";
        string val = m.Groups[1].Value;
        string unit = m.Groups[2].Value.ToLowerInvariant();
        if (unit == "g" || unit == "mg" || unit == "mcg")
        {
            return val + " " + unit + "/kg";
        }
        return "";
    }

    string ExtractPerKgVol(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = NormalizeUnits(s);
        var m = System.Text.RegularExpressions.Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mL|ml|cc)\s*/\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return "";
        string val = m.Groups[1].Value;
        return val + " mL/kg";
    }

    // Include other per-kg units such as mEq/kg, U/kg, units/kg
    string ExtractPerKgOther(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = NormalizeUnits(s);
        var m = System.Text.RegularExpressions.Regex.Match(s, @"([0-9]+(?:\.[0-9]+)?)\s*(mEq|U|units)\s*/\s*kg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return "";
        string val = m.Groups[1].Value;
        string unit = m.Groups[2].Value;
        if (unit.Equals("units", System.StringComparison.OrdinalIgnoreCase)) unit = "U"; // compact
        return val + " " + unit + "/kg";
    }

    // CHANGE NOTE (2025-09-12, mj): Class-scope UI chunk binder + wrapper
    // Binds numeric+unit (e.g., "125 mg") and per‑kg tokens with NBSP, then applies 3-line wrap logic.
    string WrapMedicationLineForUI(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        string txt = s;
        try
        {
            // Remove any stray colon between name and calc
            txt = System.Text.RegularExpressions.Regex.Replace(txt, @"\s*:\s*", " ");
            // Bind final dose following '=': "= 125 mg" -> "= 125\u00A0mg"
            txt = System.Text.RegularExpressions.Regex.Replace(txt, @"=\s*([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL|J|mEq|U|units)\b",
                m => "= " + m.Groups[1].Value + "\u00A0" + m.Groups[2].Value,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Bind per‑kg: include cc/mL/mEq/U
            txt = System.Text.RegularExpressions.Regex.Replace(txt, @"\b([0-9]+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL|cc|mEq|U|units)\s*/\s*kg\b",
                m => m.Groups[1].Value + "\u00A0" + m.Groups[2].Value + "/kg",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Collapse extra spaces
            txt = System.Text.RegularExpressions.Regex.Replace(txt, @"[ \t]{2,}", " ");
        }
        catch {}
        return WrapIfLong(txt);
    }

    // Compact/clean medication display name tweaks (e.g., remove formulation tag D10W)
    string CleanMedNameForDisplay(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        string s = name;
        try
        {
            // Remove common formulation tags that bloat line length
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\bD\s*10\s*W\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Collapse extra spaces after removals
            while (s.IndexOf("  ") >= 0) s = s.Replace("  ", " ");
            s = s.Trim();
        }
        catch {}
        return s;
    }

    // CHANGE NOTE (2025-09-12, mj): Class-scope 3-line wrapping logic (used by UI wrapper)
    string WrapIfLong(string composed)
    {
        if (string.IsNullOrWhiteSpace(composed)) return composed;
        int limit = 28;
        // allow colon-free compact form even if slightly long; wrapping rules below

        string result = composed;

        // Prefer break before per‑kg to keep medication name on line 1
        try
        {
            var mPk = System.Text.RegularExpressions.Regex.Match(result, @"\b(\d+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL|mEq|U|units)\s*/\s*kg\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (mPk.Success && mPk.Index > 0)
            {
                int idx = mPk.Index;
                string left = result.Substring(0, idx).TrimEnd();
                string right = result.Substring(idx).TrimStart();
                return left + "\n" + right;
            }
        }
        catch {}

        // Step 1: break after '=' if present (force exactly two lines for calc forms)
        int eq = result.IndexOf('=');
        if (eq >= 0 && eq < result.Length - 1)
        {
            string left = result.Substring(0, eq + 1).TrimEnd();
            string right = result.Substring(eq + 1).TrimStart();
            result = left + "\n" + right;
            // After enforcing two lines, skip further splitting for calc lines
            return result;
        }

        // Step 2: if a line still exceeds limit, break before per‑kg token
        string[] lines = result.Split('\n');
        int maxLen = 0; for (int i = 0; i < lines.Length; i++) if (lines[i].Length > maxLen) maxLen = lines[i].Length;
        if (maxLen > limit)
        {
            string raw = composed.Replace("\n", " ");
            var m = System.Text.RegularExpressions.Regex.Match(raw, @"\b(\d+(?:\.[0-9]+)?)\s*(mg|mcg|g|mL)\s*/\s*kg\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success && m.Index > 0)
            {
                string left = raw.Substring(0, m.Index).TrimEnd();
                string right = raw.Substring(m.Index).TrimStart();
                result = left + "\n" + right;
            }
        }

        // Step 3: if still too long, try splitting before units to keep at most 2 lines
        lines = result.Split('\n');
        if (lines.Length > 2)
        {
            // Join extras back to the second line to enforce 2 lines max
            result = lines[0] + "\n" + string.Join(" ", lines, 1, lines.Length - 1);
        }
        return result;
    }

}
