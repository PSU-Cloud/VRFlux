using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject principalPrefab;
    public GameObject groupiePrefab;
    public int numPrincipals = 5;
    public int numGroupies = 0;
    public float boundary = 100.0f;  // Boundary of the simulation space

    void OnValidate()
    {
        numPrincipals = 5;
        numGroupies = 25;
        boundary = 100.0f;
    }

    void Start()
    {
        InitializePrincipals();
        InitializeGroupies();
    }

    void InitializePrincipals()
    {
        for (int i = 0; i < numPrincipals; i++)
        {
            Vector3 position = Random.insideUnitSphere * boundary;
            GameObject principal = Instantiate(principalPrefab, position, Quaternion.identity);
            principal.GetComponent<Principal>().principalID = "Principal_" + i;
            principal.GetComponent<Principal>().boundary = boundary;
        }
    }

    void InitializeGroupies()
    {
        for (int i = 0; i < numGroupies; i++)
        {
            Vector3 position = Random.insideUnitSphere * boundary;
            GameObject groupie = Instantiate(groupiePrefab, position, Quaternion.identity);
            groupie.GetComponent<Groupie>().groupieID = "Groupie_" + i;
            groupie.GetComponent<Groupie>().boundary = boundary;
        }
    }
}
