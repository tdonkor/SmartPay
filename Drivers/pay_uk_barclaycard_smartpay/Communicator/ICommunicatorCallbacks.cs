namespace UK_BARCLAYCARD_SMARTPAY.Communicator
{
    public interface ICommunicatorCallbacks
    {
        void InitRequest(object parameters);

        void TestRequest(object parameters);

        void PayRequest(object parameters);

        void CancelRequest(object parameters);
         
        void ExecuteCommandRequest(object parameters);
    }
}
