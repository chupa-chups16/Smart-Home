using SmartHome.Api.Models;

namespace SmartHome.Api.Services;

public interface ICameraService
{
    Task<List<Camera>> GetAllAsync();
    Task<Camera?> GetByIdAsync(int id);
}