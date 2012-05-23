using System;

namespace SIPLib.SIP
{
    public class InviteServerTransaction : Transaction
    {
        public InviteServerTransaction(UserAgent app ) : base(app)
        {
            Server = true;
        }

        public  void Start()
        {
            State = "proceeding";
            SendResponse(CreateResponse(100, "Trying"));
            App.ReceivedRequest(this, Request,Stack);
        }

        public override void ReceivedRequest(Message receivedRequest)
        {
            if (Request.Method == receivedRequest.Method)
            {
                if (State == "proceeding" || State == "completed")
                {
                    Stack.Send(LastResponse, Remote, Transport);
                }
            }
            else if (receivedRequest.Method == "ACK")
            {
                if (State == "completed")
                {
                    State = "confirmed";
                    if (!Transport.Reliable)
                    {
                        StartTimer("I", Timer.I());
                    }
                    else
                    {
                        Timeout("I", 0);
                    }
                }
                else if (State == "confirmed")
                {
                    //Ignore duplicate ACK
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (State == "completed")
            {
                if (name == "G")
                {
                    StartTimer("G", Math.Min(2 * timeout, Timer.T2));
                    Stack.Send(LastResponse, Remote, Transport);
                }
                else if (name == "H")
                {
                    State = "terminated";
                    App.Timeout(this);
                }
            }
            else if (State == "confirmed")
            {
                if (name == "I")
                {
                    State = "terminated";
                }
            }
        }

        public  void Error(string error)
        {
            if (State == "proceeding" || State == "confirmed")
            {
                State = "terminated";
                App.Error(this,error);
            }
        }

        public override void SendResponse(Message response)
        {
            LastResponse = response;
            if (response.Is1XX())
            {
                if (State == "proceeding")
                {
                    Stack.Send(response, Remote, Transport);
                }
            }
            else if (response.Is2XX())
            {
                if (State == "proceeding")
                {
                    State = "terminated";
                    Stack.Send(response, Remote, Transport);
                }
            }
            else
            {
                if (State == "proceeding")
                {
                    State = "completed";
                    if (!Transport.Reliable)
                    {
                        StartTimer("G", Timer.G());
                    }
                    StartTimer("H", Timer.H());
                    Stack.Send(response, Remote, Transport);
                }
            }
        }
    }
}
