using System;
using System.Collections.Generic;
public class MapTask : IServiceLocatorComponent, ISaveable<TaskSaveInfo>
{
    public ServiceLocator MyServiceLocator { get; set; }

    public event Action OnMapTaskDestroyed;
    public event Action<MapTaskStatus> OnStateSwitched;
    public event Action OnCarComeBack;
    public event Action<MapTask> OnSpecialMapTaskCreated;
    public static event Action<MapTask> OnSpecialMapTaskCreatedStatic;

    public MapPart MapPart;

    public bool IsSpecial { get; private set; } = false;
    public MapTaskStatus MapTaskStatus { get; private set; }
    public PetSaveInfo AnimalInTask { get; private set; }
    public List<MapTaskType> MapTaskType { get; private set; } = new();
    public List<IMapTaskReceiver> MapTaskReceivers { get; private set; } = new();

    public float QuestDuration { get; private set; }
    public float BaseTaskDuration { get; private set; }
    public float CarRemainingTravelTime { get; private set; }
    public float CarRemainingComeBackTime { get; private set; }
    public float CarInitialTravelTime { get; private set; }
    public float BaseCarTimeOfWork { get; private set; }
    public float CarRemainingWorkTime { get; private set; }
    public int ExperienceGiven { get; private set; }
    public int MoneyGained { get; private set; }

    public void OnCarSent(Car car, bool loading = false)
    {
        CarInitialTravelTime = car.InitialTravelTime;
        BaseCarTimeOfWork = car.TimeOfWork;
        if (loading) return;
        CarRemainingTravelTime = CarInitialTravelTime;
        CarRemainingComeBackTime = 0;
        CarRemainingWorkTime = BaseCarTimeOfWork;
        SwitchState(MapTaskStatus.CarDriving);
        InformReceivers();
    }

    public TaskSaveInfo CollectData(TaskSaveInfo data)
    {
        data.Duration = QuestDuration;
        data.BaseDuration = BaseTaskDuration;
        data.GivenExperience = ExperienceGiven;
        data.MapPart = MapPart;

        data.TaskType.AddRange(MapTaskType);
        data.MoneyGained = MoneyGained;
        data.MapTaskStatus = MapTaskStatus;
        data.TimeOfWork = CarRemainingWorkTime;
        data.InitialTravelTime = CarInitialTravelTime;
        data.CarRemainingTravelTime = CarRemainingTravelTime;
        data.CarRemainingComeBackTime = CarRemainingComeBackTime;
        data.AnimalInTask = AnimalInTask;
        return data;
    }

    public void Initialize(TaskSaveInfo save)
    {
        QuestDuration = save.Duration;
        BaseTaskDuration = save.BaseDuration;
        if (BaseTaskDuration <= 0)
        {
            MarkSpecial();
        }
        ExperienceGiven = save.GivenExperience;
        MapPart = save.MapPart;

        MapTaskType.AddRange(save.TaskType);
        MoneyGained = save.MoneyGained;
        CarRemainingWorkTime = save.TimeOfWork;
        CarInitialTravelTime = save.InitialTravelTime;
        CarRemainingTravelTime = save.CarRemainingTravelTime;
        CarRemainingComeBackTime = save.CarRemainingComeBackTime;
        AnimalInTask = save.AnimalInTask;
        SwitchState(save.MapTaskStatus);
    }

    public void OnTaskLoaded()
    {
        if (MapTaskStatus == MapTaskStatus.Idle) return;
        InformReceivers();
    }

    private void InformReceivers()
    {
        foreach (var mapTaskReceiver in MapTaskReceivers)
        {
            mapTaskReceiver.InformAboutIncommingTask(this);
        }
    }

    public void UpdateMapTask(float deltaTime, TaskManager taskManager)
    {
        switch (MapTaskStatus)
        {
            case MapTaskStatus.Idle:
                IdleUpdate(deltaTime);
                break;
            case MapTaskStatus.CarDriving:
                CarTravelUpdate(deltaTime);
                break;
            case MapTaskStatus.Working:
                CarWorkingUpdate(deltaTime);
                break;
            case MapTaskStatus.CarComingBack:
                CarComingBackUpdate(deltaTime, taskManager);
                break;
        }
    }

    private void SwitchState(MapTaskStatus newStatus)
    {
        MapTaskStatus = newStatus;
        OnStateSwitched?.Invoke(newStatus);
    }

    private void IdleUpdate(float deltaTime)
    {
        if (IsSpecial) return;

        QuestDuration -= deltaTime;
        if (QuestDuration <= 0)
        {
            OnMapTaskDestroyed?.Invoke();
        }
    }

    private void CarTravelUpdate(float deltaTime)
    {
        CarRemainingTravelTime -= deltaTime;
        if (CarRemainingTravelTime <= 0)
        {
            SwitchState(MapTaskStatus.Working);
        }
    }

    private void CarWorkingUpdate(float deltaTime)
    {
        CarRemainingWorkTime -= deltaTime;
        if (CarRemainingWorkTime <= 0)
            SwitchState(MapTaskStatus.CarComingBack);

    }

    private void CarComingBackUpdate(float deltaTime, TaskManager taskManager)
    {
        CarRemainingComeBackTime += deltaTime;
        if (CarRemainingComeBackTime >= CarInitialTravelTime)
        {
            OnCarComeBack?.Invoke();
            TakeInQuest(taskManager);
        }
    }

    private void TakeInQuest(TaskManager taskManager)
    {
        taskManager.OnMapTaskTaken(this);
        ReceiveTask();

        OnMapTaskDestroyed?.Invoke();
    }

    private void MarkSpecial()
    {
        IsSpecial = true;
        OnSpecialMapTaskCreated?.Invoke(this);
        OnSpecialMapTaskCreatedStatic?.Invoke(this);
    }

    public bool HasAnyBlockers(out List<MapPinBlocker> blockers)
    {
        blockers = new();
        foreach (var mapTaskReceiver in MapTaskReceivers)
        {
            var blocker = mapTaskReceiver.CanReceiveTask(this);
            if (blocker == null) continue;

            blockers.Add(blocker);
        }

        return blockers.Count == 0;
    }

    private void ReceiveTask()
    {
        foreach (var mapTaskReceiver in MapTaskReceivers)
        {
            mapTaskReceiver.ReceiveMapTask(this);
        }
    }

    public void ClientOnDestroyTask()
    {
        OnCarComeBack?.Invoke();
    }
}
