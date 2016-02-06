using Akka.Actor;

namespace WinTail
{
    internal class Program
    {
        public static ActorSystem MyActorSystem;

        public static void Main()
        {
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            var consoleWriterProps = Props.Create<ConsoleWriterActor>();
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            var tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
            var tailCoordinatorActor = MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

            var fileValidatorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor, tailCoordinatorActor));
            var fileValidatorActor = MyActorSystem.ActorOf(fileValidatorProps, "validationActor");

            var consoleReaderProps = Props.Create<ConsoleReaderActor>(fileValidatorActor);
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.AwaitTermination();
        }
    }
}