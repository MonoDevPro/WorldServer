using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Commons.Pools;

public interface IPoolsService
{
    List<CharTemplate> RentList();
    void ReturnList(List<CharTemplate> list);

    CharTemplate RentTemplate();
    void ReturnTemplate(CharTemplate template);
    
    CharSaveTemplate RentCharSaveTemplate();
    void ReturnCharSaveTemplate(CharSaveTemplate template);

    CharTemplate[] RentArray(int minLength);
    void ReturnArray(CharTemplate[] array, bool clearArray = false);
}