using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Commons;

public interface IPoolsService
{
    List<PlayerTemplate> RentList();
    void ReturnList(List<PlayerTemplate> list);

    PlayerTemplate RentTemplate();
    void ReturnTemplate(PlayerTemplate template);
    
    PlayerState RentPlayerStateDto();
    void ReturnPlayerStateDto(PlayerState template);

    PlayerTemplate[] RentArray(int minLength);
    void ReturnArray(PlayerTemplate[] array, bool clearArray = false);
}