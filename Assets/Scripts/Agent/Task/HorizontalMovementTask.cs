using UnityEngine;

namespace MAES3D.Agent.Task {
    public class HorizontalMovementTask : ITask {

        private float _targetDistance;
        private float _speed;

        private float _traveledDistance;
        private bool _isComplete = false;

        public HorizontalMovementTask(float targetDistance, float speed) {
            _targetDistance = targetDistance;
            _speed = speed * Time.fixedDeltaTime;
            _traveledDistance = 0;
        }

        public MoveInstruction GetInstruction() {
            float _remainingDistance = _targetDistance - _traveledDistance;

            if (_traveledDistance + _speed < _targetDistance) {
                _traveledDistance += _speed;
                return new MoveInstruction(_speed, 0, 0);
            }
            else {
                _isComplete = true;
                return new MoveInstruction(_remainingDistance, 0, 0);
            }
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
