using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 砂時計の外壁を EdgeCollider2D で自動生成する。
/// Inspector でサイズを調整し、Play 前に形状を Gizmo で確認できる。
/// </summary>
public class HourglassController : MonoBehaviour
{
    [Header("形状パラメータ")]
    [SerializeField] float totalHeight  = 6f;
    [SerializeField] float bulbWidth    = 1.8f;
    [SerializeField] float neckWidth    = 0.36f;
    [SerializeField] float neckHeight   = 0.5f;
    [SerializeField] PhysicsMaterial2D wallMaterial;

    [Header("見た目")]
    [SerializeField] Material frameMaterial;
    [SerializeField] float    frameWidth = 0.08f;

    void Awake()
    {
        BuildColliders();
        BuildVisualFrame();
    }

    void BuildColliders()
    {
        // 既存の子オブジェクトをクリア（再ビルド対応）
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        float h  = totalHeight / 2f;
        float bw = bulbWidth   / 2f;
        float nw = neckWidth   / 2f;
        float nh = neckHeight  / 2f;

        // 左壁（上 → くびれ → 下）
        AddEdge(new[]
        {
            new Vector2(-bw,  h),
            new Vector2(-bw,  nh),
            new Vector2(-nw,  0f),
            new Vector2(-bw, -nh),
            new Vector2(-bw, -h),
        }, "LeftWall");

        // 右壁（左壁の鏡像）
        AddEdge(new[]
        {
            new Vector2(bw,  h),
            new Vector2(bw,  nh),
            new Vector2(nw,  0f),
            new Vector2(bw, -nh),
            new Vector2(bw, -h),
        }, "RightWall");

        // 上蓋
        AddEdge(new[] { new Vector2(-bw, h), new Vector2(bw, h) }, "TopCap");

        // 下蓋
        AddEdge(new[] { new Vector2(-bw, -h), new Vector2(bw, -h) }, "BottomCap");
    }

    void AddEdge(Vector2[] pts, string wallName)
    {
        var go  = new GameObject(wallName);
        go.transform.SetParent(transform, false);
        var col = go.AddComponent<EdgeCollider2D>();
        col.SetPoints(new List<Vector2>(pts));
        if (wallMaterial != null) col.sharedMaterial = wallMaterial;
    }

    void BuildVisualFrame()
    {
        float h  = totalHeight / 2f;
        float bw = bulbWidth   / 2f;
        float nw = neckWidth   / 2f;
        float nh = neckHeight  / 2f;

        // 砂時計の輪郭を一周するパス
        Vector3[] pts =
        {
            new Vector3(-bw,  h),
            new Vector3( bw,  h),
            new Vector3( bw,  nh),
            new Vector3( nw,  0),
            new Vector3( bw, -nh),
            new Vector3( bw, -h),
            new Vector3(-bw, -h),
            new Vector3(-bw, -nh),
            new Vector3(-nw,  0),
            new Vector3(-bw,  nh),
            new Vector3(-bw,  h),   // 始点に戻って閉じる
        };

        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace  = false;
        lr.loop           = false;
        lr.positionCount  = pts.Length;
        lr.SetPositions(pts);
        lr.startWidth     = frameWidth;
        lr.endWidth       = frameWidth;
        lr.sortingOrder   = 10;   // 液体より手前に描画

        if (frameMaterial != null)
            lr.material = frameMaterial;
    }

    // ────── Gizmo ──────
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        float h  = totalHeight / 2f;
        float bw = bulbWidth   / 2f;
        float nw = neckWidth   / 2f;
        float nh = neckHeight  / 2f;
        Vector3 o = transform.position;

        Vector3[] pts =
        {
            o + new Vector3(-bw,  h),
            o + new Vector3( bw,  h),
            o + new Vector3( bw,  nh),
            o + new Vector3( nw,  0),
            o + new Vector3( bw, -nh),
            o + new Vector3( bw, -h),
            o + new Vector3(-bw, -h),
            o + new Vector3(-bw, -nh),
            o + new Vector3(-nw,  0),
            o + new Vector3(-bw,  nh),
            o + new Vector3(-bw,  h),
        };

        for (int i = 0; i < pts.Length - 1; i++)
            Gizmos.DrawLine(pts[i], pts[i + 1]);
    }
}
