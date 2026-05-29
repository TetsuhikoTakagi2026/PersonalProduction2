using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 案A バッフル型タンク。
/// 矩形コンテナ＋葉形バッフルフィン2枚（左1枚 上半 / 右1枚 下半）でS字流路を作る。
/// [ExecuteAlways] により Play 前の Game 画面にも表示される。
/// </summary>
[ExecuteAlways]
public class HourglassController : MonoBehaviour
{
    [Header("コンテナ")]
    [SerializeField] float containerHeight = 9f;
    [SerializeField] float containerWidth = 4.5f;
    [SerializeField] PhysicsMaterial2D wallMaterial;

    [Header("バッフルフィン（左1枚 / 右1枚）")]
    [Tooltip("フィン先端と反対壁の隙間（液体が通る幅）")]
    [SerializeField] float finTipGap = 0.75f;
    [Tooltip("壁側の三角形ベースの半高さ（上下の広がり）")]
    [SerializeField] float finHalfThick = 1.2f;
    [Tooltip("左フィン（上半）と右フィン（下半）の中心 |Y|（上下対称）")]
    [SerializeField] float finCenterY = 2.0f;

    [Header("見た目")]
    [SerializeField] Material frameMaterial;
    [SerializeField] float frameOuterWidth = 0.18f;
    [SerializeField] float frameInnerWidth = 0.05f;

    // ────── ライフサイクル ──────

    void Awake() => BuildAll();

#if UNITY_EDITOR
    void OnValidate()
    {
        // Inspector 変更時に次フレームでリビルド
        EditorApplication.delayCall += () =>
        {
            if (this != null) BuildAll();
        };
    }
#endif

