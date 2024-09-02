using UnityEngine;
using UnityEditor;
using System.IO;

public class CaptureThumbnailMenu : MonoBehaviour
{
    [MenuItem("GameObject/Capture Thumbnail", false, 10)]
    private static void CaptureThumbnail()
    {
        // Récupère l'objet actuellement sélectionné
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            Debug.LogWarning("Aucun GameObject sélectionné pour capturer la miniature.");
            return;
        }

        Renderer objectRenderer = selectedObject.GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogWarning("Le GameObject sélectionné n'a pas de Renderer.");
            return;
        }

        // Masquer temporairement tous les autres objets
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        bool[] originalActiveStates = new bool[allObjects.Length];

        for (int i = 0; i < allObjects.Length; i++)
        {
            originalActiveStates[i] = allObjects[i].activeSelf;
            allObjects[i].SetActive(false);
        }

        selectedObject.SetActive(true);

        // Créer une caméra temporaire
        GameObject cameraObject = new GameObject("ThumbnailCamera");
        Camera tempCamera = cameraObject.AddComponent<Camera>();

        // Configurer la caméra
        tempCamera.backgroundColor = new Color(0, 0, 0, 0);
        tempCamera.clearFlags = CameraClearFlags.SolidColor;

        // Positionner la caméra pour qu'elle regarde l'objet
        Bounds bounds = objectRenderer.bounds;
        Vector3 cameraDirection = new Vector3(-1, 1, -1).normalized; // Angle diagonal
        float cameraDistance = bounds.extents.magnitude * 4; // Ajuster la distance
        Vector3 cameraPosition = bounds.center + cameraDirection * cameraDistance;
        tempCamera.transform.position = cameraPosition;
        tempCamera.transform.LookAt(bounds.center);

        // Ajuster le champ de vision pour une perspective plus dynamique
        tempCamera.fieldOfView = 30;

        // Créer une texture pour capturer l'image
        int resolution = 256;
        RenderTexture renderTexture = new RenderTexture(resolution, resolution, 24);
        renderTexture.format = RenderTextureFormat.ARGB32; // Support de la transparence
        tempCamera.targetTexture = renderTexture;

        // Capturer l'image
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        tempCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        texture.Apply();

        // Sauvegarder la texture en tant que fichier PNG avec transparence
        byte[] bytes = texture.EncodeToPNG();
        string thumbnailsDir = Path.Combine(Application.dataPath, "thumbnails");
        string path = Path.Combine(thumbnailsDir, $"{selectedObject.name}.png");
        File.WriteAllBytes(path, bytes);

        if (!Directory.Exists(thumbnailsDir))
        {
            Directory.CreateDirectory(thumbnailsDir);
        }

        // Libérer les ressources
        RenderTexture.active = null;
        tempCamera.targetTexture = null;
        DestroyImmediate(renderTexture);
        DestroyImmediate(cameraObject);

        // Rétablir l'état des autres objets
        for (int i = 0; i < allObjects.Length; i++)
        {
            allObjects[i].SetActive(originalActiveStates[i]);
        }

        // Actualiser la vue de l'éditeur pour voir le fichier nouvellement créé
        AssetDatabase.Refresh();

        Debug.Log($"Thumbnail saved at {path} with transparency.");
    }
}
