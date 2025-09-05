using System.Buffers;
using Microsoft.Extensions.ObjectPool;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

public class PoolsService : IPoolsService
{
    private readonly ObjectPool<List<CharTemplate>> _listPool;
    private readonly ObjectPool<CharTemplate> _templatePool;
    private readonly ObjectPool<CharSaveTemplate> _saveTemplatePool;
    private readonly ArrayPool<CharTemplate> _arrayPool;

    public PoolsService(ObjectPool<List<CharTemplate>> listPool,
        ObjectPool<CharTemplate> templatePool,
        ObjectPool<CharSaveTemplate> saveTemplatePool,
        ArrayPool<CharTemplate> arrayPool)
    {
        _listPool = listPool;
        _templatePool = templatePool;
        _saveTemplatePool = saveTemplatePool;
        _arrayPool = arrayPool;
    }

    public List<CharTemplate> RentList() => _listPool.Get();
    public void ReturnList(List<CharTemplate> list) => _listPool.Return(list);

    public CharTemplate RentTemplate() => _templatePool.Get();
    public void ReturnTemplate(CharTemplate template) => _templatePool.Return(template);
    
    public CharSaveTemplate RentCharSaveTemplate() => _saveTemplatePool.Get();
    public void ReturnCharSaveTemplate(CharSaveTemplate template) => _saveTemplatePool.Return(template);

    public CharTemplate[] RentArray(int minLength) => _arrayPool.Rent(minLength);
    public void ReturnArray(CharTemplate[] array, bool clearArray = false) => _arrayPool.Return(array, clearArray);
}