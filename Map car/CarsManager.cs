using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class CarsManager : MonoBehaviour, IManager, IServiceLocatorComponent, IAwake, IStartable, IMapTaskReceiver
{
    public ServiceLocator MyServiceLocator { get; set; }
    public event Action OnCarAvaiable;
    public event Action OnCarNotAvaiable;
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public int NumOfCars { get; private set; }

    [Header("Pathfinding settings")]
    [SerializeField] private List<Car> _cars;
    [SerializeField] private List<Node> _nodes;

    [Header("Car Images")]
    [SerializeField] private Image[] _carIcons;

    [SerializeField] private AdoptionVanController _adoptionVan;

    [SerializeField] private MapPinBlocker _notEnoughCarsBlocker;

    public void CustomAwake() { }

    public void CustomReset() { }

    public void CustomStart()
    {
        UpdateCarImageVisibility();
        if (NumOfCars >= 1)
            OnCarAvaiable?.Invoke();
        else
            OnCarNotAvaiable?.Invoke();
    }

    public void GetTablet(TabletUI tabletUI)
    {
        var map = tabletUI.MapTabletUISpawned.WorldMap;
        _cars = map.Cars;
        _carIcons = map.CarsImages.ToArray();
        _nodes = map.Nodes;
    }

    public Car TryGetCar()
    {
        Car car = GetFreeCar();
        if (car == null)
            return null;

        if (NumOfCars < 0 && !car.Avaiable)
            return null;

        return car;
    }

    public bool TrySetupCar(Car car, Vector3 target)
    {
        if (car.IsBack) return false;
        car.TargetPosition.position = target;

        List<Transform> path = FindPathWithClosestPoint(car.StartPosition.position, target, _nodes, car);
        if (path == null)
        {
            Debug.Log("Path not found");
            return false;
        }

        OnCarSent(car);
        car.StartDriving(path);

        return true;
    }

    public bool TryComeBack(Car car, Vector3 target)
    {
        if (!car.IsBack) return false;
        car.TargetPosition.position = target;

        List<Transform> path = FindPathWithClosestPoint(car.EndPosition.position, target, _nodes, car);
        if (path == null)
        {
            Debug.Log("Path not found");
            return false;
        }

        car.StartDriving(path);

        return true;
    }

    private Car GetFreeCar()
    {
        foreach (Car car in _cars)
        {
            if (!car.Avaiable) continue;

            return car;
        }

        Debug.Log("There is no free car");
        return null;
    }

    private void UpdateCarImageVisibility()
    {
        for (int i = 0; i < _carIcons.Length; i++)
            _carIcons[i].enabled = i < NumOfCars;
    }

    private void OnCarSent(Car car)
    {
        car.gameObject.SetActive(true);
        NumOfCars--;
        UpdateCarImageVisibility();
        if (NumOfCars < 1) OnCarNotAvaiable?.Invoke();
    }

    private void OnCareComeBack(Car car)
    {
        car.gameObject.SetActive(false);
        NumOfCars++;
        car.IsBack = false;
        UpdateCarImageVisibility();
        if (NumOfCars >= 1) OnCarAvaiable?.Invoke();
    }

    public void ResetCar(Car car)
    {
        car.TimeOfWork = 3f;
        car.Avaiable = true;
        car.InitialTravelTime = 0f;
        OnCareComeBack(car);
    }

    private List<Transform> FindPathWithClosestPoint(Vector3 start, Vector3 target, List<Node> nodes, Car car)
    {
        List<Node> nodePath = AStar.FindPath(start, target, nodes);

        if (nodePath == null) return null;

        if (nodePath.Count <= 1) return nodePath.ConvertAll(node => node.transform);

        Node lastNode = nodePath[nodePath.Count - 1].GetComponent<Node>();
        Vector3 currentClosest = lastNode.transform.position;
        float minimumMagnitude = float.MaxValue;
        Transform currentBestPoint = null;

        foreach (var neighbour in lastNode.Neighbors)
        {
            Vector3 neighbourClosestPoint = AStar.GetClosestPointOnFiniteLine(target, lastNode.transform.position, neighbour.transform.position);
            float currentMagnitude = Vector3.Magnitude(target - neighbourClosestPoint);

            if (currentMagnitude < minimumMagnitude)
            {
                currentClosest = neighbourClosestPoint;
                minimumMagnitude = currentMagnitude;
                currentBestPoint = neighbour.transform;
            }
        }

        car.TargetPosition.position = currentClosest;

        List<Transform> transformsPath = nodePath.ConvertAll(node => node.transform);

        if (currentBestPoint == null) return transformsPath;

        if (currentBestPoint == nodePath[nodePath.Count - 2].transform)
        {
            transformsPath.RemoveAt(transformsPath.Count - 1);
        }

        transformsPath.Add(car.TargetPosition);

        return transformsPath;
    }

    public void ReceiveMapTask(MapTask mapTask)
    {
        _adoptionVan.MapTaskReceived();
    }

    public MapPinBlocker CanReceiveTask(MapTask mapTask)
    {
        return NumOfCars > 0? null: _notEnoughCarsBlocker;
    }

    public void InformAboutIncommingTask(MapTask mapTask)
    {
        _adoptionVan.SetMapTask(mapTask);
    }
}
