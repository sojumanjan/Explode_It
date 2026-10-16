using System;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 이름 붙은 누적 수 하나(스킬별 누적 처치 등). JsonUtility가 사전을 저장하지 못해 이름·값 쌍 목록으로 둔다.
    [Serializable]
    public class SaveStat
    {
        [SerializeField] private string _id;
        [SerializeField] private int _value;

        public SaveStat(string id)
        {
            _id = id;
        }

        public string Id => _id;

        public int Value
        {
            get => _value;
            set => _value = value;
        }
    }
}
