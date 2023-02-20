using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteMovementTask : ITask {
        private bool _isComplete = false;
        private float _speed;

        private Vector2 _directionVector;
        private float _verticalDirectionModifier;

        public InfiniteMovementTask(float angle, float speed) {
            float angleInRad = Mathf.Deg2Rad * Mathf.Abs(angle);

            if (angle >= 0) {
                _verticalDirectionModifier = 1;
            }
            else {
                _verticalDirectionModifier = -1;
            }

            _directionVector = new Vector2(
                Mathf.Sin((Mathf.Deg2Rad * 90) - angleInRad) / Mathf.Sin(90),
                Mathf.Sin(angleInRad) / Mathf.Sin(90) * _verticalDirectionModifier
            );

            _speed = speed * Time.fixedDeltaTime;
        }

        public MoveInstruction GetInstruction() {
            return new MoveInstruction(_directionVector.x * _speed, _directionVector.y * _speed, 0);
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
