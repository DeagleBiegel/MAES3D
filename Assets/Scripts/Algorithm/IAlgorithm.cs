using MAES3D.Agent;

namespace MAES3D.Algorithm {
    public interface IAlgorithm {
        
        public void SetController(IAgentController controller);

        public void UpdateLogic();

        public string GetInformation();

        public void Communicate(SubmarineAgent agent);
    }
}
