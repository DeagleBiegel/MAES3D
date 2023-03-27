using UnityEngine;

namespace MAES3D.Agent.Task {
    public class VerticalMovementTask : ITask {

        private ITask _proxyTask;

        public VerticalMovementTask(float targetDistance, float maxSpeed) {
            if(targetDistance >= 0) {
                _proxyTask = new MovementTask(90, targetDistance, maxSpeed);
            }
            else {
                _proxyTask = new MovementTask(-90, targetDistance, maxSpeed);
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
