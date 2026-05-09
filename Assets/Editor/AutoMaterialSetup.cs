using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoMaterialSetup
{
    [MenuItem("Tools/Auto Setup Materials")]
    static void SetupMaterials()
    {
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model");

        foreach (string guid in fbxGuids)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(guid);

            string folder = Path.GetDirectoryName(modelPath);
            string modelName = Path.GetFileNameWithoutExtension(modelPath);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            AssignTexture(folder, modelName, mat, "_BaseMap", "BaseColor");
            AssignTexture(folder, modelName, mat, "_BaseMap", "Albedo");
            AssignTexture(folder, modelName, mat, "_BumpMap", "Normal", true);
            AssignTexture(folder, modelName, mat, "_MetallicGlossMap", "Metallic");
            AssignTexture(folder, modelName, mat, "_OcclusionMap", "AO");
            AssignTexture(folder, modelName, mat, "_EmissionMap", "Emission");

            string matPath = folder + "/" + modelName + "_MAT.mat";

            AssetDatabase.CreateAsset(mat, matPath);

            Renderer renderer = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath)
                ?.GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }

            Debug.Log("Created material for: " + modelName);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("DONE");
    }

    static void AssignTexture(string folder, string modelName, Material mat, string property, string keyword, bool normal = false)
    {
        string[] textures = Directory.GetFiles(folder);

        foreach (string tex in textures)
        {
            string lower = tex.ToLower();

            if (lower.Contains(modelName.ToLower()) &&
                lower.Contains(keyword.ToLower()))
            {
                string assetPath = tex.Replace("\\", "/");
                assetPath = assetPath.Substring(assetPath.IndexOf("Assets"));

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (texture != null)
                {
                    if (normal)
                    {
                        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                        importer.textureType = TextureImporterType.NormalMap;
                        importer.SaveAndReimport();
                    }

                    mat.SetTexture(property, texture);

                    if (property == "_EmissionMap")
                    {
                        mat.EnableKeyword("_EMISSION");
                    }

                    Debug.Log("Assigned: " + texture.name);
                }
            }
        }
    }
}