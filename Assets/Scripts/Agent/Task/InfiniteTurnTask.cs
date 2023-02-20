using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteTurnTask : ITask {
        private bool _isComplete = false;
        private float _speed;

        public InfiniteTurnTask(float speed) {
            _speed = speed * Time.fixedDeltaTime;
        }

        public MoveInstruction GetInstruction() {
            return new MoveInstruction(0, 0, _speed);
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}
