using System;
using Akka.Actor;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor
    {
        #region  -- Inner Types --

        public class AbleToAcceptJob
        {
            public RepoKey Repo { get; private set; }

            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }
        }

        public class CanAcceptJob
        {
            public RepoKey Repo { get; private set; }

            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }
        }

        public class UnableToAcceptJob
        {
            public RepoKey Repo { get; private set; }

            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }
        }

        #endregion

        private IActorRef _canAcceptJobSender;

        private IActorRef _coordinator;

        public GithubCommanderActor()
        {
            Receive<CanAcceptJob>(job =>
            {
                _canAcceptJobSender = Sender;
                _coordinator.Tell(job);
            });

            Receive<UnableToAcceptJob>(job => { _canAcceptJobSender.Tell(job); });

            Receive<AbleToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);

                //start processing messages
                _coordinator.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                //launch the new window to view results of the processing
                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(job.Repo, Sender));
            });
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            _coordinator = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name);
            base.PreStart();
        }
    }
}