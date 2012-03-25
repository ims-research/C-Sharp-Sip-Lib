using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SIPLib
{
    public class InviteClientTransaction : Transaction
    {
        public InviteClientTransaction(SIPApp app ) : base(app)
        {
            this.server = false;
        }

        public void start()
        {
            this.state = "calling";
            if (!this.transport.reliable)
                this.startTimer("A", this.timer.A());
            this.startTimer("B", this.timer.B());
            this.stack.send(this.request, this.remote, this.transport);
        }

        public override void receivedResponse(Message response)
        {
            if (response.is1xx())
            {
                if (this.state == "calling")
                {
                    this.state = "proceeding";
                    this.app.receivedResponse(this, response);
                }
                else if (this.state == "proceeding")
                {
                    this.app.receivedResponse(this, response);
                }
            }
            else if (response.is2xx())
            {
                if (this.state == "calling" || this.state == "proceeding")
                {
                    this.state = "terminated";
                    this.app.receivedResponse(this, response);
                }
            }
            else
            {
                if (this.state == "calling" || this.state == "proceeding")
                {
                    this.state = "completed";
                    this.stack.send(this.createAck(response), this.remote, this.transport);
                    this.app.receivedResponse(this, response);
                    if (!this.transport.reliable)
                    {
                        this.startTimer("D",this.timer.D());
                    }
                    else
                    {
                        this.timeout("D",0);
                    }
                }
                else if (this.state == "completed")
                {
                    this.stack.send(this.createAck(response),this.remote,this.transport);
                }
            }
        }

        public void timeout(string name, int timeout)
        {
            if (this.state == "calling")
            {
                if (name == "A")
                {
                    this.startTimer("A", 2 * timeout);
                    this.stack.send(this.request, this.remote, this.transport);
                }
                else if (name == "B")
                {
                    this.state = "terminated";
                    this.app.timeout(this);
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

        public void error(string error)
        {
            if (this.state == "calling" || this.state == "completed")
            {
                this.state = "terminated";
                this.app.error(this,error);
            }
        }

        public Message createAck(Message response)
        {
            if (this.request == null)
            {
                Debug.Assert(false, String.Format("Error creating Ack message when request is null"));
                return null;
            }
            Message m = Message.createRequest("ACK", this.request.uri);
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
            m.headers["Via"].Add(this.request.first("Via"));

            m.headers["CSeq"] = new List<Header>();
            m.headers["CSeq"].Add(new Header(this.request.first("CSeq").number + " ACK","CSeq"));

            if (this.request.headers.ContainsKey("Route"))
            {
                m.headers["Route"] = this.request.headers["Route"];
            }
            return m;
        }

    }
}
