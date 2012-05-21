using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPLib
{
    public class InviteServerTransaction : Transaction
    {
        public InviteServerTransaction(UserAgent app ) : base(app)
        {
            this.server = true;
        }

        public  void Start()
        {
            this.state = "proceeding";
            this.SendResponse(this.CreateResponse(100, "Trying"));
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
            }
            else if (request.method == "ACK")
            {
                if (this.state == "completed")
                {
                    this.state = "confirmed";
                    if (!this.transport.reliable)
                    {
                        this.StartTimer("I", this.timer.I());
                    }
                    else
                    {
                        this.Timeout("I", 0);
                    }
                }
                else if (this.state == "confirmed")
                {
                    //Ignore duplicate ACK
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (this.state == "completed")
            {
                if (name == "G")
                {
                    this.StartTimer("G", Math.Min(2 * timeout, this.timer.T2));
                    this.stack.Send(this.lastResponse, this.remote, this.transport);
                }
                else if (name == "H")
                {
                    this.state = "terminated";
                    this.app.Timeout(this);
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

        public  void Error(string error)
        {
            if (this.state == "proceeding" || this.state == "confirmed")
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
                if (this.state == "proceeding")
                {
                    this.stack.Send(response, this.remote, this.transport);
                }
            }
            else if (response.Is2xx())
            {
                if (this.state == "proceeding")
                {
                    this.state = "terminated";
                    this.stack.Send(response, this.remote, this.transport);
                }
            }
            else
            {
                if (this.state == "proceeding")
                {
                    this.state = "completed";
                    if (!this.transport.reliable)
                    {
                        this.StartTimer("G", this.timer.G());
                    }
                    this.StartTimer("H", this.timer.H());
                    this.stack.Send(response, this.remote, this.transport);
                }
            }
        }
    }
}
