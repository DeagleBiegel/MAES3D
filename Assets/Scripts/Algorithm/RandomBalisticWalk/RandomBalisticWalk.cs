using MAES3D.Agent;
using MAES3D.Algorithm;
using UnityEngine;

namespace MAES3D.Algorithm.RandomBalisticWalk {
    public class RandomBalisticWalk : IAlgorithm {

        private IAgentController _controller;

        float _directionAngle = 45;

        public void SetController(IAgentController controller) {
            _controller = controller;
        }

        public void UpdateLogic() {

            if (_controller.GetCurrentStatus() == Status.Idle) {

                switch (_controller.GetCurrentCollision()) {
                    case CollisionType.floor:
                        _directionAngle = Random.Range(15, 45);
                        _controller.Move(_directionAngle, 0);
                        break;
                    case CollisionType.ceiling:
                        _directionAngle = Random.Range(-15, -45);
                        _controller.Move(_directionAngle, 0);
                        break;
                    case CollisionType.wall:
                        _controller.Turn(Random.Range(90, 270));
                        break;
                    default:
                        _controller.Move(_directionAngle, 0);
                        break;
                }
            }
        }

        public string GetInformation(){
            return "This algorithm does not contain additional information.";
        }

        public void Communicate(SubmarineAgent agent) {
            return;
        }
    }
}
