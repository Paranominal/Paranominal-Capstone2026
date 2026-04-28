using UnityEngine;
using System;

public class WeaponEvents : MonoBehaviour
{
    public event Action<WeakPointType> ShotFired;
    public event Action<WeakPointType, bool> ShotResolved;
    public event Action<int, int> AmmoChanged;
    public event Action ReloadStarted;
    public event Action<float> ReloadProgressChanged;
    public event Action ReloadFinished;

    public void RaiseShotFired(WeakPointType shotType) => ShotFired?.Invoke(shotType);
    public void RaiseShotResolved(WeakPointType shotType, bool rewardedShot) => ShotResolved?.Invoke(shotType, rewardedShot);
    public void RaiseAmmoChanged(int currentAmmo, int magazineSize) => AmmoChanged?.Invoke(currentAmmo, magazineSize);
    public void RaiseReloadStarted() => ReloadStarted?.Invoke();
    public void RaiseReloadProgressChanged(float progress) => ReloadProgressChanged?.Invoke(progress);
    public void RaiseReloadFinished() => ReloadFinished?.Invoke();
}
