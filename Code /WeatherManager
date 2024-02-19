using Mirror;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public enum WeatherType
{
    Clear,
    Streaky,
    StreakyDense,
    Sparse,
    CloudyLight,
    CloudyDense,
    Overcast,
    Rainy,
    Stormy
};

[System.Serializable]
public class WeatherParameters
{
    [Header("General Settings")]
    public WeatherType Weather;

    [Header("Cloud Layer")]
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 Streaky = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 StreakyDense = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 Sparce = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 CloudyLight = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 CloudyDense = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 Overcast = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 Rainy = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 Stormy = new Vector2(0.0f, 1.0f);

    [Header("Volumetric Clouds")]
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 VolumetricSparce = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 VolumetricCloudy = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 VolumetricOvercast = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 VolumetricOvercastThin = new Vector2(0.0f, 1.0f);
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 VolumetricStormy = new Vector2(0.0f, 1.0f);

    [Header("Lighting Settings")]
    [MinMaxSlider(0.0f, 130000.0f, true)]
    public Vector2 SunIntensity = new Vector2(4000.0f, 13000.0f);

    [Header("Wind Settings")]
    [MinMaxSlider(0.0f, 1.0f, true)]
    public Vector2 WindStrength = new Vector2(0.5f, 1.0f);
    [HideInInspector] public float WindRotation = 0.0f;
}

public class WeatherManager : NetworkBehaviour, IStartable, IUpdateable, IServiceLocatorComponent
{
    public ServiceLocator MyServiceLocator { get; set; }
    [SyncVar] public float WeatherStrength;

    [SyncVar] public float WindStrength;
    [SyncVar] public float WindRotation;
    [SyncVar] public float WeatherChangeProgress;

    public Transform _windRotation;

    public event System.Action<WeatherType> OnWeatherChange;

    [ServiceLocatorComponent] private WorldTimeManager _worldTimeManager;

    [Header("Rain")]
    //[SerializeField] private Rain rain;
    [SerializeField] public bool _rainExperimental = false;

    [Header("Sun and Moon")]
    public Light sun;
    public Light moon;
    [SerializeField] private AnimationCurve _lightIntensityModifierUp = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [SerializeField] private AnimationCurve _lightIntensityModifierDown = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    [Header("Sky Volume")]
    public Volume skyVolume;
    private Fog skyControll;

    [Header("Weather Simple Clouds")]
    public bool UseVolumetricClouds = false;
    [SerializeField, HideInEditorMode] public WeatherType currentWeather = WeatherType.Clear;
    [SerializeField] private WeatherParameters[] _weatherPresets;
    private WeatherType oldWeather;


    [SerializeField] private float _weatherChangeDuration = 1.5f;

    [Header("Cloud Layer")]
    public Volume cloudClearVolume;
    private float cloudClearWeight = 1.0f;
    public Volume cloudStreakyVolume;
    public Volume cloudStreakyDenseVolume;
    public Volume cloudSparceVolume;
    public Volume cloudCloudyLightVolume;
    public Volume cloudCloudyDenseVolume;
    public Volume cloudOvercastVolume;
    public Volume cloudRainyVolume;
    public Volume cloudStormyVolume;
    [SerializeField][Range(0, 1)] private float cloudStreakyWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudStreakyDenseWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudSparceWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudCloudyLightWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudCloudyDenseWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudOvercastWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudRainyWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudStormyWeight = 0.0f;

    [Header("Volumetric Clouds ")]
    public Volume cloudVolumetricClearVolume;
    private float cloudVolumetricClearWeight = 1.0f;
    public Volume cloudVolumetricSparseVolume;
    public Volume cloudVolumetricCloudyVolume;
    public Volume cloudVolumetricOvercastVolume;
    public Volume cloudVolumetricOvercastThinVolume;
    public Volume cloudVolumetricStormyVolume;
    [SerializeField][Range(0, 1)] private float cloudVolumetricSparceWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudVolumetricCloudyWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudVolumetricOvercastWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudVolumetricOvercastThinWeight = 0.0f;
    [SerializeField][Range(0, 1)] private float cloudVolumetricStormyWeight = 0.0f;

    [Header("Time to Change Weather")]
    [SerializeField][Range(0, 1000)] private float timeToChangeWeatherMin = 10.0f;
    [SerializeField][Range(0, 1000)] private float timeToChangeWeatherMax = 16.0f;

    [SyncVar]WeatherParameters _oldWeatherSettings = new WeatherParameters();

