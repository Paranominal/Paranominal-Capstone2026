using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ContainerCheck : MonoBehaviour
{
    enum CheckType
    {
        exactMatch,
        duplicatesAllowed,
        anyOrder
    }
    //public GameObject tempDeleteObject; //temporary feature, deletes assigned game object on successful use.
    [SerializeField] private GameObject rewardItem;
    [SerializeField] private GameObject cauldron;

    [SerializeField] private string weakPointId;
    public Container container;
    public List<ALTGrimoireEntry> solution;
    public List<ALTGrimoireEntry> failures;
    InputAction collectAction;
    public LayerMask interactable;
    private Raycaster raycaster;
    [SerializeField]
    CheckType checkType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");
        if (raycaster == null)
        {
            raycaster = FindAnyObjectByType<Raycaster>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(raycaster.Ray, out RaycastHit hit, 1000f, interactable))
        {
            if (collectAction.WasReleasedThisFrame() && GetComponentInChildren<Collider>() == hit.collider && container.contents.Count != 0)
            {
                if (CompareLists())
                {
                    Debug.Log("Success! You solved the puzzle!");
                    //Destroy(tempDeleteObject); // Destroys the object assigned for deletion (TEMP)
                    Instantiate(rewardItem, cauldron.transform.position + new Vector3(0f,1.5f,0f), Quaternion.identity);
                    if (!string.IsNullOrEmpty(weakPointId))
                    {
                        WeakPointRegistry.UnlockWardedWeakPointById(weakPointId);
                    }
                }
                else
                {
                    if (CheckFailures())
                    {
                        Debug.Log("Uh oh! Something exploded! Ow!");
                    }
                    Debug.Log("You failed the puzzle :(");
                    container.contents = new List<ALTGrimoireEntry>();
                }

            }
        }
    }

    public bool CompareLists()
    {
        switch (checkType)
        {
            case CheckType.duplicatesAllowed:
                foreach (ALTGrimoireEntry e in solution)
                {
                    bool found = false;
                    foreach (ALTGrimoireEntry g in container.contents)
                    {
                        if (e.entryName == g.entryName)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                return true;
            case CheckType.anyOrder:
                if (solution.Count != container.contents.Count)
                {
                    return false;
                }
                List<ALTGrimoireEntry> tempSolution = solution;
                List<ALTGrimoireEntry> tempContainer = container.contents;
                
                foreach (ALTGrimoireEntry e in tempSolution)
                {
                    bool found = false;
                    foreach (ALTGrimoireEntry g in tempContainer)
                    {
                        if (e.entryName == g.entryName)
                        {
                            found = true;
                            tempContainer.Remove(g);
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                return true;

            case CheckType.exactMatch:
                if (solution.Count != container.contents.Count)
                {
                    return false;
                }
                for (int i = 0; i < solution.Count; i++)
                {
                    if (solution[i].entryName != container.contents[i].entryName)
                    {
                        return false;
                    }
                }
                return true;
            default:
                return false;
        }
    }

    public bool CheckFailures()
    {
        foreach (ALTGrimoireEntry e in failures)
        {
            bool found = false;
            foreach (ALTGrimoireEntry g in container.contents)
            {
                if (e.entryName == g.entryName)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return false;
            }
        }
        return true;
    }
}
