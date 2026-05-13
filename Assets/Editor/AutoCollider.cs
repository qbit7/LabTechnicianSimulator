using UnityEngine;
using UnityEditor;

public class AutoCollider
{
    [MenuItem("Tools/Add Mesh Colliders To Selected")]
    static void AddColliders()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            MeshFilter[] meshes = obj.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter mesh in meshes)
            {
                if (mesh.GetComponent<MeshCollider>() == null)
                {
                    mesh.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        Debug.Log("Mesh Colliders Added!");
    }
}