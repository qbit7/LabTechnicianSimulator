using UnityEngine;
using UnityEditor;

public class AutoCollider
{
    [MenuItem("Tools/Add Mesh Colliders To Selected")]
    static void AddColliders()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Mesh sharedMesh = null;

            // Обычный MeshFilter
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

            if (meshFilter != null)
            {
                sharedMesh = meshFilter.sharedMesh;
            }

            // Skinned Mesh Renderer
            SkinnedMeshRenderer skinnedMesh = obj.GetComponent<SkinnedMeshRenderer>();

            if (skinnedMesh != null)
            {
                sharedMesh = skinnedMesh.sharedMesh;
            }

            // Если меша нет вообще
            if (sharedMesh == null)
            {
                Debug.LogWarning($"{obj.name} has no valid mesh!");
                continue;
            }

            // Проверяем коллайдер
            MeshCollider collider = obj.GetComponent<MeshCollider>();

            if (collider == null)
            {
                collider = obj.AddComponent<MeshCollider>();
            }

            collider.sharedMesh = sharedMesh;

            Debug.Log($"MeshCollider added to: {obj.name}");
        }

        Debug.Log("Done!");
    }
}