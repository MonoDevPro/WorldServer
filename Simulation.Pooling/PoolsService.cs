using System.Buffers;
using Microsoft.Extensions.ObjectPool;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons;
using Simulation.Domain.Templates;

namespace Simulation.Pooling;

public class PoolsService : IPoolsService
{
    private readonly ObjectPool<List<PlayerTemplate>> _listPool;
    private readonly ObjectPool<PlayerTemplate> _templatePool;
    private readonly ObjectPool<PlayerState> _stateDtoPool;
    private readonly ArrayPool<PlayerTemplate> _arrayPool;

    public PoolsService(ObjectPool<List<PlayerTemplate>> listPool,
        ObjectPool<PlayerTemplate> templatePool,
        ObjectPool<PlayerState> stateDtoPool,
        ArrayPool<PlayerTemplate> arrayPool)
    {
        _listPool = listPool;
        _templatePool = templatePool;
        _stateDtoPool = stateDtoPool;
        _arrayPool = arrayPool;
    }

    public List<PlayerTemplate> RentList() => _listPool.Get();
    public void ReturnList(List<PlayerTemplate> list) => _listPool.Return(list);

    public PlayerTemplate RentTemplate() => _templatePool.Get();
    public void ReturnTemplate(PlayerTemplate template) => _templatePool.Return(template);
    
    public PlayerState RentPlayerStateDto() => _stateDtoPool.Get();
    public void ReturnPlayerStateDto(PlayerState template) => _stateDtoPool.Return(template);

    public PlayerTemplate[] RentArray(int minLength) => _arrayPool.Rent(minLength);
    public void ReturnArray(PlayerTemplate[] array, bool clearArray = false) => _arrayPool.Return(array, clearArray);
}