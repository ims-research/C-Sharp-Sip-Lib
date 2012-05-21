using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    class ClientTransaction : Transaction
    {
        public ClientTransaction(UserAgent app) : base(app)
        {
            this.server = false;
        }

        public void Start()
        {
            this.state = "trying";
            if (!this.transport.reliable)
            {
                this.StartTimer("E", this.timer.E());
            }
            this.StartTimer("F", this.timer.F());
            this.stack.Send(this.request,this.remote,this.transport);
        }

        public override void ReceivedResponse(Message response)
        {
            if (response.Is1xx())
            {
                if (this.state == "trying")
                {
                    this.state = "proceeding";
                    this.app.ReceivedResponse(this,response);
                }
                else if (this.state == "proceeding")
                {
                    this.app.ReceivedResponse(this,response);
                }
            }
            else if (response.IsFinal())
            {
                if (this.state == "trying" || this.state == "proceeding")
                {
                    this.state = "completed";
                    this.app.ReceivedResponse(this,response);
                    if (!this.transport.reliable)
                    {
                        this.StartTimer("K", this.timer.K());
                    }
                    else
                    {
                        this.Timeout("K", 0);
                    }
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (this.state == "trying" | this.state == "proceeding")
            {
                if (name == "E")
                {
                    if (this.state == "trying")
                    {
                        timeout = Math.Min(2 * timeout, this.timer.T2);
                    }
                    else
                    {
                        timeout = this.timer.T2;
                    }
                    this.StartTimer("E", timeout);
                    this.stack.Send(this.request, this.remote, this.transport);
                }
                else if (name == "F")
                {
                    this.state = "terminated";
                    this.app.Timeout(this);
                }
            }
            else if (this.state == "completed")
            {
                if (name == "K")
                {
                    this.state = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (this.state == "trying" || this.state == "proceeding")
            {
                this.state = "terminated";
                this.app.Error(this,error);
            }
        }
    }
}
