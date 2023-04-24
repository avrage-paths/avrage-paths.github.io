using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;

public class ConfettiController : MonoBehaviour
{
    public ParticleSystem[] confetti;
    public AudioSource confettiNoise;

    [EasyButtons.Button]
    public void StartConfetti()
    {
        foreach (ParticleSystem ps in confetti)
        {
            ps.Stop();
            ps.Play();
        }
        confettiNoise.Play();
    }
}
