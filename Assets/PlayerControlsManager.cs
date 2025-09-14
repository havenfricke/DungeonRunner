using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlsManager : MonoBehaviour
{
    private InputAction attack;
    private PlayerInput input;
    private Animator animator;
    private float attackSpeed = 0.1f;

    private void Start()
    {
        input = GetComponent<PlayerInput>();
        animator = GetComponentInChildren<Animator>();
        attack = input.actions["Attack"];
    }

    void Update()
    {
        ReadAttackInput();
    }

    void ReadAttackInput()
    {
        if (attack.triggered)
        {
            animator.SetBool("Attack", true);
            StartDelayedExecution(attackSpeed);
        }
    }

    public void StartDelayedExecution(float delayTime)
    {
        StartCoroutine(ExecuteAfterDelay(delayTime));
    }

    IEnumerator ExecuteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FinishAttackInput();
    }

    // Example function to be executed after the delay
    void FinishAttackInput()
    {
        animator.SetBool("Attack", false);
    }
}
