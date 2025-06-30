using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using NUnit.Framework.Constraints;

[System.Flags] public enum PlayerState
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
    DownAttack = 0x800,
    IsBlocking = 0x1000,
    JumpOnCooldown = 0x2000,
    DashOnCooldown = 0x4000
}


public class Igrac : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D myRigidbody2D;
    public float walkSpeed;
    public float jumpSpeed;
    public float dashDuration = 0.2f; // Duration of the dash
    public float dashADuration = 0.3f; // Duration of DashA (set in Inspector)
    public float attackCooldown = 0.3f; // Cooldown for normal attack
    public float dashACooldown = 0.4f; // Cooldown for DashA attack
    public float upACooldown = 0.4f; // Cooldown for UpA attack
    public float upAACooldown = 0.4f; // Cooldown for UpAA attack
    public float sideAACooldown = 0.5f; // Cooldown for SideAA attack
    public float downAACooldown = 0.5f; // Cooldown for DownAA
    public SpriteRenderer mySpriteRenderer;
    public ContactCheck groundCheck;
    public ContactCheck fwdCheck;
    public int jumpCount;
    public int jumpCountLimit = 2;
    public float jumpCooldown = 0.1f;
    public string horizontalAxis;
    private float InputX = 0f;

    public KeyCode JumpKeyCode;
    public KeyCode dashCode;
    public KeyCode attackCode;
    public KeyCode dashAKey1; // First key for DashA
    public KeyCode dashAKey2; // Second key for DashA
    public KeyCode upAKey1; // First key for UpA
    public KeyCode upAKey2; // Second key for UpA
    public KeyCode upAAKey1; // First key for UpAA
    public KeyCode upAAKey2; // Second key for UpAA
    public KeyCode sideAAKey1; // Key for SideAA
    public KeyCode downAAKey1; // Key for DownAA
    public PlayerState playerState = PlayerState.Normal;
    private PlayerState bufferPlayerState = PlayerState.None;
    public Vector2 savedDirection;
    public float dashSpeed;
    public float dashCooldown = 0.2f;
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
                        playerState = PlayerState.DashAttack;
                    }
                    else if (IsMS(PlayerState.Normal)) {
                        if (Input.GetKeyDown(attackCode) && Input.GetKey(downAAKey1) && !Input.GetKey(upAAKey1))
                        {
                            playerState = PlayerState.DownAttack;
                        }
                        else if (Input.GetKeyDown(attackCode) && Input.GetKey(upAAKey1) && !Input.GetKey(downAAKey1))
                        {
                            playerState = PlayerState.UpAttack;
                        }
                        else if (Input.GetKeyDown(attackCode))
                        {
                            playerState = PlayerState.Attack;
                        }
                        else if (Input.GetKeyDown(JumpKeyCode))
                        {
                            playerState = PlayerState.GroundJump;
                        }
                        else if (isShiftPressed && !IsDashOnCooldown())
                        {
                            playerState = PlayerState.Dash;
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
                        playerState = PlayerState.SecondJump;
                    }
                    else if (Input.GetKeyDown(attackCode) && Input.GetKey(downAAKey1) && !Input.GetKey(upAAKey1))
                    {
                        bufferPlayerState = playerState;
                        playerState = PlayerState.DownAAttack;
                    }
                    else if (Input.GetKeyDown(attackCode) && Input.GetKey(upAAKey1) && !Input.GetKey(downAAKey1))
                    {
                        bufferPlayerState = playerState;
                        playerState = PlayerState.UpAAttack;
                    }
                    else if (Input.GetKeyDown(attackCode))
                    {
                        bufferPlayerState = playerState;
                        playerState = PlayerState.SideAAttack;
                    }
                }
            }
        

        if (fwdCheck.HasContact)
        {
            StopHorisontalMovement();
        }
        // Movement processing depending on players state
        switch (playerState)
        {
            case PlayerState.UpAAttack:
                StopMovement();
                animator.SetTrigger("UpAA");
                Debug.Log("Up Air Attack happened");
                playerState |= PlayerState.IsBlocking;
                Invoke("ReturnPlayerState", upAACooldown);
                break;
            case PlayerState.UpAttack:
                StopMovement();
                animator.SetTrigger("UpA");
                Debug.Log("Up Attack happened");
                playerState |= PlayerState.IsBlocking;
                Invoke("ReturnPlayerState", upACooldown);
                break;
            case PlayerState.SideAAttack:
                StopMovement();
                animator.SetTrigger("SideAA");
                Debug.Log("Air Side Attack happened");
                playerState |= PlayerState.IsBlocking;
                Invoke("ReturnPlayerState", sideAACooldown);
                break;
            case PlayerState.DownAttack:
            case PlayerState.DownAAttack:
                StopMovement();
                animator.SetTrigger("DownAA");
                Debug.Log("Down Attack happened");
                playerState |= PlayerState.IsBlocking;
                Invoke("ReturnPlayerState", downAACooldown);
                break;
            case PlayerState.DashAttack:
                StopMovement();
                animator.SetTrigger("DashA");
                Debug.Log("Dash Attack hapened");
                playerState |= PlayerState.IsBlocking | PlayerState.DashOnCooldown;
                Invoke("ReturnPlayerState", dashACooldown);
                break;
            case PlayerState.Attack:
                StopMovement();
                animator.SetTrigger("Attack");
                Debug.Log("Attack happened");
                playerState |= PlayerState.IsBlocking;
                Invoke("ReturnPlayerState", attackCooldown);
                break;
            case PlayerState.GroundJump:
                Jump();
                playerState = PlayerState.Jump | PlayerState.JumpOnCooldown;
                Invoke("RemoveCooldownOnJump", jumpCooldown);
                goto default;
            case PlayerState.SecondJump & ~PlayerState.JumpOnCooldown:
                Jump();
                playerState |= PlayerState.JumpOnCooldown;
                goto default;
            case PlayerState.Dash:
                if (!fwdCheck.HasContact && !IsMSBlocking())
                {
                    myRigidbody2D.linearVelocity = new Vector2(direction * dashSpeed, myRigidbody2D.linearVelocityY);
                }
                break;
            default:
                if (!IsMSBlocking())
                {
                    ChangeDirection();
                    if (!fwdCheck.HasContact)
                    {
                        myRigidbody2D.linearVelocity = new Vector2(InputX * walkSpeed, myRigidbody2D.linearVelocityY);
                    }
                }
                else
                {
                    StopMovement();
                }
                break;
        }
    }

    void RemoveCooldownOnDash()
    {
        playerState = ~PlayerState.DashOnCooldown & playerState;
        ReturnPlayerState();
    }

    bool IsMSBlocking()
    {
        return (playerState & PlayerState.IsBlocking) == PlayerState.IsBlocking;
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
                playerState = (PlayerState.DashOnCooldown & playerState) | PlayerState.Normal;
                Debug.Log(playerState.HasFlag(PlayerState.DashOnCooldown));
                break;
            default:
                playerState = bufferPlayerState;
                bufferPlayerState = PlayerState.None;
                break;
        }
    }


    void StartDashCooldown()
    {
        playerState = PlayerState.Normal | PlayerState.DashOnCooldown;
        Invoke("RemoveCooldownOnDash", dashCooldown);
    }


    void StopMovement()
    {
        myRigidbody2D.linearVelocity = Vector2.zero;
    }

    void StopHorisontalMovement()
    {
        myRigidbody2D.linearVelocity = new Vector2(0, myRigidbody2D.linearVelocityY);
    }

    void Jump()
    {
        animator.SetTrigger("Jump");
        myRigidbody2D.linearVelocity = new Vector2(myRigidbody2D.linearVelocityX, 0);
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


