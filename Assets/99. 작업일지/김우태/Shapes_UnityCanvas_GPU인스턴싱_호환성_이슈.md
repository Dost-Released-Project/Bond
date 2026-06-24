# Shapes 에셋 & Unity Canvas GPU 인스턴싱 호환성 이슈 보고서

## 1. 개요
* **발생 현상**: `CharacterSlotVisualizer`에서 `Draw.Rectangle`로 배경을 그리도록 구성하였으나, `Draw.RectangleBorder` 주석 여부에 따라 배경이 화면에서 유실되거나 정상 출력되는 문제 발생.
* **조사 목적**: Canvas 렌더 모드 변경(Overlay, Camera, World Space) 중에도 GPU 인스턴싱 활성화 시 렌더링이 유실되는 근본 원인을 규명하고 해결책을 제시하기 위함.

---

## 2. 현상 상세 분석 (주석 여부에 따른 차이)

### A. Draw.RectangleBorder 주석 처리 시 (배경 유실)
* **배칭(Batching) 통합**: `Draw.Rectangle`(배경)만 연속 호출 시, 동일한 `mpbRect`(static MpbRect) 및 동일한 배경 머티리얼(`hollow = false`)을 계속 공유합니다.
* **GPU 인스턴싱 묶임**: 모든 슬롯의 배경 그리기 명령이 단 하나의 GPU 인스턴싱 드로우 콜(`DrawMeshInstanced`)로 묶이게 됩니다.
* **렌더링 유실**: Unity Canvas(UI)의 렌더 패스 한계로 인해 인스턴스 배열로 전달되는 위치 행렬 및 색상 데이터가 정상 바인딩되지 못하고 유실(크기가 0이 되거나 화면 밖 렌더링)됩니다.

### B. Draw.RectangleBorder 주석 해제 시 (정상 출력)
* **머티리얼 교차**: 슬롯 루프 중 `Draw.Rectangle`(배경용, `hollow = false`) 호출 직후 `Draw.RectangleBorder`(테두리용, `hollow = true`)가 호출됩니다.
* **배칭 강제 해제 (Flush)**: Shapes의 `IMDrawer`가 머티리얼 변경을 감지하고, 기존에 모여 있던 배경 데이터를 즉시 드로우 콜로 추출(`ExtractDrawCall`)하여 GPU로 보냅니다.
* **단일 드로우 콜 동작**: GPU 인스턴싱 배칭이 강제로 끊어지고, 개별 드로우 콜(`DrawMesh`)로 변환되어 화면에 오류 없이 제자리에 렌더링됩니다.

---

## 3. 근본 원인 분석

### A. Unity UI Canvas 렌더 패스와 GPU 인스턴싱의 아키텍처 충돌
* **Canvas 렌더링 특성**: Unity UI Canvas는 렌더 효율을 높이기 위해 하위 요소(Image, Text 등)의 정점 정보를 하나의 거대한 메쉬로 실시간 복원 및 병합(Reconstruction)하여 그립니다.
* **인스턴싱 미지원**: Canvas의 전용 렌더 파이프라인 단계는 일반적인 GPU 인스턴싱(`DrawMeshInstanced`)에 필요한 인스턴싱 버퍼 레지스터 및 인스턴스 데이터 바인딩을 정상적으로 공급하거나 허용하지 않습니다.
* **결과**: 이로 인해 렌더 패스 내에 삽입된 `DrawMeshInstanced` 명령은 인스턴스 정점 연산 오류를 일으키고 투명도가 0이 되거나 렌더링 자체가 생략되는 버그로 이어집니다.

### B. 캔버스 렌더 모드별 렌더러 처리 흐름 분석
Shapes 에셋의 [ImmediateModeCanvas.cs](file:///C:/Users/kot77/Desktop/Unity/Bond/Assets/Shapes/Scripts/Runtime/Immediate%20Mode/ImmediateModeCanvas.cs) 코드를 추적한 결과, `Screen Space - Camera` 및 `World Space` 모드에서도 동일한 한계가 적용됩니다.

1. **`Screen Space - Camera` 모드**:
   * Shapes는 `WorldSpace`가 아닌 캔버스 모드(`Overlay`, `Camera`)에 대해 `IsCameraBasedUI`를 `false`로 판단합니다.
   * 이에 따라 `DisplayAsWorldSpacePanel()`이 `false`가 되어, `Overlay` 모드와 완전히 동일한 **가상 카메라 행렬 계산 수식(`GetOverlayToWorldMatrix`)**을 타고 들어가 렌더 행렬 왜곡 현상이 동일하게 유지됩니다.
2. **`World Space` 모드**:
   * 월드 행렬을 직접 전달하여 행렬 연산 왜곡 오류는 피할 수 있으나, 결국 렌더 루프가 **Canvas UI 렌더러 패스**를 경유하므로 상단의 **Canvas 자체의 인스턴싱 데이터 바인딩 실패 문제(A항목)**에 걸려 동일하게 유실됩니다.

---

## 4. 해결책 및 실무 가이드라인

### A. UI 내 Shapes 사용 시 GPU 인스턴싱 완전 배제 (권장)
* **설정**: UI Canvas 내부에서 Shapes Immediate Mode를 그릴 때는 캔버스 모드와 무관하게 GPU 인스턴싱 기능을 완전히 꺼두어야 안전합니다.
* **경로**: `ShapesConfig` 에셋(또는 설정 파일)에서 **`useImmediateModeInstancing`** 옵션을 **`false`**로 변경합니다.

### B. UI 환경 성능 최적화 판단
* 캐릭터 슬롯 UI와 같이 오브젝트 개수가 수십 개 이하인 UI 레이어는 인스턴싱 배칭이 깨져 드로우 콜이 10~20개 늘어나는 것이 디바이스 성능에 미치는 영향이 전혀 없습니다.
* 따라서 UI 설계 시에는 불안정한 인스턴싱 활성화보다, **인스턴싱을 배제하여 레이어 순서(Layering)와 렌더링 안정성을 확보하는 것이 지향해야 할 방향**입니다.
