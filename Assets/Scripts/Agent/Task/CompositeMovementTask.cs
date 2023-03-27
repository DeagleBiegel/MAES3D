using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace MAES3D.Agent.Task {
    public class CompositeMovementTask : ITask {

        private bool _isComplete = false;
        private Queue<ITask> _taskQueue = new Queue<ITask>();

        private Transform _agentTransform;

        public CompositeMovementTask(List<Vector3> relativeTargets, float moveSpeed, float turnSpeed, Transform agentTransform) {

            _agentTransform = agentTransform;

            float currentAngle = _agentTransform.eulerAngles.y;
            for (int i = 0; i < relativeTargets.Count; i++) {
                Vector3 nextTarget = relativeTargets[i];

                float turnAngle = GetAngleTurn(nextTarget) - currentAngle;
                _taskQueue.Enqueue(new TurnTask(turnAngle, turnSpeed));

                float moveAngle = GetAngleMove(nextTarget);
                float moveDistance = nextTarget.magnitude;
                _taskQueue.Enqueue(new MovementTask(moveAngle, moveDistance, moveSpeed));

                currentAngle = GetAngleTurn(nextTarget);
            }
        }

        public MoveInstruction GetInstruction() {

            MoveInstruction instruction = _taskQueue.Peek().GetInstruction();

            if (_taskQueue.Peek().IsComplete()) {
                _taskQueue.Dequeue();
            }

            if (_taskQueue.Count == 0) {
                _isComplete = true;
            }

            return instruction;
        }

        private float GetAngleTurn(Vector3 targetPos) {
            if (targetPos.x == 0) {
                return 90 - Mathf.Sign(targetPos.z) * 90; ;
            }
            if (targetPos.z == 0) {
                return 90 * Mathf.Sign(targetPos.x);
            }

            float a = targetPos.x;
            float b = targetPos.z;
            float c = Mathf.Sqrt(Mathf.Pow(b, 2) + Mathf.Pow(a, 2));

            float angle = Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(b, 2) + Mathf.Pow(c, 2) - Mathf.Pow(a, 2)) / (2 * b * c));

            return angle * Mathf.Sign(targetPos.x);
        }

        private float GetAngleMove(Vector3 targetPos) {

            if (targetPos.x == 0 && targetPos.z == 0) {
                return Mathf.Sign(targetPos.y) * 90;
            }

            float a2 = targetPos.y;
            float b2 = targetPos.magnitude;
            float c2 = new Vector3(targetPos.x, 0, targetPos.z).magnitude;
            float angle = Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(b2, 2) + Mathf.Pow(c2, 2) - Mathf.Pow(a2, 2)) / (2 * b2 * c2));

            angle *= Mathf.Sign(targetPos.y);

            return angle;
        }

        public bool IsComplete() {
            return _isComplete;
        }
    }
}