using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIPLib.SIP;

namespace SIPLib.src.SIP
{
    class Proxy : UserAgent
    {
        public Proxy(SIPStack stack, Message request, bool server)
            : base(stack, request, server)
        {
            if (request == null) Debug.Assert(false, "Cannot create Proxy without incoming request");
        }
        public Transaction createTransaction(Message request)
        {
        
            this.ReceivedRequest(null,request);
            return null;
        }

        public void receivedRequest(Transaction transaction, Message request)
        {
            if ((transaction != null) && this.Transaction != null && this.Transaction != transaction && request.Method.ToUpper()!="CANCEL")
            {
                Debug.Assert(false, "Invalid transaction for received request");
            }
            this.Server = true;
            if (!request.Uri.Scheme.ToLower().Equals("sip"))
            {
                this.SendResponse(416,"Unsupported URI scheme");
                return;
            }

            if (request.First("Max-Forwards") !=null && ((int)(request.First("Max-Forwards").Value) < 0))
            {
                this.SendResponse(483,"Too many hops");
                return;
            }

            if (!request.Headers["To"][0].Attributes.ContainsKey("tag") && transaction != null)
            {
                if (this.Stack.FindOtherTransactions(request,transaction) !=null)
                {
                    this.SendResponse(482, "Loop detected - found another transaction");
                    return;
                }
            }

            if (request.First("Proxy-Require")!=null)
            {
                if (!request.Method.ToUpper().Contains("CANCEL")&&!request.Method.ToUpper().Contains("ACK"))
                {
                    Message response = this.CreateResponse(420, "Bad extension");
                    Header unsupported = request.First("Proxy-Require");
                    unsupported.Name = "Unsupported";
                    response.InsertHeader(unsupported);
                    this.SendResponse(unsupported);
                    return;
                }
                
            }

            if (transaction != null)
            {
                this.Transaction = transaction;
            }

            if (request.Method.ToUpper() == "CANCEL")
            {
                string branch;
                if (request.First("Via")!=null&& request.First("Via").Attributes.ContainsKey("branch"))
                {
                    branch = request.First("Via").Attributes["branch"];
                }
                else
                {
                    branch = Transaction.CreateBranch(request, true);
                }
                Transaction original = this.Stack.FindTransaction(Transaction.CreateId(branch, "INVITE"));
                    if (original !=null)
                    {
                       if (original.State == "proceeding" || original.State == "trying")
                       {
                           original.SendResponse(original.CreateResponse(487,"Request terminated"));
                       }
                        transaction = Transaction.CreateServer(this.Stack, this, request, this.Stack.Transport,
                                                               this.Stack.Tag,false);
                        transaction.SendResponse(transaction.CreateResponse(200, "OK"));
                    }
                this.SendCancel();
                return;
            }

            if (request.Uri.User.IsNullOrEmpty() && this.isLocal(request.Uri) && request.Uri.Parameters !=null && request.First("Route")!=null)
            {
                
                Header lastRoute = request.Headers["Route"].Last();
                request.Headers["Route"].RemoveAt(request.Headers["Route"].Count-1);
                request.Uri = ((Address) (lastRoute.Value)).Uri;
            }
            if (request.First("Route")!=null && this.isLocal(((Address) (request.First("Route").Value)).Uri))
            {
                request.Headers["Route"].RemoveAt(0);
                request.had_lr = true;
            }
            this.Stack.ReceivedRequest(this,request);
        }

        public bool isLocal(SIPURI uri)
        {
            bool host = Stack.Transport.Host.ToString() == uri.Host || uri.Host == "localhost" ||
                        uri.Host == "127.0.0.1";
            bool port = false;
            if (uri.Port == null || uri.Port == 0)
            {
                if (this.Stack.Transport.Port == 5060)
                {
                    port = true;
                }
            }
            port = (Stack.Transport.Port == uri.Port) || port ;
         return (host && port);
        }

        public void SendResponse(object response, string response_text = null, string content = null, string contentType = null, bool createDialog = true)
        {
            if (this.Transaction == null)
            {
                this.Transaction = Transaction.CreateServer(this.Stack, this, this.Request, this.Stack.Transport,
                                                            this.Stack.Tag, false);
            }
            base.SendResponse(response,response_text,content,contentType,false);
        }
        
    }
}
