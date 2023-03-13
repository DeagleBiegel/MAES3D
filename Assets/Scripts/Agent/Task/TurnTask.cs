using UnityEngine;

namespace MAES3D.Agent.Task {
    public class TurnTask : ITask
    {
        private float _targetDegrees;
        private float _maxTurnSpeed;
        private float _currentTurnSpeed;
        private float _accel = 1f * Time.fixedDeltaTime;
        private float _decel = 0.5f * Time.fixedDeltaTime;


        private float _turnedDegrees;
        private bool _isComplete = false;

        private float _directionModifier;

        public TurnTask(float targetDegrees, float maxTurnSpeed) {
            _targetDegrees = Mathf.Abs(targetDegrees % 360);
            _maxTurnSpeed = maxTurnSpeed * Time.fixedDeltaTime;
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

            float a = _currentTurnSpeed / _decel;
            float b = (_currentTurnSpeed * a) - (0.5f * _decel * Mathf.Pow(a, 2));

            if (b >= _remainingDegrees) {
                //Should start decelerating
                if (_currentTurnSpeed <= 0) {
                    _currentTurnSpeed = 0;
                }
                else {
                    _currentTurnSpeed -= _decel;
                }
            }
            else {
                //Should not decelerate
                if (_currentTurnSpeed >= _maxTurnSpeed) {
                    _currentTurnSpeed = _maxTurnSpeed;
                }
                else {
                    _currentTurnSpeed += _accel;
                }
            }

            if (_remainingDegrees < 0.001f) {
                _isComplete = true;
                return new MoveInstruction(0, 0, _remainingDegrees * _directionModifier);
            }

            _turnedDegrees += _currentTurnSpeed;
            return new MoveInstruction(0, 0, _currentTurnSpeed * _directionModifier);
        }
        
        public bool IsComplete() {
            return _isComplete;
        }
    }
}
