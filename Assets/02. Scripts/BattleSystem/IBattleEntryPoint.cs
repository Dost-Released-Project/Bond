using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IBattleEntryPoint
{
    public UniTask StartAsync(CancellationToken cancellation,
        IEnumerable<ITurnUseUnit> unit);
}
