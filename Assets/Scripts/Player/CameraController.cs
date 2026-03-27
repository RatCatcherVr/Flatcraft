using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    [Header("Targeting Values")]
    public float dampTime = 0.15f;
    public Transform target;

    [Header("Transform Values")]
    public Vector2 defaultOffset;

    [Header("Shake Values")]
    public float shakeDropoffFactorPerFrame = 0.8f;
    public float shakeSpeed = 0.8f;
    public float maxOffset = 0.5f;
    public float maxRoll = 10;

    [HideInInspector] public float currentShake;

    private float _currentRoll;
    private Vector3 _currentTargetSmoothVelocity;
    private Vector2 _currentShakeOffset;

    private void Start()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        CalculateShakeThisFrame();
        SetTranslation();
    }

    private void CalculateShakeThisFrame()
    {
        _currentRoll = maxRoll * currentShake * (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f);
        _currentShakeOffset.x = maxOffset * currentShake * (Mathf.PerlinNoise(100f, Time.time * shakeSpeed) - 0.5f);
        _currentShakeOffset.y = maxOffset * currentShake * (Mathf.PerlinNoise(200f, Time.time * shakeSpeed) - 0.5f);

        currentShake *= shakeDropoffFactorPerFrame;
        if (currentShake < 0.001f) currentShake = 0;
    }

    private void SetTranslation()
    {
        Vector3 targetPosition = target.position + new Vector3(0, 0, -10);
        targetPosition += (Vector3)defaultOffset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentTargetSmoothVelocity, dampTime);
        transform.position += (Vector3)_currentShakeOffset;
        transform.rotation = Quaternion.Euler(0, 0, _currentRoll);
    }

    public static void ShakeClientCamera(float shakeValue)
    {
        instance.currentShake = shakeValue;
    }
}