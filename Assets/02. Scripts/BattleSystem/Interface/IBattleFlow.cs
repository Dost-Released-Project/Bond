namespace BattleSystem.Interface
{
    public interface IBattleFlow
    {
        public void SetPlayerUnits(ITurnUseUnit[] playerUnits);
        public void StartBattle(ITurnUseUnit[] enemyUnits);
    }
}
