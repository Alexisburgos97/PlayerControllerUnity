using System;
using System.Collections;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GatherInput gatherInput;
    
    [SerializeField] private float moveSpeed = 5f, rotationSpeed = 500;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private LayerMask groundLayer;

    private CameraController cameraController;
    private CharacterController characterController;
    private EnvironmentScanner environmentScanner;
    private Animator animator;

    private Vector3 moveInput;
    
    private Quaternion targetRotation;
    
    private float ySpeed;

    private bool hasControl = true;

    private Vector3 desiredMoveDir;
    
    private Vector3 moveDir;

    private Vector3 velocity;
    
    public bool InAction { get; private set; }
    public bool IsHanging { get; set; }
    public bool IsOnLedge { get; set; }

    public EnvironmentScanner.LegdeData LegdeData { get; set; }
    
    private void Awake()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        characterController = GetComponent<CharacterController>();
        environmentScanner = GetComponent<EnvironmentScanner>();
        animator = GetComponent<Animator>();
        Application.targetFrameRate = 60;
    }
    
    void Update()
    {

        Vector2 direction = gatherInput.smoothedDirection;
        moveInput = new Vector3(direction.x, 0, direction.y);
        
        float moveAmount = Mathf.Clamp01(Mathf.Abs(direction.x) + Mathf.Abs(direction.y));
        
        moveInput = new Vector3(gatherInput.direction.x, 0, gatherInput.direction.y);

        desiredMoveDir = cameraController.GetYRotation * moveInput;

        moveDir = desiredMoveDir;

        if (!hasControl)
        {
            return;
        }
        
        if (IsHanging)
        {
            return;
        }
        
        velocity = Vector3.zero;
        
        animator.SetBool("isGrounded", GroundCheck());
        
        if (GroundCheck())
        {
            ySpeed = -1f;
            
            velocity = desiredMoveDir * moveSpeed;

            IsOnLedge = environmentScanner.ObstacleLedgeCheck(desiredMoveDir.normalized, out EnvironmentScanner.LegdeData legdeData);
            
            if (IsOnLedge)
            {
                LegdeData = legdeData;
                LedgeMovement();
            }
            
            animator.SetFloat("moveAmount", velocity.magnitude / moveSpeed, 0.1f, Time.deltaTime);
        }
        else
        {
            velocity = transform.forward * moveSpeed / 2;
            ySpeed += Physics.gravity.y * Time.deltaTime;
        }

        velocity.y = ySpeed;
        
        characterController.Move( velocity * Time.deltaTime);

        if (moveAmount > 0f && moveDir.sqrMagnitude > 0.05f)
        {
            // transform.position += desiredMoveDir * moveSpeed * Time.deltaTime;
            
            targetRotation = Quaternion.LookRotation(moveDir);
        }
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void LedgeMovement()
    {
        float signedAngle = Vector3.SignedAngle(LegdeData.surfaceHit.normal, desiredMoveDir, Vector3.up);
        
        float angle = Math.Abs(signedAngle);

        if (Vector3.Angle(desiredMoveDir, transform.forward) >= 80f)
        {
            //Don't move, but rotate
            velocity = Vector3.zero;
            return;
        }

        if (angle < 50f)
        {
            velocity = Vector3.zero;
            // moveDir = Vector3.zero;
        }else if (angle < 90f)
        {
            //Angle is between 60 an 90, so limit the velocity to horizontal direction
            var left = Vector3.Cross(Vector3.up, LegdeData.surfaceHit.normal);

            var dir = left * Mathf.Sign(signedAngle);

            velocity = velocity.magnitude * dir;
            
            moveDir = dir;
        }
    }

    public void SetControl(bool hasControl)
    {
        this.hasControl = hasControl;
        characterController.enabled = hasControl;

        if (!hasControl)
        {
            animator.SetFloat("moveAmount", 0);
            targetRotation = transform.rotation;
        }
    }
    
    public IEnumerator DoAction(string animName, MatchTargetParans matchTargetParans, Quaternion targetRotation, bool rotate = false, float postDelay = 0f, bool mirror = false)
    {
        InAction = true;

        animator.SetBool("mirrorAction", mirror);
        
        animator.CrossFade(animName, 0.2f);

        yield return null;

        var animState = animator.GetNextAnimatorStateInfo(0);

        if (!animState.IsName(animName))
        {
            Debug.LogError("The Parkour animation is Wrong!");
        }
        
        // yield return new WaitForSeconds(animState.length);

        float timer = 0f;
        while (timer < animState.length)
        {
            timer += Time.deltaTime;

            if (rotate)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, this.targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (matchTargetParans != null)
            {
                MatchTarget(matchTargetParans);
            }

            if (animator.IsInTransition(0) && timer > 0.5f)
            {
                break;
            }
            
            yield return null;
        }
        
        yield return new WaitForSeconds(postDelay);
        
        InAction = false;
    }
    
    private void MatchTarget(MatchTargetParans mtp)
    {
        if (animator.isMatchingTarget || animator.IsInTransition(0))
        {
            return;
        }
        
        animator.MatchTarget(
            mtp.pos, 
            transform.rotation, 
            mtp.bodyPart, 
            new MatchTargetWeightMask(mtp.posWeight, 0),
            mtp.startTime,
            mtp.targetTime
        );
    }

    public bool HasControl
    {
        get => hasControl;
        set => hasControl = value;
    }

    private bool GroundCheck()
    {
        bool isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundLayer);
        
        return isGrounded;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawWireSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
    }

    public float GetRotationSpeed()
    {
        return rotationSpeed;
    }
}

public class MatchTargetParans
{
    public Vector3 pos;
    public AvatarTarget bodyPart;
    public Vector3 posWeight;
    public float startTime;
    public float targetTime;
}