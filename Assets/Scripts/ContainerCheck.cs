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

    public Container container;
    public List<ALTGrimoireEntry> solution;
    public List<ALTGrimoireEntry> failures;
    InputAction collectAction;
    public LayerMask interactable;
    [SerializeField]
    CheckType checkType;
    public Outline outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collectAction = InputSystem.actions.FindAction("Collect");

    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactable))
        {
            if (GetComponentInChildren<Collider>() == hit.collider)
            {
                if (outline != null)
                {
                    outline.enabled = true;
                }
                if (collectAction.WasReleasedThisFrame() && container.contents.Count != 0)
                {
                    if (CompareLists())
                    {
                        Debug.Log("Success! You solved the puzzle!");
                    }
                    else
                    {
                        if (CheckFailures())
                        {
                            Debug.Log("Uh oh! Something exploded! Ow!");
                        }
                        Debug.Log("You failed the puzzle :(");
                    }

                }
            }
            else if (outline != null)
            {
                outline.enabled = false;
            }

        }
        else if (outline != null)
        {
            outline.enabled = false;
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
