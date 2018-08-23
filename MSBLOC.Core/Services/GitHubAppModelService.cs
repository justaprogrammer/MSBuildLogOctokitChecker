﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MSBLOC.Core.Interfaces;
using MSBLOC.Core.Model;
using MSBLOC.Core.Model.LogAnalyzer;
using Newtonsoft.Json;
using Octokit;
using CheckRun = MSBLOC.Core.Model.GitHub.CheckRun;
using CheckWarningLevel = MSBLOC.Core.Model.LogAnalyzer.CheckWarningLevel;

namespace MSBLOC.Core.Services
{
    public class GitHubAppModelService : GitHubAppModelServiceBase, IGitHubAppModelService
    {
        private readonly IGitHubAppClientFactory _gitHubUserClientFactory;
        private readonly ITokenGenerator _tokenGenerator;

        public GitHubAppModelService(IGitHubAppClientFactory gitHubUserClientFactory, ITokenGenerator tokenGenerator)
        {
            _gitHubUserClientFactory = gitHubUserClientFactory;
            _tokenGenerator = tokenGenerator;
        }

        public async Task GetPullRequestChangedPathsAsync(string repoOwner, string repoName, int number)
        {
            var gitHubClient = await _gitHubUserClientFactory.CreateAppClientForLoginAsync(_tokenGenerator, repoOwner);

            await GetPullRequestChangedPathsAsync(gitHubClient, repoOwner, repoName, number);
        }

        public async Task<CheckRun> CreateCheckRunAsync(string repoOwner, string repoName, string headSha,
            string checkRunName,
            string checkRunTitle, string checkRunSummary, bool checkRunIsSuccess, Annotation[] annotations,
            DateTimeOffset? startedAt,
            DateTimeOffset? completedAt)
        {
            if (repoOwner == null) throw new ArgumentNullException(nameof(repoOwner));
            if (repoName == null) throw new ArgumentNullException(nameof(repoName));
            if (headSha == null) throw new ArgumentNullException(nameof(headSha));
            if (checkRunTitle == null) throw new ArgumentNullException(nameof(checkRunTitle));
            if (checkRunSummary == null) throw new ArgumentNullException(nameof(checkRunSummary));

            if ((annotations?.Length ?? 0) > 50)
                throw new ArgumentException("Cannot create more than 50 annotations at a time");

            var gitHubClient = await _gitHubUserClientFactory.CreateAppClientForLoginAsync(_tokenGenerator, repoOwner);
            var checkRunsClient = gitHubClient?.Check?.Run;

            if (checkRunsClient == null) throw new InvalidOperationException("ICheckRunsClient is null");

            var newCheckRun = new NewCheckRun(checkRunName, headSha)
            {
                Output = new NewCheckRunOutput(checkRunTitle, checkRunSummary)
                {
                    Annotations = annotations?
                        .Select(annotation => new NewCheckRunAnnotation(annotation.Filename, annotation.BlobHref,
                            annotation.LineNumber, annotation.EndLine, GetCheckWarningLevel(annotation),
                            annotation.Message))
                        .ToArray()
                },
                Status = CheckStatus.Completed,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                Conclusion = checkRunIsSuccess ? CheckConclusion.Success : CheckConclusion.Failure
            };

            var checkRun = await checkRunsClient.Create(repoOwner, repoName, newCheckRun);

            return new CheckRun
            {
                Id = checkRun.Id,
                Url = checkRun.HtmlUrl,
            };
        }

        public async Task UpdateCheckRunAsync(long checkRunId, string repoOwner, string repoName,
            string headSha, string checkRunTitle, string checkRunSummary, Annotation[] annotations,
            DateTimeOffset? startedAt, DateTimeOffset? completedAt)
        {
            if (annotations.Length > 50)
                throw new ArgumentException("Cannot create more than 50 annotations at a time");

            var gitHubClient = await _gitHubUserClientFactory.CreateAppClientForLoginAsync(_tokenGenerator, repoOwner);
            var checkRunsClient = gitHubClient?.Check?.Run;

            if (checkRunsClient == null) throw new InvalidOperationException("ICheckRunsClient is null");

            await checkRunsClient.Update(repoOwner, repoName, checkRunId, new CheckRunUpdate()
            {
                Output = new NewCheckRunOutput(checkRunTitle, checkRunSummary)
                {
                    Annotations = annotations
                        .Select(annotation => new NewCheckRunAnnotation(annotation.Filename, annotation.BlobHref,
                            annotation.LineNumber, annotation.EndLine, GetCheckWarningLevel(annotation),
                            annotation.Message))
                        .ToArray()
                }
            });
        }

        public async Task<string> GetRepositoryFileAsync(string owner, string repository, string path, string reference)
        {
            var gitHubClient = await _gitHubUserClientFactory.CreateAppClientForLoginAsync(_tokenGenerator, owner);
            return await GetRepositoryFileAsync(gitHubClient, owner, repository, path, reference);
        }

        public async Task<LogAnalyzerConfiguration> GetLogAnalyzerConfigurationAsync(string owner, string repository, string reference)
        {
            var fileContent = await GetRepositoryFileAsync(owner, repository, "msbloc.json", reference);
            if (fileContent == null) return null;

            return JsonConvert.DeserializeObject<LogAnalyzerConfiguration>(fileContent);
        }

        private static Octokit.CheckWarningLevel GetCheckWarningLevel(Annotation annotation)
        {
            switch (annotation.CheckWarningLevel)
            {
                case CheckWarningLevel.Notice:
                    return Octokit.CheckWarningLevel.Notice;
                case CheckWarningLevel.Warning:
                    return Octokit.CheckWarningLevel.Warning;
                case CheckWarningLevel.Failure:
                    return Octokit.CheckWarningLevel.Failure;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}