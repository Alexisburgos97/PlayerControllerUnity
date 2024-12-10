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
    [SerializeField] private Vector3 handOffsetIdleToHang = new Vector3(0.3f, 0, 0.1f);
    [SerializeField] private Vector3 handOffsetUp = new Vector3(0.3f, 0.06f, 0.17f); 
    [SerializeField] private Vector3 handOffsetDown = new Vector3(0.3f, 0.09f, 0.14f); 
    [SerializeField] private Vector3 handOffsetLeft = new Vector3(0.3f, 0.06f, 0.08f);  
    [SerializeField] private Vector3 handOffsetRight = new Vector3(0.3f, 0.06f, 0.1f); 
    [SerializeField] private Vector3 handOffsetShimmyLeft = new Vector3(0.3f, 0, 0.1f);  
    [SerializeField] private Vector3 handOffsetShimmyRight = new Vector3(0.3f, 0, 0.1f); 
    [SerializeField] private Vector3 handOffsetDropToHang = new Vector3(0.3f, 0f, 0f); 
    
    // private bool isPlayerInShimmy = false;
    
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

                    // currentPoint = ledgeHit.transform.GetComponent<ClimbPoint>();
                    currentPoint = GetNearestClimbPoint(ledgeHit.transform, ledgeHit.point);
                    
                    playerController.SetControl(false);
                    StartCoroutine(JumpToLedge("IdleToHang", currentPoint.transform, 0.41f, 0.54f, handOffset: handOffsetIdleToHang));
                    gatherInput.tryToJump = false;
                }
            }

            if (gatherInput.tryToDrop && !playerController.InAction)
            {
                if (envScanner.DropLedgeCheck(out RaycastHit ledgeHit))
                {
                    currentPoint = GetNearestClimbPoint(ledgeHit.transform, ledgeHit.point);
                    
                    playerController.SetControl(false);
                    
                    StartCoroutine(JumpToLedge("DropToHang", currentPoint.transform, 0.30f, 0.45f, handOffset: handOffsetDropToHang));
                    
                    gatherInput.tryToDrop = false;
                }
            }
            
        }
        else
        {

            if (gatherInput.tryToDrop && !playerController.InAction)
            {
                StartCoroutine(JumpFromHang());
                
                gatherInput.tryToDrop = false;
                
                return;
            }
            
            // Ledge to ledge
            float h = Mathf.Round(gatherInput.Direction.x);
            float v = Mathf.Round(gatherInput.Direction.y);
            
            var inputDir = new Vector2(h, v);
            
            if(playerController.InAction || inputDir == Vector2.zero) return;

            if (currentPoint.MountPoint && inputDir.y == 1)
            {
                StartCoroutine(MountFromHang());
                
                return;
            }
            
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
                    StartCoroutine(JumpToLedge("HangHopUp", currentPoint.transform, 0.34f, 0.65f, handOffset: handOffsetUp));
                }
                else if (neighbour.direction.y == -1)
                {
                    StartCoroutine(JumpToLedge("HangHopDown", currentPoint.transform, 0.31f, 0.65f, handOffset: handOffsetDown));
                }
                else if (neighbour.direction.x == 1)
                {
                    StartCoroutine(JumpToLedge("HangHopRight", currentPoint.transform, 0.20f, 0.50f, handOffset: handOffsetRight));
                }
                else if (neighbour.direction.x == -1)
                {
                    StartCoroutine(JumpToLedge("HangHopLeft", currentPoint.transform, 0.20f, 0.50f, handOffset: handOffsetLeft));
                }
            }
            else if (neighbour.connectionType == ConnectionType.Move)
            {
                currentPoint = neighbour.point;
                if (neighbour.direction.x == 1)
                {
                    StartCoroutine(JumpToLedge("ShimmyRight", currentPoint.transform, 0, 0.38f, handOffset: handOffsetShimmyRight));
                }
                else if (neighbour.direction.x == -1)
                {
                    StartCoroutine(JumpToLedge("ShimmyLeft", currentPoint.transform, 0, 0.38f, AvatarTarget.LeftHand, handOffset: handOffsetShimmyLeft));
                }
            }
        }
    }

    private IEnumerator JumpToLedge(string anim, Transform ledge, float matchStartTime, float matchTargetTime, AvatarTarget hand = AvatarTarget.RightHand, Vector3? handOffset = null)
    {
        
        Debug.Log("anim: " + anim);
        
        // Actualizar los offsets en función de la animación
        // UpdateOffsetsForAnimation(anim);
        
        var matchParams = new MatchTargetParans()
        {
            pos = GetHandPosition(ledge, hand, handOffset),
            bodyPart = hand,
            startTime = matchStartTime,
            targetTime = matchTargetTime,
            posWeight = Vector3.one
        };

        var targetRotation = Quaternion.LookRotation(-ledge.forward);
        
        yield return playerController.DoAction(anim, matchParams, targetRotation, true);

        playerController.IsHanging = true;
    }

    private IEnumerator JumpFromHang()
    {
        playerController.IsHanging = false;

        yield return playerController.DoAction("JumpFromHang");

        playerController.ResetTargetRotation();
        
        playerController.SetControl(true);
    }

    private IEnumerator MountFromHang()
    {
        playerController.IsHanging = false;
        
        yield return playerController.DoAction("MountFromHang");
        
        playerController.EnableCharacterController(true);

        yield return new WaitForSeconds(0.5f);
        
        playerController.ResetTargetRotation();
        
        playerController.SetControl(true);
    }

    private Vector3 GetHandPosition(Transform ledge, AvatarTarget hand, Vector3? handOffset)
    {
        
        Debug.Log("handOffset: " + handOffset);
        
        Debug.Log("hand: " + hand);
        
        // Obtener el valor del offset o usar el valor por defecto
        var offsetValue = (handOffset != null) ? handOffset.Value : new Vector3(0.3f, 0.001f, 0.11f);
        
        Debug.Log("offsetValue: " + offsetValue);
        
        // Determinar la dirección horizontal en función de la mano
        var horizontalDir = (hand == AvatarTarget.RightHand) ? ledge.right : -ledge.right;
        
        Debug.Log("horizontalDir: " + horizontalDir);
        
        /*var position = ledge.position 
                       + ledge.forward * 0.11f 
                       + Vector3.up * 0.001f 
                       //+ -ledge.right * 0.00001f;
                       //+ -ledge.right * -0.3f;
                       - horizontalDir * -0.3f;*/
        
        // Calcular la posición usando offsetValue
        var position = ledge.position 
                       + ledge.forward * offsetValue.z 
                       + Vector3.up * offsetValue.y 
                       - horizontalDir * offsetValue.x;

        
        
        // Calcular el centro del obstáculo
        var obstacleCenter = ledge.position;

        // Asegurar que el offset se aplica hacia el centro del obstáculo
        /*var position = obstacleCenter
                       + ledge.forward * offsetValue.z
                       + Vector3.up * offsetValue.y
                       - horizontalDir * offsetValue.x;*/
        
        Debug.Log("position: " + position);

        return position;
    }

    private ClimbPoint GetNearestClimbPoint(Transform ledge, Vector3 hitPoint)
    {
        var points = ledge.GetComponentsInChildren<ClimbPoint>();
        
        ClimbPoint nearestPoint = null; 
        
        float nearestPointDistance = Mathf.Infinity;

        foreach (var point in points)
        {
            float distance = Vector3.Distance(point.transform.position, hitPoint);

            if (distance < nearestPointDistance)
            {
                nearestPoint = point;
                nearestPointDistance = distance;
            }
        }

        return nearestPoint;
    }
    
    /*private void UpdateOffsetsForAnimation(string anim)
    {
        if ( anim == "ShimmyRight" || anim == "ShimmyLeft" )
        {
            isPlayerInShimmy = true;
            
            // Cambiar valores de handOffsetUp, handOffsetDown, handOffsetLeft y handOffsetRight a negativo
            handOffsetUp = new Vector3(Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetDown = new Vector3(Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetLeft = new Vector3(Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetRight = new Vector3(Mathf.Abs(handOffsetRight.x), handOffsetRight.y, handOffsetRight.z);
        }
        else if ((anim == "HangHopUp" || anim == "HangHopDown" || anim == "HangHopRight" || anim == "HangHopLeft") && !isPlayerInShimmy )
        {
            isPlayerInShimmy = false;
            
            // Cambiar valores de handOffsetUp, handOffsetDown, handOffsetLeft y handOffsetRight a positivo
            handOffsetUp = new Vector3(-Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetDown = new Vector3(-Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetLeft = new Vector3(-Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetRight = new Vector3(-Mathf.Abs(handOffsetRight.x), handOffsetRight.y, handOffsetRight.z);
        }
        else
        {
            handOffsetUp = new Vector3(-Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetDown = new Vector3(-Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetLeft = new Vector3(-Mathf.Abs(handOffsetLeft.x), handOffsetLeft.y, handOffsetLeft.z);
            handOffsetRight = new Vector3(-Mathf.Abs(handOffsetRight.x), handOffsetRight.y, handOffsetRight.z);
        }
    }*/
}
