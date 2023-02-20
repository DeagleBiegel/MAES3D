namespace MAES3D.Agent {
    public class MoveInstruction {

        public readonly float HorizontalSpeed;
        public readonly float VerticalSpeed;
        public readonly float TurnSpeed;

        public static readonly MoveInstruction Forward =    new MoveInstruction(1f, 0f, 0f);
        public static readonly MoveInstruction Reverse =    new MoveInstruction(-1f, 0f, 0f);
        public static readonly MoveInstruction Up =         new MoveInstruction(0f, 1f, 0f);
        public static readonly MoveInstruction Down =       new MoveInstruction(0f, -1f, 0f);
        public static readonly MoveInstruction Right =      new MoveInstruction(0f, 0f, 1f);
        public static readonly MoveInstruction Left =       new MoveInstruction(0f, 0f, -1f);
        public static readonly MoveInstruction NoMovement = new MoveInstruction(0f, 0f, 0f);

        public MoveInstruction(float horizontalSpeed, float verticalSpeed, float turnSpeed) {
            HorizontalSpeed = horizontalSpeed;
            VerticalSpeed = verticalSpeed;
            TurnSpeed = turnSpeed;
        }
    }
}
