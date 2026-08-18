using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly ITimeEntryRepository _repository;
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly INoteRepository _noteRepository;

        public TimeEntryService(
            ITimeEntryRepository repository,
            ITaskItemRepository taskItemRepository,
            INoteRepository noteRepository)
        {
            _repository = repository;
            _taskItemRepository = taskItemRepository;
            _noteRepository = noteRepository;
        }

        public async Task<IReadOnlyList<TimeEntryResponse>> GetByDateRangeAsync(
            DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        {
            var items = await _repository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<TimeEntryResponse>> GetByTaskItemIdAsync(
            int taskItemId, CancellationToken cancellationToken = default)
        {
            _ = await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
                ?? throw new NotFoundException($"Tarea {taskItemId} no encontrada.");

            var items = await _repository.GetByTaskItemIdAsync(taskItemId, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<IReadOnlyList<TimeEntryResponse>> GetByNoteIdAsync(
            int noteId, CancellationToken cancellationToken = default)
        {
            _ = await _noteRepository.GetByIdAsync(noteId, cancellationToken)
                ?? throw new NotFoundException($"Nota {noteId} no encontrada.");

            var items = await _repository.GetByNoteIdAsync(noteId, cancellationToken);
            return items.Select(MapToResponse).ToList();
        }

        public async Task<TimeEntryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Registro de tiempo {id} no encontrado.");
            return MapToResponse(item);
        }

        public async Task<TimeEntryResponse> CreateAsync(
            CreateTimeEntryRequest request, CancellationToken cancellationToken = default)
        {
            await ValidateOwnerAsync(request.TaskItemId, request.NoteId, cancellationToken);

            if (request.DurationMinutes <= 0)
                throw new ValidationException("La duración debe ser mayor a 0 minutos.");

            var entity = new TimeEntry
            {
                TaskItemId = request.TaskItemId,
                NoteId = request.NoteId,
                WorkDate = request.WorkDate,
                DurationMinutes = request.DurationMinutes,
                Description = NormalizeDescription(request.Description),
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return MapToResponse(entity);
        }

        public async Task<TimeEntryResponse> UpdateAsync(
            int id, UpdateTimeEntryRequest request, CancellationToken cancellationToken = default)
        {
            if (request.DurationMinutes <= 0)
                throw new ValidationException("La duración debe ser mayor a 0 minutos.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Registro de tiempo {id} no encontrado.");

            entity.WorkDate = request.WorkDate;
            entity.DurationMinutes = request.DurationMinutes;
            entity.Description = NormalizeDescription(request.Description);

            _repository.Update(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return MapToResponse(entity);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Registro de tiempo {id} no encontrado.");

            _repository.Remove(entity);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidateOwnerAsync(int? taskItemId, int? noteId, CancellationToken cancellationToken)
        {
            var hasTask = taskItemId.HasValue;
            var hasNote = noteId.HasValue;

            if (hasTask == hasNote)
                throw new ValidationException("Debe indicar TaskItemId o NoteId, pero no ambos ni ninguno.");

            if (hasTask)
            {
                _ = await _taskItemRepository.GetByIdAsync(taskItemId!.Value, cancellationToken)
                    ?? throw new NotFoundException($"Tarea {taskItemId} no encontrada.");
            }
            else
            {
                _ = await _noteRepository.GetByIdAsync(noteId!.Value, cancellationToken)
                    ?? throw new NotFoundException($"Nota {noteId} no encontrada.");
            }
        }

        private static string? NormalizeDescription(string? description) =>
            string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        private static TimeEntryResponse MapToResponse(TimeEntry entity) => new()
        {
            Id = entity.Id,
            TaskItemId = entity.TaskItemId,
            NoteId = entity.NoteId,
            WorkDate = entity.WorkDate,
            DurationMinutes = entity.DurationMinutes,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt
        };
    }
}