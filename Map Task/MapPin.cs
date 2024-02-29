using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using I2.Loc;
using Mirror;
using System.Collections;

public class MapPin : MonoBehaviour, IAwake, IServiceLocatorComponent, ISaveable<TaskSaveInfo>, IUpdateable
{
	public ServiceLocator MyServiceLocator { get; set; }
	public bool Enabled => true;

	[SerializeField] private ActionStat _onSendCar;
	[SerializeField] private ActionStat _taskTaken;

	[Header("Quest Levels of Icons")]
	[SerializeField] private GameObject _smallIcon;
	[SerializeField] private GameObject _midIcon;
	[SerializeField] private GameObject _bigIcon;


	[Header("Time of Work Settings")]
	[SerializeField] private GameObject _timeOfWorkSliderObject;
	[SerializeField] public Slider _timeOfWorkSlider;


	[Header("Time of Travel Settings")]
	[SerializeField] private GameObject _carTravelSliderObject;
	[SerializeField] public Slider _carTravelSlider;
	[SerializeField] private Button _confirmButton;



	[Header("Quest Duration Settings")]
	[SerializeField] private float _redQuestDuration;
	[SerializeField] private float _orangeQuestDuration;
	[SerializeField] private GameObject _taskDurationObject;
	[SerializeField] private Slider _taskDurationSlider;
	[SerializeField] private Image _sliderImage;


	[Header("Set Task Presets")]
	[SerializeField] private Image[] _taskImage;
	[SerializeField] private List<TMP_Text> _taskName;
	[SerializeField] private TMP_Text _taskDetails;
	[SerializeField] private GameObject _details;

	[SerializeField] private MapPinBlockerList _blockers;

	private bool _pinClicked;
	private bool _deactivatedVisuals = false;
	public bool IsCarSend { get; private set; } = false;
	private bool _specialTask = false;


	[SerializeField] private float _baseClientSyncInterval = 1;
	[ServiceLocatorComponent] private MapTask _mapTask;
	[ServiceLocatorComponent] private CarsManager _carsManager;
	[ServiceLocatorComponent] private StatsManager _statsManager;
	[ServiceLocatorComponent] private MapTaskSyncer _mapTaskSyncer;
	[ServiceLocatorComponent] private DatabasesManager _networkDatabasesManager;
	[ServiceLocatorComponent] private TaskManager _taskManager;
	private ZoomMap _zoomMap;
	private Car _car;
	private TaskPreset _currentPreset;
	private float _timeTillSync;

	private Coroutine _availabilityCheckLoop = null;


	public void CustomAwake()
	{
		_smallIcon.IsNotNull(this, nameof(_smallIcon));
		_midIcon.IsNotNull(this, nameof(_midIcon));
		_bigIcon.IsNotNull(this, nameof(_bigIcon));
		_zoomMap = _taskManager.ZoomMap;

		_zoomMap.OnZoomChanged += HandleZoom;
		_mapTask.OnStateSwitched += MapTaskStateSwitched;
		_mapTask.OnSpecialMapTaskCreated += task => SpecialTaskTreatment();

		_timeTillSync = _baseClientSyncInterval;
		StartCoroutine(HandleClientSync());
	}

	private void Start()
	{
		transform.localScale = new Vector3(0.75f, 0.75f, 1f);
	}

	public void CustomUpdate()
	{
		CheckButtons();
	}

	private void OnEnable()
	{
		_availabilityCheckLoop = StartCoroutine(CheckQuestAvailability());
	}

	private void OnDisable()
	{
		StopCheckingCoroutine();
	}

	private IEnumerator CheckQuestAvailability()
	{
		RefreshStatus();

		while (true)
		{
			yield return new WaitForSeconds(1);
			RefreshStatus();
		}

	}

	private void RefreshStatus()
	{
		if (_mapTask.HasAnyBlockers(out var blockers))
		{
			_confirmButton.interactable = true;

		}
		else
		{
			_confirmButton.interactable = false;
		}
		_blockers.LoadBlockers(blockers);
	}


	public TaskSaveInfo CollectData(TaskSaveInfo data)
	{
		data.TaskPresetGuid = _networkDatabasesManager.TaskDatabase.GetGuidOfElement(_currentPreset);
		data.TaskPosition = new(MyServiceLocator.GetComponent<RectTransform>().anchoredPosition);

		return data;
	}

	public void Initialize(TaskSaveInfo save)
	{
		if (save.MapTaskStatus != MapTaskStatus.CarComingBack)
		{
			_mapTaskSyncer.TaskPresetIndex = _networkDatabasesManager.TaskDatabase.GetIndexOf(save.TaskPresetGuid);
			_mapTaskSyncer.TaskName = new(save.AnimalInTask.PetStaticInfo.PetName);
			_mapTaskSyncer.TaskDescription = new(save.AnimalInTask.PetStaticInfo.PetDescription);
		}

		MyServiceLocator.GetComponent<RectTransform>().anchoredPosition = save.TaskPosition.Vector3;
		if (save.MapTaskStatus == MapTaskStatus.Idle) return;

		if (save.MapTaskStatus == MapTaskStatus.CarComingBack) DeactivateVisuals();

		TrySendCar(transform.position, true);
	}