    [SyncVar]WeatherParameters _currentWeatherSettings = new WeatherParameters();


    [SyncVar] private float _weatherChangeDelay = 2.0f;


    // Start is called before the first frame update
    public void CustomStart()
    {
        skyVolume.profile.TryGet(out skyControll);
        oldWeather = WeatherType.Clear;
        currentWeather = WeatherType.Clear;
        sun.intensity = 100000;
        moon.intensity = sun.intensity;
        _oldWeatherSettings = _weatherPresets.Where(w => w.Weather == currentWeather).FirstOrDefault();
        if (_oldWeatherSettings == null) _oldWeatherSettings = new();
        _currentWeatherSettings = _weatherPresets.Where(w => w.Weather == currentWeather).FirstOrDefault();
        if (_currentWeatherSettings == null) _currentWeatherSettings = new();

    }

    public bool Enabled => true;

    public void CustomUpdate()
    {

        UpdateWeather();

        /*
        if (Input.GetKeyDown(KeyCode.F))
        {
            _useVolumetricClouds = !_useVolumetricClouds;
            SelectWeather();
        }*/
    }

    private void UpdateWeather()
    {
        UpdateWeatherProgress(); //4 ?
        ApplyCloudsWeights(); //5
        TrySetNewWeather(); //2
        TryUpdateWeatherParameters(); //3 ?
        TrySelectNewWeather(); //1
    }

    private void TrySelectNewWeather()
    {
        if (!NetworkServer.active) return;

        _weatherChangeDelay -= _worldTimeManager.DeltaTime;
        if (_weatherChangeDelay <= 0)
        {
            SelectWeather();
            _weatherChangeDelay = Random.Range(timeToChangeWeatherMin, timeToChangeWeatherMax);
            WeatherChangeProgress = 0;
        }
    }

    private void TryUpdateWeatherParameters()
    {
        if (_currentWeatherSettings != null && WeatherChangeProgress <= 1)
        {
            if (!UseVolumetricClouds)
            {
                UpdateSimpleClouds();
            }
            else
            {
                UpdateVolumetricClouds();
            }

            UpdateWind();
        }
    }

    private void UpdateVolumetricClouds()
    {
        cloudStreakyWeight = 0;
        cloudStreakyDenseWeight = 0;
        cloudSparceWeight = 0;
        cloudCloudyLightWeight = 0;
        cloudCloudyDenseWeight = 0;
        cloudOvercastWeight = 0;
        cloudRainyWeight = 0;
        cloudStormyWeight = 0;

        //reseting sun intensity 
        sun.intensity = 100000;

        cloudVolumetricSparceWeight = Mathf.Lerp(_oldWeatherSettings.VolumetricSparce.y, _currentWeatherSettings.VolumetricSparce.y, WeatherChangeProgress);
        cloudVolumetricCloudyWeight = Mathf.Lerp(_oldWeatherSettings.VolumetricCloudy.y, _currentWeatherSettings.VolumetricCloudy.y, WeatherChangeProgress);
        cloudVolumetricOvercastWeight = Mathf.Lerp(_oldWeatherSettings.VolumetricOvercast.y, _currentWeatherSettings.VolumetricOvercast.y, WeatherChangeProgress);
        cloudVolumetricOvercastThinWeight = Mathf.Lerp(_oldWeatherSettings.VolumetricOvercastThin.y, _currentWeatherSettings.VolumetricOvercastThin.y, WeatherChangeProgress);
        cloudVolumetricStormyWeight = Mathf.Lerp(_oldWeatherSettings.VolumetricStormy.y, _currentWeatherSettings.VolumetricStormy.y, WeatherChangeProgress);

        sun.GetComponent<HDAdditionalLightData>().affectsVolumetric = true;
        moon.GetComponent<HDAdditionalLightData>().affectsVolumetric = true;

        skyControll.enableVolumetricFog.overrideState = true;
        skyControll.enableVolumetricFog.value = true;
    }

