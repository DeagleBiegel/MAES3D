using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteVerticalMovementTask : ITask {
        private bool _isComplete = false;
        private float _speed;


        public InfiniteVerticalMovementTask(float speed) {
            _speed = speed * Time.fixedDeltaTime;
        }

        public MoveInstruction GetInstruction() {
            return new MoveInstruction(0, _speed, 0);
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
