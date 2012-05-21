using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class ServerTransaction : Transaction
    {
        public ServerTransaction(UserAgent app): base(app)
        {
            this.server = true;
        }

        public void Start()
        {
            this.state = "trying";
            this.app.ReceivedRequest(this, this.request,this.stack);
        }

        public override void ReceivedRequest(Message request)
        {
            if (this.request.method == request.method)
            {
                if (this.state == "proceeding" || this.state == "completed")
                {
                    this.stack.Send(this.lastResponse, this.remote, this.transport);
                }
                else if (this.state == "trying")
                {
                    // Ignore the retransmitted request
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (this.state == "completed")
            {
                if (name == "J")
                {
                    this.state = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (this.state == "completed")
            {
                this.state = "terminated";
                this.app.Error(this,error);
            }
        }

        public override void SendResponse(Message response)
        {
            this.lastResponse = response;
            if (response.Is1xx())
            {
                if (this.state == "trying" || this.state == "proceeding")
                {
                    this.state = "proceeding";
                    this.stack.Send(response, this.remote, this.transport);
                }
            }
            else if (response.IsFinal())
            {
                if (this.state == "trying" || this.state == "proceeding")
                {
                    this.state = "completed";
                    this.stack.Send(response, this.remote, this.transport);
                    if (!this.transport.reliable)
                    {
                        this.StartTimer("J", this.timer.J());
                    }
                    else
                    {
                        this.Timeout("J", 0);
                    }
                }
            }
        }
    }

}
