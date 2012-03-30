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

        public void start()
        {
            this.state = "trying";
            if (!this.transport.reliable)
            {
                this.startTimer("E", this.timer.E());
            }
            this.startTimer("F", this.timer.F());
            this.stack.send(this.request,this.remote,this.transport);
        }

        public override void receivedResponse(Message response)
        {
            if (response.is1xx())
            {
                if (this.state == "trying")
                {
                    this.state = "proceeding";
                    this.app.receivedResponse(this,response);
                }
                else if (this.state == "proceeding")
                {
                    this.app.receivedResponse(this,response);
                }
            }
            else if (response.isFinal())
            {
                if (this.state == "trying" || this.state == "proceeding")
                {
                    this.state = "completed";
                    this.app.receivedResponse(this,response);
                    if (!this.transport.reliable)
                    {
                        this.startTimer("K", this.timer.K());
                    }
                    else
                    {
                        this.timeout("K", 0);
                    }
                }
            }
        }

        public void timeout(string name, int timeout)
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
                    this.startTimer("E", timeout);
                    this.stack.send(this.request, this.remote, this.transport);
                }
                else if (name == "F")
                {
                    this.state = "terminated";
                    this.app.timeout(this);
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

        public void error(string error)
        {
            if (this.state == "trying" || this.state == "proceeding")
            {
                this.state = "terminated";
                this.app.error(this,error);
            }
        }
    }
}
