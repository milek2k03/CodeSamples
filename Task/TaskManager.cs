using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Sirenix.OdinInspector;
using Mirror;
using System.Runtime.Remoting.Messaging;

public class TaskManager : MonoBehaviour, IManager, IServiceLocatorComponent, IAwake, IUpdateable, ISaveable<SaveData>
{
    [Header("Spawn locations")]
    public Dictionary<MapPart, DistrictController> SpawnAreaDistricts = new();
    private MapPart RandomUnlockedDistrict => SpawnAreaDistricts.GetRandomItem(part => part.Value.IsDistrictUnlocked).Key;

    [Header("Excluded Areas")]
    [SerializeField] private Image _map2D;
    [SerializeField, ReadOnly] private RectTransform _map2DRect;

    [Header("Quests Settings")]
    [SerializeField] private float _distanceFromOtherQuest = 10f;
    [SerializeField] private int _maxTasks;
    [SerializeField] private float _spawnInterval;
    private Coroutine _questSpawningCoroutine = null;

    [field: SerializeField] public TaskDataBase TaskDatabase { get; private set; }

    [ServiceLocatorComponent] private TimeManager _timeManager;
    [ServiceLocatorComponent] private TakingInPetManager _takingInPetManager;
    [ServiceLocatorComponent] private CurrencyManager _currencyManager;
    [ServiceLocatorComponent] private ShelterStatusManager _shelterStatusManager;
    [ServiceLocatorComponent] private NotificationsSystem _notificationsManager;
    [ServiceLocatorComponent] private CarsManager _carsManager;

    public ZoomMap ZoomMap;

    [SerializeField] private GameObject _mapPinPrefab;
    [SerializeField] private Transform _questHolder;

    public GameObject MapDebugger { get; private set; }

    public ServiceLocator MyServiceLocator { get; set; }
    public bool Enabled { get; } = true;

    public void CustomAwake()
    {
        MapTask.OnSpecialMapTaskCreatedStatic += OnSpecialTaskSpawned;
        NetworkClient.RegisterPrefab(_mapPinPrefab, SpawnHandler, UnSpawnHandler);
    }

    public void GetTablet(TabletUI tablet)
    {
        var map = tablet.MapTabletUISpawned.WorldMap;
        ZoomMap = map.ZoomMap;
        _map2D = map.MapImage;
        _map2D.TryGetComponent(out _map2DRect);
        MapDebugger = map.MapDebugger;
        _questHolder = map.QuestHolder;
        foreach (var district in map.DistrictControllers)
        {
            SpawnAreaDistricts.Add(district.MapPart, district);
        }
    }

    private GameObject SpawnHandler(SpawnMessage msg)
    {
        var obj = Instantiate(_mapPinPrefab);
        StartCoroutine(SetupTaskAfterFrame(obj,msg.position,_questHolder));
        return obj;
    }

    private IEnumerator SetupTaskAfterFrame(GameObject task, Vector3 position, Transform parent)
    {
        yield return new WaitForEndOfFrame();
        task.transform.SetParent(parent);
        task.transform.localPosition = position;
    }

    private void UnSpawnHandler(GameObject spawned)
    {
        if (!spawned.TryGetComponent(out ServiceLocator serviceLocator))
        {
            Destroy(spawned);
            return;
        }
        if (!serviceLocator.TryGetServiceLocatorComponent(out MapTask task))
        {
            Destroy(spawned);
            return;
        }
        task.ClientOnDestroyTask();
        Destroy(spawned);
    }

    public void CustomUpdate()
    {
        float deltaTime = _timeManager.GetDeltaTime();
        TaskManager taskManager = this;
        var MapTasks = GetAllSpawnedTasks();
        for (int i = MapTasks.Count - 1; i >= 0; i--)
        {
            if (!MapTasks[i].TryGetServiceLocatorComponent(out MapTask mapTask)) continue;

            mapTask.UpdateMapTask(deltaTime, taskManager);
        }
        MapTasks = GetAllSpawnedTasks();
        foreach (var map in MapTasks)
        {
            if (!map.TryGetServiceLocatorComponent(out MapPin mapPin)) continue;

            mapPin.UpdateTaskVisuals();
        }
    }

    public SaveData CollectData(SaveData data)
    {
        foreach (var district in SpawnAreaDistricts)
        {
            data.Map2DSaveInfo.DistictSaveInfo.Add(district.Key, district.Value.GetDistrictSaveInfo());
        }

        var tasksSaveInfo = data.Map2DSaveInfo;
        foreach (var task in GetAllSpawnedTasks())
        {
            if (!task.TryGetServiceLocatorComponent(out TaskSaveService saveService)) continue;

            TaskSaveInfo info = new();
            saveService.CollectData(info);
            tasksSaveInfo.QuestSaveInfo.Add(info);
        }
        return data;
    }

