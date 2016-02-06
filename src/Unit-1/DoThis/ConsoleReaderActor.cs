using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    ///     Actor responsible for reading FROM the console.
    ///     Also responsible for calling <see cref="ActorSystem.Terminate" />.
    /// </summary>
    internal class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";
        public const string StartCommand = "start";

        private readonly IActorRef _validationActor;

        public ConsoleReaderActor(IActorRef validationActor)
        {
            if (validationActor == null)
            {
                throw new ArgumentNullException(nameof(validationActor));
            }

            _validationActor = validationActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }

            GetAndValidateInput();
        }

        private static void DoPrintInstructions()
        {
            Console.WriteLine("Please provide the URI of a log file on disk.\n");
        }

        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (string.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                Context.System.Shutdown();
                return;
            }

            _validationActor.Tell(message);
        }
    }
}