using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteHorizontalMovementTask : ITask {

        private ITask _proxyTask;

        public InfiniteHorizontalMovementTask(float maxSpeed) {
            _proxyTask = new InfiniteMovementTask(0, maxSpeed);
        }

        public MoveInstruction GetInstruction() {
            return _proxyTask.GetInstruction();
        }

        public bool IsComplete() {
            return _proxyTask.IsComplete();
        }
    }
}
