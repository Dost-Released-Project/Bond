using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PipeLine.PipeLineBase
{
    public class PipelineLoader
    {
        private readonly AssetReferenceT<BattlePipeLineSo> _assetRef;
        private BattlePipeLineSo _cachedPipeline;
        private AsyncOperationHandle<BattlePipeLineSo> _handle;

        // VContainer를 통해 AssetReference를 주입받음
        public PipelineLoader(AssetReferenceT<BattlePipeLineSo> assetRef)
        {
            _assetRef = assetRef;
        }

        public async Task<BattlePipeLineSo> LoadAsync()
        {
            // 1. 이미 로드되었다면 캐시된 데이터 반환
            if (_cachedPipeline != null) return _cachedPipeline;

            // 2. 어드레서블 비동기 로드 실행
            _handle = _assetRef.LoadAssetAsync<BattlePipeLineSo>();
            _cachedPipeline = await _handle.Task;

            return _cachedPipeline;
        }

        // 씬이 종료되거나 컨테이너가 해제될 때 메모리 정리
        public void Dispose()
        {
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                _cachedPipeline = null;
            }
        }
    }
}