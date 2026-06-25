using Cysharp.Threading.Tasks;
using Reactions;

/// <summary>
/// 리액션 발동 시 초상화 캔버스의 활성화·비활성화 및 아이콘 표시를 제어하는 인터페이스.
/// </summary>
public interface IReactionPortraitCanvas
{
    /// <summary>
    /// 지정한 Addressables 키로 초상화 스프라이트를 로드해 캔버스를 활성화한다.
    /// ReactionResult 에 따라 대사 텍스트를 설정한다.
    /// </summary>
    /// <param name="imageAddress">BaseCharacter.ImageAddress — Addressables 키.</param>
    /// <param name="result">리액션 판정 결과. 텍스트 설정에 사용된다.</param>
    UniTask ShowAsync(string imageAddress, ReactionResult result);

    /// <summary>
    /// 초상화 캔버스를 비활성화한다.
    /// </summary>
    void Hide();
}
