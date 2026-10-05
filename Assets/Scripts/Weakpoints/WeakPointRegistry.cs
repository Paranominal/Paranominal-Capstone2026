using System.Collections.Generic;

public static class WeakPointRegistry
{
    private static readonly HashSet<WeakPoint> weakPoints = new HashSet<WeakPoint>();
    private static readonly List<WeakPoint> staleWeakPoints = new List<WeakPoint>();

    public static void Register(WeakPoint weakPoint)
    {
        if (weakPoint == null)
            return;

        weakPoints.Add(weakPoint);
    }

    public static void Unregister(WeakPoint weakPoint)
    {
        if (weakPoint == null)
            return;

        weakPoints.Remove(weakPoint);
    }

    public static bool Exists(string weakPointId)
    {
        return TryGetWeakPointById(weakPointId, out _);
    }

    public static bool TryGetWeakPointById(string weakPointId, out WeakPoint weakPoint)
    {
        weakPoint = null;

        if (string.IsNullOrEmpty(weakPointId))
            return false;

        staleWeakPoints.Clear();

        foreach (WeakPoint candidate in weakPoints)
        {
            if (candidate == null)
            {
                staleWeakPoints.Add(candidate);
                continue;
            }

            if (candidate.PointId != weakPointId)
                continue;

            weakPoint = candidate;
            CleanupStaleEntries();
            return true;
        }

        CleanupStaleEntries();
        return false;
    }

    public static bool UnlockWardedWeakPointById(string weakPointId)
    {
        if (!TryGetWeakPointById(weakPointId, out WeakPoint weakPoint))
            return false;

        if (!weakPoint.IsWarded)
            return false;

        weakPoint.UnlockWeakPoint();
        return true;
    }

    private static void CleanupStaleEntries()
    {
        if (staleWeakPoints.Count == 0)
            return;

        foreach (WeakPoint staleWeakPoint in staleWeakPoints)
            weakPoints.Remove(staleWeakPoint);

        staleWeakPoints.Clear();
    }
}