    private void UpdateSimpleClouds()
    {
        cloudStreakyWeight = Mathf.Lerp(_oldWeatherSettings.Streaky.y, _currentWeatherSettings.Streaky.y, WeatherChangeProgress);
        cloudStreakyDenseWeight = Mathf.Lerp(_oldWeatherSettings.StreakyDense.y, _currentWeatherSettings.StreakyDense.y, WeatherChangeProgress);
        cloudSparceWeight = Mathf.Lerp(_oldWeatherSettings.Sparce.y, _currentWeatherSettings.Sparce.y, WeatherChangeProgress);
        cloudCloudyLightWeight = Mathf.Lerp(_oldWeatherSettings.CloudyLight.y, _currentWeatherSettings.CloudyLight.y, WeatherChangeProgress);
        cloudCloudyDenseWeight = Mathf.Lerp(_oldWeatherSettings.CloudyDense.y, _currentWeatherSettings.CloudyDense.y, WeatherChangeProgress);
        cloudOvercastWeight = Mathf.Lerp(_oldWeatherSettings.Overcast.y, _currentWeatherSettings.Overcast.y, WeatherChangeProgress);
        cloudRainyWeight = Mathf.Lerp(_oldWeatherSettings.Rainy.y, _currentWeatherSettings.Rainy.y, WeatherChangeProgress);
        cloudStormyWeight = Mathf.Lerp(_oldWeatherSettings.Stormy.y, _currentWeatherSettings.Stormy.y, WeatherChangeProgress);

        if (_oldWeatherSettings.SunIntensity.y > _currentWeatherSettings.SunIntensity.y)
        {
            sun.intensity = _oldWeatherSettings.SunIntensity.y + ((_currentWeatherSettings.SunIntensity.y - _oldWeatherSettings.SunIntensity.y) * _lightIntensityModifierUp.Evaluate(WeatherChangeProgress));
        }
        else
        {
            sun.intensity = _oldWeatherSettings.SunIntensity.y + ((_currentWeatherSettings.SunIntensity.y - _oldWeatherSettings.SunIntensity.y) * _lightIntensityModifierDown.Evaluate(WeatherChangeProgress));
        }

        moon.intensity = Mathf.Lerp(_oldWeatherSettings.SunIntensity.y, _currentWeatherSettings.SunIntensity.y, WeatherChangeProgress);

        cloudVolumetricSparceWeight = 0;
        cloudVolumetricCloudyWeight = 0;
        cloudVolumetricOvercastWeight = 0;
        cloudVolumetricOvercastThinWeight = 0;
        cloudVolumetricStormyWeight = 0;

        sun.GetComponent<HDAdditionalLightData>().affectsVolumetric = false;
        moon.GetComponent<HDAdditionalLightData>().affectsVolumetric = false;

        skyControll.enableVolumetricFog.overrideState = false;
        skyControll.enableVolumetricFog.value = false;
    }

    private void UpdateWind()
    {
        //TODO implement wind 
        //WindStrength = Mathf.Lerp(_oldWeatherSettings.WindStrength.y, _currentWeatherSettings.WindStrength.y, WeatherChangeProgress);
        //WindRotation = Mathf.Lerp(_oldWeatherSettings.WindRotation, _currentWeatherSettings.WindRotation, WeatherChangeProgress);
        //_windRotation.localRotation = Quaternion.Euler(0.0f, WindRotation, 0.0f);
    }

