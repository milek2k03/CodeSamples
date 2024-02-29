using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class LeaderboardUI : MonoBehaviour, IServiceLocatorComponent, IStartable, IAwake
{
	public ServiceLocator MyServiceLocator { get; set; }
	public List<LeaderboardPanel> _leaderboardPanels;
	[ServiceLocatorComponent] private LeaderboardManager _leaderboardManager;

	[field: SerializeField] public LeagueType Type { get; private set; }
	[SerializeField] private GameObject _selectedLeagueIndicator;
	private LeagueLeaderboardManager _currentLeague;

	public void CustomAwake()
	{
		_currentLeague = _leaderboardManager.GetLeagueManager(Type);
	}

	public void CustomStart()
	{
		if (_currentLeague == null) return;
		_currentLeague.OnEnemiesRefreshedPoints += Refresh;
		_leaderboardManager.OnLeagueChanged += Refresh;
		_leaderboardManager.MyShelter.OnMyShelterPointsChanged += Refresh;

		SetUpShelters(_currentLeague.Shelters);
		Refresh();
	}

	public void OnDestroy()
	{
		if (_currentLeague == null) return;
		_currentLeague.OnEnemiesRefreshedPoints -= Refresh;
		_leaderboardManager.OnLeagueChanged -= Refresh;
		_leaderboardManager.MyShelter.OnMyShelterPointsChanged -= Refresh;
	}

	public void SetUpShelters(List<Shelter> shelters)
	{
		if (shelters == null) return;
		int loopEnd = Mathf.Min(shelters.Count, _leaderboardPanels.Count);
		int i = 0;

		for (; i < loopEnd; i++)
		{
			_leaderboardPanels[i].Initialize(shelters[i]);
		}
	}

	private void Refresh(LeagueType type) => Refresh();
	[Button]
	public void Refresh()
	{
		SetUpShelters(_currentLeague.Shelters);
		SortLeaderboardByPoints();
		UpdatePanelOrderInHierarchy();
		DisableLowRankingPanels();
		foreach (var leaderboardPanel in _leaderboardPanels)
		{
			leaderboardPanel.Refresh();
		}

		if (_currentLeague.Shelters.Count == 20)
			_selectedLeagueIndicator.SetActive(true);
		else
			_selectedLeagueIndicator.SetActive(false);
	}

	#region Sorting
	private void DisableLowRankingPanels()
	{
		int panelsToDisable = Mathf.Max(0, _leaderboardPanels.Count - 10);
		for (int i = 0; i < _leaderboardPanels.Count; i++)
		{
			if (i < panelsToDisable)
			{
				_leaderboardPanels[i].gameObject.SetActive(true);
			}
			else
			{
				_leaderboardPanels[i].gameObject.SetActive(false);
			}
		}
	}

	private void SortLeaderboardByPoints()
	{
		_leaderboardPanels = _leaderboardPanels
			.OrderByDescending(panel => panel.CurrentShelter == null ? 0 : panel.CurrentShelter.Points)
			.ThenByDescending(panel => panel.CurrentShelter != null && panel.CurrentShelter.IsMyShelter)
			.ToList();

		for (int i = 0; i < _leaderboardPanels.Count; i++)
		{

			_leaderboardPanels[i].Place = i + 1;
			_leaderboardPanels[i].PlaceUI.text = "" + _leaderboardPanels[i].Place;
			_leaderboardPanels[i].PointsTextUpdate();
			
			if(_leaderboardPanels[i].CurrentShelter == null) return;
			_leaderboardPanels[i].CurrentShelter.Place = _leaderboardPanels[i].Place;
		}
	}

	private void UpdatePanelOrderInHierarchy()
	{
		for (int i = 0; i < _leaderboardPanels.Count; i++)
		{
			_leaderboardPanels[i].transform.SetSiblingIndex(i);
		}
	}
	#endregion
}
