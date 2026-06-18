namespace Bond.WT.Camping
{
    public enum CampingActionType
    {
        HealHP,
        ReduceInsanity,
        RemoveDebuff
    }

    public class CharacterStateCondition
    {
        public CampingActionType ActionType;
        public float ThresholdRatio; 
    }
}