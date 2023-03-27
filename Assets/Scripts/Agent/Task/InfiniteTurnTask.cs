using UnityEngine;

namespace MAES3D.Agent.Task {
    public class InfiniteTurnTask : ITask {

        private ITask _proxyTask;

        public InfiniteTurnTask(float maxTurnSpeed, bool moveRight = true) {
            if(moveRight) {
                _proxyTask = new TurnTask(float.PositiveInfinity, maxTurnSpeed);
            }
            else {
                _proxyTask = new TurnTask(float.NegativeInfinity, maxTurnSpeed);
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
