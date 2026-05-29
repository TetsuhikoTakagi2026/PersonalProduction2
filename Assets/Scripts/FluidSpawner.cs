using UnityEngine;

public class FluidSpawner : MonoBehaviour
{
    [Header("生成する液体のプレハブ")]
    public GameObject liquidPrefab;

    [Header("生成する間隔（秒）")]
    public float spawnInterval = 0.1f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        // 設定した間隔ごとに粒を生成
        if (timer >= spawnInterval)
        {
            SpawnLiquid();
            timer = 0f;
        }
    }

    void SpawnLiquid()
    {
        // スクリプトが配置された位置（少し左右にランダムにばらつかせる）に粒を生成
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += Random.Range(-0.1f, 0.1f);

        Instantiate(liquidPrefab, spawnPosition, Quaternion.identity);
    }
}