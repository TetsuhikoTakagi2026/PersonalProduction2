using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 案A バッフル型タンク。
/// 矩形コンテナ＋葉形バッフルフィン2枚（左1枚 上半 / 右1枚 下半）でS字流路を作る。
/// [ExecuteAlways] により Play 前の Game 画面にも表示される。
///
/// ビジュアルフレームは LineRenderer ではなく、コンテナを覆う1枚のクワッドに
/// GlassFrame シェーダーを貼って SDF でガラス枠＋フィンを描画する。
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

    [Header("ガラスフレーム見た目")]
    [Tooltip("GlassFrame シェーダーを使ったマテリアル")]
    [SerializeField] Material glassFrameMaterial;
    [SerializeField] float frameThickness = 0.18f;
    [SerializeField] float finThickness = 0.16f;
    [Tooltip("枠の描画クワッドが画面より少し大きくなるよう余白を足す")]
    [SerializeField] float quadPadding = 1.0f;
    [SerializeField] int frameSortingOrder = 11;

    // ランタイム生成物
    GameObject frameQuad;
    Material runtimeFrameMat;

    // ────── ライフサイクル ──────

    void OnEnable() => BuildAll();

#if UNITY_EDITOR
    void OnValidate()
    {
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
        frameQuad = null;
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

        // ガラスフレーム用マテリアルにも形状を渡す
        if (runtimeFrameMat != null)
        {
            float ortho = cam != null ? cam.orthographicSize : 5f;
            runtimeFrameMat.SetFloat("_ContainerHW", containerWidth / 2f);
            runtimeFrameMat.SetFloat("_ContainerHH", containerHeight / 2f);
            runtimeFrameMat.SetFloat("_OrthoSize", ortho);
            runtimeFrameMat.SetFloat("_FrameThickness", frameThickness);
            runtimeFrameMat.SetFloat("_FinThickness", finThickness);

            float hw = containerWidth / 2f;
            // 左フィン（上半）の3頂点
            var l = CreateFinPolygon(-hw, hw - finTipGap, finCenterY, finHalfThick);
            // 右フィン（下半）の3頂点
            var r = CreateFinPolygon(hw, -hw + finTipGap, -finCenterY, finHalfThick);
            runtimeFrameMat.SetVector("_FinL0", l[0]);
            runtimeFrameMat.SetVector("_FinL1", l[1]);
            runtimeFrameMat.SetVector("_FinL2", l[2]);
            runtimeFrameMat.SetVector("_FinR0", r[0]);
            runtimeFrameMat.SetVector("_FinR1", r[1]);
            runtimeFrameMat.SetVector("_FinR2", r[2]);
        }
    }

    // ────── コライダー（Play モードのみ） ──────

    void BuildColliders()
    {
        float hw = containerWidth / 2f;
        float hh = containerHeight / 2f;

        AddEdge(new[] { new Vector2(-hw, hh), new Vector2(hw, hh) }, "TopCap");
        AddEdge(new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh) }, "BottomCap");
        AddEdge(new[] { new Vector2(-hw, hh), new Vector2(-hw, -hh) }, "LeftWall");
        AddEdge(new[] { new Vector2(hw, hh), new Vector2(hw, -hh) }, "RightWall");

        AddFin(-hw, hw - finTipGap, finCenterY, "FinLeft");
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

    Vector2[] CreateFinPolygon(float startX, float tipX, float centerY, float halfThick)
    {
        return new Vector2[]
        {
            new Vector2(startX, centerY + halfThick),  // ベース上端（壁側）
            new Vector2(tipX,   centerY),               // 先端（反対壁側）
            new Vector2(startX, centerY - halfThick),  // ベース下端（壁側）
        };
    }

    // ────── ビジュアルフレーム（ガラスシェーダー1枚クワッド）──────

    void BuildVisualFrame()
    {
        // コンテナ全体を覆うクワッドを生成し、ガラスシェーダーで枠＋フィンを描く
        frameQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        frameQuad.name = "GlassFrameQuad";
        // CreatePrimitive で付く Collider は不要なので削除
        var col = frameQuad.GetComponent<Collider>();
        if (col != null) DestroyObj(col);

        frameQuad.transform.SetParent(transform, false);
        frameQuad.transform.localPosition = Vector3.zero;
        // 枠が画面端で切れないよう少し大きめに
        float w = containerWidth + quadPadding * 2f;
        float h = containerHeight + quadPadding * 2f;
        frameQuad.transform.localScale = new Vector3(w, h, 1f);

        var mr = frameQuad.GetComponent<MeshRenderer>();

        // マテリアルを用意（指定があればそれを複製、なければシェーダーから生成）
        if (glassFrameMaterial != null)
        {
            runtimeFrameMat = new Material(glassFrameMaterial);
        }
        else
        {
            var shader = Shader.Find("Custom/GlassFrame");
            if (shader != null) runtimeFrameMat = new Material(shader);
        }

        if (runtimeFrameMat != null)
        {
            mr.sharedMaterial = runtimeFrameMat;
            mr.sortingOrder = frameSortingOrder;
            // 2D 用に影など無効化
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }

    // ────── Gizmo ──────

    void OnDrawGizmos()
    {
        float hw = containerWidth / 2f;
        float hh = containerHeight / 2f;
        Vector3 o = transform.position;

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawLine(o + new Vector3(-hw, hh), o + new Vector3(hw, hh));
        Gizmos.DrawLine(o + new Vector3(hw, hh), o + new Vector3(hw, -hh));
        Gizmos.DrawLine(o + new Vector3(hw, -hh), o + new Vector3(-hw, -hh));
        Gizmos.DrawLine(o + new Vector3(-hw, -hh), o + new Vector3(-hw, hh));

        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.85f);
        DrawFinGizmo(o, -hw, hw - finTipGap, finCenterY);
        DrawFinGizmo(o, hw, -hw + finTipGap, -finCenterY);

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
