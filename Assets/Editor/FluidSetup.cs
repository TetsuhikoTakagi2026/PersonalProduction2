using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Unity メニュー Tools > Fluid Setup から実行する。
/// FluidRT アセットを作成し、FluidCamera と MetaballComposeMat に自動設定する。
/// </summary>
public static class FluidSetup
{
    [MenuItem("Tools/Fluid Setup")]
    public static void Run()
    {
        // ① FluidRT を作成（既存があれば再利用）
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/FluidRT.renderTexture");
        if (rt == null)
        {
            rt = new RenderTexture(540, 960, 16, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            rt.name       = "FluidRT";
            AssetDatabase.CreateAsset(rt, "Assets/FluidRT.renderTexture");
            Debug.Log("[FluidSetup] FluidRT を作成しました。");
        }
        else
        {
            Debug.Log("[FluidSetup] 既存の FluidRT を使用します。");
        }

        // ② FluidCamera の Output Texture に設定
        var fluidCamObj = GameObject.Find("FluidCamera");
        if (fluidCamObj != null)
        {
            var cam = fluidCamObj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.targetTexture = rt;
                EditorUtility.SetDirty(cam);
                Debug.Log("[FluidSetup] FluidCamera の Output Texture を設定しました。");
            }
        }
        else
        {
            Debug.LogWarning("[FluidSetup] 'FluidCamera' オブジェクトが見つかりません。");
        }

        // ③ MetaballComposeMat の _FluidTex に設定
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MetaballComposeMat.mat");
        if (mat != null)
        {
            mat.SetTexture("_FluidTex", rt);
            EditorUtility.SetDirty(mat);
            Debug.Log("[FluidSetup] MetaballComposeMat の _FluidTex を設定しました。");
        }
        else
        {
            Debug.LogWarning("[FluidSetup] 'MetaballComposeMat.mat' が見つかりません。");
        }

        // ④ アセットとシーンを保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveOpenScenes();   // ← シーンも保存

        Debug.Log("[FluidSetup] セットアップ完了！Play して確認してください。");
        EditorUtility.DisplayDialog("Fluid Setup", "セットアップ完了！\nシーンを保存しました。\nPlay して確認してください。", "OK");
    }
}
