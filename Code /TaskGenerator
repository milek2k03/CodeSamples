using Mirror;
using RNG;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class TaskGenerator : MonoBehaviour, IAwake, IManager, IGenerator<MapPinServiceLocator, TaskSaveInfo>, IServiceLocatorComponent
{
    public ServiceLocator MyServiceLocator { get; set; }
    [field: SerializeField] public MapPinServiceLocator QuestPrefab { get; private set; }
    public List<MapPinServiceLocator> SpawnedObjects { get; set; } = new();

    [ServiceLocatorComponent] private TaskManager _taskManager;

    [SerializeField] private WeightedList<TaskPreset> _presets;
    [SerializeField] private Transform _questHolder;
    [SerializeField] private float _chanceForMoney = 1f;
    [SerializeField] private UniformDistribution _difficultyDistribution;
    [field: SerializeField] private PetGenerator _petGenerator;

    public void CustomAwake()
    {
        _presets.IsNotNull(this, nameof(_presets));
        _questHolder.IsNotNull(this, nameof(_questHolder));
    }

    #region Generation
    public MapPinServiceLocator GenerateObject(TaskSaveInfo info)
    {
        var taskSL = Instantiate(QuestPrefab);
        NetworkServer.Spawn(taskSL.gameObject);

        taskSL.CustomAwake();
        taskSL.TryGetServiceLocatorComponent(out TaskSaveService taskSaveService);
        taskSaveService.Initialize(info);
        taskSL.CustomStart();

        SpawnedObjects.Add(taskSL);
        if (info.AnimalInTask != default)
        {
            _petGenerator.SpawnedObjects.Add(info.AnimalInTask);
        }

        if (_taskManager == null) return null;

        return taskSL;
    }

    public List<MapPinServiceLocator> GenerateObjects(List<TaskSaveInfo> infos)
    {
        List<MapPinServiceLocator> mapPins = new();
        foreach (var task in infos)
        {
            mapPins.Add(GenerateObject(task));
        }
        return mapPins;
    }

    private TaskSaveInfo GenerateRandomTask()
    {
        TaskPreset preset = _presets.PickRandomItem();
        TaskSaveInfo info = new();

        info.AnimalInTask = _petGenerator.GenerateObject(preset.AnimalBreed.PickRandomItem(), _difficultyDistribution);
        info.TaskType.Add(MapTaskType.Animal);

        if (Random.Range(0, 1) < _chanceForMoney)
        {
            info.TaskType.Add(MapTaskType.Money);
            info.MoneyGained = 100;
        }
        
        info.TaskPresetGuid = _taskManager.TaskDatabase.GetGuidOfElement(preset);
        info.BaseDuration = preset.TaskDuration;
        info.Duration = preset.TaskDuration;
        info.GivenExperience = preset.GivenExperience;

        return info;
    }
    #endregion

    public bool CanGenerateAnyPet()
    {
        return true;
    }


    public TaskSaveInfo GenerateRandomInfo()
    {
        return GenerateRandomTask();
    }

    public MapPinServiceLocator GenerateObject()
    {
        return GenerateObject(GenerateRandomInfo());
    }

    public bool RemoveObject(MapPinServiceLocator obj)
    {
        if (TryGetTaskPet(obj, out var pet))
        {
            _petGenerator.RemoveObject(pet);
        }

        return SpawnedObjects.Remove(obj);
    }

    private bool TryGetTaskPet(MapPinServiceLocator pin, out PetSaveInfo pet)
    {
        if (!pin.TryGetServiceLocatorComponent(out MapTask task))
        {
            pet = null;
            return false;
        }

        if (task.AnimalInTask != null)
        {
            pet = task.AnimalInTask;
            return true;
        }

        pet = null;
        return false;
    }
}
