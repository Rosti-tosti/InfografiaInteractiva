#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TMPFontSetup
{
    [MenuItem("Tools/Pathfinding/Preparar TextMeshPro")]
    public static void Preparar()
    {
        ImportarRecursosEsenciales();
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Resources/Fonts");
        CrearFuente("Assets/Fonts/SpaceMono-Bold.ttf", "Assets/Resources/Fonts/SpaceMono-Bold SDF.asset");
        CrearFuente("Assets/Fonts/SpaceMono-Regular.ttf", "Assets/Resources/Fonts/SpaceMono-Regular SDF.asset");
        CrearFuente("Assets/Fonts/Inter-Regular.ttf", "Assets/Resources/Fonts/Inter-Regular SDF.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fuentes TextMeshPro listas para las escenas.");
    }

    static void ImportarRecursosEsenciales()
    {
        if (File.Exists("Assets/Text Mesh Pro/Resources/TMP Settings.asset") ||
            File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;

        var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro");
        if (package == null) { Debug.LogError("No se encontró el paquete TextMeshPro."); return; }
        var unityPackage = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
        if (File.Exists(unityPackage)) AssetDatabase.ImportPackage(unityPackage, false);
        else Debug.LogError("No se encontró TMP Essential Resources.unitypackage.");
    }

    static void CrearFuente(string fontPath, string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null) return;
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null) { Debug.LogError("No se encontró la fuente: " + fontPath); return; }
        var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);
        if (asset == null) { Debug.LogError("No se pudo crear la fuente SDF: " + fontPath); return; }
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);
        if (asset.material != null) AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var atlas in asset.atlasTextures)
            if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, asset);
        EditorUtility.SetDirty(asset);
    }
}
#endif
