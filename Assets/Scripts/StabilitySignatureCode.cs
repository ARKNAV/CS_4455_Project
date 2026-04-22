using UnityEngine;

public static class StabilitySignatureCode
{
    private static string currentCode = string.Empty;

    public static bool HasCode
    {
        get { return !string.IsNullOrEmpty(currentCode); }
    }

    public static string CurrentCode
    {
        get { return currentCode; }
    }

    public static void SetCode(string newCode)
    {
        currentCode = string.IsNullOrEmpty(newCode) ? string.Empty : newCode.Trim();
        Debug.Log($"StabilitySignatureCode: Updated active stability signature to '{currentCode}'.");
    }

    public static void ClearCode()
    {
        currentCode = string.Empty;
        Debug.Log("StabilitySignatureCode: Cleared active stability signature.");
    }
}