    public void Initialize(SaveData save)
    {
        if (save == null) return;

        foreach (var districtLoad in save.Map2DSaveInfo.DistictSaveInfo)
        {
            MapPart mapPart = districtLoad.Key;
            var foundDistrict = SpawnAreaDistricts.First(d => d.Key == mapPart);
            foundDistrict.Value.LoadDistrict(districtLoad.Value);
        }

        List<MapPinServiceLocator> pins = new();
        var savedQuests = save.Map2DSaveInfo.QuestSaveInfo;
        foreach (var district in SpawnAreaDistricts)
        {
            var savedQuestsInDistrict = savedQuests.FindAll(quest => quest.MapPart == district.Key);

            pins.AddRange(district.Value.TaskGenerator.GenerateObjects(savedQuestsInDistrict));
        }

        foreach (var pin in pins)
        {
            if (!pin.TryGetServiceLocatorComponent(out MapTask task))
            {
                Debug.LogError("Something went wrong while loading tasks");
                return;
            }
            SetupTaskReceivers(pin);
            task.OnTaskLoaded();

            task.OnMapTaskDestroyed += () => RemoveMapPin(pin);
        }
    }

    public void StartSpawningLoop(MapPart mapPart)
    {
        if (_questSpawningCoroutine != null)
            StopCoroutine(_questSpawningCoroutine);

        _questSpawningCoroutine = StartCoroutine(SpawnQuestsAutomatically(mapPart));
    }

    public void StartSpawningLoop()
    {
        if (_questSpawningCoroutine != null)
            StopCoroutine(_questSpawningCoroutine);

        _questSpawningCoroutine = StartCoroutine(SpawnQuestsAutomatically());
    }

    public void StopSpawningQuests()
    {
        if (_questSpawningCoroutine == null) return;

        StopCoroutine(_questSpawningCoroutine);
        _questSpawningCoroutine = null;
    }

