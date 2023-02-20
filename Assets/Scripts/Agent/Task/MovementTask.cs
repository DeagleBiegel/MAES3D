using UnityEngine;

namespace MAES3D.Agent.Task {
    public class MovementTask : ITask {

        private Vector2 _directionVector;

        private float _speed;

        private float _targetDistance;
        private float _traveledDistance;
        private float _verticalDirectionModifier;

        private bool _isComplete = false;

        public MovementTask(float angle, float targetDistance, float speed) {

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

            _speed = speed * Time.fixedDeltaTime;
            _traveledDistance = 0;
        }

        public MoveInstruction GetInstruction() {
            float _remainingDistance = _targetDistance - _traveledDistance;

            if (_traveledDistance + _speed < _targetDistance) {
                _traveledDistance += _speed;
                return new MoveInstruction(_directionVector.x * _speed, _directionVector.y * _speed, 0);
            }
            else {
                _isComplete = true;
                return new MoveInstruction(_directionVector.x * _remainingDistance, _directionVector.y * _remainingDistance, 0);
            }
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
