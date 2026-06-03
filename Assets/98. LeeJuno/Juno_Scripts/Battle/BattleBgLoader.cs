using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투씬 진입 시 BattleMapBgChannel 에서 Sprite 를 읽어 배경 Image 에 적용하는 뷰 컴포넌트.
///
/// MapInitializer 가 맵 생성 시 한 번만 Addressables 로드 후 채널에 보관한다.
/// BattleBgLoader 는 채널에서 읽기만 하므로 배틀씬 진입마다 재로드하지 않는다.
///
/// Inspector 연결 필요:
///   _bgImage — Combat_Canvas > Panel 의 Image 컴포넌트
/// </summary>
public class BattleBgLoader : MonoBehaviour
{
    [SerializeField] private Image _bgImage;

    private void Start()
    {
        if (_bgImage == null)
        {
            Debug.LogError("[BattleBgLoader] _bgImage 가 연결되지 않았습니다.", this);
            return;
        }

        Sprite sprite = BattleMapBgChannel.Sprite;

        if (sprite == null)
        {
            Debug.LogWarning("[BattleBgLoader] BattleMapBgChannel 에 Sprite 가 없습니다. DungeonType 이 None 이거나 로드에 실패했을 수 있습니다.");
            return;
        }

        _bgImage.sprite = sprite;
    }
}
