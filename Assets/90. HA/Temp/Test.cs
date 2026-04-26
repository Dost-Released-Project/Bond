using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Ha
{
    public class Test : MonoBehaviour
    {
        public List<BaseCharacter> characters = new List<BaseCharacter>();
        
        private void Start()
        {
            foreach (var chara in characters)
            {
                chara.StatComponent.StatCalculate();
            }
        }
    }
}