    private void TrySetNewWeather()
    {
        if (oldWeather != currentWeather)
        {
            if (!NetworkServer.active) return;

            WeatherStrength = Random.value;
            _oldWeatherSettings = new WeatherParameters()
            {
                Weather = oldWeather,
                Streaky = new Vector2(cloudStreakyWeight, cloudStreakyWeight),
                StreakyDense = new Vector2(cloudStreakyDenseWeight, cloudStreakyDenseWeight),
                Sparce = new Vector2(cloudSparceWeight, cloudSparceWeight),
                CloudyLight = new Vector2(cloudCloudyLightWeight, cloudCloudyLightWeight),
                CloudyDense = new Vector2(cloudCloudyDenseWeight, cloudCloudyDenseWeight),
                Overcast = new Vector2(cloudOvercastWeight, cloudOvercastWeight),
                Rainy = new Vector2(cloudRainyWeight, cloudRainyWeight),
                Stormy = new Vector2(cloudStormyWeight, cloudStormyWeight),

                VolumetricSparce = new Vector2(cloudVolumetricSparceWeight, cloudVolumetricSparceWeight),
                VolumetricCloudy = new Vector2(cloudVolumetricCloudyWeight, cloudVolumetricCloudyWeight),
                VolumetricOvercast = new Vector2(cloudVolumetricOvercastWeight, cloudVolumetricOvercastWeight),
                VolumetricOvercastThin = new Vector2(cloudVolumetricOvercastThinWeight, cloudVolumetricOvercastThinWeight),
                VolumetricStormy = new Vector2(cloudVolumetricStormyWeight, cloudVolumetricStormyWeight),

                SunIntensity = new Vector2(sun.intensity, sun.intensity),

                WindStrength = new Vector2(WindStrength, WindStrength),
                WindRotation = WindRotation,

            };

            _currentWeatherSettings = _weatherPresets.Where(w => w.Weather == currentWeather).FirstOrDefault();
            _currentWeatherSettings = new WeatherParameters()
            {
                Weather = _currentWeatherSettings.Weather,
                Streaky = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.Streaky.x, _currentWeatherSettings.Streaky.y),
                StreakyDense = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.StreakyDense.x, _currentWeatherSettings.StreakyDense.y),
                Sparce = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.Sparce.x, _currentWeatherSettings.Sparce.y),
                CloudyLight = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.CloudyLight.x, _currentWeatherSettings.CloudyLight.y),
                CloudyDense = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.CloudyDense.x, _currentWeatherSettings.CloudyDense.y),
                Overcast = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.Overcast.x, _currentWeatherSettings.Overcast.y),
                Rainy = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.Rainy.x, _currentWeatherSettings.Rainy.y),
                Stormy = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.Stormy.x, _currentWeatherSettings.Stormy.y),

                VolumetricSparce = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.VolumetricSparce.x, _currentWeatherSettings.VolumetricSparce.y),
                VolumetricCloudy = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.VolumetricCloudy.x, _currentWeatherSettings.VolumetricCloudy.y),
                VolumetricOvercast = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.VolumetricOvercast.x, _currentWeatherSettings.VolumetricOvercast.y),
                VolumetricOvercastThin = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.VolumetricOvercastThin.x, _currentWeatherSettings.VolumetricOvercastThin.y),
                VolumetricStormy = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.VolumetricStormy.x, _currentWeatherSettings.VolumetricStormy.y),

                SunIntensity = new Vector2(_currentWeatherSettings.SunIntensity.x, _currentWeatherSettings.SunIntensity.y),

                WindStrength = CalculateParameterStrength(WeatherStrength, _currentWeatherSettings.WindStrength.x, _currentWeatherSettings.WindStrength.y),
                WindRotation = Random.Range(0.0f, 360.0f),
            };
            oldWeather = currentWeather;
            WeatherChangeProgress = 0.0f;

            OnWeatherChange?.Invoke(currentWeather);
        }
    }

    private void ApplyCloudsWeights()
    {
        cloudClearVolume.weight = cloudClearWeight;
        cloudStreakyVolume.weight = cloudStreakyWeight;
        cloudStreakyDenseVolume.weight = cloudStreakyDenseWeight;
        cloudSparceVolume.weight = cloudSparceWeight;
        cloudCloudyLightVolume.weight = cloudCloudyLightWeight;
        cloudCloudyDenseVolume.weight = cloudCloudyDenseWeight;
        cloudOvercastVolume.weight = cloudOvercastWeight;
        cloudRainyVolume.weight = cloudRainyWeight;
        cloudStormyVolume.weight = cloudStormyWeight;

        cloudVolumetricClearVolume.weight = cloudVolumetricClearWeight;
        cloudVolumetricSparseVolume.weight = cloudVolumetricSparceWeight;
        cloudVolumetricCloudyVolume.weight = cloudVolumetricCloudyWeight;
        cloudVolumetricOvercastVolume.weight = cloudVolumetricOvercastWeight;
        cloudVolumetricOvercastThinVolume.weight = cloudVolumetricOvercastThinWeight;
        cloudVolumetricStormyVolume.weight = cloudVolumetricStormyWeight;
    }

    private void UpdateWeatherProgress()
    {
        float weatherChangeRate = 1.0f / _weatherChangeDuration;

        //In this half-hour window we don't change weather, becauce simultaneous change in sun intensity in this window causes weird graphical glitch when changing from day to night
        if (_worldTimeManager.Time > 18.6f || _worldTimeManager.Time <= 17.9f)
        {
            WeatherChangeProgress += weatherChangeRate * _worldTimeManager.DeltaTime;
            WeatherChangeProgress = Mathf.Clamp01(WeatherChangeProgress);
        }
    }

    private Vector2 CalculateParameterStrength(float weatherStrength, float min, float max)
    {
        return new Vector2(1, 1) * (max + (weatherStrength * (max - min)) - (max - min));
    }


    [Button("Roll Weather")]
    private void SelectWeather()
    {
        WeatherType newWeather;

        do
        {
            newWeather = (WeatherType)Random.Range(0, 8);
        } while (newWeather == currentWeather);
        currentWeather = newWeather;
    }

}