	public void InitializeWithPreset(int presetIndex)
	{
		_currentPreset = _networkDatabasesManager.TaskDatabase.GetElementOfIndex(presetIndex);
		_timeOfWorkSlider.image.sprite = _currentPreset.TaskImage;
		foreach (var image in _taskImage)
		{
			image.sprite = _currentPreset.TaskImage;
		}
	}

	public void SetTaskTexts(LocalizedString title, LocalizedString description)
	{
		foreach (var taskName in _taskName)
		{
			taskName.text = title;
		}

		_taskDetails.text = description;
	}


	#region UpdateVisuals
	public void UpdateTaskVisuals()
	{
		switch (_mapTask.MapTaskStatus)
		{
			case MapTaskStatus.Idle:
				IdleUpdate();
				break;
			case MapTaskStatus.CarDriving:
				UpdateTravelProgress();
				break;
			case MapTaskStatus.Working:
				UpdateWorkSlider();
				break;
			case MapTaskStatus.CarComingBack:
				UpdateComingBackProgress();
				break;
		}
	}

	private void IdleUpdate() => UpdateTaskLifeSpan(_mapTask.QuestDuration, _mapTask.BaseTaskDuration);

	private void UpdateTravelProgress() => UpdateTravelProgress(_mapTask.CarRemainingTravelTime, _mapTask.CarInitialTravelTime);

	public void UpdateTravelProgress(float remaining, float baseTimeOfDriving)
	{
		float percentage = (remaining / baseTimeOfDriving);
		_carTravelSlider.value = percentage;
		if (_car == null) return;
		_car.SetCarProgress(1 - percentage);
	}

	private void UpdateComingBackProgress() => UpdateTravelProgress(_mapTask.CarRemainingComeBackTime, _mapTask.CarInitialTravelTime);

	public void UpdateComingBackProgress(float remaining, float baseTimeOfDriving)
	{
		if (_car == null) return;
		_car.SetCarProgress(1 - (remaining / baseTimeOfDriving));
	}

	private void UpdateWorkSlider() => UpdateWorkSlider(_mapTask.CarRemainingWorkTime, _mapTask.BaseCarTimeOfWork);

	public void UpdateWorkSlider(float remaining, float baseTimeOfWork)
	{
		_timeOfWorkSlider.value = (remaining / baseTimeOfWork);

		if (_timeOfWorkSlider.value > 1) return;
		_car.IsBack = true;
		Vector3 position = this.transform.position;
		_carsManager.TryComeBack(_car, position);
	}

	public void UpdateTaskLifeSpan(float currentValue, float maxValue)
	{
		float value = currentValue / maxValue;
		_taskDurationSlider.value = value;

		if (value <= _redQuestDuration)
			_sliderImage.color = Color.red;
		else if (value <= _orangeQuestDuration)
			_sliderImage.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
	}

	#endregion
	public void TrySendCar(Vector3 position, bool loading = false)
	{
		if (_car != null) return;

		if (!_mapTask.HasAnyBlockers(out var _)) return;

		_car = _carsManager.TryGetCar();
		if (_car == null) return;

		if (!_carsManager.TrySetupCar(_car, position)) return;

		_mapTask.OnCarComeBack += () => _carsManager.ResetCar(_car);
		_mapTask.OnCarSent(_car, loading);
		if (!loading) _statsManager.AddStat(_onSendCar, new StringActionStatData(_mapTask?.AnimalInTask?.PetStaticInfo?.PetBreedGuid));

		IsCarSend = true;
		StopCheckingCoroutine();
		ClickPin();
		SetIconMode(MapPinIconMode.Small);
		
		if(_taskTaken == null) return;
		_statsManager.AddStat(_taskTaken);
	}

	private void StopCheckingCoroutine()
	{
		if (_availabilityCheckLoop != null) StopCoroutine(_availabilityCheckLoop);
	}


	public void TrySendCar() => TrySendCar(transform.position, false);

	private void OnDestroy()
	{
		if (_zoomMap != null)
		{
			_zoomMap.OnZoomChanged -= HandleZoom;
		}

		if (_mapTask != null)
		{
			_mapTask.OnStateSwitched -= MapTaskStateSwitched;
		}
	}

	// Icons Settings
	private void CheckButtons()
	{
		if (!Input.GetMouseButton(0)) return;

		if (_smallIcon == null) return;
		if (_details == null) return;
		if (!RectTransformUtility.RectangleContainsScreenPoint(_smallIcon.GetComponent<RectTransform>(), Input.mousePosition) && !RectTransformUtility.RectangleContainsScreenPoint(_details.GetComponent<RectTransform>(), Input.mousePosition))
		{
			UnClickPin();
		}
	}

	private void HandleZoom() => HandleZoom(_zoomMap.CurrZoom);

