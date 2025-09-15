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

    [Header("Mouse Aim")]
    public bool useMouseAim = true;              // enable/disable mouse-driven facing
    public LayerMask aimLayerMask = ~0;          // what the aim ray can hit (e.g., Ground)
    public float rayMaxDistance = 200f;          // how far the aim ray goes
    public float fallbackPlaneY = 0f;            // if no collider hit, intersect a Y=constant plane

    private CharacterController controller;
    private PlayerInput input;
    private InputAction moveAction;
    private InputAction lookAction;              // still used for right-stick / mouse-delta if desired
    private Transform cam;
    private Animator anim;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
        moveAction = input.actions["Move"];
        lookAction = input.actions["Look"];
        anim = GetComponentInChildren<Animator>();
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

        // ---- ROTATE (Mouse > RS > Move) ----
        Vector3 faceDir = Vector3.zero;

        // 1) Mouse cursor facing (highest priority)
        if (useMouseAim && TryGetMouseAimDirection(out var mouseDir))
        {
            faceDir = mouseDir;
        }
        // 2) Right stick / Look vector
        else if (lookDir.sqrMagnitude > lookDeadzone * lookDeadzone)
        {
            faceDir = lookDir;
        }
        // 3) Move direction
        else if (moveDir.sqrMagnitude > moveDeadzone * moveDeadzone)
        {
            faceDir = moveDir;
        }

        if (faceDir != Vector3.zero)
        {
            Quaternion target = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // ---- ANIMATOR ----
        // Feed the animator with the actual facing choice (mouse > RS > move)
        UpdateAnimatorValues(moveDir, faceDir);
    }

    private void UpdateAnimatorValues(Vector3 moveWorld, Vector3 faceWorld)
    {
        bool hasMove = moveWorld.sqrMagnitude > moveDeadzone * moveDeadzone;

        // Speed from left stick magnitude (0..1)
        float speed = Mathf.Clamp01((moveAction?.ReadValue<Vector2>() ?? Vector2.zero).magnitude);

        // Movement direction relative to CURRENT facing (local space)
        Vector3 localMove = hasMove ? transform.InverseTransformDirection(moveWorld.normalized) : Vector3.zero;

        // Optional: backward flag (kept from your structure if you want it)
        // float alignment = hasMove ? Vector3.Dot(moveWorld.normalized, transform.forward) : 0f;
        // bool movingBackward = hasMove && alignment < -0.35f;

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

    // --- Mouse-to-world aim helper ---
    private bool TryGetMouseAimDirection(out Vector3 dir)
    {
        dir = Vector3.zero;
        if (cam == null) return false;

        // Support Mouse or Touch (first touch) — if you only target desktop, Mouse is enough
        Vector2 screenPos = Vector2.zero;
        if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            screenPos = Touchscreen.current.touches[0].position.ReadValue();
        else
            return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // Prefer hitting real colliders (e.g., ground/navmesh)
        if (Physics.Raycast(ray, out var hit, rayMaxDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 v = hit.point - transform.position;
            v.y = 0f;
            if (v.sqrMagnitude > 0.0001f) { dir = v.normalized; return true; }
        }
        else
        {
            // Fallback: intersect an infinite horizontal plane at Y = fallbackPlaneY
            Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);
                Vector3 v = point - transform.position;
                v.y = 0f;
                if (v.sqrMagnitude > 0.0001f) { dir = v.normalized; return true; }
            }
        }

        return false;
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
