using System;

namespace SIPLib.SIP
{
    class ClientTransaction : Transaction
    {
        public ClientTransaction(UserAgent app) : base(app)
        {
            server = false;
        }

        public void Start()
        {
            state = "trying";
            if (!transport.reliable)
            {
                StartTimer("E", timer.E());
            }
            StartTimer("F", timer.F());
            stack.Send(request,remote,transport);
        }

        public override void ReceivedResponse(Message response)
        {
            if (response.Is1xx())
            {
                if (state == "trying")
                {
                    state = "proceeding";
                    app.ReceivedResponse(this,response);
                }
                else if (state == "proceeding")
                {
                    app.ReceivedResponse(this,response);
                }
            }
            else if (response.IsFinal())
            {
                if (state == "trying" || state == "proceeding")
                {
                    state = "completed";
                    app.ReceivedResponse(this,response);
                    if (!transport.reliable)
                    {
                        StartTimer("K", timer.K());
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
            if (state == "trying" | state == "proceeding")
            {
                if (name == "E")
                {
                    if (state == "trying")
                    {
                        timeout = Math.Min(2 * timeout, timer.T2);
                    }
                    else
                    {
                        timeout = timer.T2;
                    }
                    StartTimer("E", timeout);
                    stack.Send(request, remote, transport);
                }
                else if (name == "F")
                {
                    state = "terminated";
                    app.Timeout(this);
                }
            }
            else if (state == "completed")
            {
                if (name == "K")
                {
                    state = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (state == "trying" || state == "proceeding")
            {
                state = "terminated";
                app.Error(this,error);
            }
        }
    }
}
