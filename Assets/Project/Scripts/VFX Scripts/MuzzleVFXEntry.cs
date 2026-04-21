using UnityEngine;

[System.Serializable]
public class MuzzleVFXEntry
{
    [Header("Muzzle VFX Type")]
    [Tooltip("Muzzle VFX group.")]
    public MuzzleVFXType muzzleVfxType;

    [Header("Muzzle VFX Prefab")]
    [Tooltip("Mapped muzzle VFX prefab.")]
    public GameObject muzzleVfxPrefab;
}