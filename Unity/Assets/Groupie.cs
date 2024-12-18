using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class Groupie : MonoBehaviour
{
    public string groupieID = "groupie";
    public float attractionStrength = 20.0f;
    public float maxForce = 50.0f;
    public float diffusionStrength = 50.0f;
    public float boundary = 100.0f;  // Boundary of the simulation space
    public float dampingFactor = 0.9f;  // Damping factor to prevent large jumps
    public float rotationSpeed = 10.0f;
    public float checkInterval = 1.0f; // Interval to check visibility
    private float nextCheckTime = 0.0f;
    private Vector3 velocity;

    private Camera groupieCamera1;
    private Camera groupieCamera2;
    
    private Dictionary<string, (bool, bool)> objectVisibilityStatus;

    void Start()
    {
        // Initialize the groupie position within the boundary
        float x = Random.Range(-boundary / 2, boundary / 2);
        float z = Random.Range(-boundary / 2, boundary / 2);
        float y = Random.Range(0, boundary);
        transform.position = new Vector3(x, y, z);
        // random rotation of the groupie
        transform.rotation = Random.rotation;

        velocity = Vector3.zero;

        // Get Camera components from children
        Camera[] cameras = GetComponentsInChildren<Camera>();
        if (cameras.Length < 2)
        {
            Debug.LogError("Less than two cameras found in children. Please ensure there are two cameras as children of the Groupie object.");
            return;
        }

        groupieCamera1 = cameras[0];
        groupieCamera2 = cameras[1];

        groupieCamera1.fieldOfView = 110.0f;
        groupieCamera2.fieldOfView = 140.0f;

        // Initialize the dictionary to track object visibility status
        objectVisibilityStatus = new Dictionary<string, (bool, bool)>();
    }

    void Update()
    {
        if (ManifestReader.IsLoadingComplete)
        {
            // Move the groupie towards the principal
            MoveTowardsPrincipal();
            CheckBoundary();

            // Ensure the second camera has the same position and rotation as the first camera
            groupieCamera2.transform.position = groupieCamera1.transform.position;
            groupieCamera2.transform.rotation = groupieCamera1.transform.rotation;

            // Check visibility of objects at specified intervals
            if (Time.time >= nextCheckTime)
            {
                CheckVisibleObjectsAndLog(groupieCamera1, groupieCamera2);
                nextCheckTime = Time.time + checkInterval;
            }
        }
    }

    void MoveTowardsPrincipal()
    {
        Principal[] principals = FindObjectsOfType<Principal>();
        float minDistance = float.MaxValue;
        Principal closestPrincipal = null;

        // Find the closest principal
        foreach (Principal principal in principals)
        {
            float distance = Vector3.Distance(transform.position, principal.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPrincipal = principal;
            }
        }

        if (closestPrincipal != null)
        {
            Vector3 attraction = (closestPrincipal.transform.position - transform.position).normalized * attractionStrength;
            if (attraction.magnitude > maxForce)
                attraction = attraction.normalized * maxForce;

            Vector3 diffusion = new Vector3(
                Random.Range(-diffusionStrength, diffusionStrength),
                Random.Range(-diffusionStrength, diffusionStrength),
                Random.Range(-diffusionStrength, diffusionStrength)
            );

            // Apply damping to the velocity to prevent large jumps
            velocity += (attraction + diffusion) * Time.deltaTime;
            velocity *= dampingFactor;  // Damping effect
            transform.position += velocity * Time.deltaTime;
        }

        // Rotate the camera around its own axis
        Vector3 randomDirection = new Vector3(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f)
        ).normalized;  // Normalize to ensure consistent rotation speed
        transform.Rotate(randomDirection, rotationSpeed * Time.deltaTime);
    }

    void CheckBoundary()
    {
        Vector3 pos = transform.position;

        // Clamp position to stay within boundaries
        pos.x = Mathf.Clamp(pos.x, -boundary / 2, boundary / 2);
        pos.y = Mathf.Clamp(pos.y, 0, boundary);
        pos.z = Mathf.Clamp(pos.z, -boundary / 2, boundary / 2);

        transform.position = pos;
    }

    void CheckVisibleObjectsAndLog(Camera camera1, Camera camera2)
    {
        // Prepare to capture the log details
        string logFilePath = "visible_objects_log.txt";
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            // Get the current timestamp
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // Find the closest principal
            Principal[] principals = FindObjectsOfType<Principal>();
            Principal closestPrincipal = FindClosestPrincipal(transform.position, principals);

            // Find all objects in the scene tagged with "shapenet"
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("shapenet");

            foreach (GameObject obj in taggedObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                bool isVisible1 = renderer != null && IsObjectVisible(renderer, camera1);
                bool isVisible2 = renderer != null && IsObjectVisible(renderer, camera2);
                string objectName = obj.name;

                if (objectVisibilityStatus.TryGetValue(objectName, out var prevVisibility))
                {
                    if ((prevVisibility.Item1 != isVisible1) || (prevVisibility.Item2 != isVisible2))
                    {
                        // Log the visibility state if it has changed
                        LogVisibilityChange(writer, timestamp, objectName, obj.transform.position, groupieID, camera1.transform.position, isVisible1, isVisible2, Vector3.Distance(camera1.transform.position, obj.transform.position), closestPrincipal);
                        objectVisibilityStatus[objectName] = (isVisible1, isVisible2);
                    }
                }
                else
                {
                    
                    // Log the visibility state for new objects
                    if (isVisible1 || isVisible2) {
                        LogVisibilityChange(writer, timestamp, objectName, obj.transform.position, groupieID, camera1.transform.position, isVisible1, isVisible2, Vector3.Distance(camera1.transform.position, obj.transform.position), closestPrincipal);
                    }
                    objectVisibilityStatus[objectName] = (isVisible1, isVisible2);
                }
            }
        }
    }

    Principal FindClosestPrincipal(Vector3 position, Principal[] principals)
    {
        float minDistance = float.MaxValue;
        Principal closestPrincipal = null;

        foreach (Principal principal in principals)
        {
            float distance = Vector3.Distance(position, principal.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPrincipal = principal;
            }
        }

        return closestPrincipal;
    }

    void LogVisibilityChange(StreamWriter writer, string timestamp, string objectName, Vector3 objectPosition, string groupieID, Vector3 cameraPosition, bool isVisible1, bool isVisible2, float distance, Principal closestPrincipal)
    {
        string closestPrincipalID = closestPrincipal != null ? closestPrincipal.principalID : "None";
        writer.WriteLine($"{timestamp},{objectName},{objectPosition.x},{objectPosition.y},{objectPosition.z},{groupieID},{cameraPosition.x},{cameraPosition.y},{cameraPosition.z},{isVisible1},{isVisible2},{distance},{closestPrincipalID}");
    }

    bool IsObjectVisible(Renderer renderer, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }
}
