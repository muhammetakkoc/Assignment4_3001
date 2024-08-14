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
    [SerializeField]
    Transform player;

    Health health;
    Rigidbody2D rb;

    [SerializeField]
    Transform[] waypoints;
    int waypoint = 0;

    WeaponType weaponType = 0;

    const float moveSpeed = 7.5f;
    const float turnSpeed = 1080.0f;
    const float viewDistance = 5.0f;

    [SerializeField]
    GameObject bulletPrefab;
    Timer shootCooldown = new Timer();
    Timer switchCooldown = new Timer();

    const float cooldownSniper = 0.75f;
    const float cooldownShotgun = 0.25f;

    bool hasShotgun = false;
    bool hasSniper = false;

    enum State
    {
        NEUTRAL,
        OFFENSIVE,
        DEFENSIVE
    };

    State statePrev, stateCurr;
    Color color;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        Respawn();
        switchCooldown.total = 2.5f;
    }

    void Update()
    {
        float rotation = Steering.RotateTowardsVelocity(rb, turnSpeed, Time.deltaTime);
        rb.MoveRotation(rotation);

        // Respawn enemy if health is below zero
        if (health.health <= 0.0f)
            Respawn();

        // Test defensive transition by reducing to 1/4th health!
        if (Input.GetKeyDown(KeyCode.T))
        {
            health.health *= 0.25f;
        }

        // Test weapon switching, consider removing this before submitting
        if (Input.GetKeyDown(KeyCode.Y))
        {
            int type = (int)weaponType;
            type++;
            type = type % 3;
            weaponType = (WeaponType)type;
            OnWeaponPickup(weaponType);
        }

        // State-selection
        if (stateCurr != State.DEFENSIVE)
        {
            // Don't allow state transitions if we're in the defensive state
            float playerDistance = Vector2.Distance(transform.position, player.position);
            stateCurr = playerDistance <= viewDistance ? State.OFFENSIVE : State.NEUTRAL;

            // Transition to defensive state if we're below 25% health
            if (health.health <= Health.maxHealth * 0.25f)
                stateCurr = State.DEFENSIVE;
        }

        // State-specific transition
        if (stateCurr != statePrev)
            OnTransition(stateCurr);

        // State-specific update
        switch (stateCurr)
        {
            case State.NEUTRAL:
                Patrol();
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

    void Attack()
    {
        // Seek player
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, player.position, moveSpeed);
        rb.AddForce(steeringForce);

        // LOS to player
        Vector3 playerDirection = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, viewDistance);
        bool playerHit = hit && hit.collider.CompareTag("Player");

        // Shoot player if in LOS
        if (playerHit)
        {
            Shoot();
        }
        else
        {
            // TODO -- seek visibility if no line-of-sight
        }

        // Switch weapon
        if (Armed())
        {
            switchCooldown.Tick(Time.deltaTime);
            if (switchCooldown.Expired())
            {
                switchCooldown.Reset();
                if (weaponType == WeaponType.SHOTGUN)
                {
                    weaponType = WeaponType.SNIPER;
                }
                else if (weaponType == WeaponType.SNIPER)
                {
                    weaponType = WeaponType.SHOTGUN;
                }
            }
        }
    }

    void Defend()
    {
        // Flee player
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Flee(rb, player.position, moveSpeed);
        rb.AddForce(steeringForce);

        // Shoot player
        Shoot();

        // TODO -- seek cover
    }

    void Shoot()
    {
        shootCooldown.Tick(Time.deltaTime);
        if (shootCooldown.Expired())
        {
            shootCooldown.Reset();
            switch (weaponType)
            {
                case WeaponType.SHOTGUN:
                    ShootShotgun();
                    break;

                case WeaponType.SNIPER:
                    ShootSniper();
                    break;
            }
        }
    }

    void Patrol()
    {
        // Increment waypoint if close enough
        float distance = Vector2.Distance(transform.position, waypoints[waypoint].transform.position);
        if (distance <= 2.5f)
        {
            waypoint++;
            waypoint %= waypoints.Length;
        }

        // Seek waypoint
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
        rb.AddForce(steeringForce);
    }

    void ShootShotgun()
    {
        // AB = B - A
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
    }

    void OnTransition(State state)
    {
        switch (state)
        {
            case State.NEUTRAL:
                color = Color.magenta;
                waypoint = Utilities.NearestPosition(transform.position, waypoints);
                break;

            case State.OFFENSIVE:
                color = Color.red;
                //shootCooldown.total = cooldownOffensive;
                break;

            case State.DEFENSIVE:
                color = Color.blue;
                //shootCooldown.total = cooldownDefensive;
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
                weaponType = WeaponType.SHOTGUN;
                OnWeaponPickup(weaponType);
            }
            if (collision.CompareTag("Sniper"))
            {
                hasSniper = true;
                weaponType = WeaponType.SNIPER;
                OnWeaponPickup(weaponType);
            }
        }
    }

    public bool Armed()
    {
        return hasShotgun && hasSniper;
    }
}
