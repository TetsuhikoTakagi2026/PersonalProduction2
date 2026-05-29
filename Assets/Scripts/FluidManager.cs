using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 液体粒子を砂時計の上半分に初期配置し、プールで管理する。
/// ジャイロで重力が変わると粒子は自然に流れ落ちる。
/// </summary>
public class FluidManager : MonoBehaviour
{
    [Header("粒子設定")]
    [SerializeField] GameObject fluidPrefab;
    [SerializeField] int        particleCount = 60;

    [Header("初期配置エリア（上半分）")]
    [SerializeField] float spawnWidth  = 1.4f;
    [SerializeField] float spawnYMin   = 0.3f;  // 中心から上方向の最低高さ
    [SerializeField] float spawnYMax   = 2.4f;  // 中心から上方向の最大高さ

    readonly List<Rigidbody2D> _particles = new();

    void Start() => SpawnAll();

    void SpawnAll()
    {
        foreach (var p in _particles)
            if (p != null) Destroy(p.gameObject);
        _particles.Clear();

        for (int i = 0; i < particleCount; i++)
        {
            float x   = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
            float y   = Random.Range(spawnYMin, spawnYMax);
            var   pos = (Vector2)transform.position + new Vector2(x, y);
            var   go  = Instantiate(fluidPrefab, pos, Quaternion.identity, transform);

            if (go.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Random.insideUnitCircle * 0.3f;  // 初期分散
                _particles.Add(rb);
            }
        }
    }
}
