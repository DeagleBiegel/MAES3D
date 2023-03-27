using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteVerticalMovementTask : ITask {

        private ITask _proxyTask;

        public InfiniteVerticalMovementTask(float maxSpeed, bool moveUp = true) {
            if (moveUp) {
                _proxyTask = new InfiniteMovementTask(90, maxSpeed);
            }
            else {
                _proxyTask = new InfiniteMovementTask(-90, maxSpeed);
            }
        }

        public MoveInstruction GetInstruction() {
            return _proxyTask.GetInstruction();
        }

        public bool IsComplete() {
            return _proxyTask.IsComplete();
        }
    }
}
