using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = (StartTail)message;

                // here we are creating our first parent/child relationship!
                // the TailActor instance created here is a child of this instance of TailCoordinatorActor
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(10, TimeSpan.FromSeconds(30), x =>
            {
                //Maybe we consider ArithmeticException to not be application critical
                //so we just ignore the error and keep going.
                if (x is ArithmeticException)
                {
                    return Directive.Resume;
                }

                //Error that we cannot recover from, stop the failing actor
                if (x is NotSupportedException)
                {
                    return Directive.Stop;
                }

                //In all other cases, just restart the failing actor
                return Directive.Restart;
            });
        }

        public class StartTail
        {
            public string FilePath { get; }

            public IActorRef ReporterActor { get; }

            public StartTail(string filePath, IActorRef reporterActor)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                if (reporterActor == null)
                {
                    throw new ArgumentNullException(nameof(reporterActor));
                }

                FilePath = filePath;
                ReporterActor = reporterActor;
            }
        }

        public class StopTail
        {
            public string FilePath { get; private set; }

            public StopTail(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                FilePath = filePath;
            }
        }
    }
}