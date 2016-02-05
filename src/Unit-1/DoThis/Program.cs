using System;
﻿using Akka.Actor;

namespace WinTail
{
    internal class Program
    {
        public static ActorSystem MyActorSystem;

        public static void Main()
        {
            var system = ActorSystem.Create("blah");

            var writerActor = system.ActorOf(Props.Create(() => new ConsoleWriterActor()));
            var readerActor = system.ActorOf(Props.Create(() => new ConsoleReaderActor(writerActor)));

            readerActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            system.AwaitTermination();
        }
    }
}
