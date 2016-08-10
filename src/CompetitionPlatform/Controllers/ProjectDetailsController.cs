using System;
using System.Threading.Tasks;
using CompetitionPlatform.Data.AzureRepositories.Project;
using CompetitionPlatform.Data.AzureRepositories.Vote;
using CompetitionPlatform.Helpers;
using CompetitionPlatform.Models;
using CompetitionPlatform.Models.ProjectViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CompetitionPlatform.Controllers
{
    public class ProjectDetailsController : Controller
    {
        private readonly IProjectCommentsRepository _projectCommentsRepository;
        private readonly IProjectFileRepository _projectFileRepository;
        private readonly IProjectFileInfoRepository _projectFileInfoRepository;
        private readonly IProjectVoteRepository _projectVoteRepository;
        private readonly IProjectRepository _projectRepository;

        public ProjectDetailsController(IProjectCommentsRepository projectCommentsRepository, IProjectFileRepository projectFileRepository,
            IProjectFileInfoRepository projectFileInfoRepository, IProjectVoteRepository projectVoteRepository,
            IProjectRepository projectRepository)
        {
            _projectCommentsRepository = projectCommentsRepository;
            _projectFileRepository = projectFileRepository;
            _projectFileInfoRepository = projectFileInfoRepository;
            _projectVoteRepository = projectVoteRepository;
            _projectRepository = projectRepository;
        }

        public IActionResult AddComment(ProjectCommentPartialViewModel model)
        {
            var user = GetAuthenticatedUser();
            model.UserId = user.Email;
            model.FullName = user.GetFullName();
            model.Created = DateTime.UtcNow;
            model.LastModified = model.Created;

            _projectCommentsRepository.SaveAsync(model);
            return RedirectToAction("ProjectDetails", "Project", new { id = model.ProjectId });
        }

        public async Task<IActionResult> DownloadProjectFile(string id)
        {
            var fileInfo = await _projectFileInfoRepository.GetAsync(id);

            var fileStream = await _projectFileRepository.GetProjectFile(id);
            return File(fileStream, fileInfo.ContentType, fileInfo.FileName);
        }

        public async Task<IActionResult> VoteFor(string id)
        {
            var user = GetAuthenticatedUser();

            var result = new ProjectVoteEntity
            {
                ProjectId = id,
                VoterUserId = user.Email,
                ForAgainst = 1,
            };

            var vote = await _projectVoteRepository.GetAsync(id, user.Email);

            if (vote == null)
            {
                await _projectVoteRepository.SaveAsync(result);

                var project = await _projectRepository.GetAsync(id);

                project.VotesFor += 1;

                await _projectRepository.UpdateAsync(project);
            }
            else
            {
                await _projectVoteRepository.UpdateAsync(result);
                if (vote.ForAgainst == -1)
                {
                    var project = await _projectRepository.GetAsync(id);

                    project.VotesFor += 1;
                    project.VotesAgainst -= 1;

                    await _projectRepository.UpdateAsync(project);
                }
            }

            return RedirectToAction("ProjectDetails", "Project", new { id = id });
        }

        public async Task<IActionResult> VoteAgainst(string id)
        {
            var user = GetAuthenticatedUser();

            var result = new ProjectVoteEntity
            {
                ProjectId = id,
                VoterUserId = user.Email,
                ForAgainst = -1
            };

            var vote = await _projectVoteRepository.GetAsync(id, user.Email);

            if (vote == null)
            {
                await _projectVoteRepository.SaveAsync(result);

                var project = await _projectRepository.GetAsync(id);

                project.VotesAgainst += 1;

                await _projectRepository.UpdateAsync(project);
            }
            else
            {
                await _projectVoteRepository.UpdateAsync(result);
                if (vote.ForAgainst == 1)
                {
                    var project = await _projectRepository.GetAsync(id);

                    project.VotesAgainst += 1;
                    project.VotesFor -= 1;

                    await _projectRepository.UpdateAsync(project);
                }
            }

            return RedirectToAction("ProjectDetails", "Project", new { id = id });
        }

        public IActionResult GetProjectVotesResults(int votesFor, int votesAgainst)
        {
            var viewModel = new ProjectVoteViewModel
            {
                VotesFor = votesFor,
                VotesAgainst = votesAgainst
            };

            return PartialView("~/Views/Project/VotingBarsPartial.cshtml", viewModel);
        }

        private CompetitionPlatformUser GetAuthenticatedUser()
        {
            return ClaimsHelper.GetUser(User.Identity);
        }
    }
}