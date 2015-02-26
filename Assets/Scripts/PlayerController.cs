﻿using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    const float maximal_grapple_distance = 20;

    public GameObject Explosion;

    public Background background;
    public KeyCode TheOneButton = KeyCode.Space;
    public Player Player = Player.Player1;
    private bool rotating = true;
    public float speed = 20.0f;
    public float radius = 5.0f;

    public SpriteRenderer PlayerSprite;
    public float DeathCoolDown = 5;

    private Planet[] planets;

    private Planet grappledPlanet;
    private Planet previous;

    private float distance;
    private bool clockwise;
    float lastDistance;

    private bool isInOrbit;

    public Vector3 velocity;
    private Trail trail;
    private LineRenderer lineRenderer;

    private Vector3 startVelocity;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private bool Paused;
    public float deathTimer { get; protected set; }

    public event System.Action OnUnDie;

    // Use this for initialization
    void Start()
    {
        int planetCount = background.planets.Count;
        if (background && planetCount > 0)
        {
            planets = new Planet[planetCount];
            int count = 0;
            foreach(var p in background.planets)
            {
                planets[count] = p;
                count++;
            }
        }
        else
        {
            planets = FindObjectsOfType<Transform>().
                Where(a => a.gameObject.layer == LayerMask.NameToLayer("Planet")).
                Select(a => a.GetComponent<Planet>()).
                ToArray();
        }

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.material.color = Player.GetColor();
        
        var trailController = GetComponentInChildren<TrailMaterialControler>();
        trailController.Color = Player.GetColor();
        trail = GetComponentInChildren<Trail>();

        PlayerSprite.color = Player.GetColor();

        startVelocity = velocity;
        startPosition = transform.position;
        startRotation = transform.rotation;
        Paused = false;
    }

    // Update is called once per frame
    void Update()
    {
        deathTimer = Mathf.Max(-1, deathTimer - Time.deltaTime);
        if (deathTimer < 0 && deathTimer + Time.deltaTime > 0)
        {
            if (OnUnDie != null)
            {
                OnUnDie();
            }
        }
        if (Paused || deathTimer > 0) return;
        if (Input.GetKey(TheOneButton) && grappledPlanet == null)
        {
            var planet = GetNearestPlanet();

            if (planet != null)
            {
                grappledPlanet = planet;
                lastDistance = float.MaxValue;
            }

            rotating = false;
        }
        else if (Input.GetKeyUp(TheOneButton) && grappledPlanet != null)
        {
            ReleaseGrapple();
        }

        if (rotating)
        {
            RotateAroundCenter();
        }
    }

    public void UnDie()
    {
        UnPause();
        trail.Reset();
        rotating = true;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }
    }

    public bool GetRotating()
    {
        return rotating;
    }

    private void RotateAroundCenter()
    {
        Vector3 pos = transform.position;
        Vector3 tangent = Vector3.Cross(new Vector3(0.0f, 0.0f, -1.0f), pos);
        velocity = tangent.normalized * speed;

        transform.position = Vector3.Lerp(pos, pos.normalized * radius, Time.deltaTime * 10);
    }

    Planet GetNearestPlanet()
    {

        var possiblePlanets = (from p in planets
                                     let dist = Vector3.Distance(p.transform.position, transform.position)
                                     where dist < maximal_grapple_distance
                                     orderby dist
                                     select p).ToArray();

        Planet result = null;

        var position = transform.position + velocity * 0.125f;
        var minimalDistance = float.MaxValue;
        var flag = false;

        for (int i = 0; i < possiblePlanets.Length; i++)
        {
            var planet = possiblePlanets[i];
            var planetSize = 0;

            var distanceToPlanet = Vector3.Distance(planet.transform.position, position) - planetSize;

            if (planet == previous)
            {
                if (distanceToPlanet > minimalDistance - 2.5f)
                {
                    continue;
                }
            }
            else
            {
                if (flag && distanceToPlanet > minimalDistance + 2.5f)
                {
                    continue;
                }
                if (distanceToPlanet > minimalDistance)
                {
                    continue;
                }
            }

            if (Vector3.Distance(position + velocity.normalized * distanceToPlanet, planet.transform.position) > 1 + planetSize)
            {
                flag = previous == planet;
                result = planet;
                minimalDistance = distance;
            }

        }

        return result;
        
    }

    float GetAngle(Transform planet)
    {
        var up = transform.up.normalized;

        var connection = (transform.position - planet.position).normalized;


        var angle = Mathf.Atan2(-connection.y, -connection.x) - Mathf.Atan2(up.y, up.x);

        if (Mathf.Abs(angle) > Mathf.PI)
        {
            return -angle - Mathf.PI;
        }

        return angle;
    }

    void FixedUpdate()
    {

//        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, transform.position + Vector3.back * 15 + Vector3.down * 2, Time.deltaTime * 10);
        if (Paused)
        {
            return;
        }

        if (grappledPlanet == null || !isInOrbit)
        {
            rigidbody2D.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
            lineRenderer.enabled = false;
        }
        if (grappledPlanet != null)
        {
            if (isInOrbit)
            {
                Debug.DrawLine(transform.position, grappledPlanet.transform.position, Color.green);

                var connection = (transform.position - grappledPlanet.transform.position).normalized;
                var angle = Mathf.Atan2(connection.y, connection.x) / Mathf.PI * 180;

                rigidbody2D.MovePosition(grappledPlanet.transform.position + connection * distance + velocity.magnitude * transform.up * Time.fixedDeltaTime);
                rigidbody2D.MoveRotation(angle + (clockwise ? 180 : 0));
            }
            else
            {
                var distanceToPlanet = Vector3.Distance(transform.position, grappledPlanet.transform.position);

                if (distanceToPlanet > maximal_grapple_distance)
                {
                    grappledPlanet = null;
                }
                else
                {
                    Debug.DrawLine(transform.position, grappledPlanet.transform.position, Color.red);

                    if (lastDistance < distanceToPlanet)
                    {
                        GrappleOnPlanet();
                    }
                    lastDistance = distanceToPlanet;
                }
            }

            lineRenderer.enabled = grappledPlanet != null;

            if (grappledPlanet != null)
            {
                lineRenderer.SetPosition(0, transform.position + transform.up * 0.5f);
                lineRenderer.SetPosition(1, grappledPlanet.OuterPlanet.position);
            }
        }
    }

    private void GrappleOnPlanet()
    {
        isInOrbit = true;
        distance = Vector3.Distance(transform.position, grappledPlanet.transform.position);
        clockwise = GetAngle(grappledPlanet.transform) < 0;
        lastDistance = float.MaxValue;
        background.ChangeOwner(grappledPlanet, Player);
        grappledPlanet.Grapple(this);
    }

    private void ReleaseGrapple()
    {
        var planet = grappledPlanet.GetComponent<Planet>();
        if (planet != null)
        {
            planet.ReleaseGrapple(this);
        }
        velocity = velocity.magnitude * transform.up;
        previous = grappledPlanet;
        grappledPlanet = null;
        isInOrbit = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Planet"))
        {
            Die();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Border") && grappledPlanet == null)
        {
            Die();
        }
        else if (other.gameObject.tag == "Player" && !rotating)
        {
            var pc = other.GetComponent<PlayerController>();
            if (!pc.rotating)
            {
                Die();
                pc.Die();
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Border") && grappledPlanet == null)
        {
            Die();
        }
    }

    void Die()
    {
        if (rotating || deathTimer > 0) return;
        var x = Instantiate(Explosion, transform.position, Quaternion.identity) as GameObject;
        var ps = x.GetComponent<Explosion>();
        ps.Player = this;

        transform.position = startPosition;
        transform.rotation = startRotation;
        velocity = startVelocity;

        isInOrbit = false;
        grappledPlanet = null;

        trail.Reset();
        Pause();

        deathTimer = DeathCoolDown;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }
    }

    public void Pause()
    {
        Paused = true;
        trail.Pause();
    }

    public void UnPause()
    {
        Paused = false;
        trail.UnPause();
    }
}
