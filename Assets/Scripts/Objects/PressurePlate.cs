using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("눌렀을 때 꺼질 전기 타일")]
    public ElectricTile[] targetElectricTiles;

    [Header("눌렀을 때 켜질 전기 타일")]
    public ElectricTile[] targetElectricTilesToActivate;

    [Header("스케일 애니메이션")]
    public float pressedScale = 0.9f;
    public float lerpSpeed = 10f;

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();

    private Vector3 initialScale;
    private bool isPressed = false;

    private void Start()
    {
        initialScale = transform.localScale;
        ApplyElectricTileStates();
    }

    private void Update()
    {
        CleanupInvalidOccupants();

        Vector3 targetScale = isPressed ? initialScale * pressedScale : initialScale;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * lerpSpeed
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidOccupant(other)) return;

        occupants.Add(other);
        RefreshPressedState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidOccupant(other)) return;

        occupants.Remove(other);
        RefreshPressedState();
    }

    private bool IsValidOccupant(Collider2D other)
    {
        return other.CompareTag("Player") || other.CompareTag("Box");
    }

    private void CleanupInvalidOccupants()
    {
        if (occupants.Count == 0) return;

        int removedCount = occupants.RemoveWhere(c =>
            c == null ||
            !c.enabled ||
            !c.gameObject.activeInHierarchy
        );

        if (removedCount > 0)
        {
            RefreshPressedState();
        }
    }

    private void RefreshPressedState()
    {
        bool shouldBePressed = occupants.Count > 0;
        SetPressed(shouldBePressed);
    }

    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed) return;

        isPressed = pressed;

        PlayPlateSound();
        ApplyElectricTileStates();
    }

    private void PlayPlateSound()
    {
        if (AudioManager.I == null) return;

        AudioClip clip = isPressed
            ? AudioManager.I.sfxPlateDown
            : AudioManager.I.sfxPlateUp;

        AudioManager.I.PlayOneShot(clip, 0.9f);
    }

    private void ApplyElectricTileStates()
    {
        SetElectricTiles(targetElectricTiles, !isPressed);
        SetElectricTiles(targetElectricTilesToActivate, isPressed);
    }

    private void SetElectricTiles(ElectricTile[] tiles, bool active)
    {
        if (tiles == null) return;

        foreach (ElectricTile tile in tiles)
        {
            if (tile == null) continue;

            tile.SetElectricState(active);
        }
    }
}
