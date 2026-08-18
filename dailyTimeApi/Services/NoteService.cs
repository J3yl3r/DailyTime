using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services
{
    public class NoteService : INoteService
    {
        private const int MaxDepth = 3;
        private readonly INoteRepository _repository;
        private readonly IWorkItemStatusRepository _statusRepository;
        private readonly IWorkItemCategoryRepository _categoryRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ICompanyRepository _companyRepository;

        public NoteService(
            INoteRepository repository,
            IWorkItemStatusRepository statusRepository,
            IWorkItemCategoryRepository categoryRepository,
            IPersonRepository personRepository,
            IProjectRepository projectRepository,
            ICompanyRepository companyRepository)
        {
            _repository = repository;
            _statusRepository = statusRepository;
            _categoryRepository = categoryRepository;
            _personRepository = personRepository;
            _projectRepository = projectRepository;
            _companyRepository = companyRepository;
        }

        public async Task<IReadOnlyList<NoteResponse>> GetRootsByDateRangeAsync(
            DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        {
            var items = await _repository.GetRootsByDateRangeAsync(fromDate, toDate, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<NoteResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Nota {id} no encontrada.");
            return MapToResponse(item);
        }

        public async Task<IReadOnlyList<NoteResponse>> GetChildrenAsync(
            int parentId, CancellationToken cancellationToken = default)
        {
            _ = await _repository.GetByIdAsync(parentId, cancellationToken)
                ?? throw new NotFoundException($"Nota padre {parentId} no encontrada.");

            var items = await _repository.GetChildrenAsync(parentId, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<NoteResponse> CreateAsync(
            CreateNoteRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("El contenido de la nota es obligatorio.");
            ValidateSchedule(request.WorkDate, request.StartTime, request.EndTime);

            await ValidateParentForNewItemAsync(request.ParentNoteId, cancellationToken);
            var status = await ResolveStatusAsync(request.StatusId, cancellationToken);
            var category = await ResolveCategoryAsync(request.CategoryId, cancellationToken);
            var person = await ResolvePersonAsync(request.PersonId, cancellationToken);
            var project = await ResolveProjectAsync(request.ProjectId, cancellationToken);
            var company = await ResolveCompanyAsync(request.CompanyId, cancellationToken);

            var now = DateTime.UtcNow;
            var entity = new Note
            {
                Title = NormalizeTitle(request.Title),
                Content = request.Content.Trim(),
                WorkDate = request.WorkDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ParentNoteId = request.ParentNoteId,
                StatusId = status.Id,
                CategoryId = category.Id,
                PersonId = person?.Id,
                ProjectId = project?.Id,
                CompanyId = company?.Id,
                SortOrder = request.SortOrder,
                DurationMinutes = request.DurationMinutes,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            entity.Status = status;
            entity.Category = category;
            entity.Person = person;
            entity.Project = project;
            entity.Company = company;
            return MapToResponse(entity);
        }

        public async Task<NoteResponse> UpdateAsync(
            int id, UpdateNoteRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("El contenido de la nota es obligatorio.");
            ValidateSchedule(request.WorkDate, request.StartTime, request.EndTime);

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Nota {id} no encontrada.");

            if (request.ParentNoteId != entity.ParentNoteId)
                await ValidateParentForMoveAsync(id, request.ParentNoteId, cancellationToken);

            var status = await ResolveStatusAsync(request.StatusId, cancellationToken);
            var category = await ResolveCategoryAsync(request.CategoryId, cancellationToken);
            var person = await ResolvePersonAsync(request.PersonId, cancellationToken);
            var project = await ResolveProjectAsync(request.ProjectId, cancellationToken);
            var company = await ResolveCompanyAsync(request.CompanyId, cancellationToken);

            entity.Title = NormalizeTitle(request.Title);
            entity.Content = request.Content.Trim();
            entity.WorkDate = request.WorkDate;
            entity.StartTime = request.StartTime;
            entity.EndTime = request.EndTime;
            entity.ParentNoteId = request.ParentNoteId;
            entity.StatusId = status.Id;
            entity.CategoryId = category.Id;
            entity.PersonId = person?.Id;
            entity.ProjectId = project?.Id;
            entity.CompanyId = company?.Id;
            entity.SortOrder = request.SortOrder;
            entity.DurationMinutes = request.DurationMinutes;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Status = null;
            entity.Category = null;
            entity.Person = null;
            entity.Project = null;
            entity.Company = null;

            _repository.Update(entity);
            await _repository.SaveChangesAsync(cancellationToken);

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
                ?? throw new NotFoundException($"Nota {id} no encontrada.");

            var children = await _repository.GetChildrenAsync(id, cancellationToken);
            if (children.Count > 0)
                throw new ValidationException("No se puede eliminar una nota que tiene subnotas.");

            _repository.Remove(entity);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        private async Task<WorkItemStatus> ResolveStatusAsync(
            int? statusId,
            CancellationToken cancellationToken)
        {
            if (statusId.HasValue)
            {
                var status = await _statusRepository.GetByIdAsync(statusId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Estado {statusId} no encontrado.");

                if (!string.Equals(status.ItemType, WorkItemStatusService.ItemTypeNote, StringComparison.OrdinalIgnoreCase))
                    throw new ValidationException("El estado seleccionado no es válido para notas.");

                return status;
            }

            return await _statusRepository.GetDefaultByItemTypeAsync(
                    WorkItemStatusService.ItemTypeNote,
                    cancellationToken)
                ?? throw new ValidationException("No hay estados configurados para notas.");
        }

        private async Task<WorkItemCategory> ResolveCategoryAsync(
            int? categoryId, CancellationToken cancellationToken)
        {
            if (categoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(
                        categoryId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Categoría {categoryId} no encontrada.");
                if (category.ItemType != WorkItemStatusService.ItemTypeNote)
                    throw new ValidationException("La categoría seleccionada no es válida para notas.");
                if (!category.IsActive)
                    throw new ValidationException("La categoría seleccionada está inactiva.");
                return category;
            }

            return await _categoryRepository.GetDefaultByItemTypeAsync(
                    WorkItemStatusService.ItemTypeNote, cancellationToken)
                ?? throw new ValidationException("No hay categorías activas para notas.");
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

        private async Task ValidateParentForNewItemAsync(int? parentNoteId, CancellationToken cancellationToken)
        {
            if (!parentNoteId.HasValue)
                return;

            var parentLevel = await GetLevelAsync(parentNoteId.Value, cancellationToken);
            if (parentLevel >= MaxDepth)
                throw new ValidationException($"Máximo {MaxDepth} niveles de jerarquía.");
        }

        private async Task ValidateParentForMoveAsync(int noteId, int? newParentNoteId, CancellationToken cancellationToken)
        {
            if (newParentNoteId == noteId)
                throw new ValidationException("Una nota no puede ser padre de sí misma.");

            if (newParentNoteId.HasValue)
            {
                _ = await _repository.GetByIdAsync(newParentNoteId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Nota padre {newParentNoteId} no encontrada.");

                if (await IsDescendantAsync(noteId, newParentNoteId.Value, cancellationToken))
                    throw new ValidationException("No se puede mover una nota dentro de sus propias subnotas.");
            }

            var newLevel = newParentNoteId.HasValue
                ? await GetLevelAsync(newParentNoteId.Value, cancellationToken) + 1
                : 1;

            var subtreeHeight = await GetSubtreeHeightAsync(noteId, cancellationToken);

            if (newLevel + subtreeHeight - 1 > MaxDepth)
                throw new ValidationException($"Máximo {MaxDepth} niveles de jerarquía.");
        }

        private async Task<int> GetLevelAsync(int noteId, CancellationToken cancellationToken)
        {
            var level = 1;
            var current = await _repository.GetByIdAsync(noteId, cancellationToken)
                ?? throw new NotFoundException($"Nota {noteId} no encontrada.");

            while (current.ParentNoteId.HasValue)
            {
                level++;
                current = await _repository.GetByIdAsync(current.ParentNoteId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Nota padre {current.ParentNoteId} no encontrada.");
            }

            return level;
        }

        private async Task<int> GetSubtreeHeightAsync(int noteId, CancellationToken cancellationToken)
        {
            var children = await _repository.GetChildrenAsync(noteId, cancellationToken);
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

        private static string? NormalizeTitle(string? title) =>
            string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        private static NoteResponse MapToResponse(Note entity) => new()
        {
            Id = entity.Id,
            ParentNoteId = entity.ParentNoteId,
            Title = entity.Title,
            Content = entity.Content,
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
            UpdatedAt = entity.UpdatedAt
        };

        private static void ValidateSchedule(
            DateOnly? workDate, TimeOnly? startTime, TimeOnly? endTime)
        {
            if (startTime.HasValue != endTime.HasValue)
                throw new ValidationException(
                    "La hora de inicio y la hora final deben indicarse juntas.");

            if (startTime.HasValue && endTime <= startTime)
                throw new ValidationException(
                    "La hora final debe ser posterior a la hora de inicio.");

            if (startTime.HasValue && !workDate.HasValue)
                throw new ValidationException(
                    "Una nota con horario debe tener una fecha.");
        }
    }
}
