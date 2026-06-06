using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 진입 시 MapBgChannel 에서 Sprite 를 읽어 배경 Image 에 적용하는 범용 뷰 컴포넌트.
///
/// 배틀씬, 이벤트씬 등 배경이 필요한 모든 씬의 Canvas 에 배치할 수 있다.
/// MapInitializer 가 맵 생성 시 한 번만 Addressables 로드 후 채널에 보관하므로
/// SceneBgLoader 는 채널에서 읽기만 하며 재로드하지 않는다.
///
/// Inspector 연결 필요:
///   _bgImage — 배경으로 사용할 Image 컴포넌트
/// </summary>
public class SceneBgLoader : MonoBehaviour
{
    [SerializeField] private Image _bgImage;

    private void Start()
    {
        if (_bgImage == null)
        {
            Debug.LogError("[SceneBgLoader] _bgImage 가 연결되지 않았습니다.", this);
            return;
        }

        Sprite sprite = MapBgChannel.Sprite;

        if (sprite == null)
        {
            Debug.LogWarning("[SceneBgLoader] MapBgChannel 에 Sprite 가 없습니다. DungeonType 이 None 이거나 로드에 실패했을 수 있습니다.");
            return;
        }

        _bgImage.sprite = sprite;
    }
}
