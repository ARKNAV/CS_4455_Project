using System.Collections;
using UnityEngine;

public class OnWinTriggerScript : MonoBehaviour
{
    public CanvasGroup winCanvas;
    public float winScreenDelay = 2f;

    void OnEnable()  => BlueprintConsoleController.WinTriggerEvent += OnWin;
    void OnDisable() => BlueprintConsoleController.WinTriggerEvent -= OnWin;

    void OnWin()
    {
        FreezePlayer();
        StartCoroutine(ShowCanvas());
    }

    IEnumerator ShowCanvas()
    {
        yield return new WaitForSecondsRealtime(winScreenDelay);
        winCanvas.interactable = winCanvas.blocksRaycasts = true;
        winCanvas.alpha = 1f;
        Time.timeScale = 0f;
    }

    void FreezePlayer()
    {
        DisguiseSystem ds = FindFirstObjectByType<DisguiseSystem>();
        if (ds == null) return;

        var cinput = ds.GetComponent<CharacterInputController>();
        var bcs    = ds.GetComponent<BasicControlScript>();
        var rb     = ds.GetComponent<Rigidbody>();
        var anim   = ds.GetComponent<Animator>();

        if (cinput != null) cinput.enabled = false;
        if (bcs    != null) bcs.enabled    = false;
        if (rb != null) { rb.linearVelocity = rb.angularVelocity = Vector3.zero; }

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetFloat("speed", 0f); anim.SetFloat("MoveX", 0f); anim.SetFloat("MoveY", 0f);
            anim.SetBool("isCrouching", false); anim.SetBool("isSprinting", false);
            anim.SetInteger("PeekDirection", 0); anim.SetBool("IsPeeking", false);
        }
    }
}
