using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour, IServiceLocatorComponent, IAwake
{
    public ServiceLocator MyServiceLocator { get; set; }
    public bool Avaiable { get; set; } = true;
    public bool IsBack { get; set; } = false;
    public float TimeOfWork;
    public float InitialTravelTime;
    public RectTransform TargetPosition;
    public RectTransform EndPosition;
    public RectTransform StartPosition;

    [ServiceLocatorComponent] private ZoomMap _zoomMap;
    [ServiceLocatorComponent] private CarsManager _carsManager;

    public List<Transform> _path { get; set; } = null;
    public float _basePathDistance { get; set; } = 0;

    public void CustomAwake()
    {
        TargetPosition.IsNotNull(this, nameof(TargetPosition));
        StartPosition.IsNotNull(this, nameof(StartPosition));
        EndPosition.IsNotNull(this, nameof(EndPosition));
    }

    public void StartDriving(List<Transform> path)
    {
        _path = path;
        Avaiable = false;
        _basePathDistance = GetPathDistance(_path) / _zoomMap.CurrZoom;
        InitialTravelTime = _basePathDistance / _carsManager.Speed;
    }

    public void SetCarProgress(float progress)
    {
        float accumulatedDistance = 0;
        float pathDistance = _basePathDistance * _zoomMap.CurrZoom;

        for (int i = 0; i < _path.Count - 1; i++)
        {
            float currentWaypointDistance = Vector3.Distance(_path[i].position, _path[i + 1].position);
            accumulatedDistance += currentWaypointDistance;

            if (accumulatedDistance / pathDistance > progress)
            {
                float nextWaypointPercentage = currentWaypointDistance / pathDistance;
                float previousAccumulated = accumulatedDistance - currentWaypointDistance;
                float progressDelta = progress - (previousAccumulated / pathDistance);
                Vector3 offset = _path[i + 1].position - _path[i].position;
                transform.position = _path[i].position + offset * (progressDelta / nextWaypointPercentage);
                return;
            }
        }
    }


    private float GetPathDistance(List<Transform> path)
    {
        float distance = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            distance += Vector3.Distance(path[i].position, path[i + 1].position);
        }

        return distance;
    }
}
