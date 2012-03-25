using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class InviteServerTransaction : Transaction
    {
        public InviteServerTransaction(SIPApp app ) : base(app)
        {
            this.server = true;
        }

        public  void start()
        {
            this.state = "proceeding";
            this.sendResponse(this.createResponse(100, "Trying"));
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
            }
            else if (request.method == "ACK")
            {
                if (this.state == "completed")
                {
                    this.state = "confirmed";
                    if (!this.transport.reliable)
                    {
                        this.startTimer("I", this.timer.I());
                    }
                    else
                    {
                        this.timeout("I", 0);
                    }
                }
                else if (this.state == "confirmed")
                {
                    //Ignore duplicate ACK
                }
            }
        }

        public void timeout(string name, int timeout)
        {
            if (this.state == "completed")
            {
                if (name == "G")
                {
                    this.startTimer("G", Math.Min(2 * timeout, this.timer.T2));
                    this.stack.send(this.lastResponse, this.remote, this.transport);
                }
                else if (name == "H")
                {
                    this.state = "terminated";
                    this.app.timeout(this);
                }
            }
            else if (this.state == "confirmed")
            {
                if (name == "I")
                {
                    this.state = "terminated";
                }
            }
        }

        public  void error(string error)
        {
            if (this.state == "proceeding" || this.state == "confirmed")
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
                if (this.state == "proceeding")
                {
                    this.stack.send(response, this.remote, this.transport);
                }
            }
            else if (response.is2xx())
            {
                if (this.state == "proceeding")
                {
                    this.state = "terminated";
                    this.stack.send(response, this.remote, this.transport);
                }
            }
            else
            {
                if (this.state == "proceeding")
                {
                    this.state = "completed";
                    if (!this.transport.reliable)
                    {
                        this.startTimer("G", this.timer.G());
                    }
                    this.startTimer("H", this.timer.H());
                    this.stack.send(response, this.remote, this.transport);
                }
            }
        }
    }
}
