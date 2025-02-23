namespace StatRecovery.Application.Interfaces;

public interface IProcessFilesUseCase
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}