using UnityEngine;

public class EnvironmentReactionManager : MonoBehaviour
{
    [Header("Main controlled light")]
    public Light mainLight;

    [Header("Optional accent lights")]
    public Light warmAccentLight;
    public Light coolAccentLight;

    [Header("Transition")]
    public float transitionSpeed = 2.0f;

    private Color targetMainColor;
    private float targetMainIntensity;

    private float targetWarmAccentIntensity;
    private float targetCoolAccentIntensity;

    private void Start()
    {
        SetGestureMood("neutral");
    }

    private void Update()
    {
        if (mainLight != null)
        {
            mainLight.color = Color.Lerp(
                mainLight.color,
                targetMainColor,
                Time.deltaTime * transitionSpeed
            );

            mainLight.intensity = Mathf.Lerp(
                mainLight.intensity,
                targetMainIntensity,
                Time.deltaTime * transitionSpeed
            );
        }

        if (warmAccentLight != null)
        {
            warmAccentLight.intensity = Mathf.Lerp(
                warmAccentLight.intensity,
                targetWarmAccentIntensity,
                Time.deltaTime * transitionSpeed
            );
        }

        if (coolAccentLight != null)
        {
            coolAccentLight.intensity = Mathf.Lerp(
                coolAccentLight.intensity,
                targetCoolAccentIntensity,
                Time.deltaTime * transitionSpeed
            );
        }

        // Temporary keyboard testing
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetGestureMood("thinking");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetGestureMood("amused");
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetGestureMood("impressed");
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetGestureMood("nervous");
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetGestureMood("defeated");
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetGestureMood("no");
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetGestureMood("yes");
        if (Input.GetKeyDown(KeyCode.Alpha8)) SetGestureMood("reluctant");
    }

    public void SetGestureMood(string gesture)
    {
        gesture = gesture.ToLower().Trim();

        switch (gesture)
        {
            case "thinking":
                SetLighting(
                    new Color(0.85f, 0.88f, 1.0f),
                    0.9f,
                    0.1f,
                    0.2f
                );
                break;

            case "amused":
                SetLighting(
                    new Color(1.0f, 0.72f, 0.38f),
                    1.35f,
                    0.6f,
                    0.0f
                );
                break;

            case "impressed":
                SetLighting(
                    new Color(1.0f, 0.82f, 0.48f),
                    1.55f,
                    0.8f,
                    0.0f
                );
                break;

            case "nervous":
                SetLighting(
                    new Color(0.45f, 0.65f, 1.0f),
                    0.75f,
                    0.0f,
                    0.6f
                );
                break;

            case "defeated":
                SetLighting(
                    new Color(0.35f, 0.45f, 0.75f),
                    0.55f,
                    0.0f,
                    0.7f
                );
                break;

            case "no":
                SetLighting(
                    new Color(0.72f, 0.78f, 0.95f),
                    0.9f,
                    0.0f,
                    0.25f
                );
                break;

            case "yes":
                SetLighting(
                    new Color(1.0f, 0.88f, 0.65f),
                    1.15f,
                    0.35f,
                    0.0f
                );
                break;

            case "reluctant":
                SetLighting(
                    new Color(0.62f, 0.68f, 0.82f),
                    0.75f,
                    0.0f,
                    0.35f
                );
                break;

            default:
                SetLighting(
                    Color.white,
                    1.0f,
                    0.0f,
                    0.0f
                );
                break;
        }
    }

    private void SetLighting(
        Color mainColor,
        float mainIntensity,
        float warmAccentIntensity,
        float coolAccentIntensity
    )
    {
        targetMainColor = mainColor;
        targetMainIntensity = mainIntensity;
        targetWarmAccentIntensity = warmAccentIntensity;
        targetCoolAccentIntensity = coolAccentIntensity;
    }
}