	private void HandleZoom(float zoomValue)
	{
		if (zoomValue >= 4f || _pinClicked)
		{
			SetIconMode(MapPinIconMode.Big);
		}
		else if (zoomValue >= 2f && zoomValue < 4f && !_pinClicked)
		{
			SetIconMode(MapPinIconMode.Medium);
		}
		else if (zoomValue < 2f && !_pinClicked)
		{
			SetIconMode(MapPinIconMode.Small);
		}
	}

	private void SetIconMode(MapPinIconMode iconMode)
	{
		if (_deactivatedVisuals) return;

		switch (iconMode)
		{
			case MapPinIconMode.Small:
				EnableSliders();
				_bigIcon.SetActive(false);
				_midIcon.SetActive(false);
				_smallIcon.SetActive(true);
				break;
			case MapPinIconMode.Medium:
				EnableSliders();
				_bigIcon.SetActive(false);
				_midIcon.SetActive(true);
				_smallIcon.SetActive(false);
				break;
			case MapPinIconMode.Big:
				DisableSliders();
				_bigIcon.SetActive(true);
				_midIcon.SetActive(false);
				_smallIcon.SetActive(false);
				break;
		}
	}

	private void UnClickPin()
	{
		_pinClicked = false;
		HandleZoom();
	}

	public void ClickPin()
	{
		transform.SetAsLastSibling();
		_pinClicked = true;
		HandleZoom();
	}

	private void DisableSliders()
	{
		_timeOfWorkSliderObject.transform.localScale = new Vector2(0, 0);
		_carTravelSliderObject.transform.localScale = new Vector2(0, 0);
		_taskDurationObject.transform.localScale = new Vector2(0, 0);
	}

	private void EnableSliders()
	{
		_timeOfWorkSliderObject.transform.localScale = new Vector2(1, 1);
		_carTravelSliderObject.transform.localScale = new Vector2(1, 1);
		_taskDurationObject.transform.localScale = new Vector2(1, 1);
	}

	public void MapTaskStateSwitched(MapTaskStatus status)
	{
		switch (status)
		{
			case MapTaskStatus.Idle:
				ToggleDurationSlider(true);
				_carTravelSliderObject.SetActive(false);
				_timeOfWorkSliderObject.SetActive(false);
				break;
			case MapTaskStatus.CarDriving:
				ToggleDurationSlider(false);
				_carTravelSliderObject.SetActive(true);
				TurnOffTakingTaskButton();
				break;
			case MapTaskStatus.Working:
				_carTravelSliderObject.SetActive(false);
				_timeOfWorkSliderObject.SetActive(true);
				TurnOffTakingTaskButton();
				break;
			case MapTaskStatus.CarComingBack:
				DeactivateVisuals();
				break;
		}

		if (NetworkServer.active)
		{
			ResetTimer();
			SendInfoToClients();
		}
	}
	private void TurnOffTakingTaskButton()
	{
		_confirmButton.gameObject.SetActive(false);
	}

	private void ToggleDurationSlider(bool activate)
	{
		if (_specialTask) return;
		_taskDurationObject.SetActive(activate);
	}
	private void SpecialTaskTreatment()
	{
		ToggleDurationSlider(false);
		_specialTask = true;
	}

	private void DeactivateVisuals()
	{
		_deactivatedVisuals = true;
		_smallIcon.SetActive(false);
		_bigIcon.SetActive(false);
		_midIcon.SetActive(false);
		_taskDurationObject.SetActive(false);
		_carTravelSliderObject.SetActive(false);
		_timeOfWorkSliderObject.SetActive(false);

		if (_zoomMap == null) return;

		_zoomMap.OnZoomChanged -= HandleZoom;
	}


	private IEnumerator HandleClientSync()
	{
		if (!NetworkServer.active) yield break;
		while (true)
		{
			_timeTillSync -= Time.deltaTime;
			if (_timeTillSync <= 0)
			{
				ResetTimer();
				SendInfoToClients();
			}
			yield return null;
		}
	}
	[Server]
	private void ResetTimer() => _timeTillSync = _baseClientSyncInterval;
	[Server]
	private void SendInfoToClients()
	{
		switch (_mapTask.MapTaskStatus)
		{
			case MapTaskStatus.Idle:
				_mapTaskSyncer.IdleInfoUpdate(_mapTask.QuestDuration, _mapTask.BaseTaskDuration);
				break;
			case MapTaskStatus.CarDriving:
				_mapTaskSyncer.CarDrivingUpdate(_mapTask.CarRemainingTravelTime, _mapTask.CarInitialTravelTime);
				break;
			case MapTaskStatus.Working:
				_mapTaskSyncer.WorkTimeUpdate(_mapTask.CarRemainingWorkTime, _mapTask.BaseCarTimeOfWork);
				break;
			case MapTaskStatus.CarComingBack:
				_mapTaskSyncer.ComingBackUpdate(_mapTask.CarRemainingComeBackTime, _mapTask.CarInitialTravelTime);
				break;
		}
	}
}
