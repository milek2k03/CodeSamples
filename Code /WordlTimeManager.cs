using UnityEngine;
using System.Collections.Generic;
using System;
using Mirror;

public class WorldTimeManager : NetworkBehaviour, IManager, IStartable, IAwake, IServiceLocatorComponent, IUpdateable, ISaveable<SaveData>
{
	[Serializable]
	private class TimeLock
	{
		[field: SerializeField] public PadlockList Padlocks { get; private set; } = new();
		[field: SerializeField] public float Hour { get; private set; } = 0;
		[field: SerializeField] public int Day { get; private set; } = 1;
	}
	[ServiceLocatorComponent] private StatsManager _statsManager;
	[ServiceLocatorComponent] private DifficultyLevelManager _difficultyLevelManager;

	public event Action<TimeOfDay> OnCurrentTimeOfDay;
	public event Action OnDayPassed = null;
	public event Action<int> OnClockMinutesPassed = null;
	public event Action<float> OnTimeSkipped = null;

	public const int HoursInDay = 24;
	public const int MinutesInHour = 60;

	[SerializeField] private ActionStat _dayPassedInt;
	[SerializeField] private List<TimeLock> _timeLocks;
	[SerializeField,Range(1,3600)] private float _secondsPerHour = 60;
	private float _secondsPerMinute;
	[SerializeField]
	public float SecondsPerHour
	{
		get => _secondsPerHour;
		set
		{
			_secondsPerHour = value;
			_secondsPerMinute = _secondsPerHour / MinutesInHour;
		}
	}

	public float TimeScale { get; set; } = 1f;

	[SyncVar(hook = nameof(ClientHandleTimeUpdated))] public float Time;
	public float DeltaTime { get; set; }
	public int Hour => (int)Time;
	public int Minutes => (int)((Time - Hour) * MinutesInHour);
	[SyncVar] public int Days = 1;
	public TimeOfDay CurrentTimeOfDay { get; private set; }
	[field: SerializeField, Tooltip("In hours")] public float StartTime { get; private set; } = 12f;
	private float _oldTime = 0f;
	[SerializeField] private List<TimeOfDay> _timesOfDay = new();

	[ServiceLocatorComponent] private TimeManager _timeManager;
	public ServiceLocator MyServiceLocator { get; set; }
	private bool _enabled = true;
	public bool Enabled => _enabled;

	public void CustomReset()
	{
		ResetVariables();
	}

	public void CustomAwake()
	{
		_timeManager.IsNotNull(this, nameof(_timeManager));
		_secondsPerMinute = SecondsPerHour / MinutesInHour;
	}

	public void CustomStart()
	{
		if (!NetworkServer.active)
		{
			_enabled = false;
			return;
		}

	}

	public void CustomUpdate()
	{
		float timeScale = TimeScale * _difficultyLevelManager.GetDifficultySettingFloat(SettingName.TimeScaleSetings);
		float deltaTime = _timeManager.GetDeltaTime() * timeScale;

		int previousMinute = Minutes;
		PassTime(deltaTime);
		int currentMinutes = Minutes;

		if (currentMinutes < previousMinute) currentMinutes += 60;
		int minutesPassed = currentMinutes - previousMinute;

		if(minutesPassed > 0)
		{
			SetTimeOfDay();
			OnClockMinutesPassed?.Invoke(minutesPassed);
		}
	}


	public void ClientHandleTimeUpdated(float _, float value)
	{
		SetTimeOfDay();
	}

	public Timestamp GetTimestampOfCurrentTime()
	{
		return new Timestamp(Hour, Minutes, Days);
	}

	private bool CheckTimeOfDay(TimeOfDay timeOfDay)
	{
		float startingTime = timeOfDay.GetStartTime();
		float endingTime = timeOfDay.GetEndTime();

		if (endingTime < startingTime)//meaning it's something like 23 - 4 
		{
			return Time <= endingTime || Time >= startingTime;
		}

		return Time >= startingTime && Time <= endingTime;
	}

	private void ResetVariables()
	{
		TimeScale = 1f;
		Time = StartTime;
		_oldTime = 0f;
		Days = 1;
	}

	public void SkipTime(float hours)
	{
		PassHours(hours);

		OnTimeSkipped?.Invoke(hours);
	}

	public void PassHours(float hours) => PassTime(hours * _secondsPerHour);
	private void PassTime(float value)
	{
		_oldTime = Time;

		float change = value / _secondsPerHour;
		Time = ClampTime(Time, change);

		if (Time >= HoursInDay)
		{
			Days++;
			_oldTime -= HoursInDay;
			Time -= HoursInDay;
			OnDayPassed?.Invoke();
			_statsManager.AddStat(_dayPassedInt, new IntActionStatData(Days));
		}

		DeltaTime = Time - _oldTime;
	}

	private float ClampTime(float hour, float change)
	{
		float finalHour = hour + change;
		foreach (TimeLock timeLock in _timeLocks)
		{
			if (Days != timeLock.Day) continue;
			if (hour > timeLock.Hour) continue;
			if (!timeLock.Padlocks.IsAnyPadlockLocked()) continue;
			finalHour = Mathf.Clamp(finalHour, 0, timeLock.Hour);
		}
		return finalHour;
	}

	private void SetTimeOfDay()
	{
		foreach (var timeOfDay in _timesOfDay)
		{
			if (CheckTimeOfDay(timeOfDay))
			{
				CurrentTimeOfDay = timeOfDay;
				OnCurrentTimeOfDay?.Invoke(CurrentTimeOfDay);
				return;
			}
		}
		CurrentTimeOfDay = null;
		Debug.LogWarning("Something went wrong when trying to set time of day");
	}
	private void OnValidate()
	{
		_secondsPerMinute = _secondsPerHour / 60;
	}

	public SaveData CollectData(SaveData data)
	{
		data.TimeSaveData = new(Time, Days);
		return data;
	}

	public void Initialize(SaveData save)
	{
		if (save == null)
		{
			ResetVariables();
			SetTimeOfDay();
			_oldTime = Time;
			_statsManager.AddStat(_dayPassedInt, new IntActionStatData(1));
			return;
		}

		Time = save.TimeSaveData.Time;
		Days = save.TimeSaveData.Day;
		SetTimeOfDay();
		_oldTime = Time;
	}
}
