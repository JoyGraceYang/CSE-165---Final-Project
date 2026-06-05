using UnityEngine;

public class SimpleOVRJoystickMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float turnSpeed = 80f;

    [Header("References")]
    public Transform head; // Assign CenterEyeAnchor here

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    private void Update()
    {
        if (head == null) return;

        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        Vector3 forward = head.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = head.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection = forward * leftStick.y + right * leftStick.x;

        characterController.SimpleMove(moveDirection * moveSpeed);

        transform.Rotate(
            Vector3.up,
            rightStick.x * turnSpeed * Time.deltaTime
        );
    }
}

