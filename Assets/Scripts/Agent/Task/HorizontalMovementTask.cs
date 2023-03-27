using UnityEngine;

namespace MAES3D.Agent.Task {
    public class HorizontalMovementTask : ITask {

        private ITask _proxyTask;

        public HorizontalMovementTask(float targetDistance, float maxSpeed) {
            _proxyTask = new MovementTask(0, targetDistance, maxSpeed);
        }

        public MoveInstruction GetInstruction() {
            return _proxyTask.GetInstruction();
        }

        public bool IsComplete() {
            return _proxyTask.IsComplete();
        }
    }
}
