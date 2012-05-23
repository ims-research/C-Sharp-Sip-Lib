using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIPLib.SIP
{
    public class InviteClientTransaction : Transaction
    {
        public InviteClientTransaction(UserAgent app ) : base(app)
        {
            Server = false;
        }

        public void Start()
        {
            State = "calling";
            if (!Transport.Reliable)
                StartTimer("A", Timer.A());
            StartTimer("B", Timer.B());
            Stack.Send(Request, Remote, Transport);
        }

        public override void ReceivedResponse(Message response)
        {
            if (response.Is1XX())
            {
                if (State == "calling")
                {
                    State = "proceeding";
                    App.ReceivedResponse(this, response);
                }
                else if (State == "proceeding")
                {
                    App.ReceivedResponse(this, response);
                }
            }
            else if (response.Is2XX())
            {
                if (State == "calling" || State == "proceeding")
                {
                    State = "terminated";
                    App.ReceivedResponse(this, response);
                }
            }
            else
            {
                if (State == "calling" || State == "proceeding")
                {
                    State = "completed";
                    Stack.Send(CreateAck(response), Remote, Transport);
                    App.ReceivedResponse(this, response);
                    if (!Transport.Reliable)
                    {
                        StartTimer("D",Timer.D());
                    }
                    else
                    {
                        Timeout("D",0);
                    }
                }
                else if (State == "completed")
                {
                    Stack.Send(CreateAck(response),Remote,Transport);
                }
            }
        }

        public void Timeout(string name, int timeout)
        {
            if (State == "calling")
            {
                if (name == "A")
                {
                    StartTimer("A", 2 * timeout);
                    Stack.Send(Request, Remote, Transport);
                }
                else if (name == "B")
                {
                    State = "terminated";
                    App.Timeout(this);
                }
            }
            else if (State == "completed")
            {
                if (name == "D")
                {
                    State = "terminated";
                }
            }
        }

        public void Error(string error)
        {
            if (State == "calling" || State == "completed")
            {
                State = "terminated";
                App.Error(this,error);
            }
        }

        public Message CreateAck(Message response)
        {
            if (Request == null)
            {
                Debug.Assert(false, String.Format("Error creating Ack message when request is null"));
                return null;
            }
            Message m = Message.CreateRequest("ACK", Request.Uri);
            m.Headers["Call-ID"] = Request.Headers["Call-ID"];
            m.Headers["From"] = Request.Headers["From"];

            if (response != null)
            {
                m.Headers["To"] = response.Headers["To"];
            }
            else
            {
                m.Headers["To"] = Request.Headers["To"];
            }

            m.Headers["Via"] = new List<Header> {Request.First("Via")};

            m.Headers["CSeq"] = new List<Header> {new Header(Request.First("CSeq").Number + " ACK", "CSeq")};

            if (Request.Headers.ContainsKey("Route"))
            {
                m.Headers["Route"] = Request.Headers["Route"];
            }
            return m;
        }

    }
}
