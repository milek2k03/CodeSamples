using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardPanel : MonoBehaviour, IServiceLocatorComponent
{
	public ServiceLocator MyServiceLocator { get; set; }
	[ServiceLocatorComponent] private LeaderboardManager _leaderboardManager;

	public int Place { get; set; }

	public TextMeshProUGUI PlaceUI;
	public Image Panel;
	
	[SerializeField] private Image _image;
	[SerializeField] private TextMeshProUGUI _points;
	[SerializeField] private TextMeshProUGUI _name;

	public Shelter CurrentShelter { get; private set; } = null;

	private void OnEnable() => Refresh();
	
	public void Initialize(Shelter shelter)
	{
		if (shelter == null) return;
		if (CurrentShelter != null)
		{
			CurrentShelter.OnMyShelterIconChanged -= UpdateIcon;
			CurrentShelter.OnMyShelterNameChanged -= UpdateName;
			CurrentShelter.OnMyShelterPointsChanged -= PointsTextUpdate;
			_image.sprite = null;
			_points.text = "0";
			_name.text = "Empty";
		}


		CurrentShelter = shelter;
		CurrentShelter.OnMyShelterIconChanged += UpdateIcon;
		CurrentShelter.OnMyShelterNameChanged += UpdateName;
		CurrentShelter.OnMyShelterPointsChanged += PointsTextUpdate;
		Panel.color = CurrentShelter.Color;

		Refresh();
		
	}

	
	public void Refresh()
	{
		if (CurrentShelter == null) return;
		UpdateIcon();
		UpdateName();
		PointsTextUpdate();
	}

	private void UpdateIcon() => _image.sprite = CurrentShelter.Icon;
	
	private void UpdateName()
	{
		if (_name == null) return;

		if (CurrentShelter.IsMyShelter)
		{
			if (_leaderboardManager == null) return;

			if (_leaderboardManager.MyShelter.MyShelterName == null || _leaderboardManager.MyShelter.MyShelterName == string.Empty)
				_name.text = "";
			else
				_name.text = _leaderboardManager.MyShelter.MyShelterName.ToString();
		}
		else
		{
			if (CurrentShelter.Name == null) return;
			_name.text = CurrentShelter.Name.ToString();
		}
	}

	public void PointsTextUpdate()
	{
		if (CurrentShelter == null) return;
		_points.text = CurrentShelter.Points + ".";
	}

}
