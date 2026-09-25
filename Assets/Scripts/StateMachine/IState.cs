namespace TopDownGame.StateMachine
{
    public interface IState
    {
        void Enter();
        void Update();
        void PhysicsUpdate();
        void Exit();
    }
}
