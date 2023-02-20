using UnityEngine;

namespace MAES3D.Agent.Task {
    public class VerticalMovementTask : ITask {

        private float _targetDistance;
        private float _speed;

        private float _traveledDistance;
        private bool _isComplete = false;

        public VerticalMovementTask(float targetDistance, float speed) {
            _targetDistance = targetDistance;
            _speed = speed * Time.fixedDeltaTime;
            _traveledDistance = 0;
        }

        public MoveInstruction GetInstruction() {
            float _remainingDistance = _targetDistance - _traveledDistance;

            if (_traveledDistance + _speed < _targetDistance) {
                _traveledDistance += _speed;
                return new MoveInstruction(0, _speed, 0);
            }
            else {
                _isComplete = true;
                return new MoveInstruction(0, _remainingDistance, 0);
            }
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
