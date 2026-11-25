using UnityEngine;

/// <summary>
/// Controls the flying companion delivery pet drone ("Byte-Drone / Parcel-Bot")
/// that faithfully follows the courier through cyberspace while carrying the mandatory delivery payload.
/// Automatically hovers behind the player, reacts to sprint/dash with dynamic tilt and jet thrusters,
/// pulses urgency warnings when packet TTL is critical, and delivers the cargo at the end of the stage.
/// </summary>
public class PlayerBackpackPackage : MonoBehaviour
{
    private SpriteRenderer _playerSr;
    private GameObject _droneObj;
    private SpriteRenderer _droneSr;

    private static Sprite _droneNormalSprite;
    private static Sprite _droneFlameSprite;

    private Vector3 _currentVelocity;
    private float _flameTimer;
    private bool _flameState;
    private float _sparkTimer;
    private bool _wasCarrying;

    private void Awake()
    {
        _playerSr = GetComponentInChildren<SpriteRenderer>();
        LoadSprites();
    }

    private static void LoadSprites()
    {
        if (_droneNormalSprite == null)
            _droneNormalSprite = Resources.Load<Sprite>("GeneratedArt/packet_pet_drone") 
                              ?? Resources.Load<Sprite>("GeneratedArt/package_backpack");

        if (_droneFlameSprite == null)
            _droneFlameSprite = Resources.Load<Sprite>("GeneratedArt/packet_pet_drone_flame") 
                             ?? _droneNormalSprite;
    }

    private void EnsureDroneObject()
    {
        if (_droneObj != null) return;
        LoadSprites();

        _droneObj = new GameObject("DeliveryPetDrone");
        _droneSr = _droneObj.AddComponent<SpriteRenderer>();
        _droneSr.sprite = _droneNormalSprite;
        _droneSr.sortingOrder = 14; // Above characters and obstacles

        // Snap to player immediate vicinity
        bool flip = _playerSr != null && _playerSr.flipX;
        float xOffset = flip ? 0.75f : -0.75f;
        _droneObj.transform.position = transform.position + new Vector3(xOffset, 0.95f, 0f);
        _droneObj.SetActive(false);
    }

    private void LateUpdate()
    {
        EnsureDroneObject();

        var gm = GameManager.Instance;
        var quests = QuestService.Instance;
        int world = gm != null ? gm.currentWorldIndex : 0;
        bool carrying = quests != null && quests.GetPackageState(world) == PackageState.Active;

        if (!carrying)
        {
            if (_wasCarrying)
            {
                // Delivery burst effect on handoff
                if (_droneObj != null && _droneObj.activeSelf)
                {
                    JuiceManager.SpawnImpactSparks(_droneObj.transform.position, new Color(0.2f, 1f, 0.8f, 1f));
                    _droneObj.SetActive(false);
                }
                _wasCarrying = false;
            }
            else if (_droneObj.activeSelf)
            {
                _droneObj.SetActive(false);
            }
            return;
        }

        if (!_droneObj.activeSelf)
        {
            _droneObj.SetActive(true);
            bool flipInit = _playerSr != null && _playerSr.flipX;
            float xInit = flipInit ? 0.75f : -0.75f;
            _droneObj.transform.position = transform.position + new Vector3(xInit, 0.95f, 0f);
            _currentVelocity = Vector3.zero;
        }

        _wasCarrying = true;

        bool flip = _playerSr != null && _playerSr.flipX;
        float xOffset = flip ? 0.85f : -0.85f;
        float hoverBob = Mathf.Sin(Time.time * 4.2f) * 0.12f;
        Vector3 targetPos = transform.position + new Vector3(xOffset, 0.95f + hoverBob, 0f);

        // Snap if teleports or large distance jump (e.g. checkpoint respawn)
        if (Vector3.Distance(_droneObj.transform.position, targetPos) > 10f)
        {
            _droneObj.transform.position = targetPos;
            _currentVelocity = Vector3.zero;
        }
        else
        {
            _droneObj.transform.position = Vector3.SmoothDamp(_droneObj.transform.position, targetPos, ref _currentVelocity, 0.12f);
        }

        // Dynamic tilt / banking based on horizontal movement
        float targetTilt = Mathf.Clamp(-_currentVelocity.x * 3.5f, -22f, 22f);
        float currentAngle = _droneObj.transform.eulerAngles.z;
        float smoothedAngle = Mathf.LerpAngle(currentAngle, targetTilt, Time.deltaTime * 14f);
        _droneObj.transform.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);

        // Face player movement / orientation
        _droneSr.flipX = flip;

        // Animated thruster flame
        _flameTimer += Time.deltaTime;
        float speed = _currentVelocity.magnitude;
        float flameInterval = speed > 2.0f ? 0.08f : 0.16f;
        if (_flameTimer >= flameInterval)
        {
            _flameTimer = 0f;
            _flameState = !_flameState;
            _droneSr.sprite = (_flameState || speed > 3.0f) ? _droneFlameSprite : _droneNormalSprite;
        }

        // Particle sparks on rapid sprint / dash
        if (speed > 3.5f)
        {
            _sparkTimer += Time.deltaTime;
            if (_sparkTimer >= 0.18f)
            {
                _sparkTimer = 0f;
                Vector3 nozzlePos = _droneObj.transform.position - new Vector3(0f, 0.22f, 0f);
                JuiceManager.SpawnImpactSparks(nozzlePos, new Color(0.2f, 0.95f, 1f, 0.8f));
            }
        }

        // Urgency health warning feedback: pulsing red/amber when TTL is critically low (< 30%)
        var health = PlayerHealth.Instance;
        if (health != null && health.ttlCurrent < health.ttlMax * 0.3f)
        {
            float pulse = 0.5f + 0.5f * Mathf.PingPong(Time.time * 6f, 1f);
            _droneSr.color = new Color(1f, pulse, pulse, 1f);
        }
        else
        {
            _droneSr.color = Color.white;
        }
    }

    private void OnDisable()
    {
        if (_droneObj != null) _droneObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_droneObj != null) Destroy(_droneObj);
    }
}
