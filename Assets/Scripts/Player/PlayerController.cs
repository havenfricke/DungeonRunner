using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController)), RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float rotationSpeed = 5f;
    public bool cameraRelative = false;

    [Header("Animation")]
    public float animDamp = 0.1f;      // damping for SetFloat smoothing
    public float moveDeadzone = 0.04f; // LS deadzone
    public float lookDeadzone = 0.04f; // RS deadzone

    private CharacterController controller;
    private PlayerInput input;
    private InputAction moveAction;
    private InputAction lookAction;
    private Transform cam;
    private Animator anim;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
        moveAction = input.actions["Move"];
        lookAction = input.actions["Look"];
        anim = GetComponentInChildren<Animator>(); // or GetComponent<Animator>()
        if (Camera.main) cam = Camera.main.transform;
    }

    void Update()
    {
        Vector2 move2D = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector2 look2D = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        Vector3 moveDir = StickToWorld(move2D);
        Vector3 lookDir = StickToWorld(look2D);

        // ---- MOVE ----
        if (moveDir.sqrMagnitude > moveDeadzone * moveDeadzone)
            controller.Move(moveDir * moveSpeed * Time.deltaTime);

        // ---- ROTATE (RS overrides) ----
        Vector3 faceDir = Vector3.zero;
        if (lookDir.sqrMagnitude > lookDeadzone * lookDeadzone) faceDir = lookDir;
        else if (moveDir.sqrMagnitude > moveDeadzone * moveDeadzone) faceDir = moveDir;

        if (faceDir != Vector3.zero)
        {
            Quaternion target = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // ---- ANIMATOR ----
        UpdateAnimatorValues(moveDir, lookDir);
    }

    private void UpdateAnimatorValues(Vector3 moveWorld, Vector3 lookWorld)
    {
        bool hasMove = moveWorld.sqrMagnitude > moveDeadzone * moveDeadzone;
        bool hasLook = lookWorld.sqrMagnitude > lookDeadzone * lookDeadzone;

        // Speed from left stick magnitude (0..1)
        float speed = Mathf.Clamp01((moveAction?.ReadValue<Vector2>() ?? Vector2.zero).magnitude);

        // Movement direction relative to CURRENT facing (local space)
        // If you're aiming opposite while moving, local z will be negative -> backward run.
        Vector3 localMove = hasMove ? transform.InverseTransformDirection(moveWorld.normalized) : Vector3.zero;

        // Optional: explicit backward flag based on move vs face alignment
        float alignment = 0f;
        if (hasMove)
        {
            Vector3 face = transform.forward; // after rotation above
            alignment = Vector3.Dot(moveWorld.normalized, face); // < 0 means moving opposite facing
        }
        bool movingBackward = hasMove && hasLook && alignment < -0.35f; // tweak threshold to taste

        // Feed the blend tree + helpers (smoothed)
        anim.SetFloat("Speed", speed, animDamp, Time.deltaTime);
        anim.SetFloat("MoveX", localMove.x, animDamp, Time.deltaTime);
        anim.SetFloat("MoveY", localMove.z, animDamp, Time.deltaTime);
    }

    private Vector3 StickToWorld(Vector2 stick)
    {
        if (stick.sqrMagnitude < 0.0001f) return Vector3.zero;

        if (!cameraRelative || cam == null)
            return new Vector3(stick.x, 0f, stick.y).normalized;

        // Camera-relative XZ
        Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();
        return (fwd * stick.y + right * stick.x).normalized;
    }
}

// Small helper so SetBool checks don’t throw if param doesn’t exist
public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in self.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
