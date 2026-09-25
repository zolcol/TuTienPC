using TopDownGame.StateMachine;

namespace TopDownGame.Player
{
    public abstract class PlayerBaseState : IState
    {
        protected PlayerController player;
        protected TopDownGame.StateMachine.StateMachine stateMachine;

        public PlayerBaseState(PlayerController player, TopDownGame.StateMachine.StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
    }
}
