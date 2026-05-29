using UnityEngine;

/// <summary>
/// メタボール表示用クワッドをカメラの全画面サイズに自動調整する。
/// Quad の MeshRenderer に MetaballCompose マテリアルを設定し、
/// このスクリプトをアタッチするだけで配置が完了する。
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class FluidDisplaySetup : MonoBehaviour
{
    [Tooltip("手前のソーティング調整（背景より上、ガラスフレームより下）")]
    [SerializeField] int sortingOrder = 1;
    [SerializeField] string sortingLayerName = "Default";

    void Awake()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning("[FluidDisplaySetup] Main Camera が見つからないか Orthographic でありません。");
            return;
        }

        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        transform.position   = cam.transform.position + cam.transform.forward * 0.5f;
        transform.localScale = new Vector3(w, h, 1f);

        // ソーティング設定
        var mr = GetComponent<MeshRenderer>();
        mr.sortingLayerName  = sortingLayerName;
        mr.sortingOrder      = sortingOrder;
    }
}
