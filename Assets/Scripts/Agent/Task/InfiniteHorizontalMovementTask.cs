using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteHorizontalMovementTask : ITask {
        private bool _isComplete = false;
        private float _speed;


        public InfiniteHorizontalMovementTask(float speed) {
            _speed = speed * Time.fixedDeltaTime;
        }

        public MoveInstruction GetInstruction() {
            return new MoveInstruction(_speed, 0, 0);
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