    void BuildAll()
    {
        ClearChildren();
        if (Application.isPlaying) BuildColliders();
        BuildVisualFrame();
        PushShaderParams();
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            foreach (var lr in child.GetComponents<LineRenderer>())
                if (lr.sharedMaterial != null) DestroyObj(lr.sharedMaterial);
            DestroyObj(child);
        }
    }

    void DestroyObj(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    // ────── シェーダーパラメータ ──────

    void PushShaderParams()
    {
        Shader.SetGlobalFloat("_ContainerHW", containerWidth / 2f);
        Shader.SetGlobalFloat("_ContainerHH", containerHeight / 2f);
        var cam = Camera.main;
        Shader.SetGlobalFloat("_OrthoSize", cam != null ? cam.orthographicSize : 5f);
    }

    // ────── コライダー（Play モードのみ） ──────

    void BuildColliders()
    {
        float hw = containerWidth / 2f;
        float hh = containerHeight / 2f;

        // 外壁 4辺
        AddEdge(new[] { new Vector2(-hw, hh), new Vector2(hw, hh) }, "TopCap");
        AddEdge(new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh) }, "BottomCap");
        AddEdge(new[] { new Vector2(-hw, hh), new Vector2(-hw, -hh) }, "LeftWall");
        AddEdge(new[] { new Vector2(hw, hh), new Vector2(hw, -hh) }, "RightWall");

        // 左バッフル（左壁 → 右へ、上半）
        AddFin(-hw, hw - finTipGap, finCenterY, "FinLeft");

        // 右バッフル（右壁 → 左へ、下半）
        AddFin(hw, -hw + finTipGap, -finCenterY, "FinRight");
    }

    void AddEdge(Vector2[] pts, string goName)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        var col = go.AddComponent<EdgeCollider2D>();
        col.SetPoints(new List<Vector2>(pts));
        if (wallMaterial != null) col.sharedMaterial = wallMaterial;
    }

    void AddFin(float startX, float tipX, float centerY, string goName)
    {
        var pts = CreateFinPolygon(startX, tipX, centerY, finHalfThick);
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        var col = go.AddComponent<PolygonCollider2D>();
        col.SetPath(0, pts);
        if (wallMaterial != null) col.sharedMaterial = wallMaterial;
    }

    // ────── 三角形（くさび型）ポリゴン生成 ──────
    // 壁側: 上下に広がるベース（2点）、先端: 反対側への尖った1点
    //
    //  左フィン例:
    //    startX=左壁, tipX=右向き先端
    //    (startX, centerY+halfThick)  ← ベース上端
    //    (tipX,   centerY)            ← 先端
    //    (startX, centerY-halfThick)  ← ベース下端

    Vector2[] CreateFinPolygon(float startX, float tipX, float centerY, float halfThick)
    {
        return new Vector2[]
        {
            new Vector2(startX, centerY + halfThick),  // ベース上端（壁側）
            new Vector2(tipX,   centerY),               // 先端（反対壁側）
            new Vector2(startX, centerY - halfThick),  // ベース下端（壁側）
        };
    }

    // ────── ビジュアルフレーム ──────

    void BuildVisualFrame()
    {
        float hw = containerWidth / 2f;
        float hh = containerHeight / 2f;

        // 外枠（矩形）
        Vector3[] rect = {
            new(-hw,  hh, 0), new( hw,  hh, 0),
            new( hw, -hh, 0), new(-hw, -hh, 0),
            new(-hw,  hh, 0),   // 閉じる
        };
        AddFrameLine("FrameRect", rect, frameOuterWidth, new Color(0.50f, 0.78f, 1.0f, 0.55f), 10);
        AddFrameLine("FrameRectHL", rect, frameInnerWidth, new Color(0.95f, 0.98f, 1.0f, 0.85f), 11);

        // 左バッフル（上半）
        DrawFinLine("FinVisLeft", -hw, hw - finTipGap, finCenterY);

        // 右バッフル（下半）
        DrawFinLine("FinVisRight", hw, -hw + finTipGap, -finCenterY);
    }

    void DrawFinLine(string goName, float startX, float tipX, float centerY)
    {
        var poly = CreateFinPolygon(startX, tipX, centerY, finHalfThick);  // 3点の三角形

        // 閉じたループに変換
        var pts = new Vector3[poly.Length + 1];
        for (int i = 0; i < poly.Length; i++)
            pts[i] = new Vector3(poly[i].x, poly[i].y, 0f);
        pts[poly.Length] = pts[0];

        AddFrameLine(goName, pts, frameOuterWidth, new Color(0.50f, 0.78f, 1.0f, 0.55f), 10);
        AddFrameLine(goName + "_HL", pts, frameInnerWidth, new Color(0.95f, 0.98f, 1.0f, 0.85f), 11);
    }

    void AddFrameLine(string goName, Vector3[] pts, float width, Color color, int sortOrder)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sortingOrder = sortOrder;

        Material mat = null;
        if (frameMaterial != null && goName == "FrameRect")
        {
            mat = frameMaterial;
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_Color", color);
                mat.renderQueue = 3000;
            }
        }
        if (mat != null) lr.sharedMaterial = mat;
    }

    // ────── Gizmo ──────

    void OnDrawGizmos()
    {
        float hw = containerWidth / 2f;
        float hh = containerHeight / 2f;
        Vector3 o = transform.position;

        // 外枠
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawLine(o + new Vector3(-hw, hh), o + new Vector3(hw, hh));
        Gizmos.DrawLine(o + new Vector3(hw, hh), o + new Vector3(hw, -hh));
        Gizmos.DrawLine(o + new Vector3(hw, -hh), o + new Vector3(-hw, -hh));
        Gizmos.DrawLine(o + new Vector3(-hw, -hh), o + new Vector3(-hw, hh));

        // バッフルフィン（2枚）
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.85f);
        DrawFinGizmo(o, -hw, hw - finTipGap, finCenterY);
        DrawFinGizmo(o, hw, -hw + finTipGap, -finCenterY);

        // ギミックスペース（目安）
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
        float gmHalfY = finCenterY - finHalfThick - 0.1f;
        Gizmos.DrawCube(o, new Vector3(containerWidth, gmHalfY * 2f, 0.01f));
    }

    void DrawFinGizmo(Vector3 origin, float startX, float tipX, float centerY)
    {
        var pts = CreateFinPolygon(startX, tipX, centerY, finHalfThick);
        for (int i = 0; i < pts.Length - 1; i++)
            Gizmos.DrawLine(origin + new Vector3(pts[i].x, pts[i].y),
                            origin + new Vector3(pts[i + 1].x, pts[i + 1].y));
        Gizmos.DrawLine(origin + new Vector3(pts[pts.Length - 1].x, pts[pts.Length - 1].y),
                        origin + new Vector3(pts[0].x, pts[0].y));
    }
}
