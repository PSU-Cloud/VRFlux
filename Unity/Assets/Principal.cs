using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor.UI;

public class Principal : MonoBehaviour
{
    public string principalID = "Principal";
    public Vector3 direction;
    public float velocity = 5.0f;
    public float boundary = 100.0f;  // Boundary of the simulation space

    public float rotationSpeed = 10.0f;

    private Camera principalCamera1;
    private Camera principalCamera2;
    private float nextCheckTime = 0.0f;
    public float checkInterval = 1.0f; // Interval to check visibility

    private Dictionary<string, (bool, bool)> objectVisibilityStatus;

    void OnValidate()
    {
        velocity = 5.0f;
        boundary = 100.0f;
    }

    void Start()
    {
        // initialize the position to be within the boundary
        float x = Random.Range(-boundary / 2, boundary / 2);
        float z = Random.Range(-boundary / 2, boundary / 2);
        float y = Random.Range(0, boundary);
        transform.position = new Vector3(x, y, z);
        direction = Random.insideUnitSphere.normalized;

        // Get Camera components from children
        Camera[] cameras = GetComponentsInChildren<Camera>();
        if (cameras.Length < 2)
        {
            Debug.LogError("Less than two cameras found in children. Please ensure there are two cameras as children of the Principal object.");
            return;
        }

        principalCamera1 = cameras[0];
        principalCamera2 = cameras[1];

        principalCamera1.fieldOfView = 110.0f;
        principalCamera2.fieldOfView = 140.0f;

        // Initialize the dictionary to track object visibility status
        objectVisibilityStatus = new Dictionary<string, (bool, bool)>();
    }

    void Update()
    {
        if (ManifestReader.IsLoadingComplete)
        {
            Move();
            CheckBoundary();

            // Rotate the principal at random direction
            Vector3 randomDirection = new Vector3(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            ).normalized;  // Normalize to ensure consistent rotation speed
            transform.Rotate(randomDirection, rotationSpeed * Time.deltaTime);

            // Ensure the second camera has the same position and rotation as the first camera
            principalCamera2.transform.position = principalCamera1.transform.position;
            principalCamera2.transform.rotation = principalCamera1.transform.rotation;

            // Check visibility of objects at specified intervals
            if (Time.time >= nextCheckTime)
            {
                CheckVisibleObjectsAndLog(principalCamera1, principalCamera2);
                nextCheckTime = Time.time + checkInterval;
            }
        }
    }

    void Move()
    {
        transform.position += direction * velocity * Time.deltaTime;
    }

    void CheckBoundary()
    {
        Vector3 newPos = transform.position;
        // create a random direction if the principal hits the boundary
        if (newPos.x > boundary / 2 || newPos.x < -boundary / 2) direction.x = -direction.x;
        if (newPos.y > boundary || newPos.y <= 0) direction.y = -direction.y;
        if (newPos.z > boundary / 2 || newPos.z < -boundary / 2) direction.z = -direction.z;
    }

    void CheckVisibleObjectsAndLog(Camera camera1, Camera camera2)
    {
        // Prepare to capture the log details
        string logFilePath = "visible_objects_log.txt";
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            // Get the current timestamp
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

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
                        LogVisibilityChange(writer, timestamp, objectName, obj.transform.position, principalID, camera1.transform.position, isVisible1, isVisible2, Vector3.Distance(camera1.transform.position, obj.transform.position));
                        objectVisibilityStatus[objectName] = (isVisible1, isVisible2);
                    }
                }
                else
                {
                    // Log the visibility state for new objects
                    if (isVisible1 || isVisible2)
                    {
                        LogVisibilityChange(writer, timestamp, objectName, obj.transform.position, principalID, camera1.transform.position, isVisible1, isVisible2, Vector3.Distance(camera1.transform.position, obj.transform.position));
                    }
                    objectVisibilityStatus[objectName] = (isVisible1, isVisible2);
                }
            }
        }
    }

    void LogVisibilityChange(StreamWriter writer, string timestamp, string objectName, Vector3 objectPosition, string principalID, Vector3 cameraPosition, bool isVisible1, bool isVisible2, float distance)
    {
        // the closest principal to a principal is itself
        string closestPrincipal = principalID;
        writer.WriteLine($"{timestamp},{objectName},{objectPosition.x},{objectPosition.y},{objectPosition.z},{principalID},{cameraPosition.x},{cameraPosition.y},{cameraPosition.z},{isVisible1},{isVisible2},{distance}, {closestPrincipal}");
    }

    bool IsObjectVisible(Renderer renderer, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }
}
