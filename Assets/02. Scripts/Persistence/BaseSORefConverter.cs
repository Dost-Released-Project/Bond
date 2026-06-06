using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Bond.Persistence
{
    /// <summary>
    /// 세이브에 박히는 BaseSO(카탈로그 SO) 참조를 Id 문자열로만 직렬화하고,
    /// 로드 시 DBSORegistry에서 다시 해석한다.
    /// <para/>
    /// 이유:<br/>
    /// 1) ScriptableObject는 new로 만들 수 없어 Newtonsoft가 역직렬화 시 경고를 낸다.
    ///    Id 참조로 바꾸면 SO를 재구성하지 않으므로 경고가 사라진다.<br/>
    /// 2) ClassSO 등은 공유 카탈로그(단일 진실원천)다. 세이브에 스탯을 통째로 굽지 않고
    ///    Id만 저장해 원본 에셋을 가리킨다(중복/밸런스 박제 방지).<br/>
    /// 3) BaseSO.Id/DisplayName/Description은 get-only + private 백킹필드라
    ///    임베드 방식은 로드 시 이 값들이 유실된다. Id 참조는 원본을 가리키므로 해당 없음.<br/>
    /// </summary>
    public class BaseSORefConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => typeof(BaseSO).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => writer.WriteValue((value as BaseSO)?.Id);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            string id = token.Type switch
            {
                JTokenType.String => (string)token,
                JTokenType.Object => (string)token["Id"], // 구 포맷(임베드 객체)에서 Id 추출 — 하위호환
                _                 => null
            };
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var db in DBSORegistry.All)
            {
                var so = db.GetSO(id);
                if (so != null && objectType.IsInstanceOfType(so))
                    return so;
            }

            // 카탈로그 미로드/Id 불일치 — TraitIds 패턴처럼 graceful null.
            // (호출측이 null을 방어. DBSO 프리로드가 로드보다 선행해야 함.)
            Debug.LogWarning($"[BaseSORefConverter] '{id}'({objectType.Name})를 DBSORegistry에서 찾지 못함. " +
                             $"DBSO 프리로드(PreloadByLabelAsync(\"DBSO\")) 선행 여부 확인.");
            return null;
        }
    }
}
