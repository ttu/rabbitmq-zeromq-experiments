namespace Common
{
    public interface IPub<TRequest>
    {
        void Start();

        void AddMessage(TRequest msg);
    }
}