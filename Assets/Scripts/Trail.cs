﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public class Trail : MonoBehaviour
{
    public float height = 2.0f;
    public float time = 2.0f;
    public float minDistance = 0.1f;
    public int filterWindowSize = 5;

    private List<TrailSection> sections = new List<TrailSection>();

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector2[] uv;
    int[] triangles;

    private Vector2 position;
    private Vector2 bitangent;
    private TrailSection currentSection;

    private TrailSection[] window;
    private bool pause;

    void Start()
    {
        window = new TrailSection[filterWindowSize];
    }

    void LateUpdate()
    {
        position = transform.position;
        bitangent = transform.right;

        int lastIndex = sections.Count - 1;

        if (!pause)
        {
            while (sections.Count > 0 && Time.time > sections[0].Time + time)
            {
                sections.RemoveAt(0);
            }

            lastIndex = sections.Count - 1;

            if (sections.Count == 0 || (position - sections[lastIndex].Position).sqrMagnitude > minDistance * minDistance)
            {
                TrailSection section = new TrailSection();

                section.Bitangent = transform.right;
                section.Position = position;
                section.Time = Time.time;
                sections.Add(section);
            }

            lastIndex = sections.Count - 1;
        }

        mesh = GetComponent<MeshFilter>().mesh;
        if (sections.Count > 2)
        {

            vertices = new Vector3[sections.Count * 2 + 2];
            colors = new Color[sections.Count * 2 + 2];
            uv = new Vector2[sections.Count * 2 + 2];

            currentSection = sections[0];

            Color interpolatedColor;
            float u;

            Vector2 smoothPosition = sections[lastIndex].Position;
            Vector2 smoothBitangent = sections[lastIndex].Bitangent;

            float smoothingLerp = 0.9f;

            Matrix4x4 localSpaceTransform = transform.worldToLocalMatrix;

            for (int i = 0; i < window.Length; i++)
            {
                window[i] = sections[lastIndex];
            }

            TrailSection smoothedSection = new TrailSection();

            float inverseLength = 1.0f / window.Length;

            for (int i = sections.Count - 1; i >= 0; i--)
            {
                currentSection = sections[i];

                for (int j = 1; j < window.Length; j++)
                {
                    TrailSection nextSection = window[j];

                    window[j - 1] = window[j];
                    smoothedSection.Position += nextSection.Position;
                    smoothedSection.Bitangent += nextSection.Bitangent;
                }

                smoothedSection.Position += currentSection.Position;
                smoothedSection.Bitangent += currentSection.Bitangent;

                window[window.Length - 1] = currentSection;

                smoothedSection.Position *= inverseLength;
                smoothedSection.Bitangent *= inverseLength;

                smoothPosition = Vector2.Lerp(currentSection.Position, smoothPosition, smoothingLerp);
                smoothBitangent = Vector2.Lerp(currentSection.Bitangent, smoothBitangent, smoothingLerp).normalized;


                vertices[i * 2 + 0] = localSpaceTransform.MultiplyPoint(smoothPosition + smoothBitangent * (height / 2f));
                vertices[i * 2 + 1] = localSpaceTransform.MultiplyPoint(smoothPosition - smoothBitangent * (height / 2f));

                u = i > 0 ? (sections.Count - i) / (float)sections.Count : 1.0f;
                uv[i * 2 + 0] = new Vector2(u, 0);
                uv[i * 2 + 1] = new Vector2(u, 1);

                interpolatedColor = Color.Lerp(Color.white, new Color(1, 1, 1, 0), u);
                colors[i * 2 + 0] = interpolatedColor;
                colors[i * 2 + 1] = interpolatedColor;
            }

            vertices[vertices.Length - 2] = localSpaceTransform.MultiplyPoint(position + bitangent * (height / 2f));
            vertices[vertices.Length - 1] = localSpaceTransform.MultiplyPoint(position - bitangent * (height / 2f));

            uv[uv.Length - 2] = new Vector2(0, 0);
            uv[uv.Length - 1] = new Vector2(0, 1);

            colors[colors.Length - 2] = Color.white;
            colors[colors.Length - 1] = Color.white;

            triangles = new int[sections.Count * 2 * 3];

            for (int i = 0; i < triangles.Length / 6; i++)
            {
                triangles[i * 6 + 0] = i * 2;
                triangles[i * 6 + 1] = i * 2 + 1;
                triangles[i * 6 + 2] = i * 2 + 2;

                triangles[i * 6 + 3] = i * 2 + 2;
                triangles[i * 6 + 4] = i * 2 + 1;
                triangles[i * 6 + 5] = i * 2 + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }
    }

    public void Reset()
    {
        sections.Clear();
        mesh.Clear();
    }

    private class TrailSection
    {
        public Vector2 Position;
        public Vector2 Bitangent;
        public float Time;
    }

    public void Pause()
    {
        pause = true;
    }

    public void UnPause()
    {
        pause = false;
    }

}
