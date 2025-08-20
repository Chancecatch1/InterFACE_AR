using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MedicationEventManager : MonoBehaviour
{
    public TextMeshPro drugName;

    public RawImage AmiodaroneImg;
    public RawImage CalciumGluconateImg;
    public RawImage CalciumChlorideImg;
    public RawImage EpinephrineImg;
    public RawImage LidocaineImg;
    public RawImage KetamineImg;
    public RawImage FentanylImg;

    public TextMeshProUGUI AmiodaroneCount;
    public TextMeshProUGUI CalciumGluconateCount;
    public TextMeshProUGUI CalciumChlorideCount;
    public TextMeshProUGUI EpinephrineCount;
    public TextMeshProUGUI LidocaineCount;
    public TextMeshProUGUI KetamineCount;
    public TextMeshProUGUI FentanylCount;

    RawImage Ready;
    RawImage NotReady;
    
    //Select a Texture in the Inspector to change to
    public Texture TextureReady;
    public Texture TextureNotReady;

    void Start()
    {
        //Fetch the RawImage component from the GameObject
        Ready = GetComponent<RawImage>();
        NotReady = GetComponent<RawImage>();
        //Change the Texture to be the one you define in the Inspector
        // Ready.texture = TextureReady;
        // NotReady.texture = TextureNotReady;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Amiodarone () {
        drugName.text = "Amiodarone is ready";
        AmiodaroneImg.texture = TextureReady;

        int val = int.Parse(AmiodaroneCount.text);

        val++;

        AmiodaroneCount.text = val.ToString();

        RawImage temp = GameObject.FindWithTag("Amiodarone").GetComponent<RawImage>();
        if (temp != null) {
            temp.texture = TextureReady;
        }

        if (val == 0) {
            AmiodaroneCount.enabled = false;
        } else {
            AmiodaroneCount.enabled = true;
        }
    }

    public void injectAmi() {
        drugName.text = "Amiodarone is injected";

        int val = int.Parse(AmiodaroneCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            AmiodaroneCount.text = val.ToString();
            AmiodaroneImg.texture = TextureNotReady;

            RawImage temp = GameObject.FindWithTag("Amiodarone").GetComponent<RawImage>();
            if (temp != null) {
                temp.texture = TextureNotReady;
            }
            return;
        } else {
            AmiodaroneCount.enabled = true;
        }

        AmiodaroneCount.text = val.ToString();
    }

    public void Gluconate () {
        drugName.text = "Calcium Gluconate is ready";
        CalciumGluconateImg.texture = TextureReady;

        int val = int.Parse(CalciumGluconateCount.text);

        val++;

        CalciumGluconateCount.text = val.ToString();

        if (val == 0) {
            CalciumGluconateCount.enabled = false;
        } else {
            CalciumGluconateCount.enabled = true;
        }
    }

    public void injectGluconate() {
        drugName.text = "Calcium Gluconate is injected";

        int val = int.Parse(CalciumGluconateCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            CalciumGluconateCount.text = val.ToString();
            CalciumGluconateImg.texture = TextureNotReady;
            return;
        } else {
            CalciumGluconateCount.enabled = true;
        }

        CalciumGluconateCount.text = val.ToString();
    }

    public void Chloride () {
        drugName.text = "Calcium Chloride is ready";
        CalciumChlorideImg.texture = TextureReady;

        int val = int.Parse(CalciumChlorideCount.text);

        val++;

        CalciumChlorideCount.text = val.ToString();

        if (val == 0) {
            CalciumChlorideCount.enabled = false;
        } else {
            CalciumChlorideCount.enabled = true;
        }
    }

    public void injectChloride() {
        drugName.text = "Calcium Chloride is injected";

        int val = int.Parse(CalciumChlorideCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            CalciumChlorideCount.text = val.ToString();
            CalciumChlorideImg.texture = TextureNotReady;
            return;
        } else {
            CalciumChlorideCount.enabled = true;
        }

        CalciumChlorideCount.text = val.ToString();
    }

    public void Epinephrine () {
        drugName.text = "Epinephrine is ready";
        EpinephrineImg.texture = TextureReady;

        int val = int.Parse(EpinephrineCount.text);

        val++;

        EpinephrineCount.text = val.ToString();

        RawImage temp = GameObject.FindWithTag("Epinephrine").GetComponent<RawImage>();
        if (temp != null) {
            temp.texture = TextureReady;
        }
        
        if (val == 0) {
            EpinephrineCount.enabled = false;
        } else {
            EpinephrineCount.enabled = true;
        }
    }

    public void injectEpi() {
        drugName.text = "Epinephrine is injected";

        int val = int.Parse(EpinephrineCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            EpinephrineCount.text = val.ToString();
            EpinephrineImg.texture = TextureNotReady;

            RawImage temp = GameObject.FindWithTag("Epinephrine").GetComponent<RawImage>();
            if (temp != null) {
                temp.texture = TextureNotReady;
            }
            return;
        } else {
            EpinephrineCount.enabled = true;
        }

        EpinephrineCount.text = val.ToString();
    }

    public void Lidocaine () {
        drugName.text = "Lidocaine is ready";
        LidocaineImg.texture = TextureReady;

        int val = int.Parse(LidocaineCount.text);

        val++;

        LidocaineCount.text = val.ToString();

        if (val == 0) {
            LidocaineCount.enabled = false;
        } else {
            LidocaineCount.enabled = true;
        }
    }

    public void injectLidocaine() {
        drugName.text = "Lidocaine is injected";

        int val = int.Parse(LidocaineCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            LidocaineCount.text = val.ToString();
            LidocaineImg.texture = TextureNotReady;
            return;
        } else {
            LidocaineCount.enabled = true;
        }

        LidocaineCount.text = val.ToString();
    }

    public void Ketamine () {
        drugName.text = "Ketamine is ready";
        KetamineImg.texture = TextureReady;

        int val = int.Parse(KetamineCount.text);

        val++;

        KetamineCount.text = val.ToString();

        if (val == 0) {
            KetamineCount.enabled = false;
        } else {
            KetamineCount.enabled = true;
        }
    }

    public void injectKetamine() {
        drugName.text = "Ketamine is injected";

        int val = int.Parse(KetamineCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            KetamineCount.text = val.ToString();
            KetamineImg.texture = TextureNotReady;
            return;
        } else {
            KetamineCount.enabled = true;
        }

        KetamineCount.text = val.ToString();
    }

    public void Fentanyl () {
        drugName.text = "Fentanyl is ready";
        FentanylImg.texture = TextureReady;

        int val = int.Parse(FentanylCount.text);

        val++;

        FentanylCount.text = val.ToString();

        if (val == 0) {
            FentanylCount.enabled = false;
        } else {
            FentanylCount.enabled = true;
        }
    }

    public void injectFentanyl() {
        drugName.text = "Fentanyl is injected";

        int val = int.Parse(FentanylCount.text);

        if (val == 0) return;

        val--;

        if (val == 0) {
            FentanylCount.text = val.ToString();
            FentanylImg.texture = TextureNotReady;
            return;
        } else {
            FentanylCount.enabled = true;
        }

        FentanylCount.text = val.ToString();
    }
}
