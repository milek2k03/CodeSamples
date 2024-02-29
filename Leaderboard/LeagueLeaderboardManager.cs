using System.Collections.Generic;
using UnityEngine;
using System;
using I2.Loc;
using Sirenix.OdinInspector;

[System.Serializable]
public class Shelter
{
	[field: SerializeField] public LocalizedString Name { get; private set; } = "Unnamed";
	[field: SerializeField, ReadOnly] public string MyShelterName { get; private set; } = "My Shelter";

	[field: SerializeField, ReadOnly] public int Place { get; set; } = 0;
	[field: SerializeField] public int Points { get; private set; } = 0;
	[field: SerializeField, ReadOnly] public Sprite Icon { get; private set; } = null;
	[field: SerializeField] public Color Color { get; private set; }
	[field: SerializeField, ReadOnly] public bool IsMyShelter = false;
	public event Action OnMyShelterNameChanged;
	public event Action OnMyShelterPointsChanged;
	public event Action OnMyShelterIconChanged;

	public Shelter(LocalizedString name, int points, Sprite icon, Color color, bool isMyShelter)
	{
		Name = name;
		Points = points;
		Icon = icon;
		Color = color;
		IsMyShelter = isMyShelter;
	}

	public void Rename(string name)
	{
		MyShelterName = name;
		OnMyShelterNameChanged?.Invoke();
	}

	public void SetPoints(int points)
	{
		Points = points;
		OnMyShelterPointsChanged?.Invoke();
	}

	public void ChangeIcon(Sprite old, Sprite newIcon) => SetIcon(newIcon);

	public void SetIcon(Sprite icon)
	{
		Icon = icon;
		OnMyShelterIconChanged?.Invoke();
	}

	public Shelter() { }

	public void SetAsMyShelter(ReputationManager reputation, ShelterInfo info)
	{
		if (reputation == null) return;
		if (info == null) return;
		IsMyShelter = true;
		reputation.OnReputationChanged += SetPoints;
		info.OnShelterIconChanged += (prev, curr) => SetIcon(curr);
		info.OnShelterNameSet += Rename;
	}
}

public enum LeagueType
{
	Regional,
	National,
	World,
}

public class LeagueLeaderboardManager : MonoBehaviour, IServiceLocatorComponent, IStartable
{
	public ServiceLocator MyServiceLocator { get; set; }
	[ServiceLocatorComponent] private LeaderboardManager _leaderboardManager;
	[ServiceLocatorComponent] private ReputationManager _reputationManager;
	public event Action OnEnemiesRefreshedPoints;

	[SerializeField] private SpriteDatabase _spriteDatabase;
	[field: SerializeField] public int MaxReputation { get; private set; } = 10000;
	[field: SerializeField] public int MinReputation { get; private set; } = 100;
	public List<Shelter> Shelters;

	public void CustomStart()
	{
		foreach (var shelter in Shelters)
		{
			if (shelter.IsMyShelter)
			{
				shelter.SetIcon(_spriteDatabase.EntryList[1]);
				shelter.SetPoints(_reputationManager.Reputation);
			}
			else
				shelter.SetIcon(_spriteDatabase.EntryList.GetRandomItem());
		}
	}

	public void AddToLeague()
	{
		if (_leaderboardManager.MyShelter == null) return;
		Shelters.Add(_leaderboardManager.MyShelter);
	}

	public void RemoveFromLeague()
	{
		if (_leaderboardManager.MyShelter == null) return;
		Shelters.Remove(_leaderboardManager.MyShelter);
	}

	public bool IsMyShelterInLeague()
	{
		foreach (var myShelter in Shelters)
		{
			if (myShelter.IsMyShelter) return true;
		}
		return false;
	}

	public bool TryCheckMyShelterPosition()
	{	
		bool isFirstPlace = false;
		foreach(var shelter in  Shelters)
		{
			if(shelter.IsMyShelter && shelter.Place == 1)
				isFirstPlace = true;
			else
				isFirstPlace = false;
		}
		
		if(isFirstPlace)
			return true;
		else
			return false;
	}

	private void ChangeRandomPoints()
	{
		foreach (var enemyShelter in Shelters)
		{
			if (enemyShelter.IsMyShelter) continue;

			int pickedNumber = UnityEngine.Random.Range(0, 3);

			switch (pickedNumber)
			{
				case 1:
					enemyShelter.SetPoints(+500);
					break;
				case 2:
					enemyShelter.SetPoints(-500);
					break;
			}

			enemyShelter.SetPoints(Mathf.Clamp(enemyShelter.Points, MinReputation, MaxReputation));
		}

		OnEnemiesRefreshedPoints?.Invoke();

	}

	public void UpdateSheltersPoints()
	{
		ChangeRandomPoints();
		_leaderboardManager.TryChangeLeague();
	}
}
