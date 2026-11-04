#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ActualizarDialogosFigma
{
    [MenuItem("Tools/Pathfinding/Configurar icono Avanzar como Sprite")]
    public static void ConfigurarIconoAvanzar()
    {
        var importer = AssetImporter.GetAtPath("Assets/Resources/IconoAvanzar.png") as TextureImporter;
        if (importer == null) { Debug.LogError("No se encontró IconoAvanzar.png en Resources."); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Debug.Log("IconoAvanzar configurado como Sprite para la escena del laberinto.");
    }

    [MenuItem("Tools/Pathfinding/Aplicar medidas Figma a diálogos de escena abierta")]
    public static void Aplicar()
    {
        var escena = SceneManager.GetActiveScene();
        int actualizados = 0;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.gameObject.scene != escena || (rt.name != "Dialogo" && rt.name != "Dialogo_Axel")) continue;

            Undo.RecordObject(rt, "Actualizar medidas de diálogo Figma");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(260, -641);
            rt.sizeDelta = new Vector2(1070, 200);

            foreach (Transform child in rt)
            {
                var crt = child as RectTransform;
                if (crt == null) continue;
                float x = 0, y = 0, w = crt.sizeDelta.x, h = crt.sizeDelta.y;
                bool set = true;
                switch (child.name)
                {
                    case "Retrato_Byte": case "Retrato_Axel": x = 24; y = 34; w = 117; h = 115; break;
                    case "Nombre": x = 160; y = 35; w = 850; h = 27; break;
                    case "Mensaje": x = 160; y = 75; w = 850; h = 112; break;
                    default: set = false; break;
                }
                if (!set) continue;
                Undo.RecordObject(crt, "Actualizar distribución de diálogo");
                crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
                crt.pivot = new Vector2(0, 1);
                crt.anchoredPosition = new Vector2(x, -y);
                crt.sizeDelta = new Vector2(w, h);

                var raw = child.GetComponent<RawImage>();
                if (raw != null) raw.raycastTarget = false;
                var image = child.GetComponent<Image>();
                if (image != null) { image.preserveAspect = true; image.raycastTarget = false; }
                var text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    Undo.RecordObject(text, "Actualizar tipografía de diálogo");
                    if (child.name == "Nombre")
                    {
                        text.fontSize = 18;
                        text.fontStyle = FontStyles.Bold;
                        text.characterSpacing = 0;
                    }
                    else
                    {
                        text.fontSize = 24;
                        text.fontStyle = FontStyles.Normal;
                        text.lineSpacing = 8.4f;
                        text.characterSpacing = 0;
                        text.enableWordWrapping = true;
                        text.overflowMode = TextOverflowModes.Overflow;
                    }
                }
            }
            EditorUtility.SetDirty(rt.gameObject);
            actualizados++;
        }
        if (actualizados > 0)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
        Debug.Log($"Medidas Figma aplicadas a {actualizados} diálogo(s) en {escena.name}.");
    }
}
#endif
