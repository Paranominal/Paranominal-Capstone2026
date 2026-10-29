using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DecalPool : MonoBehaviour
{
    public static DecalPool Instance;

    [SerializeField] private bool doCulling;
    [SerializeField, ShowIf("doCulling", true), Range(0,500)] private int maxPoolSize = 50;
    private List<DecalProjector> decalPool = new List<DecalProjector>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"[{this}] There can only be one DecalPool");
            Destroy(this);
        }
    }

    public static void RegisterDecal(DecalProjector decal)
    {
        Instance.decalPool.Add(decal);
        decal.transform.SetParent(Instance.transform);

        if (Instance.decalPool.Count > Instance.maxPoolSize)
        {
            DecalProjector removalTarget = Instance.decalPool[0];
            Instance.decalPool.Remove(removalTarget);
            Destroy(removalTarget.gameObject);
        }
    }
}
