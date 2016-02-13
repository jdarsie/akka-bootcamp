namespace GithubActors
{
    /// <summary>
    /// Begin processing a new Github repository for analysis
    /// </summary>
    public class ProcessRepo
    {
        public string RepoUri { get; private set; }

        public ProcessRepo(string repoUri)
        {
            RepoUri = repoUri;
        }
    }

    public class RepoKey
    {
        public string Owner { get; private set; }

        public string Repo { get; private set; }

        public RepoKey(string owner, string repo)
        {
            Repo = repo;
            Owner = owner;
        }
    }


    public class RetryableQuery
    {
        public int AllowableTries { get; private set; }

        public bool CanRetry
        {
            get { return RemainingTries > 0; }
        }

        public int CurrentAttempt { get; private set; }


        public object Query { get; private set; }

        public int RemainingTries
        {
            get { return AllowableTries - CurrentAttempt; }
        }

        public RetryableQuery(object query, int allowableTries) : this(query, allowableTries, 0) {}

        private RetryableQuery(object query, int allowableTries, int currentAttempt)
        {
            AllowableTries = allowableTries;
            Query = query;
            CurrentAttempt = currentAttempt;
        }

        public RetryableQuery NextTry()
        {
            return new RetryableQuery(Query, AllowableTries, CurrentAttempt + 1);
        }
    }
}