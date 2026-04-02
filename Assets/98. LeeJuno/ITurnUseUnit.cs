using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ITurnUseUnit: IComparable<ITurnUseUnit>
{
   int Speed { get; }
   bool IsDead { get; }
   UniTask TakeTurnAsync();
}
