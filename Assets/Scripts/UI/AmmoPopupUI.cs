using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoPopupUI : MonoBehaviour
{
    [System.Serializable]
    public class AmmoData
    {
        public string ammoName;
        public Sprite icon;
        public int count;

        [Header("발사체")]
        public GameObject projectilePrefab;
    }

    [Header("UI References")]
    public Image currentAmmoIcon;
    public TMP_Text ammoCountText;

    [Header("Ammo List")]
    public AmmoData[] ammoList;

    private int currentIndex = 0;

    void Start()
    {
        RefreshUI();
    }

    void Update()
    {
        if (!CanUseAmmoInput())
            return;

        HandleInput();
    }

    private bool CanUseAmmoInput()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CanPopupInput;
    }

    void HandleInput()
    {
        if (!HasAmmoEntries())
            return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = ammoList.Length - 1;

            RefreshUI();
            PlayAmmoSwapSound();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            currentIndex++;
            if (currentIndex >= ammoList.Length)
                currentIndex = 0;

            RefreshUI();
            PlayAmmoSwapSound();
        }
    }

    private void PlayAmmoSwapSound()
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayOneShot(AudioManager.I.sfxAimRotate, 1f);
    }

    void RefreshUI()
    {
        if (!HasAmmoEntries())
        {
            ClearCurrentAmmoUI();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, ammoList.Length - 1);
        AmmoData currentAmmo = ammoList[currentIndex];

        if (currentAmmoIcon != null)
        {
            currentAmmoIcon.sprite = currentAmmo.icon;
            currentAmmoIcon.enabled = currentAmmo.icon != null;
        }

        if (ammoCountText != null)
            ammoCountText.text = "x" + currentAmmo.count.ToString();
    }

    public bool HasAmmoEntries()
    {
        return ammoList != null && ammoList.Length > 0;
    }

    private void ClearCurrentAmmoUI()
    {
        currentIndex = 0;

        if (currentAmmoIcon != null)
        {
            currentAmmoIcon.sprite = null;
            currentAmmoIcon.enabled = false;
        }

        if (ammoCountText != null)
            ammoCountText.text = "";
    }

    public int GetCurrentAmmoIndex()
    {
        if (!HasAmmoEntries())
            return -1;

        return currentIndex;
    }

    public string GetCurrentAmmoName()
    {
        if (!HasAmmoEntries())
            return "";

        return ammoList[currentIndex].ammoName;
    }

    public void SetAmmoCount(int ammoIndex, int newCount)
    {
        if (ammoList == null)
            return;

        if (ammoIndex < 0 || ammoIndex >= ammoList.Length)
            return;

        ammoList[ammoIndex].count = newCount;

        if (ammoIndex == currentIndex)
            RefreshUI();
    }

    public int GetCurrentAmmoCount()
    {
        if (!HasAmmoEntries())
            return 0;

        return ammoList[currentIndex].count;
    }

    public GameObject GetCurrentProjectilePrefab()
    {
        if (!HasAmmoEntries())
            return null;

        return ammoList[currentIndex].projectilePrefab;
    }

    public bool CanUseCurrentAmmo(int amount = 1)
    {
        if (!HasAmmoEntries())
            return false;

        if (amount <= 0)
            return true;

        return ammoList[currentIndex].count >= amount;
    }

    public bool TryUseCurrentAmmo(int amount = 1)
    {
        if (!CanUseCurrentAmmo(amount))
            return false;

        ammoList[currentIndex].count -= amount;
        RefreshUI();

        return true;
    }

    public void AddAmmo(int ammoIndex, int amount)
    {
        if (ammoList == null)
            return;

        if (ammoIndex < 0 || ammoIndex >= ammoList.Length)
            return;

        if (amount <= 0)
            return;

        ammoList[ammoIndex].count += amount;

        if (ammoIndex == currentIndex)
            RefreshUI();
    }
}
