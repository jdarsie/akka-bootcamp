using System;
﻿using Akka.Actor;

namespace WinTail
{
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main()
        {
            var system = ActorSystem.Create("blah");

            PrintInstructions();

            var writerActor = system.ActorOf(Props.Create(() => new ConsoleWriterActor()));
            var readerActor = system.ActorOf(Props.Create(() => new ConsoleReaderActor(writerActor)));

            readerActor.Tell("start");

            // blocks the main thread from exiting until the actor system is shut down
            system.AwaitTermination();
        }

        private static void PrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.Write("Some lines will appear as");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(" red ");
            Console.ResetColor();
            Console.Write(" and others will appear as");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" green! ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
    }
}
