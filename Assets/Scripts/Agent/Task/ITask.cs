namespace MAES3D.Agent.Task {
    public interface ITask {

        public MoveInstruction GetInstruction();

        public bool IsComplete();
    }
}