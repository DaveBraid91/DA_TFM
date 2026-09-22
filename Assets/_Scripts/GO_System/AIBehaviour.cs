using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AISystem
{
    public class AIBehaviour : MonoBehaviour
    {
        [Header("Steering")]
        public float maxSpeed;
        public float steeringMaxSpeed;
        public float stoppingDistance;

        [Header("Display Settings")] 
        [SerializeField] private bool areVectorsOnDisplay;

        private float _sqrRemainingDistance;
        private bool _isPathPending;

        #region Behaviours

        public void Seek(Vector3 target, Rigidbody rb)
        {
            //Se calcula el vector de dirección y se normaliza
            var dir = target - transform.position;
            if (dir.sqrMagnitude < 0.0001f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            var desired = dir.normalized * maxSpeed;   // unidades/segundo
            var factor  = Arrive(target);              // 0..1 según distancia
            rb.linearVelocity = desired * factor; 
        }

        public void Flee(Vector3 target, Rigidbody rb)
        {
            //Se calcula el vector de dirección y se normaliza
            var targetDirection = - CalculateTargetDirection(target);

            var steeringDirection = CalculateSteeringDirection(targetDirection, rb.linearVelocity);

            var finalDirection = CalculateFinalDirection(steeringDirection, rb.linearVelocity);
            
            DisplayVectors(rb.linearVelocity, targetDirection, steeringDirection);

            rb.linearVelocity = CalculateFinalVelocity(finalDirection);
        }

        public void Pursue(Vector3 target, Rigidbody rb, Rigidbody targetRb)
        {
            var currentSqrSpeed = rb.linearVelocity.sqrMagnitude;
            var sqrDistanceToTarget = CalculateSqrDistance(target);
            if(currentSqrSpeed < 0.001f || Vector3.Dot(targetRb.linearVelocity.normalized, CalculateTargetDirection(target).normalized) < -0.8f)
                Seek(target, rb);
            else
            {
                var prediction = CalculatePrediction(sqrDistanceToTarget, currentSqrSpeed);

                var explicitTarget = CalculatePredictionExplicitTarget(target, targetRb, prediction);
                Seek(explicitTarget, rb);
            }
        }
        
        public void Evade(Vector3 target, Rigidbody rb, Rigidbody targetRb)
        {
            var currentSqrSpeed = rb.linearVelocity.sqrMagnitude;
            var sqrDistanceToTarget = CalculateSqrDistance(target);
            //Debug.Log($"Speed: {currentSqrSpeed.ToString()}\nDistance: {sqrDistanceToTarget.ToString()}, Target: {target.ToString()}");
            //Debug.Log(Vector3.Dot(targetRb.velocity.normalized, CalculateTargetDirection(target).normalized));
            if (currentSqrSpeed < 0.001f || Vector3.Dot(targetRb.linearVelocity.normalized, CalculateTargetDirection(target).normalized) < -0.8f)
            {
                Flee(target, rb);
                Debug.Log("Fleeing");
            }
            else
            {
                var prediction = CalculatePrediction(sqrDistanceToTarget, currentSqrSpeed);

                var explicitTarget = CalculatePredictionExplicitTarget(target, targetRb, prediction);
                Flee(explicitTarget, rb);
                Debug.Log("Evading");
            }
        }

        public void Wander(Rigidbody rb)
        {
            var displacement = CalculateWanderDisplacement(rb.linearVelocity);

            var wanderDirection = CalculateWanderDirection(rb.linearVelocity.normalized, displacement);
            
            var steeringDirection = CalculateSteeringDirection(wanderDirection, rb.linearVelocity);

            var finalDirection = CalculateFinalDirection(steeringDirection, rb.linearVelocity);
            
            DisplayVectors(rb.linearVelocity, wanderDirection, steeringDirection);

            rb.linearVelocity = CalculateFinalVelocity(finalDirection);
        }
        
        private float Arrive(Vector3 target)
        {
            _sqrRemainingDistance = (target - transform.position).sqrMagnitude;
            if (_sqrRemainingDistance > Mathf.Pow(stoppingDistance, 2)) 
                return 1;
            
            _isPathPending = false;

            return (_sqrRemainingDistance / Mathf.Pow(stoppingDistance, 2));
        }

        #endregion

        #region Calculations

        private float CalculateSqrDistance(Vector3 target)
        {
            return (target - transform.position).sqrMagnitude;
        }
        
        private Vector3 CalculateTargetDirection(Vector3 target)
        {
            return (target - transform.position).normalized * maxSpeed;
        }
        
        private Vector3 CalculatePredictionExplicitTarget(Vector3 target, Rigidbody targetRb, float prediction)
        {
            return target + targetRb.linearVelocity * prediction;
        }

        private Vector3 CalculateSteeringDirection(Vector3 targetDirection, Vector3 currentVelocity)
        {
            var steeringDirection = targetDirection - currentVelocity;
            
            return steeringDirection.sqrMagnitude > Mathf.Pow(steeringMaxSpeed, 2)
                ? steeringDirection.normalized * steeringMaxSpeed
                : steeringDirection;
        }

        private float CalculatePrediction(float sqrDistance, float sqrSpeed)
        {
            return Mathf.Sqrt(sqrDistance / sqrSpeed);
        }

        private Vector3 CalculateWanderDisplacement(Vector3 currentVelocity)
        {
            var randomPoint = Random.insideUnitCircle;
            
            return Quaternion.LookRotation(currentVelocity) * new Vector3(randomPoint.x, 0, randomPoint.y);
        }

        private Vector3 CalculateWanderDirection(Vector3 circleCenter, Vector3 displacement)
        {
            return (circleCenter + displacement).normalized * maxSpeed;
        }

        private Vector3 CalculateFinalDirection(Vector3 steeringDirection, Vector3 currentVelocity)
        {
            return currentVelocity + steeringDirection;
        }

        private Vector3 CalculateFinalVelocity(Vector3 finalDirection)
        {
            return finalDirection.sqrMagnitude > Mathf.Pow(maxSpeed, 2)
                ? finalDirection.normalized * maxSpeed
                : finalDirection;
        }

        #endregion

        #region DisplayOptions

        private void DisplayVectors(Vector3 currentVelocity, Vector3 targetDirection, Vector3 steeringDirection)
        {
            if(!areVectorsOnDisplay) return;
            
            Debug.DrawRay(transform.position, currentVelocity, Color.blue);
            Debug.DrawRay(transform.position, targetDirection, Color.green);
            Debug.DrawRay(transform.position + currentVelocity, steeringDirection * 10, Color.red);
        }

        #endregion
        
    }
}

