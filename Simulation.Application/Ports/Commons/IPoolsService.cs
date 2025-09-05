using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Commons;

public interface IPoolsService
{
    List<PlayerTemplate> RentList();
    void ReturnList(List<PlayerTemplate> list);

    PlayerTemplate RentTemplate();
    void ReturnTemplate(PlayerTemplate template);
    
    PlayerStateDto RentPlayerStateDto();
    void ReturnPlayerStateDto(PlayerStateDto template);

    PlayerTemplate[] RentArray(int minLength);
    void ReturnArray(PlayerTemplate[] array, bool clearArray = false);
}