using UnityEngine;

namespace MAES3D.Agent.Task {
    public class TurnTask : ITask
    {
        private float _targetDegrees;
        private float _speed;

        private float _turnedDegrees;
        private bool _isComplete = false;

        private float _directionModifier;

        public TurnTask(float targetDegrees, float speed) {
            _targetDegrees = Mathf.Abs(targetDegrees % 360);
            _speed = speed * Time.fixedDeltaTime;
            _turnedDegrees = 0;

            if(Mathf.Abs(_targetDegrees) < 180) {
                _directionModifier = 1;
            }
            else {
                _targetDegrees = 360 - _targetDegrees;
                _directionModifier = -1;
            }

            if(targetDegrees < 0) {
                _directionModifier = -_directionModifier;
            }
        }
        
        public MoveInstruction GetInstruction() {
            float _remainingDegrees = _targetDegrees - _turnedDegrees;

            if (_turnedDegrees + _speed < _targetDegrees) {
                _turnedDegrees += _speed;
                return new MoveInstruction(0, 0, _speed * _directionModifier);
            }
            else {
                _isComplete = true;
                return new MoveInstruction(0, 0, _remainingDegrees * _directionModifier);
            }
        }
        
        public bool IsComplete() {
            return _isComplete;
        }
    }
}
