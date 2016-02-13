using System;
using Akka.Actor;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
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

        private int _pendingJobReplies;

        public IStash Stash { get; set; }

        public GithubCommanderActor()
        {
            Ready();
        }

        private void Asking()
        {
            // stash any subsequent requests
            Receive<CanAcceptJob>(job => Stash.Stash());

            Receive<UnableToAcceptJob>(job =>
            {
                _pendingJobReplies--;

                if (_pendingJobReplies != 0)
                {
                    return;
                }

                _canAcceptJobSender.Tell(job);
                BecomeReady();
            });

            Receive<AbleToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);

                // start processing messages
                Sender.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                // launch the new window to view results of the processing
                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(job.Repo, Sender));

                BecomeReady();
            });
        }

        private void BecomeAsking()
        {
            _canAcceptJobSender = Sender;
            _pendingJobReplies = 3; //the number of routees
            Become(Asking);
        }

        private void BecomeReady()
        {
            Become(Ready);
            Stash.UnstashAll();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            // create three GithubCoordinatorActor instances
            var c1 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name + "1");
            var c2 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name + "2");
            var c3 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name + "3");

            // create a broadcast router who will ask all of them if they're available for work
            _coordinator =
                Context.ActorOf(Props.Empty.WithRouter(new BroadcastGroup(
                    ActorPaths.GithubCoordinatorActor.Path + "1",
                    ActorPaths.GithubCoordinatorActor.Path + "2",
                    ActorPaths.GithubCoordinatorActor.Path + "3")));

            base.PreStart();
        }

        private void Ready()
        {
            Receive<CanAcceptJob>(job =>
            {
                _coordinator.Tell(job);

                BecomeAsking();
            });
        }
    }
}