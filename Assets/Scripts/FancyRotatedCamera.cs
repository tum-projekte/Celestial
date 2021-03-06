﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FancyRotatedCamera : MonoBehaviour
{

    private PlayerController[] player;
    public float minZoom = 10;
    public float lerpSpeed = 10;
    public AnimationCurve Angle;

    private Vector3 lastPosition;

    void Start()
    {
        player = GameObject.FindGameObjectsWithTag("Player").Select(p => p.GetComponent<PlayerController>()).ToArray();
        lastPosition = new Vector3(0, 0, -42);
    }

	void Update ()
	{
	    var pos = player.Select(pc => new { pc.transform.position, 
                                pc.transform.up, 
                                pc.transform, pc,
                                grappledPlanet = pc.grappledPlanet });
        
        int count = 0;

        List<Vector3> validPositions = new List<Vector3>();
        foreach(var p in pos)
        {
            if (player[count].IsActive)
            {
                var poz = p.position;
                if(p.pc.isInOrbit && p.grappledPlanet != null) {
                    validPositions.Add(p.grappledPlanet.transform.position -
                        p.pc.distance * p.grappledPlanet.transform.position.normalized);

                    poz = p.grappledPlanet.transform.position +
                        p.pc.distance * p.grappledPlanet.transform.position.normalized;
                } else if(!p.pc.IsDead) {
                    poz = Vector3.zero;
                }
                validPositions.Add(poz);
            }
            count++;
        }

	    if (validPositions.Count == 0)
	    {
	        return;
	    }

        Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity);
        Vector3 max = new Vector3(-Mathf.Infinity, -Mathf.Infinity);
        foreach (var p in validPositions)
        {
            if (p.x < min.x)
                min.x = p.x;
            if (p.x > max.x)
                max.x = p.x;

            if (p.y < min.y)
                min.y = p.y;
            if (p.y > max.y)
                max.y = p.y;
        }
        
        Vector3 center = new Vector3(0.0f, 0.0f, 0.0f);
        if(validPositions.Count != 0)
	        center = (max - min)*0.5f + min;

	    Vector2 size = max - min;
	    size.y *= GetComponent<Camera>().aspect;

	    var maxSide = Mathf.Max(minZoom, Mathf.Max(size.x, size.y));

	    var distance = Mathf.Sqrt(maxSide)*4.2f + 0.24f*maxSide + 5;
	    var targetPosition = center + Vector3.back*distance;

        lastPosition = Vector3.Lerp(lastPosition, targetPosition, Time.deltaTime * lerpSpeed);
	    transform.position = lastPosition;

        transform.rotation = Quaternion.identity;
        transform.RotateAround(center, Vector3.right, -20);
	}
}
