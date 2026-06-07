
using System;
using System.Collections.Generic;
using System.Linq;
using Bond.Persistence;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 캐릭터 보유 목록 + 세이브 권위자. RootScope Singleton 으로 등록되어
/// 마을→전투→귀환 내내 동일 BaseCharacter 인스턴스를 유지한다(전투 HP/광기 보존).
/// 장비/역할/리액션 같은 "메타" 변경은 캐릭터 이벤트를 구독해 디바운스 저장하고,
/// HP/광기 같은 "세션" 값은 구독하지 않고 원정 종료 경계(SaveNow)에서 플러시한다.
/// </summary>
public class Roster : ISaveable<List<BaseCharacter>>, IDisposable
{
    public List<BaseCharacter> Characters = new List<BaseCharacter>();
    public int Max = 20;
    public bool IsFull => Characters.Count >= Max;
    public event Action<BaseCharacter> OnCharacterAdded;
    public event Action<BaseCharacter> OnCharacterRemoved;

    // 메타 변경 누적 후 디바운스 저장. 매 변경마다 디스크(+AssetDatabase.Refresh)를 치지 않기 위함.
    private bool _dirty;
    private bool _flushScheduled;
    private const int FLUSH_DELAY_MS = 500;

    public Roster()
    {
        SaveLoadSystem.Load(this);

        if (Characters.Count == 0)
        {
            var stageCoach = new StageCoach();
            var classDb = DBSORegistry.GetDb<ClassDataBaseSO>();
            
            // 데이터베이스 로드 실패 방어 코드
            if (classDb == null)
            {
                Debug.LogError("[Roster] ClassDataBaseSO가 로드되지 않았습니다. 기본 캐릭터를 생성할 수 없습니다.");
                return;
            }

            var dbList = classDb.Query<ClassSO>(_ => true).ToList();

            if (dbList.Count >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    var chara = stageCoach.GetCharacter(dbList[i]);
                    Hire(chara);   // Hire 가 구독까지 처리
                }
            }
            else
            {
                Debug.LogError($"[Roster] ClassSO 데이터가 부족합니다. (현재: {dbList.Count}개, 필요: 4개)");
            }
        }
        else
        {
            // 로드된 캐릭터들의 메타 이벤트 구독
            foreach (var ch in Characters) Subscribe(ch);
        }
    }

    public bool Hire(BaseCharacter character)
    {
        if (IsFull || Characters.Contains(character))
        {
            return false;
        }
        else
        {
            Characters.Add(character);
            Subscribe(character);
            OnCharacterAdded?.Invoke(character);
            SaveLoadSystem.Save(this);   // 멤버십 변경은 즉시 저장
            return true;
        }
    }

    public bool Fire(BaseCharacter character)
    {
        bool reVal = Characters.Remove(character);
        Unsubscribe(character);
        OnCharacterRemoved?.Invoke(character);
        SaveLoadSystem.Save(this);
        return reVal;
    }

    // ── 메타 변경 자동저장 ──────────────────────────────────────────────

    private void Subscribe(BaseCharacter c)
    {
        if (c == null) return;
        c.OnAccessoriesChanged += MarkDirty;
        c.OnEquipmentChanged   += MarkDirty;
        c.OnRoleChanged        += MarkDirty;
        c.OnReactionsChanged   += MarkDirty;
        c.OnPlayableChanged    += MarkDirty;
        // OnHpChanged / OnInsanityChanged 는 세션값 — 구독하지 않는다.
        // (전투 중 매 틱 디스크 저장 방지. 원정 종료 시 SaveNow 로 일괄 플러시.)
    }

    private void Unsubscribe(BaseCharacter c)
    {
        if (c == null) return;
        c.OnAccessoriesChanged -= MarkDirty;
        c.OnEquipmentChanged   -= MarkDirty;
        c.OnRoleChanged        -= MarkDirty;
        c.OnReactionsChanged   -= MarkDirty;
        c.OnPlayableChanged    -= MarkDirty;
    }

    private void MarkDirty(BaseCharacter _)
    {
        _dirty = true;
        if (_flushScheduled) return;
        _flushScheduled = true;
        ScheduleFlush().Forget();
    }

    private async UniTaskVoid ScheduleFlush()
    {
        await UniTask.Delay(FLUSH_DELAY_MS);
        _flushScheduled = false;
        Flush();
    }

    private void Flush()
    {
        if (!_dirty) return;
        _dirty = false;
        SaveLoadSystem.Save(this);
    }

    /// <summary>원정 종료 등 경계에서 즉시 강제 저장(HP/광기 포함 최신 상태 플러시).</summary>
    public void SaveNow()
    {
        _dirty = false;
        SaveLoadSystem.Save(this);
    }

    public void Dispose()
    {
        foreach (var ch in Characters) Unsubscribe(ch);
        Flush();   // 앱 종료/스코프 해제 직전 미저장분 보존
    }

    public string Key => "roster";
    public List<BaseCharacter> Data => Characters;
    public void Restore(List<BaseCharacter> data)
    {
        Characters = data;

        foreach (var ch in Characters)
        {
            ch.Init();
        }
    }
}
