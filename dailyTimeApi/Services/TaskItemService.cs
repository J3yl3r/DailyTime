using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Google;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services
{
    public class TaskItemService : ITaskItemService
    {
        private const int MaxDepth = 3;
        private readonly ITaskItemRepository _repository;
        private readonly IWorkItemStatusRepository _statusRepository;
        private readonly IWorkItemCategoryRepository _categoryRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IGoogleSyncNotifier _googleSync;

        public TaskItemService(
            ITaskItemRepository repository,
            IWorkItemStatusRepository statusRepository,
            IWorkItemCategoryRepository categoryRepository,
            IPersonRepository personRepository,
            IProjectRepository projectRepository,
            ICompanyRepository companyRepository,
            IGoogleSyncNotifier googleSync)
        {
            _repository = repository;
            _statusRepository = statusRepository;
            _categoryRepository = categoryRepository;
            _personRepository = personRepository;
            _projectRepository = projectRepository;
            _companyRepository = companyRepository;
            _googleSync = googleSync;
        }

        public async Task<IReadOnlyList<TaskItemResponse>> GetRootsByDateRangeAsync(
            DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        {
            var items = await _repository.GetRootsByDateRangeAsync(fromDate, toDate, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<TaskItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Tarea {id} no encontrada.");
            return MapToResponse(item);
        }

        public async Task<IReadOnlyList<TaskItemResponse>> GetChildrenAsync(
            int parentId, CancellationToken cancellationToken = default)
        {
            _ = await _repository.GetByIdAsync(parentId, cancellationToken)
                ?? throw new NotFoundException($"Tarea padre {parentId} no encontrada.");
            var items = await _repository.GetChildrenAsync(parentId, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<TaskItemResponse> CreateAsync(
            CreateTaskItemRequest request, CancellationToken cancellationToken = default)
        {
            ValidateSchedule(request.StartTime, request.EndTime);
            await ValidateParentForNewItemAsync(request.ParentTaskId, cancellationToken);
            var status = await ResolveStatusAsync(request.StatusId, cancellationToken);
            var category = await ResolveCategoryAsync(request.CategoryId, cancellationToken);
            var person = await ResolvePersonAsync(request.PersonId, cancellationToken);
            var project = await ResolveProjectAsync(request.ProjectId, cancellationToken);
            var company = await ResolveCompanyAsync(request.CompanyId, cancellationToken);

            var now = DateTime.UtcNow;
            var entity = new TaskItem
            {
                Title = request.Title.Trim(),
                Content = NormalizeContent(request.Content),
                WorkDate = request.WorkDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ParentTaskId = request.ParentTaskId,
                StatusId = status.Id,
                CategoryId = category.Id,
                PersonId = person?.Id,
                ProjectId = project?.Id,
                CompanyId = company?.Id,
                SortOrder = request.SortOrder,
                DurationMinutes = request.DurationMinutes,
                IsCompleted = status.IsFinal,
                CompletedAt = status.IsFinal ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            // Que el calendario de Google refleje el cambio sin esperar al ciclo automático.
            _googleSync.RequestSync();
            entity.Status = status;
            entity.Category = category;
            entity.Person = person;
            entity.Project = project;
            entity.Company = company;
            return MapToResponse(entity);
        }

        public async Task<TaskItemResponse> UpdateAsync(
            int id, UpdateTaskItemRequest request, CancellationToken cancellationToken = default)
        {
            ValidateSchedule(request.StartTime, request.EndTime);
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Tarea {id} no encontrada.");

            if (request.ParentTaskId != entity.ParentTaskId)
                await ValidateParentForMoveAsync(id, request.ParentTaskId, cancellationToken);

            var status = await ResolveStatusAsync(request.StatusId, cancellationToken);
            var category = await ResolveCategoryAsync(request.CategoryId, cancellationToken);
            var person = await ResolvePersonAsync(request.PersonId, cancellationToken);
            var project = await ResolveProjectAsync(request.ProjectId, cancellationToken);
            var company = await ResolveCompanyAsync(request.CompanyId, cancellationToken);

            entity.Title = request.Title.Trim();
            entity.Content = NormalizeContent(request.Content);
            entity.WorkDate = request.WorkDate;
            entity.StartTime = request.StartTime;
            entity.EndTime = request.EndTime;
            entity.ParentTaskId = request.ParentTaskId;
            entity.StatusId = status.Id;
            entity.CategoryId = category.Id;
            entity.PersonId = person?.Id;
            entity.ProjectId = project?.Id;
            entity.CompanyId = company?.Id;
            entity.SortOrder = request.SortOrder;
            entity.DurationMinutes = request.DurationMinutes;
            ApplyCompletionState(entity, status);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Status = null;
            entity.Category = null;
            entity.Person = null;
            entity.Project = null;
            entity.Company = null;

            _repository.Update(entity);
            await _repository.SaveChangesAsync(cancellationToken);
            // Que el calendario de Google refleje el cambio sin esperar al ciclo automático.
            _googleSync.RequestSync();

            entity.Status = status;
            entity.Category = category;
            entity.Person = person;
            entity.Project = project;
            entity.Company = company;
            return MapToResponse(entity);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Tarea {id} no encontrada.");
            var children = await _repository.GetChildrenAsync(id, cancellationToken);
            if (children.Count > 0)
                throw new ValidationException("No se puede eliminar una tarea que tiene subtareas.");
            // La lápida viaja en el mismo SaveChanges que el borrado: o se van los dos, o ninguno.
            await _googleSync.EnqueueDeletionAsync(entity, cancellationToken);
            _repository.Remove(entity);
            await _repository.SaveChangesAsync(cancellationToken);
            _googleSync.RequestSync();
        }

        private async Task<WorkItemStatus> ResolveStatusAsync(
            int? statusId,
            CancellationToken cancellationToken)
        {
            if (statusId.HasValue)
            {
                var status = await _statusRepository.GetByIdAsync(statusId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Estado {statusId} no encontrado.");

                if (!string.Equals(status.ItemType, WorkItemStatusService.ItemTypeTask, StringComparison.OrdinalIgnoreCase))
                    throw new ValidationException("El estado seleccionado no es válido para tareas.");

                return status;
            }

            return await _statusRepository.GetDefaultByItemTypeAsync(
                    WorkItemStatusService.ItemTypeTask,
                    cancellationToken)
                ?? throw new ValidationException("No hay estados configurados para tareas.");
        }

        private async Task<WorkItemCategory> ResolveCategoryAsync(
            int? categoryId, CancellationToken cancellationToken)
        {
            if (categoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(
                        categoryId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Categoría {categoryId} no encontrada.");
                if (category.ItemType != WorkItemStatusService.ItemTypeTask)
                    throw new ValidationException("La categoría seleccionada no es válida para tareas.");
                if (!category.IsActive)
                    throw new ValidationException("La categoría seleccionada está inactiva.");
                return category;
            }

            return await _categoryRepository.GetDefaultByItemTypeAsync(
                    WorkItemStatusService.ItemTypeTask, cancellationToken)
                ?? throw new ValidationException("No hay categorías activas para tareas.");
        }

        private async Task<Person?> ResolvePersonAsync(
            int? personId, CancellationToken cancellationToken)
        {
            if (!personId.HasValue)
                return null;

            var person = await _personRepository.GetByIdAsync(personId.Value, cancellationToken)
                ?? throw new NotFoundException($"Persona {personId} no encontrada.");
            if (!person.IsActive)
                throw new ValidationException("La persona seleccionada está inactiva.");
            return person;
        }

        private async Task<Project?> ResolveProjectAsync(
            int? projectId, CancellationToken cancellationToken)
        {
            if (!projectId.HasValue)
                return null;

            var project = await _projectRepository.GetByIdAsync(projectId.Value, cancellationToken)
                ?? throw new NotFoundException($"Proyecto {projectId} no encontrado.");
            if (!project.IsActive)
                throw new ValidationException("El proyecto seleccionado está inactivo.");
            return project;
        }

        private async Task<Company?> ResolveCompanyAsync(
            int? companyId, CancellationToken cancellationToken)
        {
            if (!companyId.HasValue)
                return null;

            var company = await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken)
                ?? throw new NotFoundException($"Empresa {companyId} no encontrada.");
            if (!company.IsActive)
                throw new ValidationException("La empresa seleccionada está inactiva.");
            return company;
        }

        private static string? NormalizeContent(string? content) =>
            string.IsNullOrWhiteSpace(content) ? null : content.Trim();

        private static void ApplyCompletionState(TaskItem entity, WorkItemStatus status)
        {
            if (status.IsFinal)
            {
                entity.IsCompleted = true;
                entity.CompletedAt ??= DateTime.UtcNow;
                return;
            }

            entity.IsCompleted = false;
            entity.CompletedAt = null;
        }

        private async Task ValidateParentForNewItemAsync(int? parentTaskId, CancellationToken cancellationToken)
        {
            if (!parentTaskId.HasValue)
                return;
            var parentLevel = await GetLevelAsync(parentTaskId.Value, cancellationToken);
            if (parentLevel >= MaxDepth)
                throw new ValidationException($"Máximo {MaxDepth} niveles de jerarquía.");
        }

        private async Task ValidateParentForMoveAsync(int taskId, int? newParentTaskId, CancellationToken cancellationToken)
        {
            if (newParentTaskId == taskId)
                throw new ValidationException("Una tarea no puede ser padre de sí misma.");
            if (newParentTaskId.HasValue)
            {
                _ = await _repository.GetByIdAsync(newParentTaskId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Tarea padre {newParentTaskId} no encontrada.");
                if (await IsDescendantAsync(taskId, newParentTaskId.Value, cancellationToken))
                    throw new ValidationException("No se puede mover una tarea dentro de sus propias subtareas.");
            }
            var newLevel = newParentTaskId.HasValue
                ? await GetLevelAsync(newParentTaskId.Value, cancellationToken) + 1
                : 1;
            var subtreeHeight = await GetSubtreeHeightAsync(taskId, cancellationToken);
            if (newLevel + subtreeHeight - 1 > MaxDepth)
                throw new ValidationException($"Máximo {MaxDepth} niveles de jerarquía.");
        }

        private async Task<int> GetLevelAsync(int taskId, CancellationToken cancellationToken)
        {
            var level = 1;
            var current = await _repository.GetByIdAsync(taskId, cancellationToken)
                ?? throw new NotFoundException($"Tarea {taskId} no encontrada.");
            while (current.ParentTaskId.HasValue)
            {
                level++;
                current = await _repository.GetByIdAsync(current.ParentTaskId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Tarea padre {current.ParentTaskId} no encontrada.");
            }
            return level;
        }

        private async Task<int> GetSubtreeHeightAsync(int taskId, CancellationToken cancellationToken)
        {
            var children = await _repository.GetChildrenAsync(taskId, cancellationToken);
            if (children.Count == 0)
                return 1;
            var maxChildHeight = 0;
            foreach (var child in children)
            {
                var height = await GetSubtreeHeightAsync(child.Id, cancellationToken);
                maxChildHeight = Math.Max(maxChildHeight, height);
            }
            return 1 + maxChildHeight;
        }

        private async Task<bool> IsDescendantAsync(int ancestorId, int possibleDescendantId, CancellationToken cancellationToken)
        {
            var children = await _repository.GetChildrenAsync(ancestorId, cancellationToken);
            foreach (var child in children)
            {
                if (child.Id == possibleDescendantId)
                    return true;
                if (await IsDescendantAsync(child.Id, possibleDescendantId, cancellationToken))
                    return true;
            }
            return false;
        }

        private static TaskItemResponse MapToResponse(TaskItem entity) => new()
        {
            Id = entity.Id,
            ParentTaskId = entity.ParentTaskId,
            Title = entity.Title,
            Content = entity.Content,
            IsCompleted = entity.IsCompleted,
            CompletedAt = entity.CompletedAt,
            WorkDate = entity.WorkDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            DurationMinutes = entity.DurationMinutes,
            SortOrder = entity.SortOrder,
            StatusId = entity.StatusId,
            Status = entity.Status is null
                ? null
                : new WorkItemStatusSummaryResponse
                {
                    Id = entity.Status.Id,
                    Name = entity.Status.Name,
                    Description = entity.Status.Description,
                    Color = entity.Status.Color,
                    IsFinal = entity.Status.IsFinal,
                    ItemType = entity.Status.ItemType
                },
            CategoryId = entity.CategoryId,
            Category = entity.Category is null
                ? null
                : new WorkItemCategorySummaryResponse
                {
                    Id = entity.Category.Id,
                    Name = entity.Category.Name,
                    Description = entity.Category.Description,
                    ItemType = entity.Category.ItemType,
                    IsActive = entity.Category.IsActive
                },
            PersonId = entity.PersonId,
            Person = entity.Person is null
                ? null
                : new PersonSummaryResponse
                {
                    Id = entity.Person.Id,
                    Name = entity.Person.Name,
                    Description = entity.Person.Description,
                    IsActive = entity.Person.IsActive
                },
            ProjectId = entity.ProjectId,
            Project = entity.Project is null
                ? null
                : new ProjectSummaryResponse
                {
                    Id = entity.Project.Id,
                    Name = entity.Project.Name,
                    Description = entity.Project.Description,
                    IsActive = entity.Project.IsActive
                },
            CompanyId = entity.CompanyId,
            Company = entity.Company is null
                ? null
                : new CompanySummaryResponse
                {
                    Id = entity.Company.Id,
                    Name = entity.Company.Name,
                    Description = entity.Company.Description,
                    IsActive = entity.Company.IsActive
                },
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            GoogleEventId = entity.GoogleEventId,
            SyncSource = entity.SyncSource,
            GoogleColor = entity.GoogleColor
        };

        private static void ValidateSchedule(TimeOnly? startTime, TimeOnly? endTime)
        {
            if (startTime.HasValue != endTime.HasValue)
                throw new ValidationException(
                    "La hora de inicio y la hora final deben indicarse juntas.");

            if (startTime.HasValue && endTime <= startTime)
                throw new ValidationException(
                    "La hora final debe ser posterior a la hora de inicio.");
        }
    }
}
