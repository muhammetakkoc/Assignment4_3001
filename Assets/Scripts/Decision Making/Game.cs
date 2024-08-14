using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    Player player;

    [SerializeField]
    Enemy enemy;

    [SerializeField]
    GameObject shotgunPrefab;

    [SerializeField]
    GameObject sniperPrefab;

    Timer weaponSpawner = new Timer();

    float xMin, xMax, yMin, yMax;
    float shotgunRadius, sniperRadius;

    void Start()
    {
        // Consider decreasing this for testing!
        weaponSpawner.total = 2.5f;
        float size = Camera.main.orthographicSize;
        float aspect = Camera.main.aspect;
        xMin = -size * aspect; xMax = size * aspect;
        yMin = -size; yMax = size;

        shotgunRadius = shotgunPrefab.GetComponent<CircleCollider2D>().radius;
        sniperRadius = sniperPrefab.GetComponent<CircleCollider2D>().radius;
    }

    void Update()
    {
        // TODO -- destroy all weapon pickups once player and enemy are armed!
        bool canSpawn = !(player.Armed() && enemy.Armed());
        // Could improve by spawning the required weapon:
        //enemy.hasShotgun && enemy.hasSniper &&
        //player.hasShotgun && player.hasSniper);

        if (canSpawn)
        {
            weaponSpawner.Tick(Time.deltaTime);
            if (weaponSpawner.Expired())
            {
                weaponSpawner.Reset();

                WeaponType type = (WeaponType)Random.Range(1, 3);
                GameObject weaponPrefab = null;
                float radius = 0.0f;

                switch (type)
                {
                    case WeaponType.SHOTGUN:
                        weaponPrefab = shotgunPrefab;
                        radius = shotgunRadius;
                        break;
                    case WeaponType.SNIPER:
                        weaponPrefab = sniperPrefab;
                        radius = sniperRadius;
                        break;
                }

                Vector2 position = WeaponSpawnPosition(radius);
                GameObject weapon = Instantiate(weaponPrefab);
                weapon.transform.position = position;

                // Spawner test:
                //else
                //{
                //    Debug.Log("Connor can't write if-statements ;)");
                //}
                //Debug.Log(value);
            }
        }
    }

    Vector2 WeaponSpawnPosition(float radius)
    {
        float x = 0.0f;
        float y = 0.0f;

        for (int i = 0; i < 128; i++)
        {
            x = Random.Range(xMin, xMax);
            y = Random.Range(yMin, yMax);
            if (!Physics2D.OverlapCircle(new Vector2(x, y), radius))
                break;
        }

        return new Vector2(x, y);
    }
}
