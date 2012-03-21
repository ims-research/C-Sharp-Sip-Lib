using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class ServerTransaction : Transaction
    {
        public ServerTransaction(SIPApp app): base(app)
        {
            this.server = true;
        }

        public void start()
        {
            this.state = "trying";
            this.app.receivedRequest(this, this.request,this.stack);
        }

        public override void receivedRequest(Message request)
        {
            if (this.request.method == request.method)
            {
                if (this.state == "proceeding" || this.state == "completed")
                {
                    this.stack.send(this.lastResponse, this.remote, this.transport);
                }
                else if (this.state == "trying")
                {
                    // Ignore the retransmitted request
                }
            }
        }

        public void timeout(string name, int timeout)
        {
            if (this.state == "completed")
            {
                if (name == "J")
                {
                    this.state = "terminated";
                }
            }
        }

        public void error(string error)
        {
            if (this.state == "completed")
            {
                this.state = "terminated";
                this.app.error(this,error);
            }
        }

        public override void sendResponse(Message response)
        {
            this.lastResponse = response;
            if (response.is1xx())
            {
                if (this.state == "trying" || this.state == "proceeding")
                {
                    this.state = "proceeding";
                    this.stack.send(response, this.remote, this.transport);
                }
            }
            else if (response.isFinal())
            {
                if (this.state == "trying" || this.state == "proceeding")
                {
                    this.state = "completed";
                    this.stack.send(response, this.remote, this.transport);
                    if (!this.transport.reliable)
                    {
                        this.startTimer("J", this.timer.J());
                    }
                    else
                    {
                        this.timeout("J", 0);
                    }
                }
            }
        }
    }

}
