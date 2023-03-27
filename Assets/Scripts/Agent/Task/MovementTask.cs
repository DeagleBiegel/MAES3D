using System;
using UnityEditor.PackageManager;
using UnityEngine;

namespace MAES3D.Agent.Task {
    public class MovementTask : ITask {

        private Vector2 _directionVector;

        private float _maxSpeed;
        private float _currentSpeed = 0;
        private float _accel = 0.01f * Time.fixedDeltaTime;
        private float _decel = 0.005f * Time.fixedDeltaTime;

        private float _targetDistance;
        private float _traveledDistance;
        private float _verticalDirectionModifier;

        private bool _isComplete = false;

        public MovementTask(float angle, float targetDistance, float MaxSpeed) {

            float angleInRad = Mathf.Deg2Rad * Mathf.Abs(angle);

            float forwardDistance = targetDistance * Mathf.Sin((Mathf.Deg2Rad * 90) - angleInRad) / Mathf.Sin(90);
            float verticalDistance = targetDistance * Mathf.Sin(angleInRad) / Mathf.Sin(90);

            if (angle >= 0) {
                _verticalDirectionModifier = 1;
            }
            else {
                _verticalDirectionModifier = -1;
            }

            _directionVector = new Vector2(forwardDistance, verticalDistance * _verticalDirectionModifier).normalized;
            _targetDistance = targetDistance;

            _maxSpeed = MaxSpeed * Time.fixedDeltaTime;
            _traveledDistance = 0;
        }

        public MoveInstruction GetInstruction() {
            float _remainingDistance = _targetDistance - _traveledDistance;

            /*idk man det er noget fysik nørde shit. Equation for the deceleration eller sådan noget hø. v^2 = u^2 - 2as idk man*/
            float a = _currentSpeed / _decel;
            float b = (_currentSpeed * a) - (0.5f * _decel * Mathf.Pow(a, 2));

            if (b >= _remainingDistance) {
                //Should start decelerating
                if (_currentSpeed <= 0) {
                    _currentSpeed = 0;
                }
                else {
                    _currentSpeed -= _decel;
                }
            }
            else {
                //Should not decelerate
                if (_currentSpeed >= _maxSpeed) {
                    _currentSpeed = _maxSpeed;
                }
                else {
                    _currentSpeed += _accel;
                }
            }

            if(_remainingDistance < 0.001f) {
                _isComplete = true;
                return new MoveInstruction(_directionVector.x * _remainingDistance, _directionVector.y * _remainingDistance, 0);
            }

            _traveledDistance += _currentSpeed;
            return new MoveInstruction(_directionVector.x * _currentSpeed, _directionVector.y * _currentSpeed, 0);
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
