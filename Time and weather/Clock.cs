using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Clock : MonoBehaviour, IAwake, IServiceLocatorComponent
{
    [Header("Time Scale")]
    [Range(0, 100)] public float TimeScale;

    [Header("Time Change")]
    [Range(0, WorldTimeManager.HoursInDay)] public float TimeChange;

    [Header("Other")]
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _dayText;
    [SerializeField] private TMP_Text _partOfDay;
    [SerializeField] private GameObject _sliders;

    [Header("Managers")]
    [ServiceLocatorComponent(false, false)] private WorldTimeManager _worldTimeManager;
    [SerializeField] private CursorManager _cursorManager;
    [ServiceLocatorComponent] private PlayerManager _playermanager;
    private PlayerInputBlocker _playerInputBlocker;

    public ServiceLocator MyServiceLocator { get; set; }

    private bool _examplePanelEnabled = false;

    public void CustomAwake()
    {
        _worldTimeManager.IsNotNull(this, nameof(_worldTimeManager));
        _cursorManager.IsNotNull(this, nameof(_cursorManager));
        TimeScale = 1f;
        _playermanager.OnLocalPlayerSet += (PlayerServiceLocator localPlayer) => localPlayer.TryGetServiceLocatorComponent(out _playerInputBlocker);
    }

    void Start()
    {
        StartTime();
    }

    void Update()
    {
        DisplayTime();
        DisplayDays();
    }

    public float GetValueTimeScale()
    {
        return TimeScale;
    }

    public void UpdateValueTimeScale(float value)
    {
        if (value < 0) return;
        TimeScale = value;
    }

    public float GetValueTimeChange()
    {
        return TimeChange;
    }

    public void UpdateValueTimeChange(float value)
    {
        if (value < 0 || value > WorldTimeManager.HoursInDay) return;

        TimeChange = value;
    }

    private void StartTime()
    {
        _worldTimeManager.Days = 1;
        _worldTimeManager.Time = TimeChange;
    }

    private void DisplayTime()
    {
        if (_timerText == null) return;
        _timerText.text = string.Format("{0:00}:{1:00}", _worldTimeManager.Hour, _worldTimeManager.Minutes);
    }
    private void DisplayDays()
    {
        if (_dayText == null) return;
        _dayText.text = "Day: " + _worldTimeManager.Days;
    }

    private void ToggleCursorVisibility()
    {
        if (!Input.GetKeyDown(KeyCode.Z))
            return;

        if (!_examplePanelEnabled)
        {
            _cursorManager.ActivateCursor();
            _playerInputBlocker.Block(new(this));
            _sliders.SetActive(true);
        }
        else
        {
            if (!_playerInputBlocker.TryUnblock(this)) return;
            _cursorManager.DeactivateCursor();
            _sliders.SetActive(false);
        }

        _examplePanelEnabled = !_examplePanelEnabled;
    }

}
