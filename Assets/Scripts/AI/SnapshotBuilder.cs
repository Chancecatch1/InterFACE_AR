using System.Text;
using UnityEngine;
using TMPro;

namespace HoloAI
{
    /// <summary>
    /// Builds a short text snapshot by reading UI elements that EventManager already updates.
    /// This avoids touching EventManager’s private fields.
    /// </summary>
    public static class SnapshotBuilder
    {
        public static string Build()
        {
            var sb = new StringBuilder();

            AppendIfTag(sb, "CurrentSession", "Session");
            AppendIfTag(sb, "CardiacRhythm", "CardiacRhythm");
            AppendIfTag(sb, "CPRTimer", "CPR Timer");
            AppendIfTag(sb, "EpiTimer", "Epinephrine Timer");

            // Doctor hints (current / next)
            AppendIfTag(sb, "Doc_Cur_1", "Doc Now 1");
            AppendIfTag(sb, "Doc_Cur_2", "Doc Now 2");
            AppendIfTag(sb, "Doc_Cur_3", "Doc Now 3");
            AppendIfTag(sb, "Doc_Next_1", "Doc Next 1");
            AppendIfTag(sb, "Doc_Next_2", "Doc Next 2");
            AppendIfTag(sb, "Doc_Next_3", "Doc Next 3");

            // Nurse hints (current / next)
            AppendIfTag(sb, "Nurse_Cur_1", "Nurse Now 1");
            AppendIfTag(sb, "Nurse_Cur_2", "Nurse Now 2");
            AppendIfTag(sb, "Nurse_Cur_3", "Nurse Now 3");
            AppendIfTag(sb, "Nurse_Next_1", "Nurse Next 1");
            AppendIfTag(sb, "Nurse_Next_2", "Nurse Next 2");
            AppendIfTag(sb, "Nurse_Next_3", "Nurse Next 3");

            // Medication counts (only ones that exist on screen are returned)
            AppendIfTag(sb, "AmiCount", "Amiodarone ready");
            AppendIfTag(sb, "AtroCount", "Atropine ready");
            AppendIfTag(sb, "EpiCount", "Epinephrine ready");
            AppendIfTag(sb, "LidoCount", "Lidocaine ready");
            AppendIfTag(sb, "FenCount", "Fentanyl ready");
            AppendIfTag(sb, "KenCount", "Ketamine ready");
            AppendIfTag(sb, "MidCount", "Midazolam ready");
            AppendIfTag(sb, "MorCount", "Morphine ready");
            AppendIfTag(sb, "RocCount", "Rocuronium ready");
            AppendIfTag(sb, "SucCount", "Succinylcholine ready");
            AppendIfTag(sb, "CalGCount", "10% Calcium Gluconate ready");
            AppendIfTag(sb, "CalG100Count", "10% Calcium Gluconate (higher) ready");
            AppendIfTag(sb, "CalCCount", "10% Calcium Chloride ready");
            AppendIfTag(sb, "SalCount", "Salbutamol ready");
            AppendIfTag(sb, "SodCount", "8.4% Sodium Bicarb ready");
            AppendIfTag(sb, "Sod2Count", "8.4% Sodium Bicarb (higher) ready");
            AppendIfTag(sb, "InsCount", "Insulin ready");
            AppendIfTag(sb, "GluCount", "Glucose ready");

            return sb.ToString();
        }

        private static void AppendIfTag(StringBuilder sb, string tag, string label)
        {
            var go = GameObject.FindWithTag(tag);
            if (go == null) return;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;
            var val = (tmp.text ?? "").Trim();
            if (string.IsNullOrEmpty(val)) return;
            sb.AppendLine($"{label}: {val}");
        }
    }
}
