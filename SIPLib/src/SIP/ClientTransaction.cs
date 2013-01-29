#region

using System;

#endregion

namespace SIPLib.SIP
{
    internal class ClientTransaction : Transaction
    {
        public ClientTransaction(UserAgent app) : base(app)
        {
            Server = false;
        }

        public void Start()
        {
            State = "trying";
            if (!Transport.Reliable)
            {
                StartTimer("E", Timer.E());
            }
            StartTimer("F", Timer.F());
            Stack.Send(Request, Remote, Transport);
        }

        public override void ReceivedResponse(Message response)
        {
            if (response.Is1XX())
            {
                if (State == "trying")
                {
                    State = "proceeding";
                    App.ReceivedResponse(this, response);
                }
                else if (State == "proceeding")
                {
                    App.ReceivedResponse(this, response);
                }
            }
            else if (response.IsFinal())
            {
                if (State == "trying" || State == "proceeding")
                {
                    State = "completed";
                    App.ReceivedResponse(this, response);
                    if (!Transport.Reliable)
                    {
                        StartTimer("K", Timer.K());
                    }
                    else
                    {
                        Timeout("K", 0);
                    }
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (State == "trying" | State == "proceeding")
            {
                if (name == "E")
                {
                    timeout = State == "trying" ? Math.Min(2*timeout, Timer.T2) : Timer.T2;
                    StartTimer("E", timeout);
                    Stack.Send(Request, Remote, Transport);
                }
                else if (name == "F")
                {
                    State = "terminated";
                    App.Timeout(this);
                }
            }
            else if (State == "completed")
            {
                if (name == "K")
                {
                    State = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (State == "trying" || State == "proceeding")
            {
                State = "terminated";
                App.Error(this, error);
            }
        }
    }
}