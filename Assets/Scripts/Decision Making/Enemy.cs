using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType : int
{
    NONE,
    SHOTGUN,
    SNIPER
}

public class Enemy : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform[] waypoints;
    [SerializeField] GameObject bulletPrefab;

    Health health;
    Rigidbody2D rb;

    WeaponType weaponType = WeaponType.NONE;
    Timer shootCooldown = new Timer();
    Timer switchCooldown = new Timer();
    Timer visibilityTimer = new Timer();

    const float moveSpeed = 7.5f;
    const float turnSpeed = 1080.0f;
    const float viewDistance = 5.0f;
    const float visibilityCheckTime = 1.0f;
    const float cooldownSniper = 0.75f;
    const float cooldownShotgun = 0.25f;

    bool hasShotgun;
    bool hasSniper;

    int waypoint = 0;
    State statePrev, stateCurr;
    Color color;

    enum State
    {
        NEUTRAL,
        OFFENSIVE,
        DEFENSIVE
    };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        Respawn();
        switchCooldown.total = 5.0f; 
        visibilityTimer.total = visibilityCheckTime;
    }

    void Update()
    {
        
        float rotation = Steering.RotateTowardsVelocity(rb, turnSpeed, Time.deltaTime);
        rb.MoveRotation(rotation);

        
        if (health.health <= 0.0f) Respawn();

        
        if (stateCurr != State.DEFENSIVE)
        {
            float playerDistance = Vector2.Distance(transform.position, player.position);
            stateCurr = playerDistance <= viewDistance ? State.OFFENSIVE : State.NEUTRAL;

            if (health.health <= Health.maxHealth * 0.25f || !Armed())
                stateCurr = State.DEFENSIVE;
        }

        
        if (stateCurr != statePrev) OnTransition(stateCurr);

        
        switch (stateCurr)
        {
            case State.NEUTRAL:
                NeutralBehavior();
                break;

            case State.OFFENSIVE:
                Attack();
                break;

            case State.DEFENSIVE:
                Defend();
                break;
        }

        statePrev = stateCurr;
        Debug.DrawLine(transform.position, transform.position + transform.right * viewDistance, color);
    }

    void NeutralBehavior()
    {
        visibilityTimer.Tick(Time.deltaTime);

        
        if (visibilityTimer.Expired() && !CanSeePlayer())
        {
            stateCurr = State.OFFENSIVE;
            SeekVisibility();
        }
        else
        {
            Patrol();
        }
    }

    void Attack()
    {
        Vector3 steeringForce = Steering.Seek(rb, player.position, moveSpeed);
        rb.AddForce(steeringForce);

        if (CanSeePlayer())
        {
            Shoot();
        }
        else
        {
            SeekVisibility();
        }

        if (Armed())
        {
            switchCooldown.Tick(Time.deltaTime);
            if (switchCooldown.Expired())
            {
                switchCooldown.Reset();
                weaponType = (weaponType == WeaponType.SHOTGUN) ? WeaponType.SNIPER : WeaponType.SHOTGUN;
            }
        }
    }

    void Defend()
    {
        Vector3 steeringForce = Steering.Flee(rb, player.position, moveSpeed);
        rb.AddForce(steeringForce);

        if (Vector2.Distance(transform.position, player.position) > viewDistance)
        {
            SeekCover();
        }

        
        if (Armed())
        {
            Shoot();
        }
    }

    void Patrol()
    {
        float distance = Vector2.Distance(transform.position, waypoints[waypoint].transform.position);
        if (distance <= 2.5f)
        {
            waypoint++;
            waypoint %= waypoints.Length;
        }

        Vector3 steeringForce = Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
        rb.AddForce(steeringForce);
    }

    void SeekVisibility()
    {
        int visibilityPoint = NearestVisibleWaypoint();
        if (visibilityPoint >= 0)
        {
            Vector3 steeringForce = Steering.Seek(rb, waypoints[visibilityPoint].position, moveSpeed);
            rb.AddForce(steeringForce);
            Debug.Log("Visibility Seek");
        }
    }

    void SeekCover()
    {
        int coverPoint = FurthestCoverPoint();
        if (coverPoint >= 0)
        {
            Vector3 steeringForce = Steering.Seek(rb, waypoints[coverPoint].position, moveSpeed);
            rb.AddForce(steeringForce);
            Debug.Log("Seek Cover");
        }
    }
    void Shoot()
    {
        shootCooldown.Tick(Time.deltaTime);
        if (shootCooldown.Expired())
        {
            shootCooldown.Reset();
            if (Armed())
            {
                float playerDistance = Vector2.Distance(transform.position, player.position);
                if (hasShotgun && playerDistance <= 5f)
                {
                    ShootShotgun();
                }
                else if (hasSniper && playerDistance > 5f && hasSniper && playerDistance <= 10f)
                {
                    ShootSniper();
                }
               
            }
        }
    }
    

    void ShootShotgun()
    {
        Vector3 forward = (player.position - transform.position).normalized;
        Vector3 left = Quaternion.Euler(0.0f, 0.0f, 30.0f) * forward;
        Vector3 right = Quaternion.Euler(0.0f, 0.0f, -30.0f) * forward;

        float speed = 10.0f;
        float damage = 20.0f;
        float duration = 1.0f;
        Utilities.CreateBullet(bulletPrefab, transform.position, forward, speed, damage, UnitType.ENEMY, duration);
        Utilities.CreateBullet(bulletPrefab, transform.position, left, speed, damage, UnitType.ENEMY, duration);
        Utilities.CreateBullet(bulletPrefab, transform.position, right, speed, damage, UnitType.ENEMY, duration);
    }

    void ShootSniper()
    {
        Vector3 forward = (player.position - transform.position).normalized;
        Utilities.CreateBullet(bulletPrefab, transform.position, forward, 20.0f, 50.0f, UnitType.ENEMY);
        Debug.Log("Sniper shooted");
    }

    void OnTransition(State state)
    {
        switch (state)
        {
            case State.NEUTRAL:
                color = Color.magenta;
                waypoint = NearestPosition(transform.position, waypoints);
                visibilityTimer.Reset();
                break;

            case State.OFFENSIVE:
                color = Color.red;
                break;

            case State.DEFENSIVE:
                color = Color.blue;
                break;
        }
        GetComponent<SpriteRenderer>().color = color;
    }

    void Respawn()
    {
        statePrev = stateCurr = State.NEUTRAL;
        OnTransition(stateCurr);
        health.health = Health.maxHealth;
        transform.position = new Vector3(0.0f, 3.0f);
    }

    void OnWeaponPickup(WeaponType type)
    {
        weaponType = type;
        switch (type)
        {
            case WeaponType.SHOTGUN:
                shootCooldown.total = cooldownShotgun;
                break;

                
            case WeaponType.SNIPER:
                shootCooldown.total = cooldownSniper;
                break;
                
        }
        

    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!Armed())
        {
            if (collision.CompareTag("Shotgun"))
            {
                hasShotgun = true;
                OnWeaponPickup(WeaponType.SHOTGUN);
                Debug.Log("Shotgun");
            }
            else if (collision.CompareTag("Sniper"))
            {
                hasSniper = true;
                OnWeaponPickup(WeaponType.SNIPER);
                Debug.Log("Sniper");
            }
        }
        else
        {
            Debug.Log("None");
        }
    }

    public bool Armed()
    {
        return hasShotgun && hasSniper;
    }

    bool CanSeePlayer()
    {
        Vector3 playerDirection = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, viewDistance);
        return hit && hit.collider.CompareTag("Player");
    }

    int NearestVisibleWaypoint()
    {
        int nearest = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (HasLineOfSight(waypoints[i].position))
            {
                float distance = Vector2.Distance(transform.position, waypoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = i;
                }
            }
        }

        return nearest;
    }


    int FurthestCoverPoint()
    {
            float maxDistance = 0.0f;
            int furthestPoint = -1;

            foreach (Transform waypoint in waypoints)
            {
                float distance = Vector2.Distance(waypoint.position, player.position);
                if (distance > maxDistance && !HasLineOfSight(waypoint.position))
                {
                    maxDistance = distance;
                    furthestPoint = Array.IndexOf(waypoints, waypoint);
                }
            }

            return furthestPoint;
     }

        bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Vector2.Distance(transform.position, targetPosition));
            return hit.collider == null || hit.collider.CompareTag("Player");
        }

        int NearestPosition(Vector3 position, Transform[] points)
        {
            int nearest = -1;
            float minDistance = float.MaxValue;

            for (int i = 0; i < points.Length; i++)
            {
                float distance = Vector2.Distance(position, points[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }
}

