namespace Bond.WT.Camping
{
    public enum CampingActionType
    {
        HealHP,
        RecoverInsanity,
        RemoveDebuff
    }

    public class CharacterStateCondition
    {
        public CampingActionType ActionType;
        public float ThresholdRatio; 
    }
}