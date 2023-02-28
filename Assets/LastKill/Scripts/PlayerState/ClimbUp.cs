using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastKill
{
    public class ClimbUp : AbstractAbilityState
    {
        [SerializeField] private LayerMask shortClimbMask;
        [SerializeField] private float overlapRadius = 0.75f;
        [SerializeField] private float capsuleCastRadius = 0.2f;
        [SerializeField] private float capsuleCastHeight = 1f;
        [SerializeField] private float minClimbHeight = 0.5f;
        [SerializeField] private float maxClimbHeight = 1.5f;
        [Header("Animation")]
        [SerializeField] private string shortClimbAnimState = "Climb Up";

        //private CastDebug _debug;

        private RaycastHit _targetHit;
        private bool _hasMatchTarget;

        private void Awake()
        {

        }

        public override bool ReadyToStart()
        {
            return !_move.IsGrounded() && HasShortClimb();
        }

        public override void OnStartState()
        {
            _capsule.DisableCollision();
            _move.DisableGravity();
            _move.ApplyRootMotion(Vector3.one);
            _move.StopMovement();

            _animator.CrossFadeInFixedTime(shortClimbAnimState, 0.1f);
            _hasMatchTarget = false;
        }


        public override void UpdateState()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);

            if (_animator.IsInTransition(0) || !state.IsName(shortClimbAnimState)) return;

            var normalizedTime = Mathf.Repeat(state.normalizedTime, 1f);
            if (!_animator.isMatchingTarget && !_hasMatchTarget)
            {
                // calculate target position
                Vector3 targetPosition = _targetHit.point - _targetHit.normal * _capsule.GetCapsuleRadius() * 0.5f;
                _animator.MatchTarget(targetPosition, Quaternion.identity, AvatarTarget.Root,
                    new MatchTargetWeightMask(Vector3.one, 0f), 0.15f, 0.42f);

                _hasMatchTarget = true;
            }

            if (normalizedTime > 0.95f)
                StopState();
        }

        public override void OnStopState()
        {
            _capsule.EnableCollision();
            _move.EnableGravity();
            _move.StopRootMotion();
            _move.StopMovement();
        }

        private bool HasShortClimb()
        {
            Vector3 overlapCenter = transform.position + Vector3.up * overlapRadius;

            if (Physics.OverlapSphere(overlapCenter, overlapRadius, shortClimbMask, QueryTriggerInteraction.Collide).Length > 0)
            { // found some short climb object

                // capsule cast points
                Vector3 p1 = transform.position + Vector3.up * (minClimbHeight + capsuleCastRadius);
                Vector3 p2 = transform.position + Vector3.up * (capsuleCastHeight - capsuleCastRadius);
                Vector3 castDirection = transform.forward;

                if (Physics.CapsuleCast(p1, p2, capsuleCastRadius, castDirection, out RaycastHit forwardHit,
                    overlapRadius, shortClimbMask, QueryTriggerInteraction.Collide))
                {
                    Vector3 sphereStart = forwardHit.point;
                    sphereStart.y = transform.position.y + maxClimbHeight + capsuleCastRadius;

                    // check top
                    if (Physics.SphereCast(sphereStart, capsuleCastRadius, Vector3.down, out RaycastHit topHit, maxClimbHeight - minClimbHeight,
                        shortClimbMask, QueryTriggerInteraction.Collide))
                    {
                        _targetHit = topHit;
                        _targetHit.normal = Vector3.Scale(forwardHit.normal, new Vector3(1, 0, 1)).normalized;

                        //if (_debug)
                        //    _debug.DrawSphere(_targetHit.point, 0.1f, Color.red, 3f);

                        return true;
                    }
                }

            }

            return false;
        }
    }
}

