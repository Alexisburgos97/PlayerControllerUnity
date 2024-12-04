using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkourController : MonoBehaviour
{
    [SerializeField] private GatherInput gatherInput;
    [SerializeField] private List<ParkourAction> _parkourActions;
    [SerializeField] private ParkourAction _jumpDownAction;
    [SerializeField] private float autoJumpHeightLimit = 1f;
    
    private EnvironmentScanner _scanner;

    private Animator _animator;
    
    private PlayerController _playerController;

    // private bool inAction;

    private void Awake()
    {
        _scanner = GetComponent<EnvironmentScanner>();
        _animator = GetComponent<Animator>();
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        
        var hitData = _scanner.ObstacleCheck();

        if (gatherInput.tryToJump && !_playerController.InAction && !_playerController.IsHanging)
        {
            if (hitData.forwardHitFound)
            {
                foreach (var action in _parkourActions)
                {
                    if (action.CheckIfPossible(hitData, transform))
                    {
                        StartCoroutine(DoParkourAction(action));
                        gatherInput.tryToJump = false;
                        break;
                    }
                }
            }
        }

        // if (_playerController.IsOnLedge && !inAction && !hitData.forwardHitFound && gatherInput.tryToJump)
        if (_playerController.IsOnLedge && !_playerController.InAction && !hitData.forwardHitFound)
        {

            bool shouldJump = true;

            if (_playerController.LegdeData.height > autoJumpHeightLimit && !gatherInput.tryToJump)
            {
                shouldJump = false;
            }
            
            if (shouldJump && _playerController.LegdeData.angle <= 50 )
            {
                _playerController.IsOnLedge = false;
                StartCoroutine(DoParkourAction(_jumpDownAction));

                if (gatherInput.tryToJump)
                {
                    gatherInput.tryToJump = false;
                }
            }
        }
    }

    IEnumerator DoParkourAction(ParkourAction action)
    {
        _playerController.SetControl(false);

        MatchTargetParans matchTargetParans = null;

        if (action.EnableTargetMatching)
        {
            matchTargetParans = new MatchTargetParans()
            {
                pos = action.MatchPosition,
                bodyPart = action.MatchBodyPart,
                posWeight = action.MatchPosWeight,
                startTime = action.MatchStartTime,
                targetTime = action.MatchTargetTime
            };
        }

        yield return _playerController.DoAction(action.GetAnimName(), matchTargetParans, action.TargetRotation,
            action.GetRotateToObstacle(), action.PostActionDelay, action.Mirror);
        
        _playerController.SetControl(true);
    }

    /*IEnumerator DoParkourAction(ParkourAction action)
    {
        inAction = true;

        _playerController.SetControl(false);

        _animator.SetBool("mirrorAction", action.Mirror);

        _animator.CrossFade(action.GetAnimName(), 0.2f);

        yield return null;

        var animState = _animator.GetNextAnimatorStateInfo(0);

        if (!animState.IsName(action.GetAnimName()))
        {
            Debug.LogError("The Parkour animation is Wrong!");
        }

        // yield return new WaitForSeconds(animState.length);

        float timer = 0f;
        while (timer < animState.length)
        {
            timer += Time.deltaTime;

            if (action.GetRotateToObstacle())
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, action.TargetRotation, _playerController.GetRotationSpeed() * Time.deltaTime);
            }

            if (action.EnableTargetMatching)
            {
                MatchTarget(action);
            }

            if (_animator.IsInTransition(0) && timer > 0.5f)
            {
                break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(action.PostActionDelay);

        _playerController.SetControl(true);

        inAction = false;
    }

    private void MatchTarget(ParkourAction action)
    {
        if (_animator.isMatchingTarget || _animator.IsInTransition(0))
        {
           return;
        }

        _animator.MatchTarget(
            action.MatchPosition,
            transform.rotation,
            action.MatchBodyPart,
            new MatchTargetWeightMask(action.MatchPosWeight, 0),
            action.MatchStartTime,
            action.MatchTargetTime
        );
    }*/
}
