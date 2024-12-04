using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbController : MonoBehaviour
{
   
    [SerializeField] private GatherInput gatherInput;
    private EnvironmentScanner envScanner;
    private PlayerController playerController;

    private ClimbPoint currentPoint;
    
    private void Awake()
    {
        envScanner = GetComponent<EnvironmentScanner>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!playerController.IsHanging)
        {
            if (gatherInput.tryToJump && !playerController.InAction)
            {
                if (envScanner.ClimbLedgeCheck(transform.forward, out RaycastHit ledgeHit))
                {
                    Debug.Log("Climb Ledge Found");

                    currentPoint = ledgeHit.transform.GetComponent<ClimbPoint>();
                    
                    playerController.SetControl(false);
                    StartCoroutine(JumpToLedge("IdleToHang", ledgeHit.transform, 0.41f, 0.54f));
                    gatherInput.tryToJump = false;
                }
            }
        }
        else
        {
            // Ledge to ledge
            float h = Mathf.Round(gatherInput.Direction.x);
            float v = Mathf.Round(gatherInput.Direction.y);
            
            var inputDir = new Vector2(h, v);
            
            if(playerController.InAction || inputDir == Vector2.zero) return;
            
            var neighbour = currentPoint.GetNeighbour(inputDir);

            if (neighbour == null)
            {
                return;
            }

            if (neighbour.connectionType == ConnectionType.Jump && gatherInput.tryToJump)
            {
                currentPoint = neighbour.point;
                
                if (neighbour.direction.y == 1)
                {
                    StartCoroutine(JumpToLedge("HangHopUp", currentPoint.transform, 0.34f, 0.65f));
                }
                else if (neighbour.direction.y == -1)
                {
                    StartCoroutine(JumpToLedge("HangHopDown", currentPoint.transform, 0.31f, 0.65f));
                }
                else if (neighbour.direction.x == 1)
                {
                    StartCoroutine(JumpToLedge("HangHopRight", currentPoint.transform, 0.20f, 0.50f));
                }
                else if (neighbour.direction.x == -1)
                {
                    StartCoroutine(JumpToLedge("HangHopLeft", currentPoint.transform, 0.20f, 0.50f));
                }
            }
        }
    }

    private IEnumerator JumpToLedge(string anim, Transform ledge, float matchStartTime, float matchTargetTime)
    {
        var matchParams = new MatchTargetParans()
        {
            pos = GetHandPosition(ledge),
            bodyPart = AvatarTarget.RightHand,
            startTime = matchStartTime,
            targetTime = matchTargetTime,
            posWeight = Vector3.one
        };

        var targetRotation = Quaternion.LookRotation(-ledge.forward);
        
        yield return playerController.DoAction(anim, matchParams, targetRotation, true);

        playerController.IsHanging = true;
    }

    private Vector3 GetHandPosition(Transform ledge)
    {
        return ledge.position + ledge.forward * 0.1f + Vector3.up * 0.07f + -ledge.right * 0.3f;
    }
}
