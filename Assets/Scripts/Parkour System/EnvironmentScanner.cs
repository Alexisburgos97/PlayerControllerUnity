using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EnvironmentScanner : MonoBehaviour
{
    
    [SerializeField] private Vector3 forwardRayOffset = new Vector3(0, 0.25F, 0);
    [SerializeField] private float forwardRayLength = 1.2f;
    [SerializeField] private float heightRayLength = 5f;
    [SerializeField] private float ledgeRayLength = 5f;
    [SerializeField] private float climbLedgeRayLength = 1.5f;
    
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask climbLedgeLayer;
    
    [SerializeField] private float ledgeHeightThreshold = 0.75f;
    
    public ObstacleHitData ObstacleCheck()
    {
        var hitData = new ObstacleHitData();
    
        // Calcula el origen y el punto final del rayo hacia adelante
        var forwardOrigin = transform.position + forwardRayOffset;
        var forwardEnd = forwardOrigin + transform.forward * forwardRayLength;
    
        // Verifica la colisión hacia adelante
        hitData.forwardHitFound = Physics.Raycast(forwardOrigin, transform.forward, out hitData.forwardHit, forwardRayLength, obstacleLayer);
    
        // Dibuja la línea correctamente desde forwardOrigin hasta forwardEnd
        // Debug.DrawLine(forwardOrigin, forwardEnd, hitData.forwardHitFound ? Color.green : Color.red);

        // Si encuentra un obstáculo adelante, verifica la altura del obstáculo
        if (hitData.forwardHitFound)
        {
            // Calcula el origen y el punto final del rayo hacia abajo desde el punto de impacto hacia abajo
            var heightOrigin = hitData.forwardHit.point + Vector3.up * heightRayLength;
            var heightEnd = heightOrigin + Vector3.down * heightRayLength;
        
            hitData.heightHitFound = Physics.Raycast(heightOrigin, Vector3.down, out hitData.heightHit, heightRayLength, obstacleLayer);
        
            // Dibuja la línea desde heightOrigin hacia heightEnd
            Debug.DrawLine(heightOrigin, heightEnd, hitData.heightHitFound ? Color.green : Color.red);
        }

        return hitData;
    }

    public bool ClimbLedgeCheck(Vector3 direction, out RaycastHit ledgeHit)
    {
        ledgeHit = new RaycastHit();
        
        if (direction == Vector3.zero)
        {
            return false;
        }

        var origin = transform.position + Vector3.up * 1.5f;
        var offset = new Vector3(0, 0.18f, 0);
        
        for (int i = 0; i < 10; i++)
        {
            
            Debug.DrawRay(origin + offset * i, direction, Color.red);
            
            if (Physics.Raycast(origin + offset * i, direction, out RaycastHit hit, climbLedgeRayLength,
                    climbLedgeLayer))
            {
                ledgeHit = hit;
                return true;
            }
        }

        return false;
    }

    public bool ObstacleLedgeCheck(Vector3 moveDirection, out LegdeData legdeData)
    {

        legdeData = new LegdeData();
            
        if (moveDirection == Vector3.zero)
        {
            return false;
        }

        var originOffset = 0.5f;
        var origin = transform.position + moveDirection * originOffset + Vector3.up;

        if (PhysicsUtil.ThreeRaycasts(origin, Vector3.down, 0.25f, transform, out List<RaycastHit>  hits, ledgeRayLength, obstacleLayer, true))
        {
            
            // Vector3 endPoint = origin + Vector3.down * ledgeRayLength;
            // Debug.DrawLine(origin, endPoint, Color.red);

            var validHits = hits.Where(h => transform.position.y - h.point.y > ledgeHeightThreshold).ToList();

            if (validHits.Count > 0)
            {
                // var surfaceOrigin = transform.position + moveDirection - (new Vector3(0, 0.1f, 0));

                var surfaceOrigin = validHits[0].point;
                surfaceOrigin.y = transform.position.y - 0.1f;
            
                // if(Physics.Raycast(surfaceOrigin, -moveDirection, out RaycastHit surfaceHit, 2, obstacleLayer))
                if(Physics.Raycast(surfaceOrigin, transform.position - surfaceOrigin, out RaycastHit surfaceHit, 2, obstacleLayer))
                {
                    
                    Debug.DrawLine(surfaceOrigin, transform.position, Color.cyan);
                    
                    float height = transform.position.y - validHits[0].point.y;

                    // if (height > ledgeHeightThreshold)
                    // {

                    legdeData.angle = Vector3.Angle(transform.forward, surfaceHit.normal);
                    legdeData.height = height;
                    legdeData.surfaceHit = surfaceHit;
                    
                    return true;
                    
                    // }
                }
            }
        }
        return false;
    }
    
    public struct ObstacleHitData
    {
        public bool forwardHitFound;
        public bool heightHitFound;
        
        public RaycastHit forwardHit;
        public RaycastHit heightHit;
    }

    public struct LegdeData
    {
        public float height;
        public float angle;
        public RaycastHit surfaceHit;
    }
}
