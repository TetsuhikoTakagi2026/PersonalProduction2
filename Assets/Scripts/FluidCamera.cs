using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 液体専用カメラ。FluidCircle シェーダーを使った円を RenderTexture に加算描画し、
/// MetaballCompose マテリアルを持つ UI RawImage に渡す。
///
/// 【セットアップ手順】
/// 1. 空の GameObject に Camera + この FluidCamera を追加。
///    - Clear Flags: Solid Color / Background: (0,0,0,0)
///    - Culling Mask: "Fluid" レイヤーのみ ON
///    - Projection: Orthographic
/// 2. Hierarchy > UI > Raw Image を作成（Canvas が自動生成される）。
///    - RectTransform をフルスクリーンに設定（四隅アンカー）
/// 3. Inspector で fluidRawImage に RawImage を、
///    composeMaterial に MetaballComposeMat を設定。
/// </summary>
[RequireComponent(typeof(Camera))]
public class FluidCamera : MonoBehaviour
{
    [SerializeField] int         renderWidth  = 540;
    [SerializeField] int         renderHeight = 960;
    [SerializeField] RawImage    fluidRawImage;
    [SerializeField] Material    composeMaterial;

    Camera        _cam;
    RenderTexture _rt;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        _rt = new RenderTexture(renderWidth, renderHeight, 16, RenderTextureFormat.Default)
        {
            filterMode = FilterMode.Bilinear
        };
        _cam.targetTexture = _rt;

        if (composeMaterial == null || fluidRawImage == null) return;

        var mat = Instantiate(composeMaterial);
        mat.SetTexture("_FluidTex", _rt);
        fluidRawImage.material = mat;
    }

    void LateUpdate()
    {
        var main = Camera.main;
        if (main == null || main == _cam) return;
        transform.position    = main.transform.position;
        transform.rotation    = main.transform.rotation;
        _cam.orthographicSize = main.orthographicSize;
    }

    void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }
}
