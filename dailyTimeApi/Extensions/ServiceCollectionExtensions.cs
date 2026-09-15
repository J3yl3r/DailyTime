using dailyTimeApi.Repository;
using dailyTimeApi.Repository.Interfaces;

using dailyTimeApi.Services;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDailyTimeServices(this IServiceCollection services)
        {
            // Repositories — Scoped (mismo DbContext por request)
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddScoped<INoteRepository, NoteRepository>();
            services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
            services.AddScoped<IWorkItemStatusRepository, WorkItemStatusRepository>();
            services.AddScoped<IWorkItemCategoryRepository, WorkItemCategoryRepository>();
            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<IVaultAccountRepository, VaultAccountRepository>();
            services.AddScoped<IVaultPasswordRepository, VaultPasswordRepository>();
            services.AddScoped<IVaultServiceRepository, VaultServiceRepository>();
            services.AddScoped<ICareerCompanyRepository, CareerCompanyRepository>();
            services.AddScoped<ICareerPositionRepository, CareerPositionRepository>();
            services.AddScoped<ICareerLocationRepository, CareerLocationRepository>();
            services.AddScoped<ICareerFieldRepository, CareerFieldRepository>();
            services.AddScoped<ICareerTechnologyRepository, CareerTechnologyRepository>();
            services.AddScoped<ICareerApplicationStatusRepository, CareerApplicationStatusRepository>();
            services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();
            services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
            services.AddScoped<IJobPortalRepository, JobPortalRepository>();
            services.AddScoped<IJobPortalScrapeLogRepository, JobPortalScrapeLogRepository>();
            services.AddScoped<IJobOfferRepository, JobOfferRepository>();
            services.AddScoped<ICareerProfileRepository, CareerProfileRepository>();
            services.AddScoped<IScrapeScheduleRepository, ScrapeScheduleRepository>();
            services.AddScoped<IOfferTriageConfigRepository, OfferTriageConfigRepository>();
            services.AddScoped<ITaskItemService, TaskItemService>();
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<ITimeEntryService, TimeEntryService>();
            services.AddScoped<IWorkItemStatusService, WorkItemStatusService>();
            services.AddScoped<IWorkItemCategoryService, WorkItemCategoryService>();
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IVaultAccountService, VaultAccountService>();
            services.AddScoped<IVaultPasswordService, VaultPasswordService>();
            services.AddScoped<IVaultServiceService, VaultServiceService>();
            services.AddScoped<ICareerCompanyService, CareerCompanyService>();
            services.AddScoped<ICareerPositionService, CareerPositionService>();
            services.AddScoped<ICareerLocationService, CareerLocationService>();
            services.AddScoped<ICareerFieldService, CareerFieldService>();
            services.AddScoped<ICareerTechnologyService, CareerTechnologyService>();
            services.AddScoped<ICareerApplicationStatusService, CareerApplicationStatusService>();
            services.AddScoped<IWorkExperienceService, WorkExperienceService>();
            services.AddScoped<IJobApplicationService, JobApplicationService>();
            services.AddScoped<IJobPortalService, JobPortalService>();
            services.AddScoped<IJobOfferService, JobOfferService>();
            services.AddScoped<IFitScoreService, FitScoreService>();
            services.AddScoped<ICareerProfileService, CareerProfileService>();
            services.AddScoped<IScrapeScheduleService, ScrapeScheduleService>();
            services.AddScoped<IOfferTriageService, OfferTriageService>();
            return services;
        }
    }
}
