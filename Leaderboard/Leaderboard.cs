using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour, IServiceLocatorComponent
{
	public ServiceLocator MyServiceLocator { get; set; }
	[ServiceLocatorComponent] private LeaderboardManager _leaderboardManager;
	
	[SerializeField] private List<LeaderboardUI> _leaderboardUIs;
	[SerializeField] private Button _regionalLeagueButton;
	[SerializeField] private Button _nationalLeagueButton;
	[SerializeField] private Button _worldLeagueButton;
	

	private void OnEnable() => _leaderboardManager.OnLeagueChanged += ActiveLeague;
	

	public void OnDisable()
	{
		_leaderboardManager.OnLeagueChanged -= ActiveLeague;
		_regionalLeagueButton.image.color = Color.white;
		_nationalLeagueButton.image.color = Color.white;
		_worldLeagueButton.image.color = Color.white;
	}

	public void ActiveLeague(LeagueType type)
	{
		foreach (var leaderboardUI in _leaderboardUIs)
		{
			if (leaderboardUI.Type == type)
				leaderboardUI.gameObject.SetActive(true);
			else
				leaderboardUI.gameObject.SetActive(false);
		}

		_regionalLeagueButton.image.color = Color.white;
		_nationalLeagueButton.image.color = Color.white;
		_worldLeagueButton.image.color = Color.white;
	}

	//Used by button
	public void ActiveRegionalLeague()
	{
		ActiveLeague(LeagueType.Regional);
		_regionalLeagueButton.image.color = _regionalLeagueButton.colors.selectedColor;
		_nationalLeagueButton.image.color = Color.white;
		_worldLeagueButton.image.color = Color.white;
	}

	//Used by button
	public void ActiveNationalLeague()
	{
		ActiveLeague(LeagueType.National);
		_regionalLeagueButton.image.color = Color.white;
		_nationalLeagueButton.image.color = _nationalLeagueButton.colors.selectedColor;
		_worldLeagueButton.image.color = Color.white;
	}

	//Used by button
	public void ActiveWorldLeague()
	{
		ActiveLeague(LeagueType.World);
		_regionalLeagueButton.image.color = Color.white;
		_nationalLeagueButton.image.color = Color.white;
		_worldLeagueButton.image.color = _worldLeagueButton.colors.selectedColor;
	}
}
