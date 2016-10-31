using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using PostSharp.Aspects;
using System.Collections.Concurrent;
using PostSharp.Extensibility;

// Taken from http://stackoverflow.com/questions/9788680/how-do-i-obtain-an-id-that-allows-me-to-tell-difference-instances-of-a-class-apa
namespace SIPLib.SIP
{
    /// <summary>
    /// Example code based on the page from a Google search of:
    /// postsharp "Example: Tracing Method Execution"
    /// </summary>
    [Serializable]
    public sealed class TestMyThreadSafetyCheck : OnMethodBoundaryAspect
    {
        /// <summary>
        /// We need to be able to track if a different ThreadID is seen when executing a method within the *same* instance of a class. Its
        /// ok if we see different ThreadID values when accessing different instances of a class. In fact, creating one copy of a class per
        /// thread is a reliable method to fix threading issues in the first place.
        /// 
        /// Key: unique ID for every instance of every class.
        /// Value: LastThreadID, tracks the ID of the last thread which accessed the current instance of this class.
        /// </summary>
        public static ConcurrentDictionary<long, int> DetectThreadingIssues = new ConcurrentDictionary<long, int>();

        /// <summary>
        /// Allows us to generate a unique ID for each instance of every class that we see.
        /// </summary>
        public static ObjectIDGenerator ObjectIDGenerator = new ObjectIDGenerator();

        /// <summary>
        /// These fields are initialized at runtime. They do not need to be serialized.
        /// </summary>
        [NonSerialized]
        private string MethodName;

        [NonSerialized]
        private long LastTotalMilliseconds;

        /// <summary>
        /// Stopwatch which we can use to avoid swamping the log with too many messages for threading violations.
        /// </summary>
        [NonSerialized]
        private Stopwatch sw;

        /// <summary>
        /// Invoked only once at runtime from the static constructor of type declaring the target method. 
        /// </summary>
        /// <param name="method"></param>
        public override void RuntimeInitialize(MethodBase method)
        {
            if (method.DeclaringType != null)
            {
                this.MethodName = method.DeclaringType.FullName + "." + method.Name;
            }

            this.sw = new Stopwatch();
            this.sw.Start();

            this.LastTotalMilliseconds = -1000000;
        }

        /// <summary>
        /// Invoked at runtime before that target method is invoked.
        /// </summary>
        /// <param name="args">Arguments to the function.</param>   
        public override void OnEntry(MethodExecutionArgs args)
        {
            if (args.Instance == null)
            {
                return;
            }

            if (this.MethodName.Contains(".ctor"))
            {
                // Ignore the thread that accesses the constructor.
                // If we remove this check, then we get a false positive.
                return;
            }

            bool firstTime;
            long classInstanceID = ObjectIDGenerator.GetId(args.Instance, out firstTime);

            if (firstTime)
            {
                // This the first time we have called this, there is no LastThreadID. Return.
                if (DetectThreadingIssues.TryAdd(classInstanceID, Thread.CurrentThread.ManagedThreadId) == false)
                {
                    Console.Write(string.Format("Error E20120320-1349. Could not add an initial key to the \"DetectThreadingIssues\" dictionary.\n"));
                }
                return;
            }

            int lastThreadID = DetectThreadingIssues[classInstanceID];

            // Check 1: Continue if this instance of the class was accessed by a different thread (which is definitely bad).
            if (lastThreadID != Thread.CurrentThread.ManagedThreadId)
            {
                // Check 2: Are we printing more than one message per second?
                if ((sw.ElapsedMilliseconds - this.LastTotalMilliseconds) > 1000)
                {
                    Console.WriteLine("Begin");
                    Console.WriteLine(System.Environment.StackTrace);
                    Console.WriteLine(string.Format("{0}Warning: ThreadID {1} then {2} accessed \"{3}\" ({4}). To remove warning, manually check thread safety, then add \"[MyThreadSafetyCheck(AttributeExclude = true)]\".\n",
                        "X", lastThreadID, Thread.CurrentThread.ManagedThreadId, this.MethodName, classInstanceID));
                    Console.WriteLine("End");
                    this.LastTotalMilliseconds = sw.ElapsedMilliseconds;
                }
            }

            // Update the value of "LastThreadID" for this particular instance of the class.
            DetectThreadingIssues[classInstanceID] = Thread.CurrentThread.ManagedThreadId;
        }
    }
}