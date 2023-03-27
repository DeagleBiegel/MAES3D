using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteMovementTask : ITask {

        private ITask _proxyTask;

        public InfiniteMovementTask(float angle, float maxSpeed) {
            _proxyTask = new MovementTask(angle, float.PositiveInfinity, maxSpeed);
        }

        public MoveInstruction GetInstruction() {
            return _proxyTask.GetInstruction();
        }

        public bool IsComplete() {
            return _proxyTask.IsComplete();
        }
    }
}
