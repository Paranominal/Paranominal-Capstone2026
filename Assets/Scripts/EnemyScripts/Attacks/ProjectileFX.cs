using System.Collections;
using UnityEngine;

public class Projectile_FX : MonoBehaviour
{
    [SerializeField] private Animator animator;
    // [SerializeField] private MeshRenderer[] meshes;
    // [SerializeField] private string sliderPropertyName;
    // private Material[] materials;

    // void Awake()
    // {
    //     // setup flash materials
    //     materials = new Material[meshes.Length];
    //     for (int i = 0; i < meshes.Length; i++)
    //     {
    //         materials[i] = meshes[i].material;
    //     }
    // }
    public void StartFX()
    {
        // StartCoroutine(StartFXCoroutine());
        animator.SetTrigger("rise");
    }
    
    // private Coroutine StartFXCoroutine()
    // {
    //     // float f = 0f;
    //     // float timeElapsed = 0f;
    //     // while (timeElapsed < 1)
    //     // {
    //     //     timeElapsed += Time.deltaTime;

    //     //     f = Mathf.Lerp(1f, Lerp(f, timeElapsed / 1);
    //     //     SetFlashAmount(f);

    //     //     yield return null;
    //     // }
    //     // foreach (MeshRenderer mesh in meshes)
    //     // {
    //     //     foreach (Material mat in mesh.materials)
    //     //     {
    //     //         while timeElapsed <
    //     //         mat.SetFloat(sliderPropertyName, flashColor);
    //     //     }
    //     // }
    // }
    public void EndFX()
    {
        animator.SetTrigger("fall");
    }
    
    public void DoCollisionFX()
    {
        
    }
}
