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
    bool          _rtOwned; // 自分で生成した RT か（OnDestroy で解放する対象）

    void Awake()
    {
        _cam = GetComponent<Camera>();

        // Inspector で既に targetTexture が割り当てられていれば再利用、
        // なければ実行時に新規生成する。
        if (_cam.targetTexture != null)
        {
            _rt      = _cam.targetTexture;
            _rtOwned = false;
            Debug.Log("[FluidCamera] 既存の targetTexture を使用: " + _rt.name);
        }
        else
        {
            _rt = new RenderTexture(renderWidth, renderHeight, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                name       = "FluidRT_Runtime"
            };
            _rt.Create();
            _cam.targetTexture = _rt;
            _rtOwned           = true;
            Debug.Log("[FluidCamera] 新しい RenderTexture を生成しました。");
        }

        // ★ ここが重要：GPU メモリが未初期化だとゴミデータ（≠ 黒）が
        //   MetaballCompose に渡り画面が真っ青になる。必ず明示クリアする。
        var prevRT = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = prevRT;

        if (fluidRawImage == null)
        {
            Debug.LogWarning("[FluidCamera] Fluid Raw Image が未設定です。");
            return;
        }

        if (composeMaterial != null)
        {
            var mat = Instantiate(composeMaterial);
            mat.SetTexture("_FluidTex", _rt);
            fluidRawImage.material = mat;
            Debug.Log("[FluidCamera] MetaballComposeMat に FluidRT を設定しました。");
        }
        else
        {
            fluidRawImage.texture = _rt;
        }
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
        if (_rtOwned && _rt != null) _rt.Release();
    }
}
