using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SIPLib
{
    public class InviteClientTransaction : Transaction
    {
        public InviteClientTransaction(UserAgent app ) : base(app)
        {
            this.server = false;
        }

        public void Start()
        {
            this.state = "calling";
            if (!this.transport.reliable)
                this.StartTimer("A", this.timer.A());
            this.StartTimer("B", this.timer.B());
            this.stack.Send(this.request, this.remote, this.transport);
        }

        public override void ReceivedResponse(Message response)
        {
            if (response.Is1xx())
            {
                if (this.state == "calling")
                {
                    this.state = "proceeding";
                    this.app.ReceivedResponse(this, response);
                }
                else if (this.state == "proceeding")
                {
                    this.app.ReceivedResponse(this, response);
                }
            }
            else if (response.Is2xx())
            {
                if (this.state == "calling" || this.state == "proceeding")
                {
                    this.state = "terminated";
                    this.app.ReceivedResponse(this, response);
                }
            }
            else
            {
                if (this.state == "calling" || this.state == "proceeding")
                {
                    this.state = "completed";
                    this.stack.Send(this.CreateAck(response), this.remote, this.transport);
                    this.app.ReceivedResponse(this, response);
                    if (!this.transport.reliable)
                    {
                        this.StartTimer("D",this.timer.D());
                    }
                    else
                    {
                        this.Timeout("D",0);
                    }
                }
                else if (this.state == "completed")
                {
                    this.stack.Send(this.CreateAck(response),this.remote,this.transport);
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (this.state == "calling")
            {
                if (name == "A")
                {
                    this.StartTimer("A", 2 * timeout);
                    this.stack.Send(this.request, this.remote, this.transport);
                }
                else if (name == "B")
                {
                    this.state = "terminated";
                    this.app.Timeout(this);
                }
            }
            else if (this.state == "completed")
            {
                if (name == "D")
                {
                    this.state = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (this.state == "calling" || this.state == "completed")
            {
                this.state = "terminated";
                this.app.Error(this,error);
            }
        }

        public Message CreateAck(Message response)
        {
            if (this.request == null)
            {
                Debug.Assert(false, String.Format("Error creating Ack message when request is null"));
                return null;
            }
            Message m = Message.CreateRequest("ACK", this.request.uri);
            m.headers["Call-ID"] = this.request.headers["Call-ID"];
            m.headers["From"] = this.request.headers["From"];

            if (response != null)
            {
                m.headers["To"] = response.headers["To"];
            }
            else
            {
                m.headers["To"] = this.request.headers["To"];
            }

            m.headers["Via"] = new List<Header>();
            m.headers["Via"].Add(this.request.First("Via"));

            m.headers["CSeq"] = new List<Header>();
            m.headers["CSeq"].Add(new Header(this.request.First("CSeq").number + " ACK","CSeq"));

            if (this.request.headers.ContainsKey("Route"))
            {
                m.headers["Route"] = this.request.headers["Route"];
            }
            return m;
        }

    }
}
