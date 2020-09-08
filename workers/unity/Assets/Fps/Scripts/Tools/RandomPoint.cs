using UnityEngine;

public class RandomPoint
{
    public float baseRadius = 150.0f;
    public float mapScale = 0;
    public Vector3 workerPosition;

    static RandomPoint _instance;
    public static RandomPoint Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new RandomPoint();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    public Vector3 RandomNavmeshLocation()
    {
        float radius = baseRadius * mapScale - 10;
        Vector3 randomDirection = new Vector3(Random.Range(-radius, radius), 0, Random.Range(-radius, radius));
        return randomDirection;
    }
}
