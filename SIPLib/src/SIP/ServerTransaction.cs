namespace SIPLib.SIP
{
    public class ServerTransaction : Transaction
    {
        public ServerTransaction(UserAgent app) : base(app)
        {
            Server = true;
        }

        public void Start()
        {
            State = "trying";
            if (App is Dialog)
            {
                ((App)).ReceivedRequest(this, Request);
            }
            else App.ReceivedRequest(this, Request);
        }

        public override void ReceivedRequest(Message receivedRequest)
        {
            if (Request.Method == receivedRequest.Method)
            {
                if (State == "proceeding" || State == "completed")
                {
                    Stack.Send(LastResponse, Remote, Transport);
                }
                else if (State == "trying")
                {
                    // Ignore the retransmitted request
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (State == "completed")
            {
                if (name == "J")
                {
                    State = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (State == "completed")
            {
                State = "terminated";
                App.Error(this, error);
            }
        }

        public override void SendResponse(Message response)
        {
            LastResponse = response;
            if (response.Is1XX())
            {
                if (State == "trying" || State == "proceeding")
                {
                    State = "proceeding";
                    Stack.Send(response, Remote, Transport);
                }
            }
            else if (response.IsFinal())
            {
                if (State == "trying" || State == "proceeding")
                {
                    State = "completed";
                    Stack.Send(response, Remote, Transport);
                    if (!Transport.Reliable)
                    {
                        StartTimer("J", Timer.J());
                    }
                    else
                    {
                        Timeout("J", 0);
                    }
                }
            }
        }
    }
}