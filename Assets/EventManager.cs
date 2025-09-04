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
using System.Text.RegularExpressions; //mj
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
    // Add a deduplication set at class scope (mj)
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

    // CHANGE NOTE (2025-09-02, agent): Split Nurse UI source flags for Current vs Next.
    // Why: Current panel should be state-based (medication()), Next should use server hints (advanced preparation).
    // What changed: Introduced useServerHintsForNurseCurrent and useServerHintsForNurseNext.
    // Behavior/Assumptions: Prevents overwrites and supports hybrid display.
    // Rollback: Merge back to single flag or toggle both to same value.
    bool useServerHintsForNurseCurrent = false; // state-based current panel
    bool useServerHintsForNurseNext = true;    // server hints for next panel
    // Max number of Next Steps items to display (default 3 to fill all slots)
    int MAX_NEXT_STEPS = 3;
    // De-duplication controls (defaults keep previous behavior)
    bool dedupNextSteps = true;
    bool dedupAgainstCurrent = true;

    ArrayList notiArr = new ArrayList();
    ArrayList notiCprArr = new ArrayList();
    ArrayList notiEpiArr = new ArrayList();
    ArrayList sessionArr = new ArrayList();

    EventSourceReader evt;

    private string filePath;

    //Multi language support
    //en, fr
    string lang = "en";
    SimpleJSON.JSONNode multi;
    // Patient weight (kg) from patientModel; used for dose computations when server parameter is absent (mj)
    double bodyWeightKg = 0;

    // Helper to robustly find TMP by tag, even if tag is on a parent (mj)
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

        Doc_Cur_1 = GetTMPByTag("Doc_Cur_1");
        Doc_Cur_2 = GetTMPByTag("Doc_Cur_2");
        Doc_Cur_3 = GetTMPByTag("Doc_Cur_3");

        Doc_Next_1 = GetTMPByTag("Doc_Next_1");
        Doc_Next_2 = GetTMPByTag("Doc_Next_2");
        Doc_Next_3 = GetTMPByTag("Doc_Next_3");

        Nurse_Cur_1 = GetTMPByTag("Nurse_Cur_1");
        Nurse_Cur_2 = GetTMPByTag("Nurse_Cur_2");
        Nurse_Cur_3 = GetTMPByTag("Nurse_Cur_3");

        Nurse_Next_1 = GetTMPByTag("Nurse_Next_1");
        Nurse_Next_2 = GetTMPByTag("Nurse_Next_2");
        Nurse_Next_3 = GetTMPByTag("Nurse_Next_3");

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
    void UpdateNoti (string name, string dose, int type) {
        if (noti != null && notiTransform != null) {
            if (type == 0 && notiMedPref != null) {
                GameObject myInstance = Instantiate(notiMedPref, notiTransform);

                notiArr.Add(myInstance);
                StartCoroutine(Remove_Noti(myInstance, 0));
                TextMeshProUGUI txt = myInstance.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                if (lang == "en") {
                    txt.text = name + "\n" + dose + " given";
                }
                else if (lang == "fr"){
                    txt.text = name + "\n" + dose + " administré";
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

        Debug.Log("name: " + name + ", dose: " + dose);
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
                // Volume (mj)
                s = s.Replace("cc", "mL").Replace("CC", "mL");
                s = s.Replace("ML", "mL").Replace("Ml", "mL").Replace("ml", "mL");
                // Mass
                s = s.Replace("MG", "mg").Replace("Mg", "mg");
                // Weight
                s = s.Replace("KG", "kg").Replace("Kg", "kg");
                // Ensure consistent spacing like "J/kg" (keep as is by default)
                return s;
            }

            // Extract only the final numeric result part from a calculation string (mj)
            // e.g., "0.01 mg/kg (0.1 ml/kg) = 0.87 mg" -> "0.87 mg"
            string ExtractFinalValue(string param)
            {
                if (string.IsNullOrWhiteSpace(param)) return "";
                int idx = param.LastIndexOf('=');
                string s = (idx >= 0 ? param.Substring(idx + 1) : param).Trim();
                if (!string.IsNullOrWhiteSpace(s)) return NormalizeUnits(s); // mj

                // Fallback: try to grab trailing numeric+unit even without '=' // mj
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
                s = Regex.Replace(s, @"\([^)]*?/\s*kg[^)]*\)", "", RegexOptions.IgnoreCase);
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

            // Optional: wrap long lines after the colon for readability
            string WrapIfLong(string composed)
            {
                if (string.IsNullOrWhiteSpace(composed)) return composed;
                if (composed.Length <= 28) return composed;

                int colon = composed.IndexOf(':');
                if (colon > 0 && colon < composed.Length - 1)
                {
                    string left = composed.Substring(0, colon).TrimEnd();
                    string right = composed.Substring(colon + 1).TrimStart();
                    if (right.Length > 18 && right.Contains(" (")) right = right.Replace(" (", "\n(");
                    return left + ":\n" + right;
                }
                int par = composed.IndexOf(" (");
                if (par > 0)
                {
                    return composed.Substring(0, par) + "\n" + composed.Substring(par + 1).TrimStart(' ');
                }
                return composed;
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
                s = Regex.Replace(s, @"(?<!\s)\(", " (" );
                s = Regex.Replace(s, @"\(\s+", "(");
                s = Regex.Replace(s, @"\s+\)", ")");
                s = Regex.Replace(s, @":\s*", ": ");
                s = Regex.Replace(s, @"[ \t]{2,}", " ");
                return s.Trim();
            }

            // CHANGE NOTE (2025-09-02, mj): Nurse Advanced Preparation specific post-formatting.
            // Why: Keep general wording unchanged; only standardize units/spacing.
            string FormatAdvancedPreparationNurse(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s;
                var text = NormalizeUnits(s);
                text = TidySpacing(text);
                return text;
            }

            // CHANGE NOTE (2025-09-01, mj): Compose display text with optional computed final.
            // Why: If patient weight is unknown or no parameter is provided, we must NOT strip mg/kg from the base text.
            // What changed: Perform cleaning ONLY when a final numeric value exists; otherwise keep original template text.
            // Behavior/Assumptions: For shock energy, we keep only per-kg in final and remove absolute J in base.
            // Rollback: Restore earlier flow that cleaned base text before deciding on final.
            string ComposeTextFinal(SimpleJSON.JSONNode n)
            {
                if (n == null) return "";

                // CHANGE NOTE (2025-09-02, mj): Process plain string tags through the same dose-selection logic.
                // Why: Some Nurse Next hints arrive as a raw tag (string). Previously we returned the raw template, so dose rules were skipped.
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
                    // Prefer returning ONLY the per-kg energy (e.g., "2 J/kg" or "4 J/kg").
                    string perKg = ExtractPerKgJ(param);
                    if (string.IsNullOrWhiteSpace(perKg)) perKg = ExtractPerKgJ(text);

                    if (!string.IsNullOrWhiteSpace(perKg))
                    {
                        // Apply uniform panel rule: show general instruction text and the selected per-kg energy
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
                    return $"{text}: {final}";

                return text ?? "";
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
                // CHANGE NOTE (2025-09-02, agent): Guarded by useServerHintsForNurseCurrent.
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
                    // Compose: General text + final value (if any) (mj)
                    string instruction = ComposeTextFinal(instrunctions[i]);
                    instruction = WrapIfLong(instruction); // mj
                    
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
                // CHANGE NOTE (2025-09-02, agent): Guarded by useServerHintsForNurseNext to prevent duplication with medication() UI.
                SimpleJSON.JSONNode instrunctions = obj["cprNurseHintsModel"]["nextStepHints"];

                // Next Steps: General text + final value, de-dup with current (by tag+text), cap to MAX_NEXT_STEPS (mj)
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
                int filled = 0;

                // initialize
                if (Nurse_Next_1 != null) Nurse_Next_1.text = "";
                if (Nurse_Next_2 != null) Nurse_Next_2.text = "";
                if (Nurse_Next_3 != null) Nurse_Next_3.text = "";

                for (int i = 0; i < instrunctions.Count; i++)
                {
                    if (filled >= MAX_NEXT_STEPS) break; // mj
                    string val = ComposeTextFinal(instrunctions[i]); // (mj)
                    val = FormatAdvancedPreparationNurse(val);
                    val = WrapIfLong(val);
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    string key = DedupKeyFromNode(instrunctions[i]);
                    if (dedupAgainstCurrent && curSet.Contains(key)) continue; // mj
                    if (dedupNextSteps && !seen.Add(key)) continue;

                    if (filled == 0 && Nurse_Next_1 != null)
                    {
                        Nurse_Next_1.text = val;
                        filled++;
                        continue;
                    }
                    if (filled == 1 && Nurse_Next_2 != null)
                    {
                        Nurse_Next_2.text = val;
                        filled++;
                        continue;
                    }
                    if (filled == 2 && Nurse_Next_3 != null) // mj
                    {
                        Nurse_Next_3.text = val;
                        filled++;
                        continue;
                    }
                }
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
                    // Compose: General text + final value (if any) (mj)
                    // Doctor: DO NOT split medication name and dose to two lines (nurse-only).
                    // If the line is too long, we only wrap after ':' or before '(' for readability.
                    string instruction = ComposeTextFinal(instrunctions[i]);
                    instruction = WrapIfLong(instruction);

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

                // Doctor Next Steps: General text + final value, de-dup (by tag+text), cap to MAX_NEXT_STEPS (mj)
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
                    if (filledDoc >= MAX_NEXT_STEPS) break; // (mj)

                    // Doctor: Only wrap long lines; do not move dose to a new line (nurse-only behavior).
                    string val = ComposeTextFinal(instrunctions[i]);
                    val = WrapIfLong(val);
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    string key = DedupKeyFromNode(instrunctions[i]); // mj
                    if (dedupAgainstCurrent && curDocSet.Contains(key)) continue; 
                    if (dedupNextSteps && !seenDoc.Add(key)) continue;

                    if (filledDoc == 0 && Doc_Next_1 != null) {
                        Doc_Next_1.text = val;
                        filledDoc++;
                        continue;
                    }
                    if (filledDoc == 1 && Doc_Next_2 != null) { //mj
                        Doc_Next_2.text = val;
                        filledDoc++;
                        continue;
                    }
                    if (filledDoc == 2 && Doc_Next_3 != null) { //mj
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
                    GameObject temp = (GameObject)notiEpiArr[i];
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
            GameObject temp = (GameObject)notiArr[i];
            if (onOff) {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[7];
            } else {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[8];
            }
        }

        for (int i = 0; i < notiCprArr.Count; i++) {
            GameObject temp = (GameObject)notiCprArr[i];
            if (onOff) {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[5];
            } else {
                temp.transform.GetChild(0).transform.GetChild(0).gameObject.GetComponent<CanvasElementRoundedRect>().material = mat[6];
            }
        }

        for (int i = 0; i < notiEpiArr.Count; i++) {
            GameObject temp = (GameObject)notiEpiArr[i];
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
        refMed();
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
        Debug.Log("currentStatus");
        Debug.Log(response);

        // Debug.Log("idx: " + idx);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();

        SimpleJSON.JSONNode cprProtocolModel = response["cprProtocolModel"];
        SimpleJSON.JSONNode cprHintModel = response["cprHintModel"];
        SimpleJSON.JSONNode patientModel = response["patientModel"];

        Debug.Log(cprProtocolModel);

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
                // CHANGE NOTE (2025-09-01, agent): Persist body weight for dose computations later in the same session.
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

        // Detect Confirmation of Medication (mj)
        SimpleJSON.JSONNode cprMedicationModel = response["cprMedicationModel"];
        if (cprMedicationModel != null) {
            StartCoroutine(medicationInitialize(MedicationFinder.getProcessId()));
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
                                    bool isEpi = false; // mj
                                    try { isEpi = medIdNum == 5; } catch {}

                                    bool callUpdateNoti = meds.Count > 1; // avoid duplicate noti with the single-med branch
                                    m_queueAction.Enqueue(() => {
                                        if (isAmio) HighlightAmiodaroneOrder(false);
                                        if (isEpi) HighlightEpinephrineOrder(false);
                                        ConfirmFlashOrderDisplay(medName);
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
                if (cprTimersModel["cprTimerOn"] != null) {
                    Debug.Log(cprTimersModel["cprTimerOn"]);
                    DateTime dt = DateTime.Parse((string) cprTimersModel["cprTimer"]);
                    cprStartTimestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();

                    time1 = (cprStartTimestamp - unixTime) / 1000;
                    Debug.Log("cprTimer");
                    Debug.Log(time1);
                    Debug.Log("cprTimer");
                    if (prev_cprStartTimestamp != cprStartTimestamp) {
                        StartCoroutine(SetCPR_5Sec(false));
                        cpr_5sec = false;
                        cpr_5sec_coroutine = false;
                        prev_cprStartTimestamp = cprStartTimestamp;
                    }
                }
            }
        } catch (Exception e) {
            Debug.Log(e);
        }
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
                                        if (doseInstance["status"] == "PREPARING" && doseInstance["autoPrescribed"] == false) {
                                            // if (Nurse_Cur_1.text != "" && Nurse_Cur_2.text != "" && Nurse_Cur_3.text != "") {
                                            //     Nurse_Cur_1.text = Nurse_Cur_2.text;
                                            //     Nurse_Cur_2.text = Nurse_Cur_3.text;
                                            //     Nurse_Cur_3.text = "";
                                            // }
                                            if (Nurse_Cur_1 != null && Nurse_Cur_1.text == "") {
                                                Nurse_Cur_1.text = id;
                                                if (String.IsNullOrWhiteSpace(Nurse_Cur_1.text.Replace("•", ""))) {
                                                    Nurse_Cur_1.text = "";
                                                }
                                            } else if (Nurse_Cur_2 != null && Nurse_Cur_2.text == "") {
                                                Nurse_Cur_2.text = id;
                                                if (String.IsNullOrWhiteSpace(Nurse_Cur_2.text.Replace("•", ""))) {
                                                    Nurse_Cur_2.text = "";
                                                }
                                            } else if (Nurse_Cur_3 != null && Nurse_Cur_3.text == "") {
                                                Nurse_Cur_3.text = id;
                                                if (String.IsNullOrWhiteSpace(Nurse_Cur_3.text.Replace("•", ""))) {
                                                    Nurse_Cur_3.text = "";
                                                }
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

                // CHANGE NOTE (2025-09-02, agent): Hybrid Nurse UI sources.
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

                            if (preVal > 0 || val > 0) {
                                resCount++;
                                if (Nurse_Cur_1 != null && iii == 0) { Nurse_Cur_1.text = FindMultiLang("Amiodarone") + " 125mg"; iii++; }
                                else if (Nurse_Cur_2 != null && iii == 1) { Nurse_Cur_2.text = FindMultiLang("Amiodarone") + " 125mg"; iii++; }
                                else if (Nurse_Cur_3 != null && iii == 2) { Nurse_Cur_3.text = FindMultiLang("Amiodarone") + " 125mg"; iii++; }
                                }
                                
                                // CHANGE CHANGE (KO/EN)
                                // NOTE (2025-09-04, mj)
                                // 왜(Why): 사용자 요구사항에 따라 약물 카드의 행은 "주문됨(ORDERED = PREPARING/AUTO_PREPARING)"일 때 노란색으로 하이라이트되고,
                                //         "준비됨(READY)"이 되면 하이라이트를 제거해야 합니다. DONE 유무는 하이라이트 판단에 영향을 주지 않습니다.
                                // 무엇이 바뀜(What Changed): 하이라이트 조건을 isOrdered && !hasReady 로 단순화했습니다.
                                // 동작/가정(Behaviour/Assumptions): PREPARING/AUTO_PREPARING이 하나라도 있고, READY 인스턴스가 없으면 노란색.
                                // 위험평가(Risk Assessment):
                                // - Level: Low
                                // - Impact: READY가 생성되는 즉시 노란색이 꺼져 사용자의 의도(주문 상태만 강조)와 일치합니다.
                                // - Rollback: 필요 시 DONE도 고려하거나, READY 존재 시에도 강조하도록 조건을 되돌릴 수 있습니다.
                                //
                                // CHANGE CHANGE (EN)
                                // NOTE (2025-09-04, mj)
                                // Why: Per requirement, highlight the medication row when ORDERED (PREPARING/AUTO_PREPARING) and remove it when READY.
                                //      DONE presence should not affect the highlight decision.
                                // What Changed: Simplified the condition to isOrdered && !hasReady.
                                // Behaviour/Assumptions: Yellow if there is any PREPARING/AUTO_PREPARING and no READY instances.
                                // Risk Assessment:
                                // - Level: Low
                                // - Impact: Yellow turns off as soon as READY appears, aligning with user intent.
                                // - Rollback: Reintroduce DONE checks or relax READY constraints if needed.
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
                            if (preVal > 0 || val > 0) {
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
                            // CHANGE CHANGE (KO/EN)
                            // NOTE (2025-09-04, mj)
                            // 왜(Why): 약물 카드는 "주문됨(ORDERED)"일 때 노란색, "준비됨(READY)"이면 해제되어야 합니다. DONE은 무관합니다.
                            // 무엇이 바뀜(What Changed): 하이라이트 조건을 isOrdered && !hasReady 로 변경.
                            // 동작/가정(Behaviour/Assumptions): PREPARING/AUTO_PREPARING 존재 && READY 없음 → 노란색.
                            // 위험평가(Risk Assessment):
                            // - Level: Low
                            // - Impact: READY가 생성되면 즉시 노란색 해제.
                            // - Rollback: 필요 시 DONE 고려나 조건 완화 가능.
                            //
                            // CHANGE CHANGE (EN)
                            // NOTE (2025-09-04, mj)
                            // Why: Medication row should be yellow when ORDERED and removed when READY. DONE should not affect.
                            // What Changed: Use isOrdered && !hasReady.
                            // Behaviour/Assumptions: Yellow if PREPARING/AUTO_PREPARING exists and no READY.
                            // Risk Assessment:
                            // - Level: Low
                            // - Impact: Turns off as soon as READY appears.
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

                if (resCount > 0) {
                    if (resTabOrderIcon != null) resTabOrderIcon.enabled = true;
                } else {
                    if (resTabOrderIcon != null) resTabOrderIcon.enabled = false;
                }
                
                if (intCount > 0) {
                    if (intTabOrderIcon != null) intTabOrderIcon.enabled = true;
                } else {
                    if (intTabOrderIcon != null) intTabOrderIcon.enabled = false;
                }
                
                if (hypCount > 0) {
                    if (hypTabOrderIcon != null) hypTabOrderIcon.enabled = true;
                } else {
                    if (hypTabOrderIcon != null) hypTabOrderIcon.enabled = false;
                }
            }
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
                medication(medications);
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
            Debug.Log($"{e.Event} : {e.Message}");
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
                Debug.LogWarning("PenToggle not found when togglePenMode ran"); 
                return; 
            }
            FontIconSelector fis = pen.GetComponentInChildren<FontIconSelector>(true);
            if (fis == null) { 
                Debug.LogWarning("FontIconSelector not found under PenToggle"); 
                return; 
            }

            // Disable TeamScreenCanvas, BedCanvas, MonitorCanvas on togglePenMode (mj)
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
                Debug.LogWarning("PenToggle not found when untogglePenMode ran"); 
                return; 
            }
            FontIconSelector fis = pen.GetComponentInChildren<FontIconSelector>(true);
            if (fis == null) { 
                Debug.LogWarning("FontIconSelector not found under PenToggle"); 
                return; 
            }
            
            // Disable TeamScreenCanvas, BedCanvas, MonitorCanvas on untogglePenMode (mj)
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

    // CHANGE CHANGE (KO/EN)
    // NOTE (2025-09-04, mj)
    // 왜(Why): 약물 투여 확정(DONE) 시 초록색(그린) 하이라이트/플래시를 사용하면 READY/ORDERED와 시인성 충돌이 있고,
    //          사용자 요구사항에 따라 노란색 PREPARING 하이라이트만 남기고 나머지 시각 효과(초록색/깜박임)는 제거합니다.
    // 무엇이 바뀜(What Changed): Confirm(확정) 시 적용되던 초록색 플래시 로직을 전면 비활성화(no-op)했습니다.
    // 동작/가정(Behaviour/Assumptions): DONE 이벤트가 와도 UI 텍스트 색상/스타일을 변경하지 않습니다.
    // 위험평가(Risk Assessment):
    // - Level: Low
    // - Impact: 약물 목록에서 DONE 알림의 시각적 강조가 사라집니다(요구사항).
    // - Rollback: 본 함수를 과거 구현(FlashGreen 코루틴 호출)으로 되돌리면 됩니다.
    //
    // CHANGE CHANGE (EN)
    // NOTE (2025-09-04, mj)
    // Why: A green confirm flash conflicts with the visibility model for READY/ORDERED and per requirement we keep only the yellow PREPARING highlight,
    //      removing green and blinking effects.
    // What Changed: This method is now a no-op; the previous green flash on confirmation is disabled.
    // Behaviour/Assumptions: DONE events no longer change text color/style.
    // Risk Assessment:
    // - Level: Low
    // - Impact: Visual confirmation highlight is removed from the medication list (as requested).
    // - Rollback: Reintroduce the prior FlashGreen coroutine call here.
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

    // CHANGE CHANGE (KO/EN)
    // NOTE (2025-09-04, mj)
    // 왜(Why): 약물 리스트에서 PREPARING(= ordered) 상태만 노란색으로 강조하고, 다른 상태의 초록색/깜박임 효과는 제거합니다.
    // 무엇이 바뀜(What Changed): 아래 하이라이트 색상은 노란색만 유지하며, 다른 색상/블링크 로직은 별도로 제거되었습니다.
    // 동작/가정(Behaviour/Assumptions): 키워드를 포함한 텍스트에만 굵게+노란색을 적용합니다(on=true). off면 흰색/보통 글꼴로 복구.
    // 위험평가(Risk Assessment):
    // - Level: Low
    // - Impact: READY/DONE 등에서 색상 강조가 더 이상 나타나지 않음(요구사항 반영).
    // - Rollback: 필요 시 색상/스타일을 재도입하면 됩니다.
    //
    // CHANGE CHANGE (EN)
    // NOTE (2025-09-04, mj)
    // Why: Keep only a yellow highlight for PREPARING(= ordered) items and remove any green/blink effects.
    // What Changed: This helper applies only yellow emphasis; other color/blink behaviors were removed elsewhere.
    // Behaviour/Assumptions: Applies bold+yellow when the key matches; otherwise resets to normal/white.
    // Risk Assessment:
    // - Level: Low
    // - Impact: No more highlights for READY/DONE (per requirement).
    // - Rollback: Reintroduce alternative colors or effects as needed.
    // CHANGE CHANGE (KO/EN)
    // NOTE (2025-09-04, mj)
    // 왜(Why): 노란색 하이라이트가 잘 눈에 띄지 않는 문제를 개선하기 위해, 더 진한 앰버톤 색상과
    //          TextMesh Pro 아웃라인(검정)을 함께 적용해 가독성을 높였습니다.
    // 무엇이 바뀜(What Changed): 하이라이트 on 시 굵게(Bold) + 진한 노란색(#FFDA1A 근사) + OutlineWidth(~0.2) 적용,
    //          off 시 기존 색/스타일(흰색/보통)로 복구하고 OutlineWidth=0으로 되돌립니다.
    // 동작/가정(Behaviour/Assumptions): TextMesh Pro SDF 머티리얼을 사용하며, fontMaterial 접근 시 인스턴스가 생성되어
    //          개별 텍스트에만 Outline 설정이 적용됩니다(공유 머티리얼 변경 방지).
    // 위험평가(Risk Assessment):
    // - Level: Low
    // - Impact: 텍스트 대비가 높아져 하이라이트 인지성이 향상됩니다.
    // - Rollback: 색상/Outline 폭을 원래 값으로 되돌리면 됩니다.
    //
    // CHANGE CHANGE (EN)
    // NOTE (2025-09-04, mj)
    // Why: The yellow highlight was not sufficiently visible. We now apply a richer amber color and a black outline
    //      to improve readability.
    // What Changed: On highlight ON: Bold + strong yellow (~#FFDA1A) + OutlineWidth(~0.2). On highlight OFF: restore
    //      white/normal and set OutlineWidth back to 0.
    // Behaviour/Assumptions: Uses TextMesh Pro SDF material; accessing fontMaterial creates a per-instance material
    //      to avoid changing shared assets.
    // Risk Assessment:
    // - Level: Low
    // - Impact: Better contrast and visibility of highlighted rows.
    // - Rollback: Revert color/outline width to previous values.
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

    void SetOrderNormalFor(string medDisplayName) // mj
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

    void HighlightAmiodaroneOrder(bool on)
    {
        string key = FindMultiLang("Amiodarone");
        // Highlight only in Medication Order (current), not in Next Steps
        ApplyOrderHighlight(Nurse_Cur_1, key, on);
        ApplyOrderHighlight(Nurse_Cur_2, key, on);
        ApplyOrderHighlight(Nurse_Cur_3, key, on);
    }

    // CHANGE CHANGE (KO/EN)
    // NOTE (2025-09-04, mj)
    // 왜(Why): 요구사항에 따라 약물 리스트의 깜박임(blink) 효과를 전부 제거합니다. 노란색 PREPARING 하이라이트만 유지합니다.
    // 무엇이 바뀜(What Changed): BlinkText/SetBlink 및 약물별 Blink 함수(Amiodarone/Epinephrine)를 코드에서 제거했습니다.
    // 동작/가정(Behaviour/Assumptions): 어떤 상태에서도 텍스트 깜박임을 사용하지 않습니다.
    // 위험평가(Risk Assessment):
    // - Level: Low
    // - Impact: 시각적 깜박임이 사라져 눈 피로/주의 분산이 줄어듭니다.
    // - Rollback: 필요 시 해당 함수들을 복구하여 호출부에 재연결하면 됩니다.
    //
    // CHANGE CHANGE (EN)
    // NOTE (2025-09-04, mj)
    // Why: Per requirement, remove all blinking effects from the medication list UI, keeping only the yellow PREPARING highlight.
    // What Changed: Removed BlinkText/SetBlink and per-medication Blink helpers (Amiodarone/Epinephrine).
    // Behaviour/Assumptions: No blinking text for any state.
    // Risk Assessment:
    // - Level: Low
    // - Impact: Less visual distraction; align with requested visual language.
    // - Rollback: Reintroduce the removed methods and their call sites if needed.

    void HighlightEpinephrineOrder(bool on)
    {
        string key = FindMultiLang("Epinephrine");
        // Highlight only in Medication Order (current), not in Next Steps
        ApplyOrderHighlight(Nurse_Cur_1, key, on);
        ApplyOrderHighlight(Nurse_Cur_2, key, on);
        ApplyOrderHighlight(Nurse_Cur_3, key, on);
    }
}
