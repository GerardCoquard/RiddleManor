﻿using UnityEngine;

public class LightEmiter : MonoBehaviour
{
    LightBeam beam;
    public Transform rayStartPos;
    public float width;
    public float growthSpeed;
    public Material material;
    public int maxBounces;
    public RayColor rayColor = RayColor.Anyone;
    public bool active = true;

    private void Start()
    {
        beam = new LightBeam(transform.position, transform.forward, material, Physics.AllLayers,maxBounces,rayColor,width, transform,growthSpeed);
    }

    private void Update()
    {
        if(active) beam.ExecuteRay(rayStartPos.position, rayStartPos.forward, beam.lineRenderer);
        else
        {
            beam.currentLength = 0;
            beam.lineRenderer.positionCount = 0;
        }
    }
    public void SetPower(bool state)
    {
        active = state;
    }

}