using UnityEngine;
using Sirenix.OdinInspector;
using System;


public class LeaderboardManager : MonoBehaviour, IServiceLocatorComponent, IAwake
{
	public ServiceLocator MyServiceLocator { get; set; }
	[ServiceLocatorComponent] private ReputationManager _reputationManager;
	[ServiceLocatorComponent] private ShelterInfo _shelterInfo;
	[ServiceLocatorComponent] private WorldTimeManager _worldTimeManager;
	[ServiceLocatorComponent] private DeliverySystemManager _deliverySystemManager;
	[ServiceLocatorComponent] private StatsManager _statsManager;

	public event Action<LeagueType> OnLeagueChanged;
	public Shelter MyShelter {get; set;} = new();
	
	[field: SerializeField] public LeagueLeaderboardManager RegionalLeagueLeaderboardManager { get; private set; }
	[field: SerializeField] public LeagueLeaderboardManager NationalLeagueLeaderboardManager { get; private set; }
	[field: SerializeField] public LeagueLeaderboardManager WorldLeagueLeaderboardManager { get; private set; }
	
	[SerializeField] private ActionStat _firstPlaceRegionalAchieved;
	[SerializeField] private ActionStat _firstPlaceNationalAchieved;
	[SerializeField] private ActionStat _firstPlaceWorldAchieved;

	[SerializeField, ReadOnly] public bool ReceivedRegionalAward = false;
	[SerializeField, ReadOnly] public bool ReceivedNationalAward = false;
	[SerializeField, ReadOnly] public bool ReceivedWorldAward = false;

	
	[SerializeField] private Color _myShelterPanelColor;
	[SerializeField] private ItemData _regionalAward;
	[SerializeField] private ItemData _nationalAward;
	[SerializeField] private ItemData _worldAward;

	public void CustomAwake()
	{
		MyShelter = CreateMyShelter(_reputationManager, _shelterInfo);
		RegionalLeagueLeaderboardManager.AddToLeague();

		_reputationManager.OnReputationChanged += RefreshShelterPoints;
		_shelterInfo.OnShelterNameSet += MyShelter.Rename;
		_shelterInfo.OnShelterIconChanged += MyShelter.ChangeIcon;
		_worldTimeManager.OnDayPassed += UpgradePoints;
	}

	public void OnDestroy()
	{
		if (_worldTimeManager == null) return;

		_worldTimeManager.OnDayPassed -= UpgradePoints;
		_shelterInfo.OnShelterNameSet -= MyShelter.Rename;
		_shelterInfo.OnShelterIconChanged -= MyShelter.ChangeIcon;
		_reputationManager.OnReputationChanged -= RefreshShelterPoints;
	}

	private Shelter CreateMyShelter(ReputationManager reputationManager, ShelterInfo shelterInfo)
	{
		var myshelter = new Shelter(shelterInfo.ShelterName, reputationManager.Reputation, shelterInfo.CurrentSprite, _myShelterPanelColor, true);
		myshelter.SetAsMyShelter(reputationManager, shelterInfo);
		return myshelter;
	}

	public void TryChangeLeague()
	{
		if (MyShelter == null) return;

		if (MyShelter.Points > WorldLeagueLeaderboardManager.MinReputation)
		{
			TryGetWorldAwards();
			if(WorldLeagueLeaderboardManager.IsMyShelterInLeague()) return;
						
			NationalLeagueLeaderboardManager.RemoveFromLeague();
			WorldLeagueLeaderboardManager.AddToLeague();
			
			OnLeagueChanged?.Invoke(LeagueType.World);
			
			return;
		}


		if (MyShelter.Points > NationalLeagueLeaderboardManager.MinReputation)
		{
			TryGetNationalAwards();
			if(NationalLeagueLeaderboardManager.IsMyShelterInLeague()) return;
					
			WorldLeagueLeaderboardManager.RemoveFromLeague();
			RegionalLeagueLeaderboardManager.RemoveFromLeague();
			NationalLeagueLeaderboardManager.AddToLeague();
			
			OnLeagueChanged?.Invoke(LeagueType.National);

			return;
		}

		if (MyShelter.Points > RegionalLeagueLeaderboardManager.MinReputation)
		{
			TryGetRegionalAwards();
			if (RegionalLeagueLeaderboardManager.IsMyShelterInLeague()) return;

			NationalLeagueLeaderboardManager.RemoveFromLeague();
			RegionalLeagueLeaderboardManager.AddToLeague();

			OnLeagueChanged?.Invoke(LeagueType.Regional);

			return;
		}
	}

	private void TryGetRegionalAwards()
	{
		if (ReceivedRegionalAward) return;
		if (!RegionalLeagueLeaderboardManager.TryCheckMyShelterPosition()) return;

		ReceivedRegionalAward = true;
		_deliverySystemManager.SendDelivery(_regionalAward);
		_statsManager.AddStat(_firstPlaceRegionalAchieved);
		Debug.Log("ReceivedRegionalAward : " + ReceivedRegionalAward);
	}

	private void TryGetNationalAwards()
	{
		if (ReceivedNationalAward) return;
		if (!NationalLeagueLeaderboardManager.TryCheckMyShelterPosition()) return;

		ReceivedNationalAward = true;
		_deliverySystemManager.SendDelivery(_nationalAward);
		_statsManager.AddStat(_firstPlaceNationalAchieved);
		Debug.Log("ReceivedNationalAward : " + ReceivedNationalAward);
	}

	private void TryGetWorldAwards()
	{
		if (ReceivedWorldAward) return;
		if (!WorldLeagueLeaderboardManager.TryCheckMyShelterPosition()) return;

		ReceivedWorldAward = true;
		_deliverySystemManager.SendDelivery(_worldAward);
		_statsManager.AddStat(_firstPlaceWorldAchieved);
		Debug.Log("ReceivedWorldAward : " + ReceivedWorldAward);
	}
	
	private void UpgradePoints()
	{
		RegionalLeagueLeaderboardManager.UpdateSheltersPoints();
		NationalLeagueLeaderboardManager.UpdateSheltersPoints();
		WorldLeagueLeaderboardManager.UpdateSheltersPoints();
	}

	private void RefreshShelterPoints(int points)
	{
		MyShelter.SetPoints(points);
		TryChangeLeague();
	}

	public LeagueLeaderboardManager GetLeagueManager(LeagueType type)
	{
		switch (type)
		{
			case LeagueType.Regional: return RegionalLeagueLeaderboardManager;
			case LeagueType.National: return NationalLeagueLeaderboardManager;
			case LeagueType.World: return WorldLeagueLeaderboardManager;
			default: return RegionalLeagueLeaderboardManager;
		}
	}
}