    private IEnumerator SpawnQuestsAutomatically(MapPart mapPart)
    {
        var district = GetDistrict(mapPart);
        if (district == null) yield break;
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            if (GetAllSpawnedTasks().Count >= _maxTasks) continue;

            TrySpawnQuest(district);
        }
    }
    private IEnumerator SpawnQuestsAutomatically()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            if (GetAllSpawnedTasks().Count >= _maxTasks) continue;
            var district = GetDistrict(RandomUnlockedDistrict);
            TrySpawnQuest(district);
        }
    }

    [Button("Debug remove all Idle pins")]
    public void RemoveAllIdleMapPins()
    {
        var tasks = GetAllSpawnedTasks();
        foreach (var pin in tasks)
        {
            if (pin.TryGetServiceLocatorComponent(out MapTask task))
                if (task.MapTaskStatus != MapTaskStatus.Idle) continue;

            RemoveMapPin(pin);
        }
    }

    private void RemoveMapPin(MapPinServiceLocator mapPinSL)
    {
        if (mapPinSL == null) return;
        var district = GetPinDistrict(mapPinSL);
        district.TaskGenerator.RemoveObject(mapPinSL);
        NetworkServer.Destroy(mapPinSL.gameObject);
    }

    private void SetupTaskReceivers(MapPinServiceLocator mapPin)
    {
        if (!mapPin.TryGetServiceLocatorComponent(out MapTask mapTask)) return;

        List<IMapTaskReceiver> receivers = new();

        receivers.Add(_carsManager); //always added

        foreach (var type in mapTask.MapTaskType)
        {
            if (type == MapTaskType.Animal)
            {
                receivers.Add(_takingInPetManager);
                receivers.Add(_shelterStatusManager);
            }
            else if (type == MapTaskType.Money)
            {
                receivers.Add(_currencyManager);
            }
        }

        mapTask.MapTaskReceivers.AddRange(receivers);
    }

    private DistrictController GetPinDistrict(MapPinServiceLocator mapPinSL)
    {
        if (!mapPinSL.TryGetServiceLocatorComponent(out MapTask task)) return null;

        return SpawnAreaDistricts[task.MapPart];
    }

    public List<MapPinServiceLocator> GetAllSpawnedTasks()
    {
        List<MapPinServiceLocator> mapPins = new();

        foreach (var district in SpawnAreaDistricts)
        {
            mapPins.AddRange(district.Value.SpawnedTasks);
        }

        return mapPins;
    }

    private void OnSpecialTaskSpawned(MapTask task)
    {
        _notificationsManager.SendSideNotification("Special task spawned!", NotificationType.Information);
    }

    private TaskSaveInfo PredefinedToSaveInfo(PredefinedMapTask predefinedMapTask)
    {
        TaskSaveInfo task = new();
        task.Duration = predefinedMapTask.BaseDuration;
        task.BaseDuration = predefinedMapTask.BaseDuration;
        task.TaskType = new(predefinedMapTask.TaskType);
        task.AnimalInTask = predefinedMapTask.PredefinedPet.Task;
        task.MoneyGained = predefinedMapTask.MoneyGained;
        task.TaskPresetGuid = TaskDatabase.GetGuidOfElement(predefinedMapTask.TaskPreset);
        return task;
    }

    public void OnMapTaskTaken(MapTask mapTask)
    {
        AddExperienceToGivenDistrict(mapTask.MapPart, mapTask.ExperienceGiven);
    }

    public void AddExperienceToGivenDistrict(MapPart part, int amount)
    {
        DistrictController district = SpawnAreaDistricts[part];
        district.AddExperience(amount);
    }

    public DistrictController GetDistrict(MapPart part)
    {
        return SpawnAreaDistricts.GetValueOrDefault(part);
    }
    [Button("spawn predefined task")]
    public bool TrySpawnPredefinedTask(PredefinedMapTask task, MapPart mapPart)
    {
        if (task.SpawnInPredefinedPlace)
        {
            return TrySpawnQuest(GetDistrict(mapPart), PredefinedToSaveInfo(task), _questHolder.TransformPoint(task.PlaceToSpawnIn));
        }
        if (!TryPickQuestDistrictLocation(GetDistrict(mapPart), out var position)) return false;

        return TrySpawnQuest(GetDistrict(mapPart), PredefinedToSaveInfo(task), position);

    }

    [GUIColor(1, 0, 1), Button("Spawn random quest")]
    private bool TrySpawnQuestDebug(MapPart part)
    {
        return TrySpawnQuest(GetDistrict(part));
    }
    public bool TrySpawnQuest()
    {
        return TrySpawnQuest(GetDistrict(RandomUnlockedDistrict));
    }

    public bool TrySpawnQuest(DistrictController district, int maxTries = 20)
    {
        if (district == null) return false;

        int i = 0;
        Vector2 position = default;

        if (maxTries <= 0) maxTries = 1;

        for (; i < maxTries; ++i)
            if (TryPickQuestLocation(district, out position)) break;

        if (i == maxTries) return false;

        var taskInfo = district.TaskGenerator.GenerateRandomInfo();

        return TrySpawnQuest(district, taskInfo, position);
    }

    private Vector2 LocalToWorldPosition(RectTransform districtRect, Vector2 position)
    {
        return districtRect.TransformPoint(position);
    }

    private bool NoTasksAroundPosition(Vector2 position)
    {
        return GetAllSpawnedTasks().Find(pin => Vector2.Distance(pin.transform.position, position) < _distanceFromOtherQuest * ZoomMap.CurrZoom) == null;
    }

    private bool TrySpawnQuest(DistrictController district, TaskSaveInfo info, Vector2 worldPosition)
    {
        info.MapPart = district.MapPart;
        info.AnimalInTask.PetStaticInfo.MapPart = district.MapPart;

        if (!district.TryGetComponent(out RectTransform districtRect)) return false;

        var mapPinSL = district.TaskGenerator.GenerateObject(info);
        if (!mapPinSL.TryGetServiceLocatorComponent(out MapTask task))
        {
            Debug.Log("Something went wrong");
            return false;
        }

        task.OnMapTaskDestroyed += () => RemoveMapPin(mapPinSL);
        if (!mapPinSL.TryGetComponent(out RectTransform rect))
        {
            RemoveMapPin(mapPinSL);
            Destroy(mapPinSL.gameObject);
            return false;
        }

        mapPinSL.transform.SetParent(_questHolder);
        mapPinSL.transform.position = worldPosition;
        SetupTaskReceivers(mapPinSL);
        NetworkServer.Spawn(mapPinSL.gameObject);
        return true;
    }

    private bool CheckDistrictPixelColor(Vector2 position, RectTransform districtRect)
    {
        if (!districtRect.TryGetComponent(out Image image)) return false;

        var coordinates = position / districtRect.rect.size;
        var color = image.sprite.texture.GetPixelBilinear(coordinates.x, coordinates.y);
        return color.a != 0;
    }

    private bool TryPickQuestLocation(DistrictController district, out Vector2 position)
    {
        if (!TryPickQuestDistrictLocation(district, out position)) return false;

        if (!district.TryGetComponent(out RectTransform districtRect)) return false;

        if (!CheckDistrictPixelColor(position, districtRect)) return false;

        position = LocalToWorldPosition(districtRect, position);

        if (!NoTasksAroundPosition(position)) return false;

        return true;
    }

    private bool TryPickQuestDistrictLocation(DistrictController district, out Vector2 position)
    {
        position = default;

        if (district == null) return false;

        if (!district.TryGetComponent(out RectTransform rectTransform)) return false;

        position = GetPositionWithinRect(rectTransform.rect);

        return true;
    }

    private Vector2 GetPositionWithinRect(Rect rect)
    {
        return new Vector2(
            Random.Range(0f, rect.width),
            Random.Range(0f, rect.height)
        );
    }
}