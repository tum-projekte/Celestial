﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ChordType
{
    GrabNew,
    GrabOld,
    Die,
    Revive
}

public class SoundSystem : MonoBehaviour
{
    public static SoundSystem Instance { get; protected set; }

    public float BPM = 85;
    public AudioSource BackgroundMusic;
    public AudioClip Chord;

    private Dictionary<AudioSource, IEnumerator> PlayTime;

    void Start()
    {
        Instance = this;
        PlayTime = new Dictionary<AudioSource, IEnumerator>();
    }

    public void PlayChord(AudioSource source, ChordType type)
    {
        source.Stop();
        source.clip = Chord;
        var currentBeat = BackgroundMusic.time.Sec2Beat(BPM);
        var chord = Mathf.FloorToInt(currentBeat / 8) % 4;
        var time = chord * (Utils.Beat2Sec(4.0f * 4, BPM)) + (int)type * (Utils.Beat2Sec(4 * 4 * 4, BPM));
        if (type == ChordType.GrabOld || type == ChordType.GrabNew)
        {
            time += Random.Range(0, 4)*Utils.Beat2Sec(4.0f, BPM);
        }
        source.time = time;
        source.Play();

        if (PlayTime.ContainsKey(source))
        {
            StopCoroutine(PlayTime[source]);
        }
        var tmp = StopAfterTime(3.8f.Beat2Sec(BPM), source);
        PlayTime[source] = tmp;
        StartCoroutine(tmp);
    }

    IEnumerator StopAfterTime(float time, AudioSource audio)
    {
        var startTime = audio.time;
        yield return new WaitForSeconds(time);
        if (startTime <= audio.time && audio.time <= startTime + time * 1.1f)
        {
            audio.Pause();
        }
    }
}
