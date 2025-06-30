using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using NUnit.Framework.Constraints;

[System.Flags]
public enum PlayerState
{
    None = 0x0,
    Normal = 0x1,
    GroundJump = 0x2,
    Jump = 0x4,
    SecondJump = 0x8, // Second jump
    Dash = 0x10,
    Attack = 0x20,
    DashAttack = 0x40, // Added new state for DashA
    UpAttack = 0x80,
    SideAAttack = 0x100, // Added new state for SideAA
    UpAAttack = 0x200, // Added new state for UpAA
    DownAAttack = 0x400, // Added new state for DownAA
    IsBlocking = 0x1000,
    JumpOnCooldown = 0x2000,
    DashOnCooldown = 0x4000,
    AAOnCooldown = 0x8000
}



public class Igrac : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D myRigidbody2D;
    public float walkSpeed = 5f;
    public float jumpSpeed = 8f;
    public float jumpCooldown = 0.1f;
    public float dashSpeed = 10f;
    public float dashCooldown = 0.2f;
    public float dashDuration = 0.2f; // Duration of the dash
    public float dashADuration = 0.5f; // Duration of DashA (set in Inspector)

    public float attackDuration = 1f; // Cooldown for normal attack

    public float upADuration = 0.5f; // Cooldown for UpA attack

    public float aACooldown = 0.8f;
    public float aADuration = 0.5f;

    public SpriteRenderer mySpriteRenderer;
    public ContactCheck groundCheck;
    public ContactCheck fwdCheck;
    public string horizontalAxis;
    private float InputX = 0f;

    public KeyCode JumpKeyCode;
    public KeyCode dashCode;
    public KeyCode attackCode;
    public KeyCode dashAKey1; // First key for DashA
    public KeyCode upAKey1; // First key for UpA
    public KeyCode upAAKey1; // First key for UpAA
    public KeyCode sideAAKey1; // Key for SideAA
    public KeyCode downAAKey1; // Key for DownAA
    public PlayerState playerState = PlayerState.Normal;
    private PlayerState bufferPlayerState = PlayerState.None;

    public float direction = 1.0f;
    public Collider2D attackHitbox;
    public Collider2D dashAttackHitbox;
    public Collider2D upAHitbox; // Hitbox for UpA
    public Collider2D sideAAHitbox; // Hitbox for SideAA
    public Collider2D upAAHitbox; // Hitbox for UpAA
    public Collider2D downAAHitbox; // Hitbox for DownAA
    public ContactFilter2D contactFilter;

    public string verticalAxis;


    void ChangeDirection()
    {
        if (InputX > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
            direction = 1.0f;
        }
        else if (InputX < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            direction = -1.0f;
        }
    }


    void Update()
    {
        // Auxillary bullshit
        animator.SetBool("IsWalking", InputX != 0);
        animator.SetBool("HasGround", groundCheck.HasContact);
        animator.SetFloat("DashDuration", 1f / dashDuration);
        animator.ResetTrigger("EndAttack");
        InputX = Input.GetAxis(horizontalAxis);
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float now = Time.time;
        // Checking inputs
        if (groundCheck.HasContact)
        {
            playerState &= ~PlayerState.JumpOnCooldown;
            // If we are in jump state but on ground - return to normal
            switch (playerState & ~PlayerState.JumpOnCooldown)
            {
                case PlayerState.Jump:
                case PlayerState.SecondJump:
                    ReturnPlayerState();
                    break;
            }
        }
        if (!IsMSBlocking())
        {
            if (groundCheck.HasContact)
            // Inputs while on ground
            {
                // If we are in jump state but on ground - return to normal

                // Actual on ground input processing
                if (Input.GetKeyDown(attackCode) && IsMS(PlayerState.Dash))
                {
                    SetStateWithCooldowns(PlayerState.DashAttack);
                }
                else if (IsMS(PlayerState.Normal))
                {
                    if (Input.GetKeyDown(attackCode) && Input.GetKey(upAAKey1) && !Input.GetKey(downAAKey1))
                    {
                        SetStateWithCooldowns(PlayerState.UpAttack);
                    }
                    else if (Input.GetKeyDown(attackCode))
                    {
                        SetStateWithCooldowns(PlayerState.Attack);
                    }
                    else if (Input.GetKeyDown(JumpKeyCode))
                    {
                        SetStateWithCooldowns(PlayerState.GroundJump);
                    }
                    else if (isShiftPressed && !IsDashOnCooldown())
                    {
                        SetStateWithCooldowns(PlayerState.Dash);
                        animator.SetTrigger("Dash");
                        Invoke("StartDashCooldown", dashDuration);
                    }
                }
            }

            else// if (!groundCheck.HasContact )
                // Inputs while in air
            {
                if (Input.GetKeyDown(JumpKeyCode) && IsMS(PlayerState.Jump) && !IsJumpOnCooldown())
                {
                    SetStateWithCooldowns(PlayerState.SecondJump);
                }
                else if (!IsAAOnCooldown()) {
                    if (Input.GetKeyDown(attackCode) && Input.GetKey(downAAKey1) && !Input.GetKey(upAAKey1))
                    {
                        bufferPlayerState = playerState;
                        SetStateWithCooldowns(PlayerState.DownAAttack);
                    }
                    else if (Input.GetKeyDown(attackCode) && Input.GetKey(upAAKey1) && !Input.GetKey(downAAKey1))
                    {
                        bufferPlayerState = playerState;
                        SetStateWithCooldowns(PlayerState.UpAAttack);
                    }
                    else if (Input.GetKeyDown(attackCode))
                    {
                        bufferPlayerState = playerState;
                        SetStateWithCooldowns(PlayerState.SideAAttack);
                    }
                }
                    
            }
        }


        if (fwdCheck.HasContact && myRigidbody2D.bodyType == RigidbodyType2D.Dynamic)
        {
            StopHorisontalMovement();
        }
        // Movement processing depending on players state
        if (!IsMSBlocking())
        {
            ContinueMovement();
            switch (playerState & ~PlayerState.DashOnCooldown & ~PlayerState.JumpOnCooldown)
            {
                case PlayerState.UpAAttack:
                    StopMovement();
                    animator.SetTrigger("UpAA");
                    Debug.Log("Up Air Attack happened");
                    playerState |= PlayerState.IsBlocking;
                    Invoke("StartAACooldown", aADuration);
                    break;
                case PlayerState.UpAttack:
                    StopMovement();
                    animator.SetTrigger("UpA");
                    Debug.Log("Up Attack happened");
                    playerState |= PlayerState.IsBlocking;
                    Invoke("ReturnPlayerState", upADuration);
                    break;
                case PlayerState.SideAAttack:
                    StopMovement();
                    animator.SetTrigger("SideAA");
                    Debug.Log("Air Side Attack happened");
                    playerState |= PlayerState.IsBlocking;
                    Invoke("StartAACooldown", aADuration);
                    break;
                case PlayerState.DownAAttack:
                    StopMovement();
                    animator.SetTrigger("DownAA");
                    Debug.Log("Down Attack happened");
                    playerState |= PlayerState.IsBlocking;
                    Invoke("StartAACooldown", aADuration);
                    break;
                case PlayerState.DashAttack:
                    animator.SetTrigger("DashA");
                    Debug.Log("Dash Attack hapened");
                    playerState |= PlayerState.IsBlocking | PlayerState.DashOnCooldown;
                    Invoke("EndDashA", dashADuration);
                    goto case PlayerState.Dash;
                case PlayerState.Attack:
                    StopMovement();
                    animator.SetTrigger("Attack");
                    Debug.Log("Attack happened");
                    playerState |= PlayerState.IsBlocking;
                    Invoke("ReturnPlayerState", attackDuration);
                    break;
                case PlayerState.GroundJump:
                    Jump();
                    SetStateWithCooldowns(PlayerState.Jump | PlayerState.JumpOnCooldown);
                    Invoke("RemoveCooldownOnJump", jumpCooldown);
                    goto default;
                case PlayerState.SecondJump:
                case PlayerState.SecondJump | PlayerState.AAOnCooldown:
                    if (!IsJumpOnCooldown())
                    {
                        Jump();
                        playerState |= PlayerState.JumpOnCooldown;
                    }
                    goto default;
                case PlayerState.Dash:
                case PlayerState.DashAttack | PlayerState.IsBlocking:
                    if (!fwdCheck.HasContact)
                    {
                        myRigidbody2D.linearVelocity = new Vector2(direction * dashSpeed, myRigidbody2D.linearVelocityY);
                    }
                    break;
                default:
                    ChangeDirection();
                    if (!fwdCheck.HasContact)
                    {
                        myRigidbody2D.linearVelocity = new Vector2(InputX * walkSpeed, myRigidbody2D.linearVelocityY);
                    }
                    break;
            }
        }
    }

    void SetStateWithCooldowns(PlayerState state)
    {
        playerState = state | (playerState & PlayerState.DashOnCooldown) | (playerState & PlayerState.JumpOnCooldown) | (playerState & PlayerState.AAOnCooldown);
    }


    void RemoveCooldownOnDash()
    {
        bufferPlayerState = ~PlayerState.DashOnCooldown & bufferPlayerState;
        playerState = ~PlayerState.DashOnCooldown & playerState;
        ReturnPlayerState();
    }

    bool IsMSBlocking()
    {
        return (playerState & PlayerState.IsBlocking) == PlayerState.IsBlocking;
    }

    bool IsAAOnCooldown()
    {
        return (playerState & PlayerState.AAOnCooldown) == PlayerState.AAOnCooldown;
    }

    bool IsJumpOnCooldown()
    {
        return (playerState & PlayerState.JumpOnCooldown) == PlayerState.JumpOnCooldown;
    }

    bool IsDashOnCooldown()
    {
        return (playerState & PlayerState.DashOnCooldown) == PlayerState.DashOnCooldown;
    }


    bool IsMS(PlayerState ms)
    {
        return playerState.HasFlag(ms);
    }


    void RemoveCooldownOnJump()
    {
        switch (playerState)
        {
            case PlayerState.DownAAttack:
                bufferPlayerState = PlayerState.Jump;
                break;
            default:
                playerState = PlayerState.Jump;
                break;
        }
    }


    void ReturnPlayerState()
    {
        EndAttack();
        switch (bufferPlayerState)
        {
            case PlayerState.None:
                SetStateWithCooldowns(PlayerState.Normal); ;
                break;
            default:
                SetStateWithCooldowns(bufferPlayerState);
                bufferPlayerState = PlayerState.None;
                break;
        }
    }


    void StartDashCooldown()
    {
        playerState = PlayerState.Normal | PlayerState.DashOnCooldown;
        Invoke("RemoveCooldownOnDash", dashCooldown);
    }

    void StartAACooldown()
    {
        ReturnPlayerState();
        playerState |= PlayerState.AAOnCooldown;
        playerState &= ~PlayerState.IsBlocking;
        Invoke("RemoveCooldownOnAA", aACooldown);
    }

    void RemoveCooldownOnAA()
    {
        playerState &= ~PlayerState.AAOnCooldown;
        ReturnPlayerState();
    }


    void StopMovement()
    {
        myRigidbody2D.bodyType = RigidbodyType2D.Static;
    }

    void ContinueMovement()
    {
        myRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    }

    void StopHorisontalMovement()
    {
        myRigidbody2D.linearVelocity = new Vector2(0, myRigidbody2D.linearVelocityY);
    }
    void Jump()
    {
        animator.SetTrigger("Jump");
        myRigidbody2D.linearVelocity = Vector2.zero;
        myRigidbody2D.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
    }
    public void Hit()
    {
        var listOverlaps = new List<Collider2D>();
        attackHitbox.Overlap(contactFilter, listOverlaps);
        foreach (var item in listOverlaps)
        {
            Debug.Log(item.name);
        }
    }

    // Add this method for DashA hit detection
    public void DashHit()
    {
        if (dashAttackHitbox == null) return;
        var listOverlaps = new List<Collider2D>();
        dashAttackHitbox.Overlap(contactFilter, listOverlaps);
        foreach (var item in listOverlaps)
        {
            Debug.Log("DashA hit: " + item.name);
        }
    }

    public void UpAHit()
    {
        if (upAHitbox == null) return;
        var listOverlaps = new List<Collider2D>();
        upAHitbox.Overlap(contactFilter, listOverlaps);
        foreach (var item in listOverlaps)
        {
            Debug.Log("UpA hit: " + item.name);
        }
    }

    public void SideAAHit()
    {
        if (sideAAHitbox == null) return;
        var listOverlaps = new List<Collider2D>();
        sideAAHitbox.Overlap(contactFilter, listOverlaps);
        foreach (var item in listOverlaps)
        {
            Debug.Log("SideAA hit: " + item.name);
        }
    }

    public void UpAAHit()
    {
        if (upAAHitbox == null) return;
        var listOverlaps = new List<Collider2D>();
        upAAHitbox.Overlap(contactFilter, listOverlaps);
        foreach (var item in listOverlaps)
        {
            Debug.Log("UpAA hit: " + item.name);
        }
    }

    public void DownAAHit()
    {
        if (downAAHitbox == null) return;
        var listOverlaps = new List<Collider2D>();
        downAAHitbox.Overlap(contactFilter, listOverlaps);
        foreach (var item in listOverlaps)
        {
            Debug.Log("DownAA hit: " + item.name);
        }
    }

    public void EndAttack()
    {
        animator.SetTrigger("EndAttack");
    }

    public void EndDashA()
    {
        animator.SetTrigger("EndAttack");
        ReturnPlayerState();
    }

    public void EndUpA()
    {
        animator.SetTrigger("EndAttack");
    }

    public void EndSideAA()
    {
        animator.SetTrigger("EndAttack");
    }

    public void EndUpAA()
    {
        animator.SetTrigger("EndAttack");
    }

    public void EndDownAA()
    {
        animator.SetTrigger("EndAttack");
    }
}


