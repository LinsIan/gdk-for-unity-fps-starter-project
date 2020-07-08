using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomPoint : MonoBehaviour
{
    public float baseRadius = 150.0f;
    public float mapScale = 1;

    public Vector3 mapPosition;
    public Vector3 workerPosition;

    static RandomPoint _instance;
    public static RandomPoint Instance
    {
        get
        {
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    public Vector3 RandomNavmeshLocation()
    {
        float radius = baseRadius * mapScale;
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }

        finalPosition.y -= transform.position.y;
        return finalPosition;
    }
}
