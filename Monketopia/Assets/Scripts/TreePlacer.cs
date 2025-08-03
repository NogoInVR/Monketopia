#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

public class TreePlacer : MonoBehaviour
{
    [Header("Made by Keo.CS")]
    [Header("No need for credits")]
    public GameObject treePrefab;
    public GameObject parentObject;
    public int numberOfTrees = 100;
    public Vector3 areaSize = new Vector3(50, 0, 50);
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public float minScale = 0.5f;
    public float maxScale = 2.0f;
    public bool randomizeRotationX = false;
    public bool randomizeRotationY = true;
    public bool randomizeRotationZ = false;
    public bool avoidOverlap = true;
    public float minDistanceBetweenTrees = 2.0f;
    public bool grassMode = false;
    public GameObject surfaceObject;
    [Header("Dont ask me i just love it to make it cooler")]
    public Color lineColor;

    private List<Vector3> placedPositions = new List<Vector3>();

#if UNITY_EDITOR
    [CustomEditor(typeof(TreePlacer))]
    public class TreePlacerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TreePlacer script = (TreePlacer)target;

            // Do math to find the max of trees to not have the editor crashing for maybe 3 times cause i mest it up ;)
            float maxPossibleTrees = Mathf.Floor((script.areaSize.x * script.areaSize.z) / (script.minDistanceBetweenTrees * script.minDistanceBetweenTrees));
            maxPossibleTrees *= 0.8f; // Safety margin of 80% cause of crashing cause it said (Max number when in ideal generation)

            script.numberOfTrees = Mathf.Clamp(script.numberOfTrees, 0, (int)maxPossibleTrees);

            EditorGUILayout.LabelField("Max Possible Trees (with safety margin): " + maxPossibleTrees);

            DrawDefaultInspector();

            if (GUILayout.Button("Place Trees"))
            {
                script.PlaceTrees();
            }
            if (GUILayout.Button("Regenerate Trees"))
            {
                script.DeleteTrees();
                script.PlaceTrees();
            }
            if (GUILayout.Button("Delete Trees"))
            {
                script.DeleteTrees();
            }
        }
    }
#endif

    public void PlaceTrees()
    {
        placedPositions.Clear();

        for (int i = 0; i < numberOfTrees; i++)
        {
            Vector3 randomPosition;
            bool positionValid;
            RaycastHit hit = new RaycastHit(); // Initialize hit to avoid unassigned variable error

            do
            {
                randomPosition = new Vector3(
                    Random.Range(-areaSize.x / 2, areaSize.x / 2),
                    Random.Range(-areaSize.y / 2, areaSize.y / 2),
                    Random.Range(-areaSize.z / 2, areaSize.z / 2)
                ) + positionOffset;

                if (grassMode && surfaceObject != null)
                {
                    if (Physics.Raycast(randomPosition + Vector3.up * 100, Vector3.down, out hit))
                    {
                        randomPosition = hit.point;
                    }
                }

                positionValid = true;

                if (avoidOverlap)
                {
                    foreach (var pos in placedPositions)
                    {
                        if (Vector3.Distance(pos, randomPosition) < minDistanceBetweenTrees)
                        {
                            positionValid = false;
                            break;
                        }
                    }
                }

            } while (!positionValid);

            placedPositions.Add(randomPosition);

            Quaternion randomRotation = Quaternion.Euler(
                (randomizeRotationX ? Random.Range(0f, 360f) : 0f) + rotationOffset.x,
                (randomizeRotationY ? Random.Range(0f, 360f) : 0f) + rotationOffset.y,
                (randomizeRotationZ ? Random.Range(0f, 360f) : 0f) + rotationOffset.z
            );

            float randomScale = Random.Range(minScale, maxScale);

            GameObject tree = Instantiate(treePrefab, randomPosition + transform.position, randomRotation, parentObject.transform);
            tree.transform.localScale = Vector3.one * randomScale;

            if (grassMode && surfaceObject != null)
            {
                tree.transform.up = hit.normal; // Align the object to the surface normal
            }
        }
    }

    public void DeleteTrees()
    {
        if (parentObject != null)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parentObject.transform)
            {
                children.Add(child.gameObject);
            }

            foreach (var child in children)
            {
                DestroyImmediate(child);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = lineColor;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}

/*
 * Script by Keo.CS love yall
 * Can i get an Hyaaaa
 * Trololololo lololo huhu hu hu haaa
 */
