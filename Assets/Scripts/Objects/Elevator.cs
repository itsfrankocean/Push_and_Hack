using System.Collections;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    private Animator animator;
    private BoxCollider2D boxCollider;
    private bool isOpen = false;

    [Header("Stage UI")]
    public StageUIController stageUI;
    public string nextSceneName = "";
    public float clearPopupDelay = 0.6f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (stageUI == null)
            stageUI = FindObjectOfType<StageUIController>();
    }

    public void OpenDoor()
    {
        if (isOpen) return;

        isOpen = true;
        animator.SetTrigger("Open");

        AudioManager.I.PlayOneShot(AudioManager.I.sfxElevatorOpen, 1f);

        Debug.Log("Elevator opened.");

        StartCoroutine(ShowClearRoutine());
    }

    private IEnumerator ShowClearRoutine()
    {
        yield return new WaitForSeconds(clearPopupDelay);

        if (stageUI != null)
            stageUI.ShowStageClear(nextSceneName);
    }
}